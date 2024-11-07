using P2PShare.CLI;
using System;
using System.Net.Sockets;
using P2PShare.Libs;
using System.Net.NetworkInformation;

namespace P2PShare
{
    class Program
    {
        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "P2PShare";

            NetworkInterfaceType interfaceType;
            int? port;
            int? interfaceInt;

            do
            {
                List<NetworkInterface> interfacesUp = new List<NetworkInterface>();
                NetworkInterface?[] interfacesNullable = NetworkInterface.GetAllNetworkInterfaces();

                Console.Clear();

                // interfaces check
                if (interfacesNullable is null)
                {
                    throw new Exception("No network interfaces found");
                }

                NetworkInterface[] interfaces = interfacesNullable.Where(ni => ni != null).Cast<NetworkInterface>().ToArray();

                // up interfaces check
                for (int j = 0; j < interfaces.Length; j++)
                {
                    if (interfaces[j].OperationalStatus == OperationalStatus.Up)
                    {
                        interfacesUp.Add(interfaces[j]);
                    }
                }
                if (interfacesUp.Count == 0)
                {
                    throw new Exception("No up network interfaces found");
                }

                Console.WriteLine("Welcome to P2PShare software\n");

                Console.WriteLine("Up network interfaces:\n----------------------");
                for (int j = 0; j < interfacesUp.Count; j++)
                {
                    Console.WriteLine($"{j + 1} - {interfacesUp[j].Name} ({interfacesUp[j].NetworkInterfaceType})");
                }
                interfaceInt = CLIHelp.getNullableInt("\nChoose a network interface / press [Enter] key to refresh: ", 1, interfacesUp.Count - 1);

                Console.WriteLine();
            }
            while (interfaceInt is null);
            interfaceType = (NetworkInterfaceType)interfaceInt;

            port = CLIHelp.getNullablePortInt("If you would like to wait for a connection / choose a custom port, type a port number\nIf not press [Enter] key\n\nType a port number: ", interfaceType);

            

            Console.ReadKey();
        }

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

        private static async void receiveInviteLoop(TcpClient client)
        {
            while (true)
            {
                string? invite = await ClientConnection.ReceiveInvite(client);

                if (invite is null)
                {
                    Console.WriteLine("The connection was lost\n");

                    continue;
                }

                char reply;

                while (char.TryParse(CLIHelp.getString(invite).ToLower(), out reply) || (reply != 'y' && reply != 'n'))
                {
                    Console.WriteLine("Wrong input\n");
                }

                if (reply == 'n')
                {
                    Console.WriteLine("The file was not accepted\n");

                    continue;
                }

                int indexOfClosure = invite.IndexOf('(') + 1;
                int fileLength = int.Parse(invite.Substring(indexOfClosure, invite.IndexOf(')') - indexOfClosure));
                string filePath = CLIHelp.GetFileInfo("Insert the file path to save the file: ").FullName;
                FileInfo? fileInfo;

                Console.Clear();
                Console.WriteLine("The file will be received in a while...");

                fileInfo = ClientConnection.ReceiveFile(client, fileLength, filePath);

                if (fileInfo is null)
                {
                    Console.WriteLine("The file transfer failed\n");

                    continue;
                }

                Console.Clear();
                Console.WriteLine("The file was received successfully\n");

                CLIHelp.PrintFileInfo(fileInfo);
            }
        }

        private static void sharing(int? port, NetworkInterfaceType interfaceType)
        {
            do
            {
                
            }
            while (true);
        }

        //private static async Task<TcpClient> GetClient(int? port, NetworkInterfaceType interfaceType)
        //{
        //    TcpClient client;
            
        //    if (port is not null)
        //    {
        //        client = await listenLoop((int)port, interfaceType);
        //    }

        //    // treba dokoncit nacitanie ip adresy
        //}
    }
}