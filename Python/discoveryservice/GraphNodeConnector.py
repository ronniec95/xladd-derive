import GraphNode

class GraphNodeConnector(object):
    def __init__(self, name: str, input: bool, tag: str, node: GraphNode):
        self.Name = name
        self.Input = input
        self.Tag = tag
        #self.Node = node
