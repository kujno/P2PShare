using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Controls;

namespace P2PShare.GUI.Utils
{
    public class GUIConnection
    {
        public async static Task MonitorClientConnection(TcpClient client, TextBlock State, ComboBox Interface, Button Cancel)
        {
            while (client.Connected)
            {
                try
                {
                    if (client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Available == 0)
                    {
                        break;
                    }
                }
                catch
                {
                    break;
                }

                await Task.Delay(1000);
            }

            Elements.Disconnected(State, Cancel, Interface);
        }
    }
}
