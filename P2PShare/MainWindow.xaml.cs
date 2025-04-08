using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using P2PShare.Utils;
using P2PShare.Libs;
using System.Security.Cryptography;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetworkInterface? _interface;
        private IPAddress? _localIP;
        private Task?[] _listening = new Task?[2];
        private Task?[] _monitorConnections = new Task?[2];
        private Task? _monitorInterface;
        private Task?[] _connecting = new Task?[2];
        private int _portListen;
        private int _portConnect;
        private Send_Receive? _sendReceiveWindow;
        private TcpClient?[] _clients = new TcpClient?[2];
        private CancellationTokenSource? _cancelConnecting;
        private CancellationTokenSource? _cancelMonitoring;
        private RSAParameters[] rsaParameters;

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
            _sendReceiveWindow?.Close();
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
                Elements.ShowDialog("Select an interface & enter a valid port number");

                return;
            }

            _cancelConnecting = new CancellationTokenSource();

            for (int i = 0; i < 2; i++)
            {
                _listening[i] = ListenerConnection.ListenLoop(_portListen + i, _interface, _cancelConnecting.Token);
            }

            Elements.Listening(_portListen, State, Cancel);
        }

        private async void OnConnected(object? sender, TcpClient client2)
        {
            IPAddress? ipRemote;
            int i = 0;
            bool check = false;

            do
            {
                if (_clients[i] is not null)
                {
                    i++;
                    
                    continue;
                }

                _clients[i] = client2;
                
                check = true;
            }
            while (i < _clients.Length && !check);

            if (_clients[i] is null)
            {
                return;
            }
            
            ipRemote = IPv4Handling.GetRemoteIPAddress(_clients[i]!);

            if (ipRemote is null)
            {
                _clients[i]!.Dispose();
                _clients[i] = null;

                return;
            }

            _monitorConnections[i] = GUIConnection.MonitorClientConnection(_clients[i]!, State, Interface, Cancel);

            check = true;
            
            foreach (TcpClient? client in _clients)
            {
                if (client is not null)
                {
                    continue;
                }

                check = false;
                
                break;
            }

            if (!check)
            {
                return;
            }

            Elements.Connected(State, Cancel, ipRemote);

            await receiveInvite();
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            Elements.Disconnected(State, Cancel, Interface);

            for (int i = 0; i < _clients.Length; i++)
            {
                _clients[i]?.Dispose();
                _clients[i] = null;
            }
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            IPAddress? remoteIP;

            if (_cancelConnecting is not null)
            {
                _cancelConnecting = Cancellation.Cancel(_cancelConnecting);
                _cancelConnecting = null;
            }

            if (_interface is null || _localIP is null || !IPAddress.TryParse(RemoteIP.Text.Trim(), out remoteIP))
            {
                Elements.ShowDialog("Select an interface & enter a valid IP address");

                return;
            }
            
            _portConnect = PortHandling.FindPort(_localIP);

            _cancelConnecting = new CancellationTokenSource();

            for (int i = 0; i < 2; i++)
            {
                _connecting[i] = ClientConnection.Connect(remoteIP, _interface, _portConnect + i, _cancelConnecting.Token);
            }

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

        private async void onInviteReceived(object? sender, string? invite)
        {
            if (!String.IsNullOrEmpty(invite))
            {
                bool accepted;
                Invite inviteWindow = new();
                FileInfo? file = null;

                inviteWindow.Text.Text = invite;
                inviteWindow.ShowDialog();
                accepted = inviteWindow.Accepted;

                if (_clients[0] is not null || _clients[0]!.Connected)
                {
                    bool? selected = null;
                    bool receive;
                    string? path = null;

                    if (accepted)
                    {
                        path = FileDialogs.SelectFolder(out selected);
                    }

                    if (selected is not null && path is not null && selected == true)
                    {
                        receive = true;

                        rsaParameters = AsymmetricCryptography.GenerateKeys();
                    }
                    else
                    {
                        receive = false;
                    }

                    await FileTransport.Reply(_clients[0]!, receive, rsaParameters[0]);

                    if (path is not null)
                    {
                        file = await FileTransport.ReceiveFile(_clients[0]!, FileTransport.GetFileLenghtFromInvite(invite), $"{path}\\{FileTransport.GetFileNameFromInvite(invite)}", rsaParameters[1]);
                    }
                }

                if (file is null)
                {
                    Elements.FileTransferEndDialog(false);
                }
                else 
                {
                    Elements.ShowDialog($"The file has been saved to:\n{file.FullName}");
                }

                await receiveInvite();
            }
        }

        private void onFileBeingReceived(object? sender, EventArgs e)
        {
            _sendReceiveWindow = new();
            _sendReceiveWindow.Text.Text = "Received: 0%";
            _sendReceiveWindow.Show();
        }

        private void onFilePartReceived(object? sender, int part)
        {
            if (_sendReceiveWindow is null)
            {
                return;
            }

            Elements.ChangeFileTransferState(_sendReceiveWindow, part, Models.Receive_Send.Receive);
        }

        private void onFilePartSent(object? sender, int part)
        {
            if (_sendReceiveWindow is null)
            {
                return;
            }

            Elements.ChangeFileTransferState(_sendReceiveWindow, part, Models.Receive_Send.Send);
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (_clients[0] is null || !_clients[0]!.Connected)
            {
                Elements.ShowDialog("You must be connected to share");
                return;
            }
            
            FileInfo fileInfo = new(File.Text.Trim());

            if (!fileInfo.Exists)
            {
                Elements.ShowDialog("Select a valid file");
                return;
            }

            Elements.FileTransferEndDialog(await FileTransport.SendFile(_clients!, fileInfo));

            await receiveInvite();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            File.Text = FileDialogs.SelectFile();
        }

        private void onTransferFailed(object? sender, EventArgs e)
        {
            _sendReceiveWindow?.Close();

            Elements.ShowDialog("The file transfer failed");
        }

        private void onFileBeingSent(object? sender, EventArgs e)
        {
            _sendReceiveWindow = new();
            _sendReceiveWindow.Text.Text = "Sent: 0%";
            _sendReceiveWindow.Show();
        }

        private async Task receiveInvite()
        {
            if (_clients[1] is null || !_clients[1]!.Connected)
            {
                return;
            }
            
            await FileTransport.ReceiveInvite(_clients[1]!);
        }
    }
}