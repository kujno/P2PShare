using P2PShare.Libs.Models;
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

        public CustomMessageBox(FilePartTransportedEventArgs transportInfo, KeyValuePair<string, long> file, Window window) : this(String.Empty, ButtonContent.Cancel, window, true) => ChangeContent(transportInfo, file);

        public CustomMessageBox(string content, ButtonContent buttonContent, Window window, bool modal)
        {
            InitializeComponent();

            TextBlock_File.Text = content;
            _buttonContent = buttonContent;
            Btn.Content = buttonContent;
            if (window.Dispatcher.CheckAccess())
            {
                Owner = window;
                if (modal)
                {
                    Owner.Visibility = Visibility.Hidden;
                }
            }
        }

        private void OnWindowClosed() => WindowClosed?.Invoke(this, _buttonContent is ButtonContent.Cancel ? true : false);

        public void ChangeContent(FilePartTransportedEventArgs transportInfo, KeyValuePair<string, long> file)
        {
            TextBlock_File.Text = $"{(transportInfo.SendReceive is SendReceive.Send ? "Sending" : "Receiving")}: {file.Key} {file.Value}B ({transportInfo.CurrentFile}/{transportInfo.AmountOfFiles})";
            if (Grid_Progress.Visibility is not Visibility.Visible) Grid_Progress.Visibility = Visibility.Visible;
            ProgressBar.Value = transportInfo.Part;
            TextBlock_Percentage.Text = $"{transportInfo.Part}%";
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
    }
}
