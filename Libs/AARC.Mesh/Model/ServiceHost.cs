using System;
namespace AARC.Mesh.Model
{
    public class ServiceHost
    {
        public ServiceHost() { }

        public ServiceHost(string address)
        {
            var separator = address?.IndexOf(":") ?? -1;
            int port;
            // We have atleast ipaddress:NNNN or :NNNN
            if (separator >= 0)
            {
                var sport = address.Substring(separator + 1);
                int.TryParse(sport, out port);
                Port = port;
            }
            // We have atleast ipaddress:NNNN or ipaddress
            if (separator > 0)
                HostName = address.Substring(0, separator);
            else if (separator < 0)
                HostName = address;
            else
                HostName = "localhost";
        }
        public string HostName { get; set; }
        public int Port { get; set; }
        public override string ToString() => $"{HostName}:{Port}";
    }
}
