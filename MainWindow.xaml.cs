using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using P2PShare.Utils;
using P2PShare.Libs;
using P2PShare.Libs.Models;

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
        private Send_ReceiveWindow? _sendReceiveWindow;
        private TcpClient?[] _tcpClients;
        private Cancellation _cancelConnecting;
        private Cancellation _cancelMonitoring;
        private DecryptorAsymmetrical? _decryptor;
        private bool _inviteSent;
        private Task? _timeOut;
        private EncryptionEnum _encryption;

        public MainWindow()
        {
            InitializeComponent();
            Elements.RefreshInterfaces(Interface, null);
            Interface.SelectedIndex = 0;
            Elements.InitializeEncryptionComboBox(Encryption);
            Encryption.SelectedIndex = 0;

            TCPConnectionClient.Connected += OnConnected;
            TCPConnectionClient.Disconnected += OnDisconnected;
            InterfaceHandling.InterfaceDown += onInterfaceDown;
            FileTransport.InviteReceived += onInviteReceived;
            FileTransport.FilePartTransported += onFilePartTransported;
            FileTransport.FilesBeingTransported += onFilesBeingTransported;

            _listening = new Task?[2];
            _monitorConnections = new Task?[2];
            _connecting = new Task?[2];
            _tcpClients = new TcpClient?[2];
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
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Elements.RefreshInterfaces(Interface, _interface?.Name);
        }

        private void Interface_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _cancelMonitoring?.Cancel();
            _monitorInterface = null;

            if (Interface.SelectedItem is null)
            {
                Elements.ResetYourIp(YourIP);

                return;
            }

            _interface = Elements.GetSelectedInterface(Interface);

            if (_interface is null) return;

            _localIP = IPHandling.GetLocalIPv4(_interface);

            if (_localIP is null) return;

            YourIP.Text = $"Your IP address: {_localIP}";

            _cancelMonitoring?.NewTokenSource();

            if (_cancelMonitoring is null) return;

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
                _listening[i] = TCPConnectionListener.ListenLoop(_portListen + i, _interface, _cancelConnecting);
            }

            Elements.Listening(_portListen, State, Cancel);
        }

        private async void OnConnected(object? sender, TcpClient client2)
        {
            IPAddress? ipRemote;
            int i = 0;

            for (; i < _tcpClients.Length; i++)
            {
                if (_tcpClients[i] is not null) continue;

                _tcpClients[i] = client2;

                break;
            }

            if (_tcpClients[i] is null) return;

            ipRemote = IPHandling.GetRemoteIPAddress(_tcpClients[i]!);

            if (ipRemote is null)
            {
                _tcpClients[i]!.Dispose();
                _tcpClients[i] = null;

                return;
            }

            _monitorConnections[i] = GUIConnection.MonitorClientConnection(_tcpClients[i]!, State, Interface, Cancel);

            if (!TCPConnectionClient.AreClientsConnected(_tcpClients)) return;

            Elements.Connected(State, Cancel, Disconnect, ipRemote);

            await FileTransport.ReceiveInvite(_tcpClients);
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            Elements.Disconnected(State, Cancel, Disconnect, Interface, _interface?.Name);

            TCPConnectionClient.GetRidOfClients(_tcpClients);

            if (Interface.Items.Contains(_interface?.Name)) Interface.SelectedItem = _interface?.Name;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            IPAddress? remoteIP;

            if (TCPConnectionClient.AreClientsConnected(_tcpClients))
            {
                Elements.ShowDialog("You must first disconnect to connect to another device");

                return;
            }

            if (_cancelConnecting.TokenSource is not null) _cancelConnecting.Cancel();

            if (_interface is null || _localIP is null || !IPAddress.TryParse(RemoteIP.Text.Trim(), out remoteIP))
            {
                Elements.ShowDialog("Select an interface & enter a valid IP address");

                return;
            }
            
            _portConnect = PortHandling.FindPort(_localIP);

            _cancelConnecting!.NewTokenSource();

            _timeOut = _cancelConnecting.TimeOut();

            _connecting = TCPConnectionClient.ConnectAll(remoteIP, _interface, _portConnect, _cancelConnecting);

            Elements.Connecting(_portConnect, State, Cancel);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cancelConnecting.TokenSource is null) return;

            _cancelConnecting.Cancel();

            TCPConnectionClient.GetRidOfClients(_tcpClients);
        }

        private void onInterfaceDown(object? sender, EventArgs e)
        {
            Elements.RefreshInterfaces(Interface, _interface?.Name);

            _interface = null;
        }

        private async void onInviteReceived(object? sender, string? invite)
        {
            if (!String.IsNullOrEmpty(invite))
            {
                bool accepted;
                InviteWindow inviteWindow;
                FileInfo[]? fileInfos = null;
                EncryptionEnum encryption;
                string[] filesAndEncryption = invite.Split(FileTransport.EncryptionSymbol);
                string[] files = filesAndEncryption[0].Split(FileTransport.FileSeparator);

                Enum.TryParse<EncryptionEnum>(filesAndEncryption[1], out encryption);

                invite = String.Empty;
                foreach (string file in files)
                {
                    invite += file + "\n";
                }
                inviteWindow = new(invite + "Accept?");
                inviteWindow.ShowDialog();
                accepted = inviteWindow.Accepted;

                try
                {
                    if (_tcpClients[0] is not null || _tcpClients[0]!.Connected)
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

                            if (encryption == EncryptionEnum.Enabled)
                            {
                                _decryptor = new();
                            }
                        }
                        else
                        {
                            receive = false;
                        }

                        await FileTransport.Reply(_tcpClients[0]!, receive);

                        if (path is not null)
                        {
                            string[] fileNames = FileTransport.GetNamesFromFiles(files);
                            string[] paths = new string[files.Length];

                            if (encryption == EncryptionEnum.Enabled)
                            {
                                await FileTransport.SendRSAPublicKey(_tcpClients[0]!.GetStream(), _decryptor!.PublicKey);
                            }

                            for (int i = 0; i < paths.Length; i++)
                            {
                                paths[i] = $"{path}\\{fileNames[i]}";
                            }

                            fileInfos = await FileTransport.ReceiveFile(_tcpClients[0]!, paths, FileTransport.GetLenghtsFromFiles(files), _decryptor, encryption);
                        }
                    }
                }
                catch
                {
                    fileInfos = null;
                }

                if (fileInfos is null) Elements.FileTransferEndDialog(false, _sendReceiveWindow);
                else 
                {
                    string message;

                    switch (fileInfos.Length)
                    {
                        case 1:
                            message = $"The file has been saved as:\n{fileInfos[0].FullName}";

                            break;

                        default:
                            message = $"The files have been saved to:\n{fileInfos[0].DirectoryName}";

                            break;
                    }

                    Elements.ShowDialog(message);
                }
            }

            await FileTransport.ReceiveInvite(_tcpClients);
        }

        private void onFilePartTransported(object? sender, int part)
        {
            _sendReceiveWindow?.ChangeText(part);
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            string fileText = File.Text.Trim();

            if (_inviteSent)
            {
                Elements.ShowDialog("You cannot send multiple sharing invites at once");
                
                return;
            }

            if (fileText.Equals(String.Empty))
            {
                Elements.ShowDialog("Choose a file to send");

                return;
            }
            
            if (_tcpClients[0] is null || !_tcpClients[0]!.Connected)
            {
                Elements.ShowDialog("You must be connected to share");
                return;
            }

            string[] paths = fileText.Split(FileTransport.FileSeparator);
            FileInfo[] fileInfos = new FileInfo[paths.Length];
            for (int i = 0; i < fileInfos.Length; i++)
            {
                fileInfos[i] = new FileInfo(paths[i]);
            }

            if (!fileInfos.All(fileInfo => fileInfo.Exists))
            {
                Elements.ShowDialog("Select a valid file(s)");
                return;
            }

            _inviteSent = true;

            Elements.FileTransferEndDialog(await FileTransport.SendFile(_tcpClients!, fileInfos, _encryption), _sendReceiveWindow);

            _inviteSent = false;
            
            await FileTransport.ReceiveInvite(_tcpClients);
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            string[]? paths = FileDialogs.SelectFiles();
            string pathsString = "";
            
            for (int i = 0; i < paths?.Length; i++)
            {
                pathsString += paths[i];
                
                if (paths.Length > 1 && i != paths.Length - 1) pathsString += FileTransport.FileSeparator;
            }
            
            File.Text = pathsString;
        }

        private void onTransferFailed(object? sender, EventArgs e)
        {
            _sendReceiveWindow?.Close();

            Elements.ShowDialog("The file transfer failed");
        }

        private void onFilesBeingTransported(object? sender, FilesBeingTransportedEventArgs filesBeingTransportedEventArgs)
        {
            _sendReceiveWindow = new(filesBeingTransportedEventArgs.ReceiveSend, filesBeingTransportedEventArgs.FileInfos);
            _sendReceiveWindow.Show();
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            TCPConnectionClient.GetRidOfClients(_tcpClients);
        }

        private void Encryption_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Enum.TryParse<EncryptionEnum>(Encryption.SelectedItem.ToString(), out _encryption);
        }
    }
}