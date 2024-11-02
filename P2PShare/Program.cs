using System;
using System.Net.Sockets;

namespace P2PShare
{
    class Program
    {
        static void Main(string[] args)
        {
            string ip;
            
            Console.Write("IP addresss: ");

            ip = Console.ReadLine();
            
            Console.ReadKey();
        }
    }
}