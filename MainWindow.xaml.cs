using P2PShare.Libs;
using P2PShare.Libs.Models;
using P2PShare.Utils;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Send_ReceiveWindow? _sendReceiveWindow;
        private ConnectionHandler _tcpConnection;
        private EncryptionEnum _encryption;

        public MainWindow()
        {
            InitializeComponent();
            RefreshInterfaces();
            Interface.SelectedIndex = 0;

            FileTransport.InviteReceived += onInviteReceived;
            FileTransport.FilePartTransported += onFilePartTransported;
            FileTransport.FilesBeingTransported += onFilesBeingTransported;
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
            RefreshInterfaces();
        }

        private void Interface_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            NetworkInterface? ni = GetSelectedInterface();
            IPAddress? ip = null;

            if (ni is not null) ip = IPHandling.GetLocalIPv4(ni);

            // assing the local IP to the connection object

            YourIP.Text = $"Your IP address:" + (ip is not null ? $" {ip}" : String.Empty);
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

                if (fileInfos is null) FileTransferEndDialog(false);
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

                    ShowDialog(message);
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

            if (fileText.Equals(String.Empty))
            {
                ShowDialog("Choose a file to send");

                return;
            }

            FileInfo[] fileInfos = fileText.Split(FileTransport.FileSeparator).Select(x => new FileInfo(x)).ToArray();

            if (!fileInfos.All(fileInfo => fileInfo.Exists))
            {
                ShowDialog("Select a valid file(s)");
                return;
            }

            FileTransferEndDialog(await FileTransport.SendFile(_tcpClients!, fileInfos, _encryption), _sendReceiveWindow);
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

            ShowDialog("The file transfer failed");
        }

        private void onFilesBeingTransported(object? sender, FilesBeingTransportedEventArgs filesBeingTransportedEventArgs)
        {
            _sendReceiveWindow = new(filesBeingTransportedEventArgs.ReceiveSend, filesBeingTransportedEventArgs.FileInfos);
            _sendReceiveWindow.Show();
        }

        private void RefreshInterfaces()
        {
            string? selectedNI = Interface.SelectedItem?.ToString() ?? null;

            Interface.Items.Clear();
            foreach (NetworkInterface ni in InterfaceHandling.GetUpInterfaces()) Interface.Items.Add(ni.Name);

            if (Interface.Items.Contains(selectedNI)) Interface.SelectedItem = selectedNI;
            else Interface.SelectedIndex = 0;
        }

        private void ShowDialog(string message)
        {
            CustomMessageBox messageBox = new CustomMessageBox();

            messageBox.Text.Text = message;

            messageBox.ShowDialog();
        }

        private NetworkInterface? GetSelectedInterface()
        {
            foreach (NetworkInterface @interface in InterfaceHandling.GetUpInterfaces())
            {
                if (@interface.Name == Interface.SelectedItem.ToString()) return @interface;
            }

            return null;
        }
    }
}