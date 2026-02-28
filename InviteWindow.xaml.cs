using System.Windows;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for InviteWindow.xaml
    /// </summary>
    public partial class InviteWindow : Window
    {
        public bool Accepted { get; private set; }

        public InviteWindow(string text, Window window)
        {
            InitializeComponent();
            Text.Text = text;
            if (window.Dispatcher.CheckAccess()) Owner = window;
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;

            Close();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;

            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
    }
}
