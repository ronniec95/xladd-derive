import sys, traceback
import time
from typing import List
from datetime import datetime
import json
import jsonpickle
import threading
import socketserver
import functools
import logging
from DiscoveryStates import DiscoveryStates
from DiscoveryMessage import DiscoveryMessage
from VisNode import VisNode
from GraphNode import GraphNode
from GraphNodeConnector import GraphNodeConnector

class SetEncoder(json.JSONEncoder):
    def default(self, obj):
       if isinstance(obj, set):
          return list(obj)
       return json.JSONEncoder.default(self, obj)
       
class MeshServiceManager:
    def __init__(self):
        self._lock = threading.Lock()
        self.inputQs = dict()
        self.outputQs = dict()
        self._servers = dict()
        self._usedPorts = set()
        self.currentPort = 6000
        jsonpickle.set_preferred_backend('json')

    def RegisterInputs(self, ipaddress: str, inputs: list):
        for q in inputs:
            if q in self.inputQs:
                self.inputQs[q].add(ipaddress)
            else:
                self.inputQs[q] = { ipaddress }

    def RegisterOutputs(self, ipaddress: str, outputs: list):
        for q in outputs:
            if q in self.outputQs:
                self.outputQs[q].add(ipaddress)
            else:
                self.outputQs[q] = { ipaddress }

    def Unregister(self, routes: dict, transportId: str):
        kvp = list(routes.items())
        for key,val in kvp:
            if transportId in val:
                val.remove(transportId)
            if not val:
                del routes[key]
            
    def Disconnect(self, clienttag: str):
        try:
            logging.info("Disconnecting %s %s" % (clienttag, self._servers))
            logging.info("Disconnecting %s %s" % (self.inputQs, self.outputQs))
            if clienttag in self._servers:
                transportId = self._servers[clienttag]
                self.Unregister(self.inputQs, transportId)
                self.Unregister(self.outputQs, transportId)
                del self._servers[clienttag]
            logging.info("Disconnected %s %s" % (clienttag, self._servers))
            logging.info("Disconnected %s %s" % (self.inputQs, self.outputQs))
        except Exception as e:
            logging.error ("ER Disconnecting from MSM routes: %r" % e)
            logging.exception(e)

    def Process(self, clienttag: str, m: DiscoveryMessage):
        try:
            state = DiscoveryStates(m.State)
            if state == DiscoveryStates.Register:
                if m.Port == 0:
                    while self.currentPort in self._usedPorts:
                        self.currentPort += 1
                    m = DiscoveryMessage(m.State, self.currentPort, m.HostServer, None)
                else:
                    m = DiscoveryMessage(m.State, m.Port, m.HostServer, None)
                self._servers[clienttag] = m.HostServer + ':' + str(m.Port)
                if not m.Port in self._usedPorts:
                    self._usedPorts.add(m.Port)
                return m
            elif state == DiscoveryStates.GetInputQs:
                if m.PayLoad:
                    qs = m.PayLoad.split(',')
                    transportId = m.HostServer + ':' + str(m.Port)
                    self.RegisterInputs(transportId, qs)
                payload = self.InputQPayload()
                return DiscoveryMessage(m.State, m.Port, m.HostServer, payload)
            elif state == DiscoveryStates.GetOutputQs:
                if m.PayLoad:
                    qs = m.PayLoad.split(',')
                    transportId = m.HostServer + ':' + str(m.Port)
                    self.RegisterOutputs(transportId, qs)
                payload = self.OutputQPayload()
                return DiscoveryMessage(m.State, m.Port, m.HostServer, payload)
            elif state == DiscoveryStates.Connect:
                return DiscoveryMessage(m.State, 0, m.HostServer, None)
            else:
                logging.info ("MSM Unknown state [%i : %s]" % (m.State, m.HostServer))
                return DiscoveryMessage(DiscoveryStates.Error, 0, m.HostServer, "{ ERROR: \"BAD MESSAGE\"}")
        except Exception as e:
            logging.error ("ER MeshMessage: %r" % e)
            logging.exception(e)
            #exc_type, exc_value, exc_traceback = sys.exc_info()
            #traceback.print_tb(exc_traceback, limit=1, file=sys.stdout)

        logging.info ("MSM Bad Message [%s][%s : %s]" % (clienttag, m.State, m.HostServer))
        return DiscoveryMessage(DiscoveryStates.Error, 0, m.HostServer, "{ ERROR: \"BAD MESSAGE\"}")

    def InputQPayload(self):
        payload = json.dumps(self.inputQs, sort_keys=True, cls=SetEncoder)
        return payload

    def OutputQPayload(self):
        payload = json.dumps(self.outputQs, sort_keys=True, cls=SetEncoder)
        return payload

    def QueueEdges(self, queues: dict, serverIds: dict, id: int, dir: bool):
        for k,v in queues.items():
            queuename = k
            if queuename not in serverIds.keys():
                serverIds[queuename] = VisNode(str(id), queuename, 'database')
                id+=1

            for s in v:
                if s not in serverIds.keys():
                    serverIds[s] = VisNode(str(id), s, 'server')
                    id+=1
                if dir:
                    serverIds[queuename].connections.append(serverIds[s].id)
                else:
                        serverIds[s].connections.append(serverIds[queuename].id)

        return id
    #
    # GraphNode
    #
    def CreateGraphNodes(self):
        channelNodes = dict()
        jsonstr = None
        try:
            id = 0
            self._lock.acquire()
            id = self.CreateNodeConnectors(id,self.inputQs, True, channelNodes)
            id = self.CreateNodeConnectors(id,self.outputQs, False, channelNodes)
            for k,v in channelNodes.items():
                if jsonstr is None:
                    jsonstr = "[ "
                else:
                    jsonstr += ","
                tmp = jsonpickle.encode(v, False)
                logging.info ("type is %s", (type(tmp)))
                jsonstr += tmp
        except Exception as e:
            logging.error ("ER MeshMessage: %r" % e)
            logging.exception(e)
        finally:
            self._lock.release()
        if jsonstr is None:
            jsonstr = "["
        jsonstr += "]"
        return jsonstr

    def CreateNodeConnectors(self, id: int, channels: dict, dir: bool, nodes : dict):
        for c,v in channels.items():
            for s in v:
                if s not in nodes.keys():
                    newnode = GraphNode(id, s, s, [], [])
                    nodes[s] = newnode
                    id+=1
                
                node = nodes[s]
                connector = GraphNodeConnector(c, dir, c, node)
                node.addConnector(connector)
        return id

    def MeshNodesRoutes(self):
        routes = """    function meshRoutes() {
    var inputData =
    """
        routes += self.MeshNodesRoutesData()
        routes += ";\nreturn inputData;\n}\n"
        return routes

    def MeshNodesRoutesData(self):
        routes = ""
        try:
            id = 0
            serverIds = dict()
            self._lock.acquire()
            id = self.QueueEdges(self.inputQs, serverIds, id, True)
            id = self.QueueEdges(self.outputQs, serverIds, id, False)
            common = [k for k in (set(self.inputQs) & set(self.outputQs)) if (self.inputQs[k] - self.outputQs[k])]
            for queuename in common:
                for oq in self.outputQs[queuename]:
                    for iq in self.inputQs[queuename]:
                        if iq not in serverIds.keys():
                            serverIds[iq] = VisNode(str(id), iq, 'server')
                            id+=1
                        if oq not in serverIds.keys():
                            serverIds[oq] = VisNode(str(id), oq, 'server')
                            id+=1
                        if queuename not in serverIds.keys():
                            serverIds[queuename] = VisNode(str(id), queuename, 'database')
                            id+=1

                        serverIds[queuename].connections.append(serverIds[iq].id)
                        serverIds[oq].connections.append(serverIds[queuename].id)
            routes += "["
            first = True
            for value in serverIds.values():
                if not first:
                    routes += ","
                routes += str(value)
                first = False
            routes += "]\n"
        except Exception as e:
            logging.error ("ER MeshMessage: %r" % e)
            logging.exception(e)
        finally:
            self._lock.release()
            return routes

    def MeshRoutes(self):
        routes = ""
        try:
            self._lock.acquire()
            common = [k for k in (set(self.inputQs) & set(self.outputQs)) if (self.inputQs[k] - self.outputQs[k])]
            routes = "digraph ROUTES {"
            for queuename in common:
                for oq in self.outputQs[queuename]:
                    for iq in self.inputQs[queuename]:
                        routes += self.MeshRoute(oq, iq, queuename)
            routes += "}"
        finally:
            self._lock.release()
            return routes

    def MeshRoute(self, outputq: str, inputq: str, queuename: str):
        return "    \"%s\"->\"%s\" [label = \"q:%s\" ];" % (outputq, inputq, queuename)

    def PersistMesh(self):
        logging.debug("ToDo: Persist routes")

    def RestoreMesh(self):
        logging.debug("ToDo: Restore routes")