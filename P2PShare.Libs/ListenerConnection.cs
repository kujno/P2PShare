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
                using (cancellationToken.Register(() => listener.Stop()))
                {
                    client = await listener.AcceptTcpClientAsync();
                }
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
