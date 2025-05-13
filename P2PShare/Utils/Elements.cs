using P2PShare.Models;
using P2PShare.Libs;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using P2PShare.Libs.Models;

namespace P2PShare.Utils
{
    public class Elements
    {
        public static void RefreshInterfaces(ComboBox @interface, string? nic)
        {
            List<NetworkInterface> interfaces = InterfaceHandling.GetUpInterfaces();

            @interface.Items.Clear();

            foreach (NetworkInterface @interface2 in interfaces)
            {
                @interface.Items.Add(@interface2.Name);
            }

            if (nic is not null)
            {
                pickNICAgain(@interface, nic);
            }
        }

        public static void Disconnected(TextBlock State, Button Cancel, Button Disconnect, ComboBox @interface, string? nic)
        {
            State.Text = "Disconnected";
            State.Foreground = System.Windows.Media.Brushes.Red;
            Cancel.Visibility = Visibility.Collapsed;

            Disconnect.Visibility = Visibility.Collapsed;

            RefreshInterfaces(@interface, nic);
        }

        public static void Connected(TextBlock State, Button Cancel, Button Disconnect, IPAddress ip)
        {
            string ipString = ip.ToString();

            if (ipString.Contains("::ffff:"))
            {
                ipString = ipString.Substring("::ffff:".Length);
            }

            State.Text = $"Connected to {ipString}";
            State.Foreground = System.Windows.Media.Brushes.Green;
            Cancel.Visibility = Visibility.Collapsed;
            
            Disconnect.Visibility = Visibility.Visible;
        }

        public static void Listening(int port, TextBlock State, Button Cancel)
        {
            State.Text = $"Listening on port {port}";
            State.Foreground = System.Windows.Media.Brushes.Yellow;
            Cancel.Visibility = Visibility.Visible;
        }

        public static void Connecting(int port, TextBlock State, Button Cancel)
        {
            State.Text = $"Connecting on port {port}";
            State.Foreground = System.Windows.Media.Brushes.Yellow;
            Cancel.Visibility = Visibility.Visible;
        }

        public static void ShowDialog(string message)
        {
            CustomMessageBox messageBox = new CustomMessageBox();
            
            messageBox.Text.Text = message;
            
            messageBox.ShowDialog();
        }

        public static void ResetYourIp(TextBlock YourIP)
        {
            YourIP.Text = "Your IP address:";
        }

        public static NetworkInterface? GetSelectedInterface(ComboBox Interface)
        {
            foreach (NetworkInterface @interface in InterfaceHandling.GetUpInterfaces())
            {
                if (@interface.Name == Interface.SelectedItem.ToString())
                {
                    return @interface;
                }
            }

            return null;
        }

        public static void ChangeFileTransferState(Send_Receive sendReceiveWindow, int part, ReceiveSendEnum receive_Send)
        {
            sendReceiveWindow.Text.Text = $"{Received_Sent(receive_Send)}: {part}%";

            if (part != 100)
            {
                return;
            }

            sendReceiveWindow.Close();
        }

        public static string Received_Sent(ReceiveSendEnum receive_Send)
        {
            switch (receive_Send)
            {
                case ReceiveSendEnum.Receive:
                    return "Received";

                case ReceiveSendEnum.Send:
                    return "Sent";
            }

            return "";
        }

        public static void FileTransferEndDialog(bool succeeded, Send_Receive? sendReceiveWindow)
        {
            string message;
            
            switch (succeeded)
            {
                case true:
                    message = "succeeded";
                    
                    break;

                case false:
                    message = "failed";

                    break;
            }

            sendReceiveWindow?.Close();

            ShowDialog($"The file transfer {message}");
        }

        private static void pickNICAgain(ComboBox nicComboBox, string nic)
        {
            if (nicComboBox.SelectedItem?.ToString() != nic && nicComboBox.Items.Contains(nic))
            {
                nicComboBox.SelectedItem = nic;
            }
        }

        public static void InitializeEncryptionComboBox(ComboBox encryption)
        {
            Enum.GetNames(typeof(EncryptionEnum)).ToList().ForEach(option => encryption.Items.Add(option));
        }
    }
}
