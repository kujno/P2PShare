using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class ConnectionClient
    {
        public static event EventHandler<TcpClient>? Connected;
        public static event EventHandler? Disconnected;
        public static int Timeout { get; } = 120000;

        public static async Task Connect(IPAddress ip, NetworkInterface @interface, int port, Cancellation cancellation)
        {
            IPAddress? ipLocal = IPHandling.GetLocalIPv4(@interface);
            TcpClient client = new();

            if (ipLocal is null || cancellation.TokenSource is null)
            {
                client.Dispose();

                OnDisconnected();

                return;
            }

            client.Client.Bind(new IPEndPoint(ipLocal, port));

            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation.TokenSource.Token);

            try
            {
                await Task.WhenAny(client.ConnectAsync(ip, port, cancellationTokenSource.Token).AsTask(), Task.Delay(Timeout, cancellationTokenSource.Token));

                cancellationTokenSource.Cancel();
                cancellation.Cancel();

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

        public static bool AreClientsConnected(TcpClient?[] clients)
        {
            foreach (TcpClient? client in clients)
            {
                if (client is null || !client.Connected)
                {
                    return false;
                }
            }

            return true;
        }

        public static void GetRidOfClients(TcpClient?[] clients)
        {
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i]?.Dispose();
                clients[i] = null;
            }
        }

        public static Task[] ConnectAll(IPAddress ip, NetworkInterface @interface, int port, Cancellation cancellation)
        {
            Task[] connecting = new Task[2];

            for (int i = 0; i < 2; i++)
            {
                connecting[i] = Connect(ip, @interface, port + i, cancellation);
            }

            return connecting;
        }
    }
}