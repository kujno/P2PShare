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

            CLIFileTransport.SharingLoop();

            CLIHelp.Goodbye();

            Console.ReadKey();
        }
    }
}