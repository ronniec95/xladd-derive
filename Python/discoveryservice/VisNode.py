import json
from JsonSerializable import JsonSerializable

class VisNode(JsonSerializable):
    def __init__(self, id : int, label: str, group : str):
        self.id = id
        self.group = group
        self.label = label
        self.connections = []

    @classmethod
    def from_json(cls, data: str):
        decoded = json.loads(data)
        return cls(**decoded)