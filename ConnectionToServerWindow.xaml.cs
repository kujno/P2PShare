using P2PShare.Connection;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for ConnectionToServerWindow.xaml
    /// </summary>
    public partial class ConnectionToServerWindow : Window
    {
        public ConnectionToServerHandler? ConnectionHandler { get; private set; } = null;

        private Task? _connecting;
        private CancellationTokenSource? _cts;
        private IPAddress _serverIP;

        public ConnectionToServerWindow(IPAddress serverIP)
        {
            InitializeComponent();
            _serverIP = serverIP;
            _connecting = ConnectAsync();
        }

        private void TryAgain_Click(object sender, RoutedEventArgs e) => _connecting = ConnectAsync();

        private async void ChangeIP_Click(object sender, RoutedEventArgs e)
        {
            IPAddress? newIP;

            ServerIPWindow ipWindow = new("Cancel");
            ipWindow.ShowDialog();
            newIP = await ServerIP.GetAsync();

            if (newIP is not null && _serverIP != newIP)
            {
                _serverIP = newIP;
                _connecting = ConnectAsync();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private async Task ConnectAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            ConnectionHandler?.Dispose();
            ButtonChangeIP.Visibility = Visibility.Hidden;
            ButtonTryAgain.Visibility = Visibility.Hidden;

            _cts = new();
            ConnectionHandler = new()
            {
                CancellationToken = _cts.Token,
                IPLocal = IPAddress.Any,
                IPServer = _serverIP
            };

            Text.Text = $"Connecting to {ConnectionHandler.IPServer}...";

            try
            {
                await ConnectionHandler.ConnectAsync();

                Close();
            }
            catch
            {
                Text.Text = $"Failed to connect.";
                ButtonChangeIP.Visibility = Visibility.Visible;
                ButtonTryAgain.Visibility = Visibility.Visible;
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            ConnectionHandler?.Dispose();

            Close();
        }
    }
}
