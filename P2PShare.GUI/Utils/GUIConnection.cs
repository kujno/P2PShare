using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Controls;

namespace P2PShare.GUI.Utils
{
    public class GUIConnection
    {
        public async static Task MonitorClientConnection(TcpClient client, TextBlock State, ComboBox @interface)
        {
            while (client.Connected)
            {
                try
                {
                    if (client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Available == 0)
                    {
                        Elements.Disconnected(State, @interface);

                        return;
                    }
                }
                catch
                {
                    Elements.Disconnected(State, @interface);

                    break;
                }

                await Task.Delay(1000);
            }
        }
    }
}
