use futures::channel::mpsc::*;
use futures::task::SpawnError;

#[derive(Debug)]
pub enum MeshError {
    MajorError,
    NetworkError(&'static str),
    BinCodeSerializationError,
    ChannelError,
    TaskSpawnError,
    UnknownError,
}

impl From<std::io::Error> for MeshError {
    fn from(err: std::io::Error) -> MeshError {
        use std::io::ErrorKind;

        match err.kind() {
            ErrorKind::NotFound => MeshError::NetworkError("entity not found"),
            ErrorKind::PermissionDenied => MeshError::NetworkError("permission denied"),
            ErrorKind::ConnectionRefused => MeshError::NetworkError("connection refused"),
            ErrorKind::ConnectionReset => MeshError::NetworkError("connection reset"),
            ErrorKind::ConnectionAborted => MeshError::NetworkError("connection aborted"),
            ErrorKind::NotConnected => MeshError::NetworkError("not connected"),
            ErrorKind::AddrInUse => MeshError::NetworkError("address in use"),
            ErrorKind::AddrNotAvailable => MeshError::NetworkError("address not available"),
            ErrorKind::BrokenPipe => MeshError::NetworkError("broken pipe"),
            ErrorKind::AlreadyExists => MeshError::NetworkError("entity already exists"),
            ErrorKind::WouldBlock => MeshError::NetworkError("operation would block"),
            ErrorKind::InvalidInput => MeshError::NetworkError("invalid input parameter"),
            ErrorKind::InvalidData => MeshError::NetworkError("invalid data"),
            ErrorKind::TimedOut => MeshError::NetworkError("timed out"),
            ErrorKind::WriteZero => MeshError::NetworkError("write zero"),
            ErrorKind::Interrupted => MeshError::NetworkError("operation interrupted"),
            ErrorKind::Other => MeshError::NetworkError("other os error"),
            ErrorKind::UnexpectedEof => MeshError::NetworkError("unexpected end of file"),
            _ => MeshError::UnknownError,
        }
    }
}

impl From<bincode::Error> for MeshError {
    fn from(_: bincode::Error) -> MeshError {
        MeshError::BinCodeSerializationError
    }
}

impl From<TryRecvError> for MeshError {
    fn from(_: TryRecvError) -> MeshError {
        MeshError::ChannelError
    }
}

impl<T> From<TrySendError<T>> for MeshError {
    fn from(_: TrySendError<T>) -> MeshError {
        MeshError::ChannelError
    }
}

impl From<SpawnError> for MeshError {
    fn from(_: SpawnError) -> MeshError {
        MeshError::TaskSpawnError
    }
}
