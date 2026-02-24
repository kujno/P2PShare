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

        public NewFolderWindow()
        {
            InitializeComponent();
        }

        private void TextBlockExit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Close();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string name = TextBoxFolderName.Text.Trim();

            if (name.All(x => Path.GetInvalidFileNameChars().All(y => y != x)))
            {
                FolderName = name;

                Close();
            }
            else
            {
                TextBlockValid.Text = "Folder name contains invalid character(s).";
                TextBlockValid.Visibility = Visibility.Visible;
            }
        }
    }
}
