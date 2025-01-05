using P2PShare.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace P2PShare.GUI.Utils
{
    public class Elements
    {
        public static void RefreshInterfaces(ComboBox @interface)
        {
            List<NetworkInterface> interfaces = InterfaceHandling.GetUpInterfaces();

            @interface.Items.Clear();

            foreach (NetworkInterface @interface2 in interfaces)
            {
                @interface.Items.Add(@interface2.Name);
            }
        }
    }
}
