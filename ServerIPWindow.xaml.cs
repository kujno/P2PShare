using System.Net;
using System.Windows;

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

        private void TextBlockExit_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => Close();

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
