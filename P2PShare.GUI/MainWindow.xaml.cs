using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        protected Task? listen;
        protected Task? monitorConnection;
        protected Task? connecting;
        protected int portListen;
        protected int portConnect;
        protected CustomMessageBox messageBox = new CustomMessageBox();
        protected TcpClient? client;
        protected CancellationTokenSource? cancel;

        public MainWindow()
        {
            InitializeComponent();
            Elements.RefreshInterfaces(Interface);
            ClientConnection.Connected += OnConnected;
            ClientConnection.Disconnected += OnDisconnected;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
            messageBox.Close();
        }

        private void ToolBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Elements.RefreshInterfaces(Interface);
        }

        private void Interface_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Interface.SelectedItem is null)
            {
                Elements.ResetYourIp(YourIP);

                return;
            }

            @interface = Elements.GetSelectedInterface(Interface);

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

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            bool parsed = int.TryParse(Port.Text.Trim(), out portListen);

            if (localIP is null || !parsed || @interface is null || !PortHandling.IsPortAvailable(localIP, portListen))
            {
                Elements.ShowDialog("Select an interface & enter a valid port number", messageBox);

                return;
            }

            cancel = new CancellationTokenSource();

            listen = ListenerConnection.ListenLoop(portListen, @interface, cancel.Token);

            Elements.Listening(portListen, State, Cancel);
        }

        private void OnConnected(object? sender, TcpClient client2)
        {
            IPAddress? ipRemote;
            
            client = client2;

            ipRemote = IPv4Handling.GetRemoteIPAddress(client);

            if (ipRemote is null)
            {
                return;
            }

            Elements.Connected(State, Cancel, ipRemote);

            monitorConnection = GUIConnection.MonitorClientConnection(client2, State, Interface, Cancel);
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            Elements.Disconnected(State, Cancel, Interface);
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            IPAddress? remoteIP;

            if (cancel is not null)
            {
                cancel = Cancellation.Cancel(cancel);
            }

            if (@interface is null || localIP is null || !IPAddress.TryParse(RemoteIP.Text.Trim(), out remoteIP))
            {
                Elements.ShowDialog("Select an interface & enter a valid IP address", messageBox);

                return;
            }
            
            portConnect = PortHandling.FindPort(localIP);

            cancel = new CancellationTokenSource();

            connecting = ClientConnection.Connect(remoteIP, @interface, portConnect, cancel.Token);

            Elements.Connecting(portConnect, State, Cancel);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (cancel is null)
            {
                return;
            }
            
            cancel.Cancel();
            cancel.Dispose();
            cancel = null;
        }
    }
}