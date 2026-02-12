using P2PShare.Connection;
using P2PShare.Libs;
using P2PShare.Libs.Models;
using P2PShare.Models;
using P2PShare.Utils;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CustomMessageBox? _messageBox;
        private Dictionary<string, long>? _files;
        private Task? _receiveLoop;
        private CancellationTokenSource? _cancellationTokenSource;
        private NetworkInterface? _interface;
        private int _lastPercentage = -1;

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshInterfaces();
        private void OnContacted(object? sender, IPAddress ip) => NewMessageBox($"Contacting {ip}...", ButtonContent.Cancel, false);

        public MainWindow()
        {
            InitializeComponent();
            RefreshInterfaces();
            Interface.SelectedIndex = 0;

            ConnectionHandler.FilePartTransported += OnFilePartTransported;
            ConnectionTranscieverHandler.Contacted += OnContacted;

            if (_receiveLoop is null) _receiveLoop = ReceiveLoopAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Dispose();

            Close();
        }

        private async void OnWindowClosed(object? sender, bool cancelled)
        {
            _messageBox = null;

            if (cancelled)
            {
                _cancellationTokenSource?.Cancel();
                await _receiveLoop!;
                _receiveLoop = ReceiveLoopAsync();
            }
        }

        private void ToolBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Interface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NetworkInterface? ni = GetSelectedInterface();

            if (ni == _interface || ni is null) return;

            _interface = ni;

            IPAddress? ip = ni is not null ? InterfaceHandling.GetLocalIP(ni) : null;

            YourIP.Text = $"Your IP address:" + (ip is not null ? $" {ip}" : String.Empty);

            _cancellationTokenSource?.Cancel();
        }

        private void OnFilePartTransported(object? sender, FilePartTransportedEventArgs e)
        {
            KeyValuePair<string, long> file;

            if (e.CurrentFile == e.AmountOfFiles && e.Part == 100)
            {
                _messageBox?.Close();
                _messageBox = null;
                return;
            }

            file = _files!.ElementAt(e.CurrentFile - 1);

            if (_messageBox is null) NewMessageBox(e, file, false);
            else
            {
                if (_lastPercentage != e.Part) _messageBox.ChangeContent(e, file);
            }

            _lastPercentage = e.Part;
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            FileInfo[] files;
            IPAddress? ipRemote, ipLocal = _interface is not null ? InterfaceHandling.GetLocalIP(_interface) : null;
            string fileText = File.Text.Trim(), messageBoxContent = String.Empty;
            var encryption = CheckBoxEncryption.IsChecked;

            _cancellationTokenSource?.Cancel();
            await _receiveLoop!;

            try
            {
                if (encryption is null) throw new Exception("An error occurred with the encryption option.");
                if (ipLocal is null) throw new Exception("Select a valid interface!");
                if (!IPAddress.TryParse(RemoteIP.Text.Trim(), out ipRemote)) throw new Exception("Enter a valid IP address!");
                if (fileText == String.Empty) throw new Exception("Choose a file to send!");

                files = fileText
                    .Split(ConnectionHandler.FileSeparator)
                    .Select(x => new FileInfo(x))
                    .ToArray();

                _files = files
                    .Select(x => new KeyValuePair<string, long>(x.Name, x.Length))
                    .ToDictionary();

                using (_cancellationTokenSource = new())
                {
                    await new ConnectionTranscieverHandler()
                    {
                        IPLocal = ipLocal,
                        IPRemote = ipRemote,
                        CancellationToken = _cancellationTokenSource.Token
                    }.SendFilesAsync(files, (bool)encryption);
                }

                messageBoxContent = "File(s) transmission succeeded.";
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                messageBoxContent = ex.Message;
            }
            finally
            {
                _messageBox?.Close();
                _messageBox = null;

                _lastPercentage = -1;

                _receiveLoop = ReceiveLoopAsync();
            }

            NewMessageBox(messageBoxContent, ButtonContent.OK, true);
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
            var interfaces = InterfaceHandling.GetUpInterfaces().Select(x => x.Name);

            if (interfaces.All(Interface.Items.Contains) && interfaces.Count() == Interface.Items.Count) return;

            Interface.Items.Clear();

            foreach (var @interface in interfaces) Interface.Items.Add(@interface);

            if (Interface.Items.Contains(selectedNI)) Interface.SelectedItem = selectedNI;
            else Interface.SelectedIndex = 0;
        }

        private NetworkInterface? GetSelectedInterface()
        {
            NetworkInterface? @interface = null;

            try
            {
                @interface = InterfaceHandling.GetUpInterfaces().FirstOrDefault(x => x.Name == Interface.SelectedItem?.ToString()?.Trim());
            }
            catch (Exception ex)
            {
                NewMessageBox(ex.Message, ButtonContent.OK, true);
            }

            return @interface;
        }

        private async Task ReceiveAsync()
        {
            IPAddress? localIP = _interface is not null ? InterfaceHandling.GetLocalIP(_interface) : null; // maybe refresh UI element if local IP changed
            InviteWindow inviteWindow;
            ConnectionReceiverHandler connectionHandler;
            string invite = String.Empty;
            string? dictionary, messageBoxContent = null;
            string[] savedFiles;

            if (localIP is null)
            {
                NewMessageBox("Select a valid interface.", ButtonContent.OK, true);
                throw new OperationCanceledException();
            }

            if (_cancellationTokenSource!.Token.IsCancellationRequested) throw new OperationCanceledException();

            connectionHandler = new() 
            {
                IPLocal = localIP,
                CancellationToken = _cancellationTokenSource.Token
            };
            try
            {
                _files = await connectionHandler.ReceiveInviteAsync();

                foreach (var file in _files) invite += $"{file.Key} - {file.Value}B\n";
                inviteWindow = new($"{invite}({(connectionHandler.Encrypted ? "Encrypted" : "Unencrypted")})\nAccept?", this);
                inviteWindow.ShowDialog();

                if (!inviteWindow.Accepted)
                {
                    await connectionHandler.RejectFilesAsync();
                    return;
                }

                dictionary = FileDialogs.SelectFolder();

                if (dictionary is null) messageBoxContent = "Receiving files was cancelled.";
                else
                {
                    savedFiles = await connectionHandler.ReceiveFilesAsync(dictionary);

                    messageBoxContent = $"Files saved to {dictionary} as:";
                    foreach (string file in savedFiles) messageBoxContent += $"\n{file}";
                }

                try
                {
                    connectionHandler.Dispose();
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                if (_messageBox is null || ex.Message == ConnectionReceiverHandler.InviteErrorMessage || ex is OperationCanceledException) return;
                else messageBoxContent = ex.Message;
            }
            finally
            {
                _messageBox?.Close();
                _messageBox = null;

                RefreshInterfaces();
            }

            NewMessageBox(messageBoxContent!, ButtonContent.OK, true);
        }

        private async Task ReceiveLoopAsync()
        {
            NetworkInterface? @interface;

            do
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new();
                @interface = _interface;

                try
                {
                    await ReceiveAsync();
                }
                catch
                {
                }
            }
            while (!_cancellationTokenSource.IsCancellationRequested || @interface != _interface);
        }

        private void NewMessageBox(string content, ButtonContent buttonContent, bool modal)
        {
            _messageBox = new(content, buttonContent, this);
            _messageBox.WindowClosed += OnWindowClosed;
            ShowMessageBox(modal);
        }

        private void NewMessageBox(FilePartTransportedEventArgs transportInfo, KeyValuePair<string, long> file, bool modal)
        {
            _messageBox = new(transportInfo, file, this);
            _messageBox.WindowClosed += OnWindowClosed;
            ShowMessageBox(modal);
        }

        private void ShowMessageBox(bool modal)
        {
            try
            {
                if (modal) _messageBox?.ShowDialog();
                else _messageBox?.Show();
            }
            catch
            {
            }

            if (modal) _messageBox = null;
        }
    }
}