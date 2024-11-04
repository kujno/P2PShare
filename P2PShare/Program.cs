using P2PShare.CLI;
using System;
using System.Net.Sockets;

namespace P2PShare
{
    class Program
    {
        static void Main()
        {
            WaitOrConnectEnum choice;
            int[] waitOrConnectValues = (int[])Enum.GetValuesAsUnderlyingType(typeof(WaitOrConnectEnum));
            string[] waitOrConnectNames = Enum.GetNames(typeof(WaitOrConnectEnum));

            // design
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "P2PShare";

            Console.WriteLine("Welcome to P2PShare software");

            for (int i = 0; i < waitOrConnectValues.Length; i++)
            {
                Console.WriteLine($"{waitOrConnectValues[i]} - {waitOrConnectNames[i]}");
            }

            choice = (WaitOrConnectEnum)Enum.ToObject(typeof(WaitOrConnectEnum), CLIHelp.getInt("Choose: ", waitOrConnectValues[0], waitOrConnectValues[waitOrConnectValues.Length - 1]));

            //switch (choice)
            //{

            //}

            Console.ReadKey();
        }
    }
}