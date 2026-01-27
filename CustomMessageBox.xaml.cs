using P2PShare.Models;
using System.Windows;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        private readonly ButtonContent _buttonContent;

        public event EventHandler<bool>? WindowClosed;
        
        public void ChangeContent(string content) => Text.Text = content;

        public CustomMessageBox(string content, ButtonContent buttonContent, Window window)
        {
            InitializeComponent();

            Text.Text = content;
            _buttonContent = buttonContent;
            Btn.Content = buttonContent;
            if (window.Dispatcher.CheckAccess())
            {
                Owner = window;
                Owner.Visibility = Visibility.Hidden;
            }
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            OnWindowClosed();
            Owner.Visibility = Visibility.Visible;
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void OnWindowClosed()
        {
            WindowClosed?.Invoke(this, _buttonContent == ButtonContent.Cancel ? true : false);
        }
    }
}
