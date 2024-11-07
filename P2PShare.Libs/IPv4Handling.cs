using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace P2PShare.Libs
{
    public class IPv4Handling
    {
        // https://stackoverflow.com/questions/6803073/get-local-ip-address
        public static IPAddress? GetLocalIPv4(NetworkInterface @interface)
        {
            IPAddress? output = null;

            // finds the ip address of the selected network interface
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.Name == @interface.Name && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address;
                        }
                    }
                }
            }

            return output;
        }
    }
}
