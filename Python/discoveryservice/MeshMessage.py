import json
from JsonSerializable import JsonSerializable

class MeshMessage(JsonSerializable):
    def __init__(self, Service: str, QueueName: str, PayLoad: str, Split: bool, Monitor: bool, GraphId: int, XId: int):
        self.XId = XId
        self.GraphId = GraphId
        self.Service = Service
        self.QueueName = QueueName
        self.PayLoad = PayLoad
        self.Split = Split
        self.Monitor = Monitor

    @classmethod
    def from_json(cls, data: str):
        decoded = json.loads(data)
        return cls(**decoded)