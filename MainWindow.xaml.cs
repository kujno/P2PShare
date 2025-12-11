using P2PShare.Libs;
using P2PShare.Libs.Models;
using P2PShare.Models;
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
        private ConnectionHandler? _connectionHandler;
        private CustomMessageBox? _messageBox;
        private Dictionary<string, long>? _files;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        public MainWindow()
        {
            InitializeComponent();
            RefreshInterfaces();
            Interface.SelectedIndex = 0;
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

            if (ni is not null) ip = InterfaceHandling.GetLocalIP(ni);

            // assing the local IP to the connection object

            YourIP.Text = $"Your IP address:" + (ip is not null ? $" {ip}" : String.Empty);
        }

        private void OnFilePartTransported(object? sender, FilePartTransportedEventArgs e)
        {
            KeyValuePair<string, long> file;
            string content;

            if (e.CurrentFile == e.AmountOfFiles && e.Part == 100)
            {
                _messageBox?.Close();
                return;
            }

            file = _files!.ElementAt(e.CurrentFile - 1);

            content = $"{file.Key} ({e.CurrentFile}/{e.AmountOfFiles}) {e.Part}% of {file.Value}B";
            if (_messageBox is null) ShowMessageBox(content, ButtonContent.Cancel);
            else _messageBox?.ChangeContent(content);
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
            string[]? paths = Dialog.SelectFiles();
            string pathsString = "";

            for (int i = 0; i < paths?.Length; i++)
            {
                pathsString += paths[i];

                if (paths.Length > 1 && i != paths.Length - 1) pathsString += FileTransport.FileSeparator;
            }

            File.Text = pathsString;
        }

        private void RefreshInterfaces()
        {
            string? selectedNI = Interface.SelectedItem?.ToString() ?? null;

            Interface.Items.Clear();
            foreach (NetworkInterface ni in InterfaceHandling.GetUpInterfaces()) Interface.Items.Add(ni.Name);

            if (Interface.Items.Contains(selectedNI)) Interface.SelectedItem = selectedNI;
            else Interface.SelectedIndex = 0;
        }

        private NetworkInterface? GetSelectedInterface()
        {
            foreach (NetworkInterface @interface in InterfaceHandling.GetUpInterfaces())
            {
                if (@interface.Name == Interface.SelectedItem.ToString()) return @interface;
            }

            return null;
        }

        private async Task ReceiveAsync() // do not call recursively. Use a loop instead.
        {
            NetworkInterface? @interface = GetSelectedInterface();
            IPAddress? localIP = @interface is not null ? InterfaceHandling.GetLocalIP(@interface) : null; // maybe refresh UI element if local IP changed
            InviteWindow inviteWindow;
            ConnectionReceiverHandler connectionReceiverHandler;
            string invite = String.Empty;
            string? dictionary, messageBoxContent = null;
            string[] savedFiles;

            if (localIP is null)
            {
                ShowMessageBox("Select a valid interface.", ButtonContent.OK);
                return;
            }

            _connectionHandler = new ConnectionReceiverHandler(localIP);
            connectionReceiverHandler = (ConnectionReceiverHandler)_connectionHandler;
            connectionReceiverHandler.FilePartTransported += OnFilePartTransported;
            try
            {
                _files = await connectionReceiverHandler.ReceiveInviteAsync();

                foreach (var file in _files) invite += $"{file.Key} - {file.Value}B\n";
                inviteWindow = new(invite + "Accept?", this);
                inviteWindow.ShowDialog();

                if (!inviteWindow.Accepted)
                {
                    await connectionReceiverHandler.DenyFilesAsync();
                    return;
                }

                dictionary = FileDialogs.SelectFolder();

                if (dictionary is null) messageBoxContent = "Receiving files was cancelled.";
                else
                {
                    savedFiles = await connectionReceiverHandler.AcceptFilesAsync(dictionary);

                    messageBoxContent = $"Files saved to {dictionary} as:";
                    foreach (string file in savedFiles) messageBoxContent += $"\n{file}";
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                messageBoxContent = ex.Message;
            }
            finally
            {
                _connectionHandler.Dispose();
            }

            ShowMessageBox(messageBoxContent!, ButtonContent.OK);
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            _connectionHandler?.Cancel();
        }

        private void ShowMessageBox(string content, ButtonContent buttonContent)
        {
            _messageBox = new(content, buttonContent, this);
            try
            {
                _messageBox.ShowDialog();
            }
            catch
            {
            }
            finally
            {
                _messageBox = null;
            }
        }
    }
}