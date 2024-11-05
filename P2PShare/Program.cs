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
            NetworkInterface?[] interfacesNullable = NetworkInterface.GetAllNetworkInterfaces();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "P2PShare";
            
            // interfaces check
            if (interfacesNullable is null)
            {
                Console.WriteLine("No network interfaces found");
                
                return;
            }

            NetworkInterface[] interfaces = interfacesNullable.Where(ni => ni != null).Cast<NetworkInterface>().ToArray();
            List<NetworkInterface> interfacesUp = new List<NetworkInterface>();

            // up interfaces check
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].OperationalStatus != OperationalStatus.Up)
                {
                    interfacesUp.Add(interfaces[i]);
                }
            }
            if (interfacesUp.Count == 0)
            {
                Console.WriteLine("No up network interfaces found");

                return;
            }

            NetworkInterfaceType interfaceType;
            int? port;

            Console.WriteLine("Welcome to P2PShare software\n");

            Console.WriteLine("Up network interfaces:\n--------------------");
            for (int i = 0; i < interfacesUp.Count; i++)
            {
                Console.WriteLine($"{i + 1} - {interfacesUp[i].Name} ({interfacesUp[i].NetworkInterfaceType})");
            }
            interfaceType = (NetworkInterfaceType)(CLIHelp.getInt("\nChoose a network interface: ", 1, interfacesUp.Count) - 1);

            port = CLIHelp.getNullablePortInt("If you would like to wait for a connection / choose a custom port, type a port number\nIf not press [Enter] key\n\nType a port number: ", interfaceType);

            //if ()
            //{

            //}

            Console.ReadKey();
        }
    }
}