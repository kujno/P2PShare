using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class ClientConnection
    {
        public static event EventHandler<TcpClient>? Connected;
        public static event EventHandler? Disconnected;

        public static async Task Connect(IPAddress ip, NetworkInterface @interface, int port)
        {
            IPAddress? ipLocal = IPv4Handling.GetLocalIPv4(@interface);
            TcpClient? client = new TcpClient();

            if (ipLocal is null)
            {
                client.Dispose();

                OnDisconnected();

                return;
            }

            try
            {
                await Task.WhenAny(client.ConnectAsync(ip, port), Task.Delay(30000));

                if (client.Connected)
                {
                    OnConnected(client);

                    return;
                }
            }
            catch
            {
            }

            client.Dispose();

            OnDisconnected();
        }

        public static void OnConnected(TcpClient client)
        {
            Connected?.Invoke(null, client);
        }

        public static void OnDisconnected()
        {
            Disconnected?.Invoke(null, EventArgs.Empty);
        }
    }
}