import json

class JsonSerializable(object):
    def toJson(self):
        return json.dumps(self.__dict__)

    def __repr__(self):
        return self.toJson()