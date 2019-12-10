using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AARC.Mesh.TCP
{
    public static class NetworkExt
    {
        static async ValueTask SendMultiSegmentAsync(Socket socket, ReadOnlySequence<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
			var position = buffer.Start;
			buffer.TryGet(ref position, out var prevSegment);
			while (buffer.TryGet(ref position, out var segment))
			{
				await socket.SendAsync(prevSegment, socketFlags);
				prevSegment = segment;
			}
			await socket.SendAsync(prevSegment, socketFlags);
#else
            var position = buffer.Start;
            buffer.TryGet(ref position, out var prevSegment);
            while (buffer.TryGet(ref position, out var segment))
            {
                var isArray = MemoryMarshal.TryGetArray(prevSegment, out var arraySegment);
                Debug.Assert(isArray);
                await socket.SendAsync(arraySegment, socketFlags);
                prevSegment = segment;
            }
            var isArrayEnd = MemoryMarshal.TryGetArray(prevSegment, out var arraySegmentEnd);
            Debug.Assert(isArrayEnd);
            await socket.SendAsync(arraySegmentEnd, socketFlags);
#endif
        }

        static (int Length, bool IsEndOfMessage) PeekFrame(ReadOnlySequence<byte> sequence) { var reader = new SequenceReader<byte>(sequence); return reader.TryRead(out byte b1) && reader.TryRead(out byte b2) && reader.TryRead(out byte b3) ? ((b1 << 8 * 2) | (b2 << 8 * 1) | (b3 << 8 * 0), true) : (0, false); }

        public static ValueTask SendAsync(this Socket socket, ReadOnlySequence<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            if (buffer.IsSingleSegment)
            {
                return socket.SendAsync(buffer.First, webSocketMessageType, endOfMessage: true, cancellationToken);
            }
            else { return SendMultiSegmentAsync(socket, buffer, socketFlags, cancellationToken); }
#else
            if (buffer.IsSingleSegment)
            {
                var isArray = MemoryMarshal.TryGetArray(buffer.First, out var segment);
                Debug.Assert(isArray);
                return new ValueTask(socket.SendAsync(segment, socketFlags));       //TODO Cancellation?
            }
            else { return SendMultiSegmentAsync(socket, buffer, socketFlags, cancellationToken); }
#endif
        }

        public static async ValueTask<SequencePosition> SendAsync(this Socket socket, ReadOnlySequence<byte> buffer, SequencePosition position, SocketFlags socketFlags, CancellationToken cancellationToken = default)
        {
            for (var frame = PeekFrame(buffer.Slice(position)); frame.Length > 0; frame = PeekFrame(buffer.Slice(position)))
            {
                //Console.WriteLine($"Send Frame[{frame.Length}]");
                var length = frame.Length + MemoryExtensions.FRAMELENGTHSIZE;
                var offset = buffer.GetPosition(MemoryExtensions.MESSAGEFRAMESIZE - MemoryExtensions.FRAMELENGTHSIZE, position);
                if (buffer.Slice(offset).Length < length) { break; }    //If there is a partial message in the buffer, yield to accumulate more. Can't compare SequencePositions...
                await socket.SendAsync(buffer.Slice(offset, length), socketFlags, cancellationToken);
                position = buffer.GetPosition(length, offset);
            }
            return position;

            //buffer.TryGet(ref position, out var memory, advance: false);
            //var (length, isEndOfMessage) = RSocketProtocol.MessageFrame(System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(memory.Span));
            //position = buffer.GetPosition(1, position);
        }

        public static Socket Bind(string server, int port)
        {
            Socket s = null;

            // Get host related information.
            IPHostEntry hostEntry = Dns.GetHostEntry(server);

            // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
            // an exception that occurs when the host IP Address is not compatible with the address family
            // (typical in the IPv6 case).
            foreach (IPAddress address in hostEntry.AddressList)
            {
                IPEndPoint ipe = new IPEndPoint(address, port);
                Socket tempSocket =
                    new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                tempSocket.Bind(ipe);

                if (tempSocket.Connected)
                {
                    s = tempSocket;
                    break;
                }
                continue;
            }
            return s;
        }

        public static void AddressHelper(string hostname, int port)
        {
            IPHostEntry host = Dns.GetHostEntry(hostname);
            foreach (IPAddress address in host.AddressList)
            {
                IPEndPoint endpoint = new IPEndPoint(address, port);

                Console.WriteLine("IPAddress " + address.ToString());
                    Console.WriteLine("IPEndPoint information:" + endpoint);
                    Console.WriteLine("\tMaximum allowed Port Address :" + IPEndPoint.MaxPort);
                    Console.WriteLine("\tMinimum allowed Port Address :" + IPEndPoint.MinPort);
                    Console.WriteLine("\tAddress Family :" + endpoint.AddressFamily);

            }
        }

        public static void DoGetHostEntry(string hostname)
        {
            IPHostEntry host = Dns.GetHostEntry(hostname);

            Console.WriteLine($"GetHostEntry({hostname}) returns:");

            foreach (IPAddress address in host.AddressList)
            {
                Console.WriteLine($"    {address}");
            }
        }

        public static IPAddress GetLocalHost() => GetHostIPAddress(Dns.GetHostName());

        public static IPAddress GetHostIPAddress(string address)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(address);

            return addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
        }

        public static string TransportId(this Socket socket)
        {
            var endpoint = (IPEndPoint)(socket?.RemoteEndPoint);
            var url = CreateUrl(endpoint.Address.ToString(), endpoint.Port);
            return url.AbsoluteUri;
        }

        public static void DoGetHostAddresses(string hostname)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(hostname);

            Console.WriteLine($"GetHostAddresses({hostname}) returns:");

            foreach (IPAddress address in addresses)
            {
                Console.WriteLine($"    {address}");
            }
        }

        public static Uri GetHostNameUrl(int? port = null) => CreateUrl(Dns.GetHostName(), port);

        public static Uri CreateUrl(string hostname, int? port = null, string scheme = "tcp")
        {
            var builder = new UriBuilder { Scheme = scheme, Host = hostname };
            if (port.HasValue)
                builder.Port = port.Value;

            return builder.Uri;
        }

        public static Uri GetServiceHost(this Socket socket)
        {
            var endpoint = (IPEndPoint)(socket?.RemoteEndPoint);

#if __DNS_WORKING
            var address = endpoint.Address.ToString();
            var entry = Dns.GetHostEntry(address);
            var hostname = entry.HostName ?? "127.0.0.1";
#else
            var hostname = endpoint?.Address.ToString() ?? "127.0.0.1";
#endif
            return CreateUrl(hostname);
        }
    }
}
