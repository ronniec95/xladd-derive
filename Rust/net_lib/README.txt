To build rust applications:-

Install rust
https://www.rust-lang.org/tools/install

# cd Rust\net-lib
# cargo build --release
# 

Release in target\release\*.exe

Run discovery service
# discovery_server.exe -p 9999

Run smart monitor service
# smart_monitor_server.exe -c Channel.toml

To call the webservices 

DiscoverService runs on :8080 but may be virtualised to another port via Docker.
http://<ip>:<port>/ gives a list of live channels in a json format

Smart monitoring runs on :8080 but may be virtualised to another port via Docker.
To get a live view on the channels that this process is serving

http://<ip>:<port>/all&start=12345&end=56789 <- this will retrieve all the messages for all
queues between the times specified. Timestamps are specified in MilliSeconds since epoch 1/1/1970
The returned format has the row ids. How we then visually display this information is open for discussion

To drill down to a specific msg in a queue

http://<ip>:<port>/msg&row_id=123456 <- use the row id returned by the previous request to see the actual message.
The message payload is opaque bytes so a suitable decoder would be necessary in order to visualise
the message appropriately.