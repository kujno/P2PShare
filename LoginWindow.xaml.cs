using P2PShare.Connection;
using P2PShare.Utils;
using System.Windows;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public bool IsLoggedIn { get; private set; } = false;

        private ConnectionToServerHandler _connectionHandler;

        private LoginWindow(ConnectionToServerHandler connectionHandler)
        {
            InitializeComponent();

            _connectionHandler = connectionHandler;
        }

        private void OnDisconnected(object? sender, EventArgs e) => AppHelper.CloseAppForServer(this);

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void ChangeIP_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
