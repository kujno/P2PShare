using P2PShare.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace P2PShare.CLI
{
    public class CLIHelp
    {
        public static string getString(string message)
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

        public static int getInt(string message)
        {
            int output;

            while (!int.TryParse(getString(message), out output))
            {

            }

            return output;
        }

        public static int getInt(string message, int min, int max)
        {
            int output;

            do
            {
                output = getInt(message);
            }
            while (output < min || output > max);

            return output;
        }

        public static int? getNullableInt(string message)
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

        public static int? getNullableInt(string message, int min, int max)
        {
            int? input;

            do
            {
                input = getNullableInt(message);

                if (input is null)
                {
                    return null;
                }
            }
            while (input < min || input > max);

            return input;
        }

        public static int? getNullablePortInt(string message, NetworkInterfaceType interfaceType)
        {
            int? input;
            IPAddress? ip;

            do
            {
                input = getNullableInt(message);
                ip = ClientConnection.GetLocalIPv4(interfaceType);

                // checks
                if (input is null)
                {
                    return null;
                }
                if (ip is null)
                {
                    return null;
                }
            }
            while (input < 49152 || !ClientConnection.IsPortAvailable(ip, (int)input));

            return input;
        }
    }
}
