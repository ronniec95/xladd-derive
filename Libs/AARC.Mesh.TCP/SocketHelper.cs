using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace AARC.Mesh.TCP
{
    public static class SocketHelper
    {
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
