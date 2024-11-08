using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace P2PShare.Libs
{
    public class ClientConnection
    {
        public static TcpClient? Connect(IPAddress ip, NetworkInterface @interface, ref int? port)
        {
            IPAddress? ipLocal = IPv4Handling.GetLocalIPv4(@interface);
            TcpClient? client = new TcpClient();
            bool customPort = port.HasValue;

            if (ipLocal is null)
            {
                client.Dispose();

                return null;
            }

            switch (port.HasValue)
            {
                case true:
                    if (!PortHandling.IsPortAvailable(ipLocal, (int)port))
                    {
                        client.Dispose();

                        port = PortHandling.FindPort(ipLocal);

                        return null;
                    }
                    
                    break;

                case false:
                    port = PortHandling.FindPort(ipLocal);

                    break;
            }

            try
            {
                client.Client.Bind(new IPEndPoint(ipLocal, (int)port));
                
                client.Connect(ip, (int)port);
            }
            catch
            {
                client.Dispose();

                if (customPort)
                {
                    return null;
                }

                port = null;

                return null;
            }

            return client;
        }
    }
}