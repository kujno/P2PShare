using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class ListenerConnection
    {
        public static async Task<TcpClient?> WaitForConnection(int port, NetworkInterface @interface, CancellationToken cancellationToken)
        {
            IPAddress? ipLocal = IPv4Handling.GetLocalIPv4(@interface);

            if (ipLocal is null)
            {
                return null;
            }

            TcpListener listener = new TcpListener(ipLocal, port);
            TcpClient client;

            try
            {
                listener.Start();
                client = await listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                listener.Stop();
            }

            return client;
        }

        public static async Task ListenLoop(int port, NetworkInterface @interface, CancellationToken cancellationToken)
        {
            while (true && !cancellationToken.IsCancellationRequested)
            {
                TcpClient? client = await WaitForConnection(port, @interface, cancellationToken);

                if (client is null)
                {
                    continue;
                }

                ClientConnection.OnConnected(client);

                return;
            }

            ClientConnection.OnDisconnected();
        }

        public static void GetRidOfListener(ref TcpListener listener)
        {
            listener.Stop();
            listener.Server.Dispose();
        }

        public static void StartTestOfListener(TcpListener listener)
        {
            listener.Start();
            listener.Stop();
        }
    }
}
