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
        private static async Task<TcpClient> listenLoop(int port, NetworkInterfaceType interfaceType)
        {
            while (true)
            {
                TcpClient? client = await ListenerConnection.WaitForConnection(port, interfaceType);

                if (client is null)
                {
                    Console.WriteLine("A device failed to connect\n");

                    continue;
                }

                Console.WriteLine("Connection established\n");

                return client;
            }
        }

        public static async Task<TcpClient> GetClient(int? port, NetworkInterfaceType interfaceType)
        {
            IPAddress ip;

            if (port is not null)
            {
                return await listenLoop((int)port, interfaceType);
            }
            if (port is not null)
            {
                Console.WriteLine("Wait for a device to connect or");
            }
            
            ip = CLIHelp.GetIPv4("Insert the IP address of the device you want to connect to: ");

            TcpClient? client;

            do
            {
                client = ClientConnection.Connect(ip, interfaceType, port);
            }
            while (client is null);

            return client;
        }
    }
}
