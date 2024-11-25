using P2PShare.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PShare.CLI
{
    public class CLIHelp
    {
        public static string GetString(string? message)
        {
            string? output;
            
            do
            {
                Console.Write(message);
                
                output = Console.ReadLine();

                if (String.IsNullOrEmpty(output))
                {
                    continue;
                }

                output = output.Trim();
            }
            while (String.IsNullOrEmpty(output));

            return output;
        }

        public static int GetInt(string message)
        {
            int output;

            while (!int.TryParse(GetString(message), out output))
            {

            }

            return output;
        }

        public static int GetInt(string message, int min, int max)
        {
            int output;

            do
            {
                output = GetInt(message);
            }
            while (output < min || output > max);

            return output;
        }

        public static int? GetNullableInt(string message)
        {
            string? input;
            int output;

            do
            {
                Console.Write(message);
                input = Console.ReadLine();
                if (!String.IsNullOrEmpty(input))
                {
                    input = input.Trim();
                }
                if (String.IsNullOrEmpty(input))
                {
                    return null;
                }
            }
            while (!int.TryParse(input, out output));

            return output;
        }

        public static int? GetNullableInt(string message, int min, int max)
        {
            int? input;

            do
            {
                input = GetNullableInt(message);

                if (input is null)
                {
                    return null;
                }
            }
            while (input < min || input > max);

            return input;
        }

        public static int? GetNullablePortInt(string message, NetworkInterface @interface)
        {
            int? input;
            int i = 0;
            IPAddress? ip = IPv4Handling.GetLocalIPv4(@interface); ;

            do
            {
                if (i > 0)
                {
                    Console.WriteLine("Selected port is unavailiable. Please Select another one!\n");
                }

                input = GetNullableInt(message);

                // checks
                if (input is null)
                {
                    return null;
                }
                if (ip is null)
                {
                    return null;
                }

                i++;
            }
            while (input < 49152 || !PortHandling.IsPortAvailable(ip, (int)input));

            return input;
        }

        public static FileInfo GetFileInfo(string message)
        {
            FileInfo output;

            do
            {
                output = new FileInfo(GetString(message));
            }
            while (!output.Exists);

            return output;
        }

        public static DirectoryInfo GetDirectoryInfo(string message)
        {
            DirectoryInfo output;
            
            do
            {
                output = new DirectoryInfo(GetString(message));
            }
            while (!output.Exists);
            
            return output;
        }

        public static void PrintFileInfo(FileInfo fileInfo)
        {
            Console.WriteLine($"File informations:\n-------------------\nFile path: {fileInfo.FullName}\nSize: {fileInfo.Length}\n");
        }

        public static IPAddress? GetIPv4(string message, bool nullable)
        {
            IPAddress? output;
            string? input;
            
            do
            {
                Console.Write(message);

                input = Console.ReadLine();

                if (!String.IsNullOrEmpty(input))
                {
                    input = input.Trim();
                }

                if (!String.IsNullOrEmpty(input))
                {
                    continue;
                }

                switch (nullable)
                {
                    case true:
                        return null;
                        
                    case false:
                        continue;
                }
            }
            while (!IPAddress.TryParse(input, out output));

            return output;
        }

        public static bool GetBool(string message)
        {
            string input;

            do
            {
                input = GetString(message).ToLower();
            }
            while (input != "y" && input != "n");

            switch (input)
            {
                case "y":
                    return true;
                
                default:
                    return false;
            }
        }

        public static void SetDesign()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "P2PShare";
        }

        public static void Goodbye()
        {
            Console.WriteLine("Goodbye");
        }

        public static void Hello()
        {
            Console.WriteLine("Welcome to P2PShare software\n");
        }
    }
}
