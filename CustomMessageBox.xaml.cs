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
        private bool _btnPressed = false;

        public bool BtnPressed
        {
            get
            {
                return _btnPressed;
            }
        }

        public void ChangeContent(string content) => Text.Text = content;

        public CustomMessageBox(string content, ButtonContent buttonContent, Window window)
        {
            InitializeComponent();

            Text.Text = content;
            Btn.Content = buttonContent;
            if (window.Dispatcher.CheckAccess()) Owner = window;
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            _btnPressed = true;

            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
    }
}
