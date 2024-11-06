using P2PShare.CLI;
using System;
using System.Net.Sockets;
using P2PShare.Libs;
using System.Net.NetworkInformation;

namespace P2PShare
{
    class Program
    {
        static async Task Main()
        { 
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "P2PShare";
            
            NetworkInterfaceType interfaceType;
            int? port;
            TcpClient? client;
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

            if (port is not null)
            {
                client = await ListenerConnection.WaitForConnection((int)port, interfaceType);

                if (client is null)
                {
                    // vymysliet ako urobit nekonecny loop async
                }
            }

            Console.ReadKey();
        }
    }
}