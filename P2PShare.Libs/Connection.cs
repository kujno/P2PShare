using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class Connection
    {
        public static async Task<TcpClient?> Connect(string ip, NetworkInterfaceType @interface)
        {
            IPAddress? ipLocal = GetLocalIPv4(@interface);
            TcpClient? client = null;

            if (ipLocal is null)
            {
                return client;
            }

            client = new TcpClient();

            await client.ConnectAsync(ip, FindPort(ipLocal));
            
            return client;
        }

        private static int FindPort(IPAddress ip)
        {
            Random random = new Random();
            int port;
            int test;

            do
            {
                TcpListener listener;
                TcpClient testClient;
                NetworkStream stream;
                port = random.Next(49152, 65536);

                listener = new TcpListener(ip, port);
                testClient = listener.AcceptTcpClient();
                stream = testClient.GetStream();
                test = stream.Read(new byte[1], 0, 1);
            }
            while (test != 0);

            return port;
        }

        // https://stackoverflow.com/questions/6803073/get-local-ip-address
        private static IPAddress? GetLocalIPv4(NetworkInterfaceType @interface)
        {
            IPAddress? output = null;
            
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == @interface && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address;
                        }
                    }
                }
            }

            return output;
        }
    }
}
