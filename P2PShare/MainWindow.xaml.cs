using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using P2PShare.Utils;
using P2PShare.Libs;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetworkInterface? _interface;
        private IPAddress? _localIP;
        private Task?[] _listening;
        private Task?[] _monitorConnections;
        private Task? _monitorInterface;
        private Task?[] _connecting;
        private int _portListen;
        private int _portConnect;
        private Send_Receive? _sendReceiveWindow;
        private TcpClient?[] _clients;
        private Cancellation _cancelConnecting;
        private Cancellation _cancelMonitoring;
        private DecryptorAsymmetrical? _decryptographer;
        private bool _inviteSent;
        private Task? _timeOut;

        public MainWindow()
        {
            InitializeComponent();
            Elements.RefreshInterfaces(Interface);
            Interface.SelectedIndex = 0;

            ConnectionClient.Connected += OnConnected;
            ConnectionClient.Disconnected += OnDisconnected;
            InterfaceHandling.InterfaceDown += onInterfaceDown;
            FileTransport.InviteReceived += onInviteReceived;
            FileTransport.FileBeingReceived += onFileBeingReceived;
            FileTransport.FilePartReceived += onFilePartReceived;
            FileTransport.FilePartSent += onFilePartSent;
            FileTransport.FileBeingSent += onFileBeingSent;

            _listening = new Task?[2];
            _monitorConnections = new Task?[2];
            _connecting = new Task?[2];
            _clients = new TcpClient?[2];
            _inviteSent = false;
            _cancelConnecting = new();
            _cancelMonitoring = new();
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

            _localIP = IPHandling.GetLocalIPv4(_interface);

            if (_localIP is null)
            {
                return;
            }

            YourIP.Text = $"Your IP address: {_localIP}";

            _cancelMonitoring?.NewTokenSource();

            if (_cancelMonitoring is null)
            {
                return;
            }
            
            _monitorInterface = InterfaceHandling.MonitorInterface(_interface, _cancelMonitoring);
        }

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            _cancelConnecting.NewTokenSource();

            if (_localIP is null || !int.TryParse(Port.Text.Trim(), out _portListen) || _interface is null || !PortHandling.IsPortAvailable(_localIP, _portListen))
            {
                Elements.ShowDialog("Select an interface & enter a valid port number");

                return;
            }

            _timeOut = _cancelConnecting.TimeOut();
            
            for (int i = 0; i < 2; i++)
            {
                _listening[i] = ConnectionListener.ListenLoop(_portListen + i, _interface, _cancelConnecting);
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
            
            ipRemote = IPHandling.GetRemoteIPAddress(_clients[i]!);

            if (ipRemote is null)
            {
                _clients[i]!.Dispose();
                _clients[i] = null;

                return;
            }

            _monitorConnections[i] = GUIConnection.MonitorClientConnection(_clients[i]!, State, Interface, Cancel);

            if (!ConnectionClient.AreClientsConnected(_clients))
            {
                return;
            }

            Elements.Connected(State, Cancel, Disconnect, ipRemote);

            await FileTransport.ReceiveInvite(_clients);
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            Elements.Disconnected(State, Cancel, Disconnect, Interface);

            ConnectionClient.GetRidOfClients(_clients);

            if (Interface.Items.Contains(_interface?.Name))
            {
                Interface.SelectedItem = _interface?.Name;
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            IPAddress? remoteIP;

            if (ConnectionClient.AreClientsConnected(_clients))
            {
                Elements.ShowDialog("You must first disconnect to connect to another device");

                return;
            }
            
            if (Interface.SelectedItem?.ToString() != _interface?.Name)
            {
                Interface.SelectedItem = _interface?.Name;
            }

            if (_cancelConnecting.TokenSource is not null)
            {
                _cancelConnecting.Cancel();
            }

            if (_interface is null || _localIP is null || !IPAddress.TryParse(RemoteIP.Text.Trim(), out remoteIP))
            {
                Elements.ShowDialog("Select an interface & enter a valid IP address");

                return;
            }
            
            _portConnect = PortHandling.FindPort(_localIP);

            _cancelConnecting!.NewTokenSource();

            _connecting = ConnectionClient.ConnectAll(remoteIP, _interface, _portConnect, _cancelConnecting);

            Elements.Connecting(_portConnect, State, Cancel);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cancelConnecting.TokenSource is null)
            {
                return;
            }
            
            _cancelConnecting.Cancel();

            ConnectionClient.GetRidOfClients(_clients);
        }

        private void onInterfaceDown(object? sender, EventArgs e)
        {
            Elements.RefreshInterfaces(Interface);

            _interface = null;
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

                        _decryptographer = new();
                    }
                    else
                    {
                        receive = false;
                    }

                    await FileTransport.Reply(_clients[0]!, receive);

                    if (path is not null)
                    {
                        await FileTransport.SendRSAPublicKey(_clients[0]!.GetStream(), _decryptographer!.PublicKey);
                        
                        file = await FileTransport.ReceiveFile(_clients[0]!, FileTransport.GetFileLenghtFromInvite(invite), $"{path}\\{FileTransport.GetFileNameFromInvite(invite)}", _decryptographer);
                    }
                }

                if (file is null)
                {
                    Elements.FileTransferEndDialog(false, _sendReceiveWindow);
                }
                else 
                {
                    Elements.ShowDialog($"The file has been saved to:\n{file.FullName}");
                }
            }

            await FileTransport.ReceiveInvite(_clients);
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
            if (_inviteSent)
            {
                Elements.ShowDialog("You cannot send multiple files at once");
                
                return;
            }
            
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

            _inviteSent = true;

            Elements.FileTransferEndDialog(await FileTransport.SendFile(_clients!, fileInfo), _sendReceiveWindow);

            _inviteSent = false;
            
            await FileTransport.ReceiveInvite(_clients);
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

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            ConnectionClient.GetRidOfClients(_clients);
        }
    }
}