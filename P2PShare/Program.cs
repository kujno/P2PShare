using P2PShare.CLI;
using System;
using System.Net.Sockets;

namespace P2PShare
{
    class Program
    {
        static void Main(string[] args)
        {
            string ip = CLIHelp.getString("IP address: ");
            
            Console.ReadKey();
        }
    }
}