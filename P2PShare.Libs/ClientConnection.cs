using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class ClientConnection
    {
        public static async Task<TcpClient?> Connect(IPAddress ip, NetworkInterface @interface, int port)
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
                await ConnectOrExpire(client, ip, port);

                if (client.Connected)
                {
                    return client;
                }

                client.Dispose();

                return null;
            }
            catch
            {
                client.Dispose();

                return null;
            }
        }

        private static async Task ConnectOrExpire(TcpClient client, IPAddress ip, int port)
        {
            await Task.WhenAny(client.ConnectAsync(ip, port), Task.Delay(30000));

            // treba dokoncit ukoncenie pripajania
        }
    }
}