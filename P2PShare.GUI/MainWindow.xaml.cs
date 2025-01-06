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
        protected CancellationTokenSource? listenCancel;

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
                YourIP.Text = "Your IP address:";

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

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            bool parsed = int.TryParse(Port.Text.Trim(), out portListen);

            if (!parsed || @interface is null)
            {
                Elements.ShowDialog("Select an interface & enter a valid port number", messageBox);

                return;
            }

            listenCancel = new CancellationTokenSource();

            listen = ListenerConnection.ListenLoop(portListen, @interface, listenCancel.Token);

            State.Text = $"Listening on port {portListen}";
            State.Foreground = System.Windows.Media.Brushes.Yellow;
        }

        private void OnConnected(object? sender, TcpClient client2)
        {
            IPEndPoint? ipEndPoint;
            
            client = client2;

            ipEndPoint = (IPEndPoint?)client.Client.RemoteEndPoint;

            if (ipEndPoint is null)
            {
                return;
            }

            Elements.Connected(State, ipEndPoint.Address);

            monitorConnection = GUIConnection.MonitorClientConnection(client2, State, Interface);
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            Elements.Disconnected(State, Interface);
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            IPAddress? remoteIP;

            if (listenCancel is not null)
            {
                listenCancel.Cancel();
                listenCancel.Dispose();
            }

            if (@interface is null || localIP is null || !IPAddress.TryParse(RemoteIP.Text.Trim(), out remoteIP))
            {
                Elements.ShowDialog("Select an interface & enter a valid IP address", messageBox);

                return;
            }
            
            portConnect = PortHandling.FindPort(localIP);

            connecting = ClientConnection.Connect(remoteIP, @interface, portConnect);

            State.Text = $"Connecting on port {portConnect}";
            State.Foreground = System.Windows.Media.Brushes.Yellow;
        }
    }
}