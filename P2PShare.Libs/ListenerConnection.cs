using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class ListenerConnection
    {
        public static async Task<TcpClient?> WaitForConnection(int port, NetworkInterface @interface)
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
                client = await listener.AcceptTcpClientAsync();
            }
            catch
            {
                return null;
            }
            finally
            {
                listener.Dispose();
            }

            return client;
        }

        public static async Task<TcpClient> ListenLoop(int port, NetworkInterface @interface)
        {
            while (true)
            {
                TcpClient? client = await WaitForConnection(port, @interface);

                if (client is null)
                {
                    continue;
                }

                return client;
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
