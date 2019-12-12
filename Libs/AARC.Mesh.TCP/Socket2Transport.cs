using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AARC.Mesh.Model;

namespace AARC.Mesh.TCP
{
    public class Socket2Transport
    {
        static async Task Test(string[] args)
        {
            var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 8087));

            Console.WriteLine("Listening on port 8087");

            listenSocket.Listen(120);

            var socketservices = new ConcurrentQueue<Task>();

            while (true)
            {
                var socket = await listenSocket.AcceptAsync();

                var ss = new Socket2Transport();
                var t = ss.ProcessLinesAsync(socket);
                socketservices.Enqueue(t);
            }
        }

        async Task ProcessLinesAsync(Socket socket)
        {
            var pipe = new Pipe();
            Task writing = FillPipeAsync(socket, pipe.Writer);
            Task reading = ReadPipeAsync(pipe.Reader);

            await Task.WhenAll(reading, writing);
        }

        async Task FillPipeAsync(Socket socket, PipeWriter writer)
        {
            const int minimumBufferSize = 512;

            while (true)
            {
                // Allocate at least 512 bytes from the PipeWriter.
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                try
                {
                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    // Tell the PipeWriter how much was read from the Socket.
                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }

                // Make the data available to the PipeReader.
                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // By completing PipeWriter, tell the PipeReader that there's no more data coming.
            await writer.CompleteAsync();
        }

        async Task ReadPipeAsync(PipeReader reader)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadPacket(ref buffer, out ReadOnlySequence<byte> message))
                {
                    // Process the line.
                    Decode(message);
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming.
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();
        }

        private void Decode(ReadOnlySequence<byte> message)
        {
            var bytes = message.ToArray();
            var dm = new DiscoveryMessage();
            dm.Decode(bytes);
        }

        bool TryReadPacket(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> message)
        {
            var s = buffer.Slice(0, 4);


            var len = MemoryMarshal.Read<uint>(s.ToArray());
            // Skip the line + the \n.
            message = buffer.Slice(4, len);
            buffer = buffer.Slice(buffer.GetPosition(len + 4));
            return true;
        }
    }
}