using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class ClientConnection
    {
        public static TcpClient? Connect(string ip, NetworkInterfaceType @interface, ref int? port)
        {
            IPAddress? ipLocal = GetLocalIPv4(@interface);
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
                    if (!IsPortAvailable(ipLocal, (int)port))
                    {
                        client.Dispose();

                        return null;
                    }
                    
                    break;

                case false:
                    port = FindPort(ipLocal);

                    break;
            }

            try
            {
                client.Client.Bind(new IPEndPoint(ipLocal, (int)port));
                
                client.Connect(ip, (int)port);
            }
            catch (SocketException)
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

        private static int FindPort(IPAddress ip)
        {
            Random random = new Random();
            int port;

            do
            {
                port = random.Next(49152, 65536);
            }
            while (!IsPortAvailable(ip, port));

            return port;
        }

        // https://stackoverflow.com/questions/6803073/get-local-ip-address
        public static IPAddress? GetLocalIPv4(NetworkInterfaceType @interface)
        {
            IPAddress? output = null;

            // finds the ip address of the selected network interface
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

        public static bool IsPortAvailable(IPAddress ip, int port)
        {
            try
            {
                TcpListener listener = new TcpListener(ip, port);
                
                listener.Start();
                listener.Stop();
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}