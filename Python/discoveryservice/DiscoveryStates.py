from enum import Enum, unique

@unique
class DiscoveryStates(Enum):
    Connect = 0
    Register = 1
    GetInputQs = 2
    GetOutputQs = 3
    Error = 666
