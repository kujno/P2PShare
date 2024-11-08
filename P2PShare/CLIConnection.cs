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

        public static async Task<TcpClient?> GetClient(int? port, NetworkInterface @interface)
        {
            IPAddress? ip;
            TcpClient? client = null;
            Task<TcpClient>? listenTask = null;

            if (port is not null)
            {
                listenTask = listenLoop((int)port, @interface);
            }
            if (port is not null)
            {
                Console.WriteLine("Press [Enter] key to wait for connection or");
            }
            
            ip = CLIHelp.GetIPv4Nullable("Insert the IP address of the device you want to connect to: ");

            if (port is null)
            {
                IPAddress? ipLocal = IPv4Handling.GetLocalIPv4(@interface);

                if (ipLocal is not null)
                {
                    port = PortHandling.FindPort(ipLocal);
                }
            }

            if (ip is not null)
            {
                while (client is null && (listenTask is null || !listenTask.IsCompleted))
                {
                    Console.WriteLine(port);

                    client = ClientConnection.Connect(ip, @interface, ref port);
                }
            }
            // toto treba dokoncit a refaktorovat asi

            if (client is not null)
            {
                return client;
            }

            while (listenTask is null && ip is null)
            {

            }

            if (listenTask is not null)
            {
                client = await listenTask;
            }

            return client;
        }
    }
}
