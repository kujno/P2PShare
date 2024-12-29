using P2PShare.Libs;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace P2PShare.CLI
{
    public class CLIConnection
    {
        public static async Task<TcpClient> GetClient(int? portListen, NetworkInterface @interface)
        {
            IPAddress? ip;
            TcpClient? client = null;
            Task<TcpClient>? listenTask = null;
            int portConnect = 0;
            IPAddress? ipLocal = IPv4Handling.GetLocalIPv4(@interface);

            if (portListen is not null)
            {
                listenTask = ListenerConnection.ListenLoop((int)portListen, @interface);
            }

            if (ipLocal is not null)
            {
                portConnect = PortHandling.FindPort(ipLocal);
            }

            while (true)
            {
                bool nullable;

                switch (portListen)
                {
                    case null:
                        nullable = false;

                        break;

                    default:
                        Console.WriteLine("Press [Enter] key to chceck for any outer connection or ");
                        
                        nullable = true;

                        break;
                }

                ip = CLIHelp.GetIPv4("Insert the IP address of the device you want to connect to: ", nullable);

                if (ip is null)
                {
                    if (listenTask is not null && listenTask.IsCompleted)
                    {
                        Console.WriteLine("A foreign device has established connection with your device");

                        return listenTask.Result;
                    }

                    continue;
                }

                if ((listenTask is not null && !listenTask.IsCompleted) || listenTask is null)
                {
                    Console.WriteLine($"Trying to connect on port {portConnect}...\n");
                    client = ClientConnection.Connect(ip, @interface, portConnect).Result;
                }

                if (client is not null)
                {
                    Console.WriteLine("A connection has been established\n");

                    return client;
                }

                Console.WriteLine("Could not establish a connection. Try again\n");
            }
        }
    }
}
