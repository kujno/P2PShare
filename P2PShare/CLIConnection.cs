using P2PShare.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace P2PShare.CLI
{
    public class CLIConnection
    {
        public static async Task<TcpClient> GetClient(int? port, NetworkInterface @interface)
        {
            IPAddress? ip;
            TcpClient? client = null;
            Task<TcpClient>? listenTask = null;

            if (port is not null)
            {
                listenTask = ListenerConnection.ListenLoop((int)port, @interface);
            }

            if (port is null)
            {
                IPAddress? ipLocal = IPv4Handling.GetLocalIPv4(@interface);

                if (ipLocal is not null)
                {
                    port = PortHandling.FindPort(ipLocal);
                }
            }

            while (true)
            {
                if (listenTask is not null && listenTask.IsCompleted)
                {
                    Console.WriteLine("A foreign device has established connection with your device");

                    return listenTask.Result;
                }

                if (port is not null)
                {
                    Console.WriteLine("Press [Enter] key to chceck for any outer connection or ");
                }

                ip = CLIHelp.GetIPv4Nullable("Insert the IP address of the device you want to connect to: ");

                if (ip is null)
                {
                    continue;
                }

                if (listenTask is null)
                {
                    client = ClientConnection.Connect(ip, @interface, port);

                    Console.WriteLine("Established a connection");
                }

                if (client is not null)
                {
                    return client;
                }

                Console.WriteLine("Could not establish connection. Try again\n");
            }
        }
    }
}
