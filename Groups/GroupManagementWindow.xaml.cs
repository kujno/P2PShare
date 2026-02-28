using P2PShare.Connection;
using P2PShare.Libs.Models.FileSytem;
using P2PShare.Models;
using P2PShare.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace P2PShare.Groups
{
    /// <summary>
    /// Interaction logic for GroupManagementWindow.xaml
    /// </summary>
    public partial class GroupManagementWindow : Window
    {
        private ConnectionToServerHandler _connectionHandler;
        private Brush _brush = (Brush?)new BrushConverter().ConvertFrom("#C2C2C2") ?? Brushes.White;

        public GroupManagementWindow(ConnectionToServerHandler connectionHandler)
        {
            InitializeComponent();

            _connectionHandler = connectionHandler;

            LoadGroups();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private Grid CreateRow(Group group)
        {
            Button editBtn = new Button()
            {
                Content = "Edit",
                Margin = new Thickness(5)
            };
            Button delBtn = new Button()
            {
                Content = "Delete",
                Margin = new Thickness(5)
            };

            editBtn.Click += (s, e) => EditClicked((Button)s);
            delBtn.Click += (s, e) => DeleteClicked((Button)s);

            return new()
            {
                Height = 40,
                Tag = group,

                ColumnDefinitions =
                {
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) }
                },

                Children =
                {
                    MainWindow.SetColumnAndReturnTheSameElement(new TextBlock()
                    {
                        Text = group.Name,
                        Margin = new Thickness(5),
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = _brush
                    }, 0),

                    MainWindow.SetColumnAndReturnTheSameElement(editBtn, 1),

                    MainWindow.SetColumnAndReturnTheSameElement(delBtn, 2)
                }
            };
        }

        private async void EditClicked(Button sender)
        {
            GroupEditingWindow editWindow = new((Group)((Grid)sender.Parent).Tag, _connectionHandler.UserInfo!.Users!);

            editWindow.ShowDialog();

            try
            {
                if (editWindow.Change)
                {
                    if (await _connectionHandler.EditGroupAsync(editWindow.Group))
                        new CustomMessageBox("Group edited successfully.", ButtonContent.OK).ShowDialog();
                    else
                        new CustomMessageBox("Group edit failed.", ButtonContent.OK).ShowDialog();
                }

                await UpdateGroupsAsync();
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private async void DeleteClicked(Button sender)
        {
            try
            {
                if (await _connectionHandler.DeleteGroupAsync((Group)((Grid)sender.Parent).Tag))
                {
                    new CustomMessageBox("Group deleted successfully.", ButtonContent.OK).ShowDialog();

                    await UpdateGroupsAsync();
                }
                else
                    new CustomMessageBox("Group deletion failed.", ButtonContent.OK).ShowDialog();
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private void LoadGroups()
        {
            StackPanelGroups.Children.Clear();

            Array.ForEach(_connectionHandler.UserInfo?.UserGroups ?? [], x => StackPanelGroups.Children.Add(CreateRow(x)));
        }

        private async Task UpdateGroupsAsync()
        {
            try
            {
                await _connectionHandler.GetAsync();

                LoadGroups();
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private void CloseIfServerDisconnected()
        {
            if (!(_connectionHandler?.IsConnected ?? false))
                AppHelper.CloseAppForServer();
            else
                new CustomMessageBox("An error occured.", ButtonContent.OK).ShowDialog();
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NewFolderWindow nameWindow = new("New group name:")
                {
                    BadNameMessage = "Group name contain(s) invalid characters."
                };

                nameWindow.ShowDialog();

                if (nameWindow.FolderName is not null)
                {
                    if (await _connectionHandler.CreateGroupAsync(nameWindow.FolderName))
                    {
                        new CustomMessageBox("Group created successfully.", ButtonContent.OK).ShowDialog();

                        await UpdateGroupsAsync();
                    }
                    else
                        new CustomMessageBox("Group creation failed.", ButtonContent.OK).ShowDialog();
                }
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }
    }
}
