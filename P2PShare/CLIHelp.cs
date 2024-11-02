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
    }
}
