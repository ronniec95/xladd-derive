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
mod discovery_msg;
pub mod discovery_service;

mod error;
pub mod queues;
//mod service;

pub use error::MeshError;

pub fn register() {
    //   service::register_services().unwrap();
}
