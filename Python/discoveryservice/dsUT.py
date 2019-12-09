import sys, traceback
import time
import json
import logging
import unittest

from MeshMessage import MeshMessage
from MeshSerializable import MeshSerializable
from MeshServiceManager import MeshServiceManager
from DiscoveryMessage import DiscoveryMessage
from DiscoveryStates import DiscoveryStates
from VisNode import VisNode

class SetEncoder(json.JSONEncoder):
    def default(self, obj):
       if isinstance(obj, set):
          return list(obj)
       return json.JSONEncoder.default(self, obj)

class MeshUT(unittest.TestCase):
    def setUp(self):
        self.msm = MeshServiceManager()

    def TestMeshJsonSerialize(self):
        xid = time.time_ns() // 1000000 
        payload = "test"
        service = MeshMessage("gateway", "register", payload, False, False, 0, xid)
        testdata = service.toJson()
        logging.info (testdata)
        decoded = MeshMessage.from_json(testdata)
        logging.info(decoded)

    def TestMeshJsonSerialize2(self):
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

    def TestMSM(self):
        testmsm = self.msm
        XId = time.time_ns() // 1000000 
        m = testmsm.InputQPayload()
        logging.info("inputqs %s" % m.toJson())
        o = testmsm.OutputQPayload()
        logging.info("outputqs %s" % o.toJson())

    def TestMSMqs(self):
        msg = '{"GraphId":0,"XId":43924392,"Service":null,"QueueName":"GetInputQs","PayLoad":"getcloseprice","Split":false,"Monitor":false}'
        im = MeshMessage.from_json(msg)
        om = self.msm.Process("unknown", im)
        logging.info("inputqs %s" % om.toJson())
        self.msm.inputQs['testservice']= ['testinputQ']
        om = self.msm.Process("unknown", im)
        logging.info("inputqs %s" % om.toJson())
        queuename = 'closeprices'
        sp = queuename.split(',')
        logging.info(sp)
        for q in sp:
            logging.info("q:%s" % q)

    def TestMeshFail(self):
        dm = DiscoveryMessage(DiscoveryStates.GetInputQs, 6500, 'abMac.local', None)
        address = dm.HostServer + ':' + str(dm.Port)
        bytesin = DiscoveryMessage.toBytes(dm)
        self.assertEqual(23, len(bytesin))
        response = self.msm.Process(address, dm)
        bytesout = DiscoveryMessage.toBytes(response)
        self.assertGreater(len(bytesout), 1)

    def TestMSMqs2(self):
        self.msm.RegisterInputs('tests1', ['testiq1'])
        self.msm.RegisterOutputs('tests2', ['testiq1'])

    def TestMeshSerializer(self):
        ijson = '{"GraphId":0,"XId":43924392,"Service":"ds","QueueName":"GetInputQs","PayLoad":"getcloseprice","Split":false,"Monitor":false}'
        im = MeshMessage.from_json(ijson)
        bm = MeshSerializable.toBytes(im)
        om = MeshSerializable.fromBytes(bm)
        ojson = om.toJson()
        logging.info (ojson)

    def TestVisNode(self):
        n = VisNode(1, "Test", "Test")
        logging.info(n)
        self.assertIsNotNone(n)

    def TestVisNodeList(self):
        n = VisNode(1, "Test", "Test")
        l = [n]
        logging.info(l)

    def TestVisNodeList2(self):
        self.msm.RegisterInputs('tests1', ['testiq1'])
        self.msm.RegisterOutputs('tests2', ['testiq1'])
        logging.info(self.msm.MeshNodesRoutes())

def suiteVisNodes():
    suite = unittest.TestSuite()
    suite.addTest(MeshUT('TestVisNode'))
    suite.addTest(MeshUT('TestVisNodeList'))
    suite.addTest(MeshUT('TestVisNodeList2'))
    return suite

def suiteMeshFail():
    suite = unittest.TestSuite()
    suite.addTest(MeshUT('TestMeshFail'))
    return suite

if __name__ == "__main__":
    global MSM
    try:
        logging.basicConfig(format='%(asctime)s %(message)s', level=logging.INFO)
        runner = unittest.TextTestRunner()
        runner.run(suiteMeshFail())
#        TestMeshJsonSerialize2()
#        TestMeshSerializer()
#        TestMSMqs2(MSM)
#        logging.info(MSM.MeshNodesRoutes())
#        logging.info(MSM.MeshRoutes())

    except KeyboardInterrupt:
        pass
    finally:
        logging.info ("Service finished")