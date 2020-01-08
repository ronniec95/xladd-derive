import json
import GraphNodeConnector

class GraphNode(object):
    def __init__(self, id : int, label: str, name: str, inputs : [], outputs : []):
        self.Id = id
        self.label = label
        self.Inputs = inputs
        self.Outputs = outputs
        self.Name = name
        self.Connectors = dict()

    def addConnector(self, connector : GraphNodeConnector):
        if connector.Input:
            self.Inputs.append(connector.Name)
        else:
            self.Outputs.append(connector.Name)
        self.Connectors[connector.Name] = connector


    