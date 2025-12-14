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
        private Task? _receiveLoop;
        private CancellationTokenSource? _cancellationTokenSource;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void OnContacted(object? sender, IPAddress ip) => ShowMessageBox($"Contacting {ip}...", ButtonContent.Cancel);

        public MainWindow()
        {
            InitializeComponent();
            RefreshInterfaces();
            Interface.SelectedIndex = 0;

            ConnectionHandler.FilePartTransported += OnFilePartTransported;
            ConnectionTranscieverHandler.Contacted += OnContacted;
            
            _receiveLoop = ReceiveLoopAsync();
        }

        private void ToolBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshInterfaces();
        }

        private async void Interface_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            NetworkInterface? ni = GetSelectedInterface();
            IPAddress? ip = ni is not null ? InterfaceHandling.GetLocalIP(ni) : null;

            YourIP.Text = $"Your IP address:" + (ip is not null ? $" {ip}" : String.Empty);

            await RestartReceiveLoopAsync();
        }

        private async Task RestartReceiveLoopAsync()
        {
            await StopTransportAsync();
            _receiveLoop = ReceiveLoopAsync();
        }

        private async Task StopTransportAsync()
        {
            _connectionHandler?.Cancel();
            _cancellationTokenSource?.Cancel();
            if (_receiveLoop is not null) await _receiveLoop;
            _receiveLoop = null;
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

            content = $"{(e.SendReceive == SendReceive.Send ? "Sending" : "Receiving")}: {file.Key} ({e.CurrentFile}/{e.AmountOfFiles}) {e.Part}% of {file.Value}B";
            if (_messageBox is null) ShowMessageBox(content, ButtonContent.Cancel);
            else _messageBox?.ChangeContent(content);
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            FileInfo[] files;
            NetworkInterface? @interface = GetSelectedInterface();
            IPAddress? ipRemote, ipLocal = @interface is not null ? InterfaceHandling.GetLocalIP(@interface) : null;
            string fileText = File.Text.Trim(), messageBoxContent = String.Empty;
            bool? encryption = CheckBoxEncryption.IsChecked;

            await StopTransportAsync();

            try
            {
                if (encryption is null) throw new Exception("An error occurred with the encryption option.");
                if (ipLocal is null) throw new Exception("Select a valid interface!");
                if (!IPAddress.TryParse(RemoteIP.Text.Trim(), out ipRemote)) throw new Exception("Enter a valid IP address!");
                if (fileText == String.Empty) throw new Exception("Choose a file to send!");

                files = fileText.Split(ConnectionHandler.FileSeparator).Select(x => new FileInfo(x)).ToArray();

                _connectionHandler = new ConnectionTranscieverHandler();

                await ((ConnectionTranscieverHandler)_connectionHandler).SendAsync(ipRemote, ipLocal, files, (bool)encryption);

                messageBoxContent = "File(s) transmission succeeded.";
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                messageBoxContent = ex.Message;
            }

            _connectionHandler?.Dispose();

            _messageBox?.Close();
            ShowMessageBox(messageBoxContent, ButtonContent.OK);

            await RestartReceiveLoopAsync();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            string[]? paths = FileDialogs.SelectFiles();

            if (paths is null) return;

            string pathsString = String.Empty;

            for (int i = 0; i < paths.Length; i++)
            {
                pathsString += paths[i];

                if (i != paths.Length - 1) pathsString += ConnectionHandler.FileSeparator;
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

        private async Task ReceiveAsync(CancellationToken cancellationToken)
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
                throw new OperationCanceledException();
            }

            _connectionHandler = new ConnectionReceiverHandler(localIP);
            connectionReceiverHandler = (ConnectionReceiverHandler)_connectionHandler;

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

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
                throw new OperationCanceledException();
            }
            catch (Exception ex)
            {
                messageBoxContent = ex.Message;
            }

            _connectionHandler.Dispose();

            _messageBox?.Close();
            ShowMessageBox(messageBoxContent!, ButtonContent.OK);

            RefreshInterfaces();
        }

        private async Task ReceiveLoopAsync()
        {
            OperationCanceledException? ex = null;
            _cancellationTokenSource = new();

            do
            {
                try
                {
                    await ReceiveAsync(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException exc)
                {
                    ex = exc;
                }
            }
            while (ex is null);
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await RestartReceiveLoopAsync();
        }

        private void ShowMessageBox(string content, ButtonContent buttonContent)
        {
            _messageBox = new(content, buttonContent, this);
            _messageBox.CancelClicked += OnCancelClicked;
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