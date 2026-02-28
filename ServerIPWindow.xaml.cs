using System.Net;
using System.Windows;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for ServerIPWindow.xaml
    /// </summary>
    public partial class ServerIPWindow : Window
    {
        public ServerIPWindow(string exitText)
        {
            InitializeComponent();

            TextBlockExit.Text = exitText;
        }

        private void TextBlockExit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Close();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            IPAddress? ip;

            if (IPAddress.TryParse(TextBoxServerIP.Text, out ip))
            {
                await ServerIP.SetAsync(ip);
                Close();
            }
            else
            {
                TextBlockValid.Text = "Enter a valid IP address.";
                TextBlockValid.Visibility = Visibility.Visible;
            }
        }
    }
}
