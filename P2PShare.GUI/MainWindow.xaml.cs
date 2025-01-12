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
        protected NetworkInterface? _interface;
        protected IPAddress? _localIP;
        protected Task? _listen;
        protected Task? _monitorConnection;
        protected Task? _monitorInterface;
        protected Task? _connecting;
        protected Task? _receiveInvite;
        protected int _portListen;
        protected int _portConnect;
        protected CustomMessageBox _messageBox = new CustomMessageBox();
        protected Invite _inviteWindow = new Invite();
        protected Send_Receive _sendReceiveWindow = new Send_Receive();
        protected TcpClient? _client;
        protected CancellationTokenSource? _cancelConnecting;
        protected CancellationTokenSource? _cancelMonitoring;
        protected CancellationTokenSource? _cancelReceivingInvite;

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
            _messageBox.Close();
            _inviteWindow.Close();
            _sendReceiveWindow.Close();
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
            if (_monitorInterface is not null)
            {
                _cancelMonitoring?.Cancel();
                _monitorInterface = null;
            }
            
            if (Interface.SelectedItem is null)
            {
                Elements.ResetYourIp(YourIP);

                return;
            }

            _interface = Elements.GetSelectedInterface(Interface);

            if (_interface is null)
            {
                return;
            }

            _localIP = IPv4Handling.GetLocalIPv4(_interface);

            if (_localIP is null)
            {
                return;
            }

            YourIP.Text = $"Your IP address: {_localIP}";

            _cancelMonitoring = new CancellationTokenSource();

            _monitorInterface = InterfaceHandling.MonitorInterface(_interface, _cancelMonitoring.Token);
        }

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            if (_localIP is null || !int.TryParse(Port.Text.Trim(), out _portListen) || _interface is null || !PortHandling.IsPortAvailable(_localIP, _portListen))
            {
                Elements.ShowDialog("Select an interface & enter a valid port number", _messageBox);

                return;
            }

            _cancelConnecting = new CancellationTokenSource();

            _listen = ListenerConnection.ListenLoop(_portListen, _interface, _cancelConnecting.Token);

            Elements.Listening(_portListen, State, Cancel);
        }

        private async void OnConnected(object? sender, TcpClient client2)
        {
            IPAddress? ipRemote;
            
            _client = client2;

            ipRemote = IPv4Handling.GetRemoteIPAddress(_client);

            if (ipRemote is null)
            {
                _client.Dispose();
                _client = null;

                return;
            }

            Elements.Connected(State, Cancel, ipRemote);

            _monitorConnection = GUIConnection.MonitorClientConnection(_client, State, Interface, Cancel);

            _cancelReceivingInvite = new CancellationTokenSource();

            _receiveInvite = FileTransport.ReceiveInvite(_client, _cancelReceivingInvite.Token);
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            Elements.Disconnected(State, Cancel, Interface);
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            IPAddress? remoteIP;

            if (_cancelConnecting is not null)
            {
                _cancelConnecting = Cancellation.Cancel(_cancelConnecting);
            }

            if (_interface is null || _localIP is null || !IPAddress.TryParse(RemoteIP.Text.Trim(), out remoteIP))
            {
                Elements.ShowDialog("Select an interface & enter a valid IP address", _messageBox);

                return;
            }
            
            _portConnect = PortHandling.FindPort(_localIP);

            _cancelConnecting = new CancellationTokenSource();

            _connecting = ClientConnection.Connect(remoteIP, _interface, _portConnect, _cancelConnecting.Token);

            Elements.Connecting(_portConnect, State, Cancel);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cancelConnecting is null)
            {
                return;
            }
            
            _cancelConnecting.Cancel();
            _cancelConnecting.Dispose();
            _cancelConnecting = null;
        }

        private void onInterfaceDown(object? sender, EventArgs e)
        {
            Elements.RefreshInterfaces(Interface);
        }

        private async void onInviteReceived(object? sender, string? invite) // todo: add calling of waiting for an invite again
        {
            if (invite is null)
            {
                return;
            }

            bool accepted;

            _inviteWindow.Text.Text = invite;
            _inviteWindow.ShowDialog();
            accepted = _inviteWindow.Accepted;

            if (_client is null)
            {
                Elements.ShowDialog("The file transfer failed", _messageBox);

                return;
            }

            bool? selected;
            bool receive;
            string? path = FileDialogs.SelectFolder(out selected);

            if (selected is not null && path is not null && (accepted && (bool)selected) == true)
            {
                receive = true;
            }
            else
            {
                receive = false;
            }

            await FileTransport.Reply(_client, receive);

            if (!receive || path is null)
            {
                return;
            }

            FileInfo? file = await FileTransport.ReceiveFile(_client, FileTransport.GetFileLenghtFromInvite(invite), path);

            if (file is null)
            {
                Elements.FileTransferEndDialog(_messageBox, false);
                return;
            }

            Elements.ShowDialog($"The file has been saved to:\n{file.FullName}", _messageBox);
        }

        private void onFileBeingReceived(object? sender, EventArgs e)
        {
            _sendReceiveWindow.Text.Text = "Received: 0%";
            _sendReceiveWindow.ShowDialog();
        }

        private void onFilePartReceived(object? sender, int part)
        {
            Elements.ChangeFileTransferState(_sendReceiveWindow, part, Models.Receive_Send.Receive);
        }

        private void onFilePartSent(object? sender, int part)
        {
            Elements.ChangeFileTransferState(_sendReceiveWindow, part, Models.Receive_Send.Send);
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (_receiveInvite is not null)
            {
                _cancelReceivingInvite?.Cancel();
                
                _receiveInvite = null;
            }
            
            if (_client is null || !_client.Connected)
            {
                Elements.ShowDialog("You must be connected to share", _messageBox);
                return;
            }
            
            FileInfo fileInfo = new(File.Text.Trim());

            if (!fileInfo.Exists)
            {
                Elements.ShowDialog("Select a valid file", _messageBox);
                return;
            }

            Elements.FileTransferEndDialog(_messageBox, await FileTransport.SendFile(_client, fileInfo));
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            File.Text = FileDialogs.SelectFile();
        }

        private void onTransferFailed(object? sender, EventArgs e)
        {
            _sendReceiveWindow.Close();

            Elements.ShowDialog("The file transfer failed", _messageBox);
        }

        private void onFileBeingSent(object? sender, EventArgs e)
        {
            _sendReceiveWindow.Text.Text = "Sent: 0%";
            _sendReceiveWindow.ShowDialog();
        }
    }
}