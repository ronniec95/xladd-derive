class DiscoveryMessage:
    def __init__(self, State: int, Port: int, HostServer: str, PayLoad: str):
        self.State = State
        self.Port = Port
        self.HostServer = HostServer
        self.PayLoad = PayLoad

    @staticmethod
    def fromBytes(byteMessage: bytearray):
        state = byteMessage[0]
        msgPtr = 1 #(8bits)
        Port = int.from_bytes(byteMessage[msgPtr:msgPtr+2], byteorder = 'little')
        msgPtr += 2 #(16bits)
        serviceLen = int.from_bytes(byteMessage[msgPtr : msgPtr+1], byteorder = 'little')
        msgPtr += 1 #(8bits)
        HostServer = byteMessage[msgPtr:msgPtr+serviceLen].decode()
        msgPtr += serviceLen
        payloadLen = int.from_bytes(byteMessage[msgPtr: msgPtr+8], byteorder = 'little')
        msgPtr += 8 #(64bits)
        if payloadLen > 0:
            PayLoad = byteMessage[msgPtr:msgPtr+payloadLen].decode()
        else:
            PayLoad = None
        message = DiscoveryMessage(state, Port, HostServer, PayLoad)
        return message

    @staticmethod
    def toBytes(message):
        byteMessage = message.State.to_bytes(1, byteorder = 'little')
        byteMessage += message.Port.to_bytes(2, byteorder = 'little')
        # 8 Bytes
        # Length of Service
        byteMessage += len(message.HostServer).to_bytes(1, byteorder = 'little')
        byteMessage += message.HostServer.encode()
        # Length of PayLoad
        if not message.PayLoad:
            zerolen = 0
            byteMessage += zerolen.to_bytes(8, byteorder = 'little')
        else:
            byteMessage += len(message.PayLoad).to_bytes(8, byteorder = 'little')
            byteMessage += message.PayLoad.encode()
        # Ignore Split and Monitor
        return byteMessage