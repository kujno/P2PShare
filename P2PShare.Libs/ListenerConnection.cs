using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace P2PShare.Libs
{
    public class ListenerConnection
    {
        public static async Task<TcpClient?> WaitForConnection(int port, NetworkInterfaceType interfaceType)
        {
            IPAddress? ipLocal = ClientConnection.GetLocalIPv4(interfaceType);
            
            if (ipLocal is null)
            {
                return null;
            }
            
            TcpListener listener = new TcpListener(ipLocal, port);
            TcpClient client;

            try
            {
                listener.Start();
                client = await listener.AcceptTcpClientAsync();
            }
            catch (SocketException)
            {
                return null;
            }
            finally
            {
                listener.Dispose();
            }

            // lock the client to the local IP (interface) and port
            client.Client.Bind(new IPEndPoint(ipLocal, port));

            return client;
        }
    }
}
