using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AARC.Mesh.Interface;

namespace AARC.Mesh.TCP
{
    public class SocketPipeTransport : SubscriberPattern<byte[]>, IMeshServiceTransport
    {
        public class DuplexPipe : IDuplexPipe
        {
            public PipeReader Input { get; }
            public PipeWriter Output { get; }

            public DuplexPipe(PipeWriter writer, PipeReader reader) { Input = reader; Output = writer; }

            public static (IDuplexPipe Front, IDuplexPipe Back) CreatePair(PipeOptions fronttobackOptions, PipeOptions backtofrontOptions)
            {
                Pipe FrontToBack = new Pipe(backtofrontOptions ?? DefaultOptions), BackToFront = new Pipe(fronttobackOptions ?? DefaultOptions);
                return (new DuplexPipe(FrontToBack.Writer, BackToFront.Reader), new DuplexPipe(BackToFront.Writer, FrontToBack.Reader));
            }

            public static readonly PipeOptions DefaultOptions = new PipeOptions(writerScheduler: PipeScheduler.ThreadPool, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false, pauseWriterThreshold: 0, resumeWriterThreshold: 0);
            public static readonly PipeOptions ImmediateOptions = new PipeOptions(writerScheduler: PipeScheduler.Inline, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: true, pauseWriterThreshold: 0, resumeWriterThreshold: 0);
        }

        protected Uri _url;
        protected Socket _socket;
        IDuplexPipe Front, Back;
        public PipeReader Input { get; }
        public PipeWriter Output { get; }
        internal Task Running { get; private set; } = Task.CompletedTask;
        private CancellationTokenSource _localCancelSource;


        public async Task StartAsync(CancellationToken cancel = default)
        {
            var ipaddress = NetworkExt.GetHostIPAddress(_url.Host);
            var ipEndPoint = new IPEndPoint(ipaddress, _url.Port);
            _socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _socket.Connect(ipEndPoint);  //TODO Would like this to be async... Why so serious???

            Running = ProcessSocketAsync(_socket);
        }

        public Task StopAsync() => Task.Factory.StartNew(() => { Shutdown(); });

        public SocketPipeTransport(Uri url, PipeOptions outputoptions = default, PipeOptions inputoptions = default)
        {
            _url = url;
            if (string.Compare(url.Scheme, "TCP", true) != 0) { throw new ArgumentException("Only TCP connections are supported.", nameof(Url)); }
            if (url.Port == -1) { throw new ArgumentException("TCP Port must be specified.", nameof(Url)); }

            //Options = options ?? WebSocketsTransport.DefaultWebSocketOptions;
            (Front, Back) = DuplexPipe.CreatePair(outputoptions, inputoptions);
        }

        private async Task ProcessSocketAsync(Socket socket)
        {
            // Begin sending and receiving. Receiving must be started first because ExecuteAsync enables SendAsync.
            var receiving = StartReceiving(socket);
            var sending = StartSending(socket);

            var trigger = await Task.WhenAny(receiving, sending);
        }

        private async Task StartReceiving(Socket socket)
        {
            var token = default(CancellationToken); //Cancellation?.Token ?? default;

            try
            {
                while (!token.IsCancellationRequested)
                {
#if NETCOREAPP3_0
                    // Do a 0 byte read so that idle connections don't allocate a buffer when waiting for a read
                    var received = await socket.ReceiveAsync(Memory<byte>.Empty, token);
					if(received == 0) { continue; }
					var memory = Back.Output.GetMemory(out var memoryframe, haslength: true);    //RSOCKET Framing
                    var received = await socket.ReceiveAsync(memory, token);
#else
                    var memory = Back.Output.GetMemory(out var memoryframe, haslength: true);    //RSOCKET Framing
                    var isArray = MemoryMarshal.TryGetArray<byte>(memory, out var arraySegment);
                    Debug.Assert(isArray);
                    var received = await socket.ReceiveAsync(arraySegment, SocketFlags.None);   //TODO Cancellation?
#endif
                    //Log.MessageReceived(_logger, receive.MessageType, receive.Count, receive.EndOfMessage);
                    Back.Output.Advance(received);
                    var flushResult = await Back.Output.FlushAsync();
                    if (flushResult.IsCanceled || flushResult.IsCompleted) { break; }
                }
            }
            //catch (SocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            //{
            //	// Client has closed the WebSocket connection without completing the close handshake
            //	Log.ClosedPrematurely(_logger, ex);
            //}
            catch (OperationCanceledException)
            {
                // Ignore aborts, don't treat them like transport errors
            }
            catch (Exception ex)
            {
                if (!_localCancelSource.IsCancellationRequested && !token.IsCancellationRequested) { Back.Output.Complete(ex); throw; }
            }
            finally { Back.Output.Complete(); }
        }

        private async Task StartSending(Socket socket)
        {
            Exception error = null;

            try
            {
                while (true)
                {
                    var result = await Back.Input.ReadAsync();
                    var buffer = result.Buffer;
                    var consumed = buffer.Start;        //RSOCKET Framing

                    try
                    {
                        if (result.IsCanceled) { break; }
                        if (!buffer.IsEmpty)
                        {
                            try
                            {
                                //Log.SendPayload(_logger, buffer.Length);
                                consumed = await socket.SendAsync(buffer, buffer.Start, SocketFlags.None);     //RSOCKET Framing
                            }
                            catch (Exception)
                            {
                                if (_localCancelSource.IsCancellationRequested) { /*Log.ErrorWritingFrame(_logger, ex);*/ }
                                break;
                            }
                        }
                        else if (result.IsCompleted) { break; }
                    }
                    finally
                    {
                        Back.Input.AdvanceTo(consumed, buffer.End);     //RSOCKET Framing
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                //// Send the close frame before calling into user code
                //if (WebSocketCanSend(socket))
                //{
                //	// We're done sending, send the close frame to the client if the websocket is still open
                //	await socket.CloseOutputAsync(error != null ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                //}
                Back.Input.Complete();
            }
        }

        public bool Connected { get { return _socket?.Connected ?? false; } }

        public string Url { get { return _url.ToString(); } }

        public bool ConnectionAlive() => !(_socket.Poll(5000, SelectMode.SelectRead) && _socket.Available == 0);

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void OnPublish(byte[] value)
        {
            throw new NotImplementedException();
        }

        public void ReadAsync()
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            // ToDo : Cancellation token
//            _localCancelSource.Cancel();
//            _logger?.LogInformation($"[{_url}]: Closing Socket");
            if (_socket?.Connected ?? false)
            {
                _socket?.Shutdown(SocketShutdown.Both);
                _socket?.Close();
            }
            _socket = null;
        }
    }
}
