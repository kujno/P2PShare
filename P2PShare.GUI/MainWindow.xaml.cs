using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using P2PShare.GUI.Utils;
using P2PShare.Libs;

namespace P2PShare.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected NetworkInterface? @interface;
        protected IPAddress? localIP;
        
        public MainWindow()
        {
            InitializeComponent();
            Elements.RefreshInterfaces(Interface);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToolBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            Elements.RefreshInterfaces(Interface);
        }

        private void Interface_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Interface.SelectedItem is null)
            {
                return;
            }

            foreach (NetworkInterface @interface2 in InterfaceHandling.GetUpInterfaces())
            {
                if (@interface2.Name == Interface.SelectedItem.ToString())
                {
                    @interface = @interface2;
                    
                    break;
                }
            }

            if (@interface is null)
            {
                return;
            }

            localIP = IPv4Handling.GetLocalIPv4(@interface);

            if (localIP is null)
            {
                return;
            }

            YourIP.Text = $"Your IP address: {localIP}";
        }
    }
}