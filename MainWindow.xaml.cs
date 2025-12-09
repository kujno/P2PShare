using P2PShare.Libs;
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
        private CustomMessageBox? _customMessageBox;
        private ConnectionHandler? _connectionHandler;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        public MainWindow()
        {
            IPAddress? localIP;
            NetworkInterface? @interface;
            
            InitializeComponent();
            RefreshInterfaces();
            Interface.SelectedIndex = 0;

            @interface = GetSelectedInterface();
            localIP = @interface is not null ? IPHandling.GetLocalIPv4(@interface) : null;
            if (localIP is not null) _connectionHandler = new ConnectionReceiverHandler(localIP);

            if (_connectionHandler is not null) ((ConnectionReceiverHandler)_connectionHandler)?.ReceiveInviteAsync(); // get this done
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