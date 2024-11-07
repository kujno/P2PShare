using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PShare.Libs
{
    public class PortHandling
    {
        public static int FindPort(IPAddress ip)
        {
            Random random = new Random();
            int port;

            do
            {
                port = random.Next(49152, 65536);
            }
            while (!IsPortAvailable(ip, port));

            return port;
        }

        public static bool IsPortAvailable(IPAddress ip, int port)
        {
            try
            {
                TcpListener listener = new TcpListener(ip, port);

                listener.Start();
                listener.Stop();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
