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
        private static async Task<TcpClient> listenLoop(int port, NetworkInterface @interface)
        {
            while (true)
            {
                TcpClient? client = await ListenerConnection.WaitForConnection(port, @interface);

                if (client is null)
                {
                    Console.WriteLine("A device failed to connect\n");

                    continue;
                }

                Console.WriteLine("Connection established\n");

                return client;
            }
        }

        public static Task<TcpClient> GetClient(int? port, NetworkInterface @interface)
        {
            return Task.Run(() =>
            {
                IPAddress? ip;
                TcpClient? client = null;
                Task<TcpClient>? listenTask = null;

                if (port is not null)
                {
                    listenTask = listenLoop((int)port, @interface);
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

                    ip = CLIHelp.GetIPv4("Insert the IP address of the device you want to connect to: ");

                    if (listenTask is null)
                    {
                        client = ClientConnection.Connect(ip, @interface, port);
                    }

                    if (client is not null)
                    {
                        return client;
                    }

                    Console.WriteLine("Could not establish connection\n");
                }
            });
        }
    }
}
