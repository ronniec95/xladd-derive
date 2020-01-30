static LOGGER: Logger = Logger {};

struct Logger {}

impl Logger {
    fn debug(bytes: &[u8]) {}
}

// Tcp stream
// Enter/Exit/Time/FunctionName/ThreadId/Pid/
// Arg 0 , Arg1 , Data
