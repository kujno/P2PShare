using P2PShare.Libs.Models.FileSytem;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace P2PShare.Groups
{
    /// <summary>
    /// Interaction logic for GroupEditingWindow.xaml
    /// </summary>
    public partial class GroupEditingWindow : Window
    {
        public bool Change { get; private set; } = false;
        public Group Group { get; private set; }

        private Brush _brush = (Brush?)new BrushConverter().ConvertFrom("#C2C2C2") ?? Brushes.White;

        public GroupEditingWindow(Group group, User[] users)
        {
            InitializeComponent();

            Group = group;

            TextBlockGroup.Text = Group.Name;
            Array.ForEach(users, x => StackPanelUsers.Children.Add(CreateRow(x)));
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            NewFolderWindow newNameWindow = new("New name:")
            {
                BadNameMessage = "Group name contain invalid characters(s)."
            };

            newNameWindow.ShowDialog();

            if (newNameWindow.FolderName is not null)
            {
                Group.Name = newNameWindow.FolderName;

                TextBlockGroup.Text = Group.Name;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Group.Users = StackPanelUsers.Children
                .OfType<Grid>()
                .Where(x => x.Children
                .OfType<CheckBox>()
                .First().IsChecked ?? false)
                .Select(x => (User)x.Tag)
                .ToArray();

            Change = true;

            Close();
        }

        private Grid CreateRow(User user)
        {
            return new()
            {
                Height = 40,
                Tag = user,

                ColumnDefinitions =
                {
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }
                },

                Children =
                {
                    MainWindow.SetColumnAndReturnTheSameElement(new CheckBox()
                    {
                        Margin = new Thickness(5),
                        IsChecked = Group.Users.Any(x => x.Username == user.Username)
                    }, 0),

                    MainWindow.SetColumnAndReturnTheSameElement(new TextBlock()
                    {
                        Text = $"{user.Name} {user.Surename}\n({user.Username})",
                        Margin = new Thickness(5),
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = _brush
                    }, 1)
                }
            };
        }
    }
}
