using P2PShare.Connection;
using P2PShare.Libs.Models.Requests;
using P2PShare.Models;
using P2PShare.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public bool IsLoggedIn { get; private set; } = false;

        public required ConnectionToServerHandler ConnectionHandler { get; init; }

        public LoginWindow()
        {
            InitializeComponent();

            ConnectionToServerHandler.Disconnected += OnDisconnected;
        }

        private void OnDisconnected(object? sender, EventArgs e) => AppHelper.CloseAppForServer(this);

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ConnectionToServerHandler.Disconnected -= OnDisconnected;
            ConnectionHandler.Dispose();

            Close();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = GetTextFromControl(TextBoxUsernameLogIn), password = GetTextFromControl(PasswordBoxLogIn);

            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                new CustomMessageBox("Fill in all fields!", ButtonContent.OK, this)
                    .ShowDialog();
            }
            else
            {
                if (await ConnectionHandler.SendRequestYNAsync(new Request()
                {
                    Tag = Libs.Models.Requests.Tag.Login,
                    Username = username,
                    Password = password
                }.ToJSON()))
                {
                    IsLoggedIn = true;

                    Close();
                }
                else
                {
                    new CustomMessageBox("Wrong credentials.", ButtonContent.OK, this)
                    .ShowDialog();
                }
            }
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = GetTextFromControl(TextBoxUsernameRegistration), password = GetTextFromControl(PasswordBoxRegistration), passwordRepeat = GetTextFromControl(PasswordBoxRepeatRegistration), name = GetTextFromControl(TextBoxNameRegistration), surename = GetTextFromControl(TextBoxSurenameRegistration);

            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password) || String.IsNullOrEmpty(passwordRepeat) || String.IsNullOrEmpty(name) || String.IsNullOrEmpty(surename))
            {
                new CustomMessageBox("Fill in all fields!", ButtonContent.OK, this)
                    .ShowDialog();
            }
            else if (password != passwordRepeat)
            {
                new CustomMessageBox("Passwords do not match.", ButtonContent.OK, this)
                    .ShowDialog();
            }
            else
            {
                if (await ConnectionHandler.SendRequestYNAsync(new Request()
                {
                    Tag = Libs.Models.Requests.Tag.Register,
                    Username = username,
                    Password = password,
                    Name = name,
                    Surename = surename
                }.ToJSON()))
                {
                    new CustomMessageBox("Registration successful. You can log in to this account after admin's verification.", ButtonContent.OK, this)
                        .ShowDialog();
                }
                else
                {
                    new CustomMessageBox("Username already exists.", ButtonContent.OK, this)
                    .ShowDialog();
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private string GetTextFromControl<T>(T control)
        {
            var type = typeof(T);
            string text;

            if (control is null)
                throw new ArgumentNullException();

            if (type == typeof(TextBox))
                text = ((TextBox)(object)control).Text;
            else if (type == typeof(PasswordBox))
                text = ((PasswordBox)(object)control).Password;
            else
                throw new NotImplementedException();

            return text.Trim();
        }
    }
}
