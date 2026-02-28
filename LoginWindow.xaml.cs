using P2PShare.Connection;
using P2PShare.Libs.Models.Requests;
using P2PShare.Models;
using P2PShare.Utils;
using System.IO;
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
        public required ConnectionToServerHandler ConnectionHandler { get; init; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void OnDisconnected(object? sender, EventArgs e) => AppHelper.CloseAppForServer();

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ConnectionHandler.Dispose();

            Close();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = GetTextFromControl(TextBoxUsernameLogIn), password = GetTextFromControl(PasswordBoxLogIn);

                if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
                {
                    new CustomMessageBox("Fill in all of the fields!", ButtonContent.OK)
                        .ShowDialog();
                }
                else if (ContainsInvalidChars(username))
                {
                    ShowInvalidCharsMessage();
                }
                else
                {
                    if (await ConnectionHandler.LogInAsync(username, password))
                    {
                        Close();
                    }
                    else
                    {
                        new CustomMessageBox("Wrong credentials or account not verified.", ButtonContent.OK)
                        .ShowDialog();
                    }
                }
            }
            catch
            {
                HandleError();
            }
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = GetTextFromControl(TextBoxUsernameRegistration), password = GetTextFromControl(PasswordBoxRegistration), passwordRepeat = GetTextFromControl(PasswordBoxRepeatRegistration), name = GetTextFromControl(TextBoxNameRegistration), surename = GetTextFromControl(TextBoxSurenameRegistration);

                if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password) || String.IsNullOrEmpty(passwordRepeat) || String.IsNullOrEmpty(name) || String.IsNullOrEmpty(surename))
                {
                    new CustomMessageBox("Fill in all fields!", ButtonContent.OK)
                        .ShowDialog();
                }
                else if (ContainsInvalidChars(username))
                {
                    ShowInvalidCharsMessage();
                }
                else if (DoesNotContainValidChars(name) || DoesNotContainValidChars(surename))
                {
                    new CustomMessageBox("Name & surename can only contain letters.", ButtonContent.OK).ShowDialog();
                }
                else if (password != passwordRepeat)
                {
                    new CustomMessageBox("Passwords do not match.", ButtonContent.OK)
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
                        new CustomMessageBox("Registration successful. You can log in to this account after admin's verification.", ButtonContent.OK)
                            .ShowDialog();
                    }
                    else
                    {
                        new CustomMessageBox("Username already exists.", ButtonContent.OK)
                        .ShowDialog();
                    }
                }
            }
            catch
            {
                HandleError();
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

        private void HandleError()
        {
            ConnectionHandler.UserInfo = null;

            new CustomMessageBox("Couldn't authenticate.", ButtonContent.OK)
                .ShowDialog();

            Close();
        }

        private bool ContainsInvalidChars(string text)
        {
            var invalidChars = Path.GetInvalidFileNameChars();

            return text.Any(c => invalidChars.Contains(c));
        }

        private bool DoesNotContainValidChars(string text)
        {
            return text.Any(x => !char.IsLetter(x));
        }

        private void ShowInvalidCharsMessage()
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            string invalidCharsString = string.Join(", ", invalidChars);

            new CustomMessageBox($"Username can't contain characters: {invalidCharsString}.", ButtonContent.OK)
                .ShowDialog();
        }

    }
}
