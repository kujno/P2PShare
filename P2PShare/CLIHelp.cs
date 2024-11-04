using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
