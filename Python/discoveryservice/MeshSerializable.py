from MeshMessage import MeshMessage

class MeshSerializable():
    # Message is a fixed format
    @staticmethod
    def toBytes(message: MeshMessage):
        byteMessage = message.GraphId.to_bytes(4, byteorder = 'little')
        byteMessage += message.XId.to_bytes(4, byteorder = 'little')
        # 8 Bytes
        # Length of Service
        byteMessage += len(message.Service).to_bytes(4, byteorder = 'little')
        byteMessage += message.Service.encode()
        # Length of QueueName
        byteMessage += len(message.QueueName).to_bytes(4, byteorder = 'little')
        byteMessage += message.QueueName.encode()
        # Length of PayLoad
        byteMessage += len(message.PayLoad).to_bytes(4, byteorder = 'little')
        byteMessage += message.PayLoad.encode()
        # Ignore Split and Monitor
        return byteMessage

    @staticmethod
    def fromBytes(byteMessage: bytearray):
        msgPtr = 0
        GraphId = int.from_bytes(byteMessage[msgPtr:msgPtr+4], byteorder = 'little')
        msgPtr += 4
        Xid = int.from_bytes(byteMessage[msgPtr:msgPtr+4], byteorder = 'little')
        msgPtr += 4
        serviceLen = int.from_bytes(byteMessage[msgPtr : msgPtr+4], byteorder = 'little')
        msgPtr = 12
        Service = byteMessage[msgPtr:msgPtr+serviceLen].decode()
        msgPtr += serviceLen
        queueStrLen = int.from_bytes(byteMessage[msgPtr : msgPtr+4], byteorder = 'little')
        msgPtr += 4
        QueueName = byteMessage[msgPtr: msgPtr+queueStrLen].decode()
        msgPtr += queueStrLen
        payloadLen = int.from_bytes(byteMessage[msgPtr: msgPtr+4], byteorder = 'little')
        msgPtr += 4
        PayLoad = byteMessage[msgPtr:msgPtr+payloadLen].decode()
        message = MeshMessage(Service, QueueName, PayLoad, False, False, GraphId, Xid)
        return message