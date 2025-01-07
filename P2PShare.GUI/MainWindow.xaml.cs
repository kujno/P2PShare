using System.IO;
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
        protected Task? monitorInterface;
        protected Task? connecting;
        protected int portListen;
        protected int portConnect;
        protected CustomMessageBox messageBox = new CustomMessageBox();
        protected Invite inviteWindow = new Invite();
        protected Send_Receive sendReceiveWindow = new Send_Receive();
        protected TcpClient? client;
        protected CancellationTokenSource? cancelConnecting;
        protected CancellationTokenSource? cancelMonitoring;

        public MainWindow()
        {
            InitializeComponent();
            Elements.RefreshInterfaces(Interface);
            
            ClientConnection.Connected += OnConnected;
            ClientConnection.Disconnected += OnDisconnected;
            InterfaceHandling.InterfaceDown += onInterfaceDown;
            FileTransport.InviteReceived += onInviteReceived;
            FileTransport.FileBeingReceived += onFileBeingReceived;
            FileTransport.TransferFailed += onTransferFailed;
            FileTransport.FilePartReceived += onFilePartReceived;
            FileTransport.FilePartSent += onFilePartSent;
            FileTransport.FileBeingSent += onFileBeingSent;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
            messageBox.Close();
            inviteWindow.Close();
            sendReceiveWindow.Close();
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
            if (monitorInterface is not null)
            {
                cancelMonitoring?.Cancel();
                monitorInterface = null;
            }
            
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

            cancelMonitoring = new CancellationTokenSource();

            monitorInterface = InterfaceHandling.MonitorInterface(@interface, cancelMonitoring.Token);
        }

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            bool parsed = int.TryParse(Port.Text.Trim(), out portListen);

            if (localIP is null || !parsed || @interface is null || !PortHandling.IsPortAvailable(localIP, portListen))
            {
                Elements.ShowDialog("Select an interface & enter a valid port number", messageBox);

                return;
            }

            cancelConnecting = new CancellationTokenSource();

            listen = ListenerConnection.ListenLoop(portListen, @interface, cancelConnecting.Token);

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

            if (cancelConnecting is not null)
            {
                cancelConnecting = Cancellation.Cancel(cancelConnecting);
            }

            if (@interface is null || localIP is null || !IPAddress.TryParse(RemoteIP.Text.Trim(), out remoteIP))
            {
                Elements.ShowDialog("Select an interface & enter a valid IP address", messageBox);

                return;
            }
            
            portConnect = PortHandling.FindPort(localIP);

            cancelConnecting = new CancellationTokenSource();

            connecting = ClientConnection.Connect(remoteIP, @interface, portConnect, cancelConnecting.Token);

            Elements.Connecting(portConnect, State, Cancel);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (cancelConnecting is null)
            {
                return;
            }
            
            cancelConnecting.Cancel();
            cancelConnecting.Dispose();
            cancelConnecting = null;
        }

        private void onInterfaceDown(object? sender, EventArgs e)
        {
            Elements.RefreshInterfaces(Interface);
        }

        private async void onInviteReceived(object? sender, string? invite)
        {
            if (invite is null)
            {
                return;
            }

            bool accepted;

            inviteWindow.Text.Text = invite;
            inviteWindow.ShowDialog();
            accepted = inviteWindow.Accepted;

            if (client is null)
            {
                return;
            }

            bool? selected;
            string? path = FileDialogs.SelectFolder(out selected);

            if (selected is null || path is null)
            {
                return;
            }

            bool recieve = accepted && (bool)selected;

            await FileTransport.Reply(client, recieve);

            if (!recieve)
            {
                return;
            }

            FileInfo? file = await FileTransport.ReceiveFile(client, FileTransport.GetFileLenghtFromInvite(invite), path);

            if (file is null)
            {
                Elements.FileTransferEndDialog(messageBox, false);
                return;
            }

            Elements.ShowDialog($"The file has been saved to:\n{file.FullName}", messageBox);
        }

        private void onFileBeingReceived(object? sender, EventArgs e)
        {
            sendReceiveWindow.Text.Text = "Received: 0%";
            sendReceiveWindow.ShowDialog();
        }

        private void onFilePartReceived(object? sender, int part)
        {
            Elements.ChangeFileTransferState(sendReceiveWindow, part, Models.Receive_Send.Receive);
        }

        private void onFilePartSent(object? sender, int part)
        {
            Elements.ChangeFileTransferState(sendReceiveWindow, part, Models.Receive_Send.Send);
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (client is null || !client.Connected)
            {
                Elements.ShowDialog("You must be connected to share", messageBox);
                return;
            }
            
            FileInfo fileInfo = new FileInfo(File.Text);

            if (!fileInfo.Exists)
            {
                Elements.ShowDialog("Select a valid file", messageBox);
                return;
            }

            Elements.FileTransferEndDialog(messageBox, await FileTransport.SendFile(client, fileInfo));
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            File.Text = FileDialogs.SelectFile();
        }

        private void onTransferFailed(object? sender, EventArgs e)
        {
            sendReceiveWindow.Close();

            Elements.ShowDialog("The file transfer failed", messageBox);
        }

        private void onFileBeingSent(object? sender, EventArgs e)
        {
            sendReceiveWindow.Text.Text = "Sent: 0%";
            sendReceiveWindow.ShowDialog();
        }
    }
}