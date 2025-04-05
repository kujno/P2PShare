using P2PShare.GUI.Models;
using P2PShare.Libs;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
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

        public static void Disconnected(TextBlock State, Button Cancel, ComboBox @interface)
        {
            State.Text = "Disconnected";
            State.Foreground = System.Windows.Media.Brushes.Red;
            Cancel.Visibility = Visibility.Collapsed;

            RefreshInterfaces(@interface);
        }

        public static void Connected(TextBlock State, Button Cancel, IPAddress ip)
        {
            State.Text = $"Connected to {ip}";
            State.Foreground = System.Windows.Media.Brushes.Green;
            Cancel.Visibility = Visibility.Collapsed;
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

        public static void ChangeFileTransferState(Send_Receive sendReceiveWindow, int part, Receive_Send receive_Send)
        {
            sendReceiveWindow.Text.Text = $"{received_Sent(receive_Send)}: {part}%";

            if (part != 100)
            {
                return;
            }

            sendReceiveWindow.Close();
        }

        private static string received_Sent(Receive_Send receive_Send)
        {
            switch (receive_Send)
            {
                case Receive_Send.Receive:
                    return "Received";

                case Receive_Send.Send:
                    return "Sent";
            }

            return "";
        }

        public static void FileTransferEndDialog(bool succeeded)
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

            ShowDialog($"The file transfer {message}");
        }
    }
}
