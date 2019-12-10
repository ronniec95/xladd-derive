import sys, traceback
import time
from typing import List
from datetime import datetime
import json
import threading
import socketserver
import functools
import logging
from http.server import HTTPServer, BaseHTTPRequestHandler
from pprint import pprint

from DiscoveryMessage import DiscoveryMessage
from MeshServiceManager import MeshServiceManager
from DSRequestHandler import DSRequestHandler
import config

def multidispatch(*types):
    def register(function):
        name = function.__name__
        mm = multidispatch.registry.get(name)
        if mm is None:
            @functools.wraps(function)
            def wrapper(self, *args):
                types = tuple(arg.__class__ for arg in args) 
                function = wrapper.typemap.get(types)
                if function is None:
                    raise TypeError("no match")
                return function(self, *args)
            wrapper.typemap = {}
            mm = multidispatch.registry[name] = wrapper
        if types in mm.typemap:
            raise TypeError("duplicate registration")
        mm.typemap[types] = function
        return mm
    return register
multidispatch.registry = {}

def WrapMessage(message: str):
    b = message.encode()
    lenb = len(b)
    byteMessage = lenb.to_bytes(4, byteorder = 'little')
    byteMessage += b
    return byteMessage

def WrapBytes(b: bytearray):
    lenb = len(b)
    byteMessage = lenb.to_bytes(4, byteorder = 'little')
    byteMessage += b
    return byteMessage

# When a new client connects
# It should send the following MeshMessages
#   QueueName:inputq - with a list of inputQ names separated by commas
#   QueueName:outputq - list of output q names separated by commas
# digraph G {
#  "connect" -> "register" - Todo : suggest port
#  "getinputqs" -> "getoutputqs"
##  "register" -> "getinputqs"
#  "getoutputqs" -> "register"
#}

class MeshDSHandler(socketserver.BaseRequestHandler):
    def handle(self):
        # self.request is the TCP socket connected to the client
        m = None
        response = None
        clienttag = str(self.client_address)
        try:
            logging.info ("DC [%s]: New Connection" % (clienttag))
            while True:
                m = self.readMeshEncodedPacket()
                response = config.MSM.Process(clienttag, m)
                self.sendMeshMessage(response)
        except Exception as e:
            logging.error ("ER [%s]: %r" % (clienttag, e))
            logging.exception(e)
            if m:
                pprint(vars(m))
            if response:
                pprint(vars(response))
            #exc_type, exc_value, exc_traceback = sys.exc_info()
            #traceback.print_tb(exc_traceback, limit=1, file=sys.stdout)
        config.MSM.Disconnect(clienttag)
        logging.info ("DC [%s]: Disconnected" % (clienttag))

    def sendString(self, message: str, name: str = None):
        b = WrapMessage(message)
        self.sendPacket(b)

    def sendMeshMessage(self, message: DiscoveryMessage):
        b = DiscoveryMessage.toBytes(message)
        raw = WrapBytes(b)
        self.sendPacket(raw)
    
    def sendPacket(self, b:bytearray):
        logging.debug ("Tx [%s]: %i bytes" % (self.client_address[0], len(b)))
        self.request.sendall(b)

    def sendMeshjsonPacket(self, message: DiscoveryMessage):
        s = message.toJson()
        self.sendString(s, message.QueueName)

    def readPacket(self):
        bytesMsgLen = self.request.recv(4)
        msgLen = int.from_bytes(bytesMsgLen, byteorder = 'little')
        bmsgLen = int.from_bytes(bytesMsgLen, byteorder = 'big')
        logging.debug ("Rx [%s]: MsgLen %i %i" % (self.client_address, msgLen, bmsgLen))
        print(bytesMsgLen)
        packet = b''
        bytes_recd = 0
        while bytes_recd < msgLen:
            chunk = self.request.recv(msgLen - bytes_recd)
            if not chunk:
                    return None
            packet += chunk
            bytes_recd = bytes_recd + len(chunk)
        logging.debug ("Rx [%s]: %s bytes" % (self.client_address, len(packet)))
        return packet

    def readMeshjsonPacket(self):
        packet = self.readPacket()
        return packet.decode()

    def readMeshEncodedPacket(self):
        packet = self.readPacket()       
        msg = DiscoveryMessage.fromBytes(packet)
        return msg

    def readSimple(self):
        request = self.request.recv(1024)
        if not request:
            return None
        return request.strip()


class ThreadedTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    pass

def DiscoveryService(httpPort: int, dsPort : int):
    global TDC_servers
    global TDC_server_threads

    TDC_servers = []
    TDC_server_threads =[]

    logging.info ("ST Discovery Service [%s]" % (dsPort))
    TDC_servers.append(ThreadedTCPServer(('', dsPort), MeshDSHandler))
    TDC_servers.append(ThreadedTCPServer(('', httpPort), DSRequestHandler))

    for TDC_server in TDC_servers:
        TDC_server_threads.append(threading.Thread(target=TDC_server.serve_forever))

    for TDC_server_thread in TDC_server_threads:
        TDC_server_thread.setDaemon(True)
        TDC_server_thread.start()

    while True:
        continue

def SocketServerMain():
    HOST, PORT = "localhost", 9999

    logging.info ("ST Discovery Service [%s]: %s" % (HOST, PORT))
    logging.info ("INIT MSM created")
        # Create the server, binding to localhost on port 9999
    server = ThreadedTCPServer((HOST, PORT), MeshDSHandler)
    with server:
        ip, port = server.server_address

        # Start a thread with the server -- that thread will then start one
        # more thread for each request
        server_thread = threading.Thread(target=server.serve_forever)
        # Exit the server thread when the main thread terminates
        server_thread.daemon = True
        server_thread.start()
        logging.info ("SMS starting thread: %s [%s:%i]" % (server_thread.name, ip, port))
#        app.run(host='0.0.0.0', port=8080, debug=True)
        server_thread.join()

if __name__ == "__main__":
    try:
        logging.basicConfig(format='%(asctime)s %(message)s', level=logging.DEBUG)
        #TestMeshJsonSerialize2()
        #TestMeshSerializer()
#        TestMSM()
        #app.run(host='0.0.0.0', port=8080, debug=True)
 
        DiscoveryService(9998, 9999)
        #TestMSMqs(MSM)

    except KeyboardInterrupt:
        pass
    finally:
        logging.info ("Service finished")