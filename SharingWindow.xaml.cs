using P2PShare.Libs.Models.FileSytem;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for SharingWindow.xaml
    /// </summary>
    public partial class SharingWindow : Window
    {
        private User[]? _users;
        private Group[]? _groups;
        private Unit _unit;

        public Share[]? Shares { get; private set; }
        public bool Changed { get; private set; } = false;
        public bool DidErrorOccur { get; private set; } = false;

        public static event EventHandler? ErrorOccured;

        public SharingWindow(Unit unit, string unitName, User[]? users = null, Group[]? groups = null, Share[]? shares = null)
        {
            try
            {
                InitializeComponent();
                TextBlockFile.Text = $"{(unit == Unit.File ? "File" : "Directory")}: {unitName}";

                _users = users;
                _groups = groups;
                _unit = unit;
                Shares = shares;

                if (_users is not null)
                    Array.ForEach(_users, x => StackPanelUsers.Children.Add(CreateRow($"{x.Name} {x.Surename}\n({x.Username})", _unit, x.Username, Shares?.FirstOrDefault(y => y.User?.Username == x.Username))));

                if (_groups is not null)
                    Array.ForEach(_groups, x => StackPanelGroups.Children.Add(CreateRow($"{x.Name}\nAdmin: {x.Admin.Name}", _unit, x.ID.ToString(), Shares?.FirstOrDefault(y => y.Group?.Name == x.Name))));
            }
            catch
            {
                OnErrorOccured();
            }
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

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Changed = true;

                List<Share> shares = new();

                foreach (Grid child in StackPanelUsers.Children.OfType<Grid>())
                {
                    var checkboxes = child.Children.OfType<CheckBox>();

                    if (checkboxes.First(x => x.Name == "Shared").IsChecked ?? false)
                    {
                        shares.Add(new()
                        {
                            User = _users?.First(x => x.Username == (string)child.Tag),
                            Type = _unit,
                            CanDelete = checkboxes.First(x => x.Name == "CanDelete").IsChecked ?? false,
                            CanRename = checkboxes.First(x => x.Name == "CanRename").IsChecked ?? false,
                            CanAdd = _unit == Unit.Directory ? checkboxes.First(x => x.Name == "CanAdd")?.IsChecked ?? false : false
                        });
                    }
                }

                foreach (Grid child in StackPanelGroups.Children.OfType<Grid>())
                {
                    var checkboxes = child.Children.OfType<CheckBox>();

                    if (checkboxes.First(x => x.Name == "Shared").IsChecked ?? false)
                    {
                        shares.Add(new()
                        {
                            Group = _groups?.First(x => x.ID == int.Parse((string)child.Tag)),
                            Type = _unit,
                            CanDelete = checkboxes.First(x => x.Name == "CanDelete").IsChecked ?? false,
                            CanRename = checkboxes.First(x => x.Name == "CanRename").IsChecked ?? false,
                            CanAdd = _unit == Unit.Directory ? checkboxes.First(x => x.Name == "CanAdd")?.IsChecked ?? false : false
                        });
                    }
                }

                Shares = shares.ToArray();
            }
            catch
            {
                OnErrorOccured();
            }
            finally
            {
                Close();
            }
        }

        private Grid CreateRow(string name, Unit unit, string idTag, Share? share = null)
        {
            var shareNull = share is null;
            CheckBox sharedCheckBox = new()
            {
                Name = "Shared",
                IsChecked = !shareNull,
                Margin = new Thickness(5)
            };

            sharedCheckBox.Checked += (s, e) => CheckBoxSharedChanged((CheckBox)s);
            sharedCheckBox.Unchecked += (s, e) => CheckBoxSharedChanged((CheckBox)s);
            Grid output = new()
            {
                Height = 40,
                Tag = idTag,

                ColumnDefinitions =
                {
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) }
                },

                Children =
                {
                    MainWindow.SetColumnAndReturnTheSameElement(sharedCheckBox, 0),

                    MainWindow.SetColumnAndReturnTheSameElement(new TextBlock()
                    {
                        Text = name,
                        Margin = new Thickness(5),
                        VerticalAlignment = VerticalAlignment.Center
                    }, 1),

                    MainWindow.SetColumnAndReturnTheSameElement(new CheckBox()
                    {
                        Name = "CanDelete",
                        Content = "Can delete",
                        IsEnabled = !shareNull,
                        IsChecked = share?.CanDelete ?? false,
                        Margin = new Thickness(5)
                    }, 2),

                    MainWindow.SetColumnAndReturnTheSameElement(new CheckBox()
                    {
                        Name = "CanRename",
                        Content = "Can rename",
                        IsEnabled = !shareNull,
                        IsChecked = share?.CanRename ?? false,
                        Margin = new Thickness(5)
                    }, 3)
                }
            };

            if (unit == Unit.Directory)
            {
                output.Children.Add(MainWindow.SetColumnAndReturnTheSameElement(new CheckBox()
                {
                    Name = "CanAdd",
                    Content = "Can add",
                    IsEnabled = !shareNull,
                    IsChecked = share?.CanAdd ?? false,
                    Margin = new Thickness(5)
                }, 4));
            }

            return output;
        }

        private void CheckBoxSharedChanged(CheckBox sender)
        {
            ((Grid)sender.Parent).Children
                .OfType<CheckBox>()
                .Where(x => x.Name != "Shared")
                .ToList()
                .ForEach(y =>
                {
                    bool isChecked = sender.IsChecked ?? false;


                    y.IsEnabled = isChecked;
                    if (!isChecked)
                    {
                        y.IsChecked = isChecked;
                    }
                });
        }

        private void OnErrorOccured()
        {
            DidErrorOccur = true;

            ErrorOccured?.Invoke(this, EventArgs.Empty);
        }
    }
}
