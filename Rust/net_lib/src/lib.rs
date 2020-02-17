//#![feature(try_trait)]
// Logging
// if,then,else,
// selector
// parallel.for_each
// thread_id, method, count/total
// timing

// thread_local
// Single tcp/Queue
// Multiplex data
pub mod discovery_service;
mod error;
pub mod queues;
pub mod smart_monitor;
mod smart_monitor_sqlite;
pub use error::MeshError;
mod msg_serde;
