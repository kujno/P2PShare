using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace P2PShare.Libs
{
    public class ClientConnection
    {
        public static TcpClient? Connect(IPAddress ip, NetworkInterface @interface, int port)
        {
            IPAddress? ipLocal = IPv4Handling.GetLocalIPv4(@interface);
            TcpClient? client = new TcpClient();

            if (ipLocal is null)
            {
                client.Dispose();

                return null;
            }

            try
            {
                client.Connect(ip, port);
            }
            catch
            {
                client.Dispose();

                return null;
            }

            return client;
        }
    }
}