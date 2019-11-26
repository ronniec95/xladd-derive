import sys, traceback
import time
import json
import logging
from MeshMessage import MeshMessage
from MeshSerializable import MeshSerializable
from MeshServiceManager import MeshServiceManager
from VisNode import VisNode

class SetEncoder(json.JSONEncoder):
    def default(self, obj):
       if isinstance(obj, set):
          return list(obj)
       return json.JSONEncoder.default(self, obj)

def TestMeshJsonSerialize():
    xid = time.time_ns() // 1000000 
    payload = "test"
    service = MeshMessage("gateway", "register", payload, False, False, 0, xid)
    testdata = service.toJson()
    logging.info (testdata)
    decoded = MeshMessage.from_json(testdata)
    logging.info(decoded)

def TestMeshJsonSerialize2():
    GraphId = 0
    XId = time.time_ns() // 1000000 
    inputQs = {}
    inputQs["127.0.0.1:9999"]=""
    testdata = json.dumps(inputQs)
    logging.info (testdata)
    message = MeshMessage("ds", "getinputqs", testdata, False, False, GraphId, XId)
    logging.info(message.toJson())
    servers = set()
    testdata = json.dumps(servers, cls=SetEncoder)
    logging.info (testdata)

def TestMSM():
    testmsm = MeshServiceManager()
    XId = time.time_ns() // 1000000 
    m = testmsm.InputQPayload()
    logging.info("inputqs %s" % m.toJson())
    o = testmsm.OutputQPayload()
    logging.info("outputqs %s" % o.toJson())

def TestMSMqs(msm: MeshServiceManager):
    msg = '{"GraphId":0,"XId":43924392,"Service":null,"QueueName":"GetInputQs","PayLoad":"getcloseprice","Split":false,"Monitor":false}'
    im = MeshMessage.from_json(msg)
    om = msm.Process("unknown", im)
    logging.info("inputqs %s" % om.toJson())
    msm.inputQs['testservice']= ['testinputQ']
    om = msm.Process("unknown", im)
    logging.info("inputqs %s" % om.toJson())
    queuename = 'closeprices'
    sp = queuename.split(',')
    logging.info(sp)
    for q in sp:
        logging.info("q:%s" % q)

def TestMSMqs2(msm: MeshServiceManager):
    msm.RegisterInputs('tests1', ['testiq1'])
    msm.RegisterOutputs('tests2', ['testiq1'])

def TestMeshSerializer():
    ijson = '{"GraphId":0,"XId":43924392,"Service":"ds","QueueName":"GetInputQs","PayLoad":"getcloseprice","Split":false,"Monitor":false}'
    im = MeshMessage.from_json(ijson)
    bm = MeshSerializable.toBytes(im)
    om = MeshSerializable.fromBytes(bm)
    ojson = om.toJson()
    logging.info (ojson)

def TestVisNode():
    n = VisNode(1, "Test", "Test")
    logging.info(n)

def TestVisNodeList():
    n = VisNode(1, "Test", "Test")
    l = [n]
    logging.info(l)

def TestVisNodeList2(msm: MeshServiceManager):
    msm.RegisterInputs('tests1', ['testiq1'])
    msm.RegisterOutputs('tests2', ['testiq1'])
    logging.info(msm.MeshNodesRoutes())

if __name__ == "__main__":
    global MSM
    try:
        MSM = MeshServiceManager()
        logging.basicConfig(format='%(asctime)s %(message)s', level=logging.INFO)
#        TestMeshJsonSerialize2()
#        TestMeshSerializer()
#        TestMSMqs2(MSM)
#        logging.info(MSM.MeshNodesRoutes())
#        logging.info(MSM.MeshRoutes())
        TestVisNode()
        TestVisNodeList()
        TestVisNodeList2(MSM)
    except KeyboardInterrupt:
        pass
    finally:
        logging.info ("Service finished")