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
            CLIHelp.SetDesign();

            NetworkInterface @interface;
            int? interfaceInt;
            List<NetworkInterface> interfacesUp;

            do
            {
                Console.Clear();

                interfacesUp = InterfaceHandling.GetUpInterfaces();

                Console.WriteLine("Welcome to P2PShare software\n");

                Console.WriteLine("Up network interfaces:\n----------------------");
                for (int j = 0; j < interfacesUp.Count; j++)
                {
                    Console.WriteLine($"{j + 1} - {interfacesUp[j].Name} ({interfacesUp[j].NetworkInterfaceType})");
                }
                interfaceInt = CLIHelp.GetNullableInt("\nChoose a network interface / press [Enter] key to refresh: ", 1, interfacesUp.Count);

                Console.WriteLine();
            }
            while (interfaceInt is null);

            @interface = interfacesUp[(int)interfaceInt - 1];

            do
            {
                CLIFileTransport.Sharing(@interface);
            }
            while (CLIHelp.GetBool("Would you like to send/receive any other file? [y/n]: "));

            Console.WriteLine("Goodbye");

            Console.ReadKey();
        }
    }
}