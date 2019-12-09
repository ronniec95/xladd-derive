from enum import IntEnum, unique

@unique
class DiscoveryStates(IntEnum):
    Connect = 0
    Register = 1
    GetInputQs = 2
    GetOutputQs = 3
    Error = 255
