using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class ClientConnection
    {
        public static event EventHandler<TcpClient>? Connected;
        public static event EventHandler? Disconnected;

        public static async Task Connect(IPAddress ip, NetworkInterface @interface, int port, CancellationToken cancellationToken)
        {
            IPAddress? ipLocal = IPv4Handling.GetLocalIPv4(@interface);
            TcpClient? client = new();

            if (ipLocal is null)
            {
                client.Dispose();

                OnDisconnected();

                return;
            }

            client.Client.Bind(new IPEndPoint(ipLocal, port));

            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                await Task.WhenAny(client.ConnectAsync(ip, port, cancellationTokenSource.Token).AsTask(), Task.Delay(30000, cancellationTokenSource.Token));

                cancellationTokenSource.Cancel();

                cancellationTokenSource.Dispose();

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