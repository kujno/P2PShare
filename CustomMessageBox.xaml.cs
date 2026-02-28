using P2PShare.Libs.Models;
using P2PShare.Models;
using P2PShare.Utils;
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

        private int _part;
        private Window? _owner;
        public bool ClosedOnPurpose { get; set; } = false;

        public event EventHandler<bool>? WindowClosed;

        public CustomMessageBox(FilePartTransportedEventArgs transportInfo, KeyValuePair<string, long> file, ButtonContent buttonContent, Window window) : this(String.Empty, buttonContent, window) => ChangeContent(transportInfo, file);

        public CustomMessageBox(string content, ButtonContent buttonContent, Window? window = null)
        {
            InitializeComponent();

            TextBlock_File.Text = content;
            _buttonContent = buttonContent;
            _owner = window;
            if (_buttonContent is ButtonContent.None)
                Btn.Visibility = Visibility.Hidden;
            else
                Btn.Content = _buttonContent;

            _owner?.Visibility = Visibility.Hidden;

            Closed += OnClosed;
        }

        private void OnWindowClosed() => WindowClosed?.Invoke(this, _buttonContent is ButtonContent.Cancel ? true : false);

        private void OnClosed(object? sender, EventArgs e)
        {
            _owner?.Visibility = Visibility.Visible;

            if (!ClosedOnPurpose)
                AppHelper.CloseAppForServer();
        }

        public void ChangeContent(FilePartTransportedEventArgs transportInfo, KeyValuePair<string, long> file)
        {
            _part = transportInfo.Part;
            TextBlock_File.Text = $"{(transportInfo.SendReceive is SendReceive.Send ? "Sending" : "Receiving")}: {file.Key} {file.Value}B ({transportInfo.CurrentFile}/{transportInfo.AmountOfFiles})";
            if (Grid_Progress.Visibility is not Visibility.Visible) Grid_Progress.Visibility = Visibility.Visible;
            ProgressBar.Value = _part;
            TextBlock_Percentage.Text = $"{_part}%";
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            ClosedOnPurpose = true;
            OnWindowClosed();
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
