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
pub mod dict_queue;
pub mod discovery_service;
mod error;
pub mod queues;
pub mod scalar_queue;
pub mod set_queue;
pub mod smart_monitor;
mod smart_monitor_sqlite;
mod smart_monitor_ws;
pub mod tcp_listener;
pub mod tcp_sender;
pub use error::MeshError;
mod discovery_ws;
pub mod msg_serde;
mod queue_sqlite;
