using P2PShare.Libs;
using System.Net.NetworkInformation;

namespace P2PShare.CLI
{
    public class CLIInterfaceHandling
    {
        public static NetworkInterface GetInterface(int count)
        {
            List<NetworkInterface> interfacesUp;
            int? interfaceInt;

            do
            {
                Console.Clear();

                interfacesUp = InterfaceHandling.GetUpInterfaces();

                if (count == 0)
                {
                    CLIHelp.Hello();
                }

                Console.WriteLine("Up network interfaces:\n----------------------");
                for (int j = 0; j < interfacesUp.Count; j++)
                {
                    Console.WriteLine($"{j + 1} - {interfacesUp[j].Name} ({interfacesUp[j].NetworkInterfaceType})");
                }
                interfaceInt = CLIHelp.GetNullableInt("\nChoose a network interface / press [Enter] key to refresh: ", 1, interfacesUp.Count);

                Console.WriteLine();
            }
            while (interfaceInt is null);

            return interfacesUp[(int)interfaceInt - 1];
        }
    }
}
