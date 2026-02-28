using System.IO;
using System.Windows;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for ServerIPWindow.xaml
    /// </summary>
    public partial class NewFolderWindow : Window
    {
        public string? FolderName { get; private set; } = null;
        public string BadNameMessage { get; init; } = "Folder name contains invalid character(s).";

        public NewFolderWindow(string? header = null)
        {
            InitializeComponent();

            if (header is not null)
                GroupBoxHeader.Header = header;
        }

        private void TextBlockExit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Close();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private async void OK_Click(object sender, RoutedEventArgs e)
        {
            string name = TextBoxFolderName.Text.Trim();
            var invalidChars = Path.GetInvalidFileNameChars();

            if (name.All(x => invalidChars.All(y => y != x)))
            {
                FolderName = name;

                Close();
            }
            else
            {
                TextBlockValid.Text = $"{BadNameMessage}\nCan't contain: {string.Join(", ", invalidChars)}.";
                TextBlockValid.Visibility = Visibility.Visible;
            }
        }
    }
}
