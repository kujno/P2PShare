using P2PShare.Connection;
using P2PShare.Groups;
using P2PShare.Libs;
using P2PShare.Libs.Models;
using P2PShare.Libs.Models.FileSytem;
using P2PShare.Models;
using P2PShare.Utils;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CustomMessageBox? _messageBox;
        private Dictionary<string, long>? _files;
        private Task? _receiveLoop;
        private Task _auth;
        private CancellationTokenSource? _cancellationTokenSource;
        private NetworkInterface? _interface;
        private int _lastPercentage = -1, _curFile;
        private ConnectionToServerHandler? _serverConnection;
        private SolidColorBrush _textColor = new(Color.FromRgb(194, 194, 194));
        private BitmapImage _fileIcon = new(new Uri(Path.Combine(AppContext.BaseDirectory, "images/file.png"), UriKind.Absolute)), _folderIcon = new(new Uri(Path.Combine(AppContext.BaseDirectory, "images/folder.png"), UriKind.Absolute)), _userIcon = new(new Uri(Path.Combine(AppContext.BaseDirectory, "images/User.png"), UriKind.Absolute));
        private SharingWindow? _sharingWindow;

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshInterfaces();
        private void OnContacted(object? sender, IPAddress ip) => NewMessageBox($"Contacting {ip}...", ButtonContent.Cancel, false);

        public MainWindow()
        {
            InitializeComponent();
            Visibility = Visibility.Hidden;
            RefreshInterfaces();
            Interface.SelectedIndex = 0;

            ConnectionHandler.FilePartTransported += OnFilePartTransported;
            ConnectionTranscieverHandler.Contacted += OnContacted;
            SharingWindow.ErrorOccured += (s, e) => OnSharingWindowErrorOccured();

            _auth = AuthAsync();
        }

        private void OnSharingWindowErrorOccured()
        {
            _sharingWindow?.Close();

            NewMessageBox("An error occured.", ButtonContent.OK, true);
        }

        private async Task AuthAsync()
        {
            try
            {
                IPAddress? serverIP;

                if (await ServerIP.GetAsync() is null)
                    new ServerIPWindow("Skip").ShowDialog();

                serverIP = await ServerIP.GetAsync();

                if (serverIP is not null)
                {
                    ConnectionToServerWindow connectionWindow = new(serverIP);

                    connectionWindow.ShowDialog();

                    _serverConnection = connectionWindow.ConnectionHandler;
                    if (_serverConnection!.IsConnected)
                    {
                        LoginWindow loginWindow = new()
                        {
                            ConnectionHandler = _serverConnection
                        };

                        loginWindow.ShowDialog();

                        if (_serverConnection.UserInfo is not null)
                        {
                            TextBlockUser.Text = $"{_serverConnection!.UserInfo!.User.Name} {_serverConnection.UserInfo.User.Surename} ({_serverConnection.UserInfo.User.Username})";
                            TabItemServer.Visibility = Visibility.Visible;
                            RefreshTreeView(_serverConnection.UserInfo);
                        }
                    }
                }
            }
            catch
            {
                NewMessageBox("Couldn't connect to the server.", ButtonContent.OK, true);

                TabItemServer.Visibility = Visibility.Hidden;
            }

            Visibility = Visibility.Visible;

            if (_receiveLoop is null)
                _receiveLoop = ReceiveLoopAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Dispose();

            Close();
        }

        private async void OnWindowClosed(object? sender, bool cancelled)
        {
            _messageBox = null;

            if (cancelled)
            {
                _cancellationTokenSource?.Cancel();
            }
        }

        private void ToolBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Interface_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NetworkInterface? ni = GetSelectedInterface();

            if (ni == _interface || ni is null) return;

            _interface = ni;

            IPAddress? ip = ni is not null ? InterfaceHandling.GetLocalIP(ni) : null;

            YourIP.Text = $"Your IP address:" + (ip is not null ? $" {ip}" : String.Empty);

            _cancellationTokenSource?.Cancel();
        }

        private void OnFilePartTransported(object? sender, FilePartTransportedEventArgs e)
        {
            KeyValuePair<string, long> file;

            if (e.CurrentFile == e.AmountOfFiles && e.Part == 100)
            {
                _messageBox?.ClosedOnPurpose = true;
                _messageBox?.Close();
                _messageBox = null;
                return;
            }

            file = _files!.ElementAt(e.CurrentFile - 1);

            if (_messageBox is null) NewMessageBox(e, file, ButtonContent.Cancel);
            else
            {
                if (_lastPercentage != e.Part) _messageBox.ChangeContent(e, file);
            }

            _lastPercentage = e.Part;
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            FileInfo[] files;
            IPAddress? ipRemote, ipLocal = _interface is not null ? InterfaceHandling.GetLocalIP(_interface) : null;
            string fileText = File.Text.Trim(), messageBoxContent = String.Empty;
            var encryption = CheckBoxEncryption.IsChecked;

            _cancellationTokenSource?.Cancel();
            await _receiveLoop!;

            try
            {
                if (encryption is null) throw new Exception("An error occurred with the encryption option.");
                if (ipLocal is null) throw new Exception("Select a valid interface!");
                if (!IPAddress.TryParse(RemoteIP.Text.Trim(), out ipRemote)) throw new Exception("Enter a valid IP address!");
                if (fileText == String.Empty) throw new Exception("Choose a file to send!");

                files = fileText
                    .Split(ConnectionHandler.FileSeparator)
                    .Select(x => new FileInfo(x))
                    .ToArray();

                _files = files
                    .Select(x => new KeyValuePair<string, long>(x.Name, x.Length))
                    .ToDictionary();

                using (_cancellationTokenSource = new())
                {
                    using (ConnectionTranscieverHandler connectionHandler = new()
                    {
                        IPLocal = ipLocal,
                        IPRemote = ipRemote,
                        CancellationToken = _cancellationTokenSource.Token
                    })
                        await connectionHandler.SendAsync(files, (bool)encryption);
                }

                messageBoxContent = "File(s) transmission succeeded.";
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                messageBoxContent = ex.Message;
            }
            finally
            {
                _messageBox?.ClosedOnPurpose = true;
                _messageBox?.Close();
                _messageBox = null;

                _lastPercentage = -1;

                _receiveLoop = ReceiveLoopAsync();
            }

            NewMessageBox(messageBoxContent, ButtonContent.OK, true);
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            string[]? paths = FileDialogs.SelectFiles();

            if (paths is null) return;

            string pathsString = String.Empty;

            for (int i = 0; i < paths.Length; i++)
            {
                pathsString += paths[i];

                if (i != paths.Length - 1) pathsString += ConnectionHandler.FileSeparator;
            }

            File.Text = pathsString;
        }

        private void RefreshInterfaces()
        {
            string? selectedNI = Interface.SelectedItem?.ToString() ?? null;
            var interfaces = InterfaceHandling.GetUpInterfaces().Select(x => x.Name);

            if (interfaces.All(Interface.Items.Contains) && interfaces.Count() == Interface.Items.Count) return;

            Interface.Items.Clear();

            foreach (var @interface in interfaces) Interface.Items.Add(@interface);

            if (Interface.Items.Contains(selectedNI)) Interface.SelectedItem = selectedNI;
            else Interface.SelectedIndex = 0;
        }

        private NetworkInterface? GetSelectedInterface()
        {
            NetworkInterface? @interface = null;

            try
            {
                @interface = InterfaceHandling.GetUpInterfaces().FirstOrDefault(x => x.Name == Interface.SelectedItem?.ToString()?.Trim());
            }
            catch (Exception ex)
            {
                NewMessageBox(ex.Message, ButtonContent.OK, true);
            }

            return @interface;
        }

        private async Task ReceiveAsync()
        {
            IPAddress? localIP = _interface is not null ? InterfaceHandling.GetLocalIP(_interface) : null; // maybe refresh UI element if local IP changed
            InviteWindow inviteWindow;
            ConnectionReceiverHandler connectionHandler;
            string invite = String.Empty;
            string? dictionary, messageBoxContent = null;
            string[] savedFiles;

            if (localIP is null)
            {
                NewMessageBox("Select a valid interface.", ButtonContent.OK, true);
                throw new OperationCanceledException();
            }

            if (_cancellationTokenSource!.Token.IsCancellationRequested) throw new OperationCanceledException();

            connectionHandler = new()
            {
                IPLocal = localIP,
                CancellationToken = _cancellationTokenSource.Token
            };
            try
            {
                _files = await connectionHandler.ReceiveInviteAsync();

                foreach (var file in _files) invite += $"{file.Key} - {file.Value}B\n";
                inviteWindow = new($"{invite}({(connectionHandler.Encrypted ? "Encrypted" : "Unencrypted")})\nAccept?", this);
                inviteWindow.ShowDialog();

                if (!inviteWindow.Accepted)
                {
                    await connectionHandler.RejectFilesAsync();
                    return;
                }

                dictionary = FileDialogs.SelectFolder();

                if (dictionary is null) messageBoxContent = "Receiving files was cancelled due to folder not chosen.";
                else
                {
                    savedFiles = await connectionHandler.ReceiveFilesAsync(dictionary);

                    messageBoxContent = $"Files saved to {dictionary} as:";
                    foreach (string file in savedFiles) messageBoxContent += $"\n{file}";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == ConnectionReceiverHandler.InviteErrorMessage || ex is OperationCanceledException) return;
                else
                {
                    messageBoxContent = ex.Message == ConnectionHandler.CouldNotOpenFileErrorMessage ? ex.Message : "Receiving file(s) failed.";
                }
            }
            finally
            {
                try
                {
                    connectionHandler.Dispose();
                }
                catch
                {
                }

                _messageBox?.ClosedOnPurpose = true;
                _messageBox?.Close();
                _messageBox = null;

                RefreshInterfaces();
            }

            NewMessageBox(messageBoxContent!, ButtonContent.OK, true);
        }

        private async Task ReceiveLoopAsync()
        {
            NetworkInterface? @interface;

            do
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new();
                @interface = _interface;

                try
                {
                    await ReceiveAsync();
                }
                catch
                {
                }
            }
            while (!_cancellationTokenSource.IsCancellationRequested || @interface != _interface);
        }

        private void NewMessageBox(string content, ButtonContent buttonContent, bool modal)
        {
            _messageBox = new(content, buttonContent, modal ? null : this);
            ShowMessageBox(modal);
        }

        private void NewMessageBox(FilePartTransportedEventArgs transportInfo, KeyValuePair<string, long> file, ButtonContent buttonContent)
        {
            _messageBox = new(transportInfo, file, buttonContent, this);
            ShowMessageBox(false);
        }

        private void ShowMessageBox(bool modal)
        {
            _messageBox?.WindowClosed += OnWindowClosed;

            try
            {
                if (modal) _messageBox?.ShowDialog();
                else _messageBox?.Show();
            }
            catch
            {
            }

            if (modal) _messageBox = null;
        }

        private async void RefreshServer_Click(object sender, RoutedEventArgs e) => await RefreshFilesAsync();

        private async Task RefreshFilesAsync()
        {
            try
            {
                await _serverConnection!.GetAsync();

                RefreshTreeView(_serverConnection.UserInfo!);
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private void RefreshTreeView(AllUserInfo userInfo)
        {
            TreeViewFiles.Items.Clear();

            TreeViewFiles.Items.Add(CreateFromDir(userInfo.MyDir));
            Array.ForEach(CreateFromSharedDirsAndFiles(userInfo.SharedDirs ?? [], userInfo.SharedFils ?? []), x => TreeViewFiles.Items.Add(x));
        }

        private sealed record TreeNodeTag(string Owner, string Path, int? id = null);

        private TreeViewItem CreateFromDir(Dir dir, string? previousName = null, bool my = true)
        {
            var curDirName = previousName is null ? dir.Name : $"{previousName}\\{dir.Name}";

            TreeViewItem item = new()
            {
                Header = CreateTreeViewItemHeader(true, $"{dir.Name}{(my ? String.Empty : $" | Shared with rights: (can download){(dir.CanRename ? " (can rename)" : String.Empty)}{(dir.CanDelete ? " (can delete)" : String.Empty)}{(dir.CanAdd ? " (can add)" : String.Empty)}")}"),
                Tag = new TreeNodeTag(dir.Owner, curDirName, dir.ID),
                Foreground = _textColor
            };

            if (dir.Dirs is not null)
            {
                foreach (var subDir in dir.Dirs)
                    item.Items.Add(CreateFromDir(subDir, curDirName, my));
            }

            if (dir.Fils is not null)
            {
                foreach (var fil in dir.Fils)
                    item.Items.Add(new TreeViewItem()
                    {
                        Header = CreateTreeViewItemHeader(false, $"{fil.Name}{(my ? String.Empty : $" | Shared with rights: (can download){(fil.CanRename ? " (can rename)" : String.Empty)}{(fil.CanDelete ? " (can delete)" : String.Empty)}")}", fil.Size),
                        Tag = new TreeNodeTag(fil.Owner, $"{curDirName}\\{fil.Name}", fil.ID),
                        Foreground = _textColor
                    });
            }

            return item;
        }

        private TreeViewItem[] CreateFromSharedDirsAndFiles(Dir[] sharedDirs, Fil[] sharedFils)
        {
            List<TreeViewItem> items = [];

            foreach (var dir in sharedDirs)
            {
                CreateTreeViewItemIfOwnerNotThere(ref items, dir.Owner, dir.ID);

                items.First(x => ((TreeNodeTag)x.Tag).Owner == dir.Owner).Items.Add(CreateFromDir(dir, dir.Owner, false));
            }

            foreach (var fil in sharedFils)
            {
                CreateTreeViewItemIfOwnerNotThere(ref items, fil.Owner, fil.ID);

                var item = items.First(x => ((TreeNodeTag)x.Tag).Owner == fil.Owner);

                item.Items.Add(new TreeViewItem()
                {
                    Header = CreateTreeViewItemHeader(false, $"{fil.Name} | Shared with rights: (can download){(fil.CanRename ? " (can rename)" : String.Empty)}{(fil.CanDelete ? " (can delete)" : String.Empty)}", fil.Size),
                    Tag = new TreeNodeTag(fil.Owner, $"{fil.Owner}\\{fil.Name}", fil.ID),
                    Foreground = _textColor,
                });
            }

            return items.ToArray();
        }

        private void CreateTreeViewItemIfOwnerNotThere(ref List<TreeViewItem> items, string owner, int? id)
        {
            if (items.All(x => ((TreeNodeTag)x.Tag).Owner != owner))
                items.Add(new()
                {
                    Header = CreateTreeViewItemHeader(true, $"{owner} (Shared)"),
                    Tag = new TreeNodeTag(owner, owner, id)
                });
        }

        private async void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            NewFolderWindow newFolderWindow = new();
            bool my;
            var item = (TreeViewItem?)TreeViewFiles.SelectedItem;

            try
            {
                if (item is null)
                    return;

                newFolderWindow.ShowDialog();
                if (newFolderWindow.FolderName is null)
                    return;

                var path = GetPath(item, out my, out int? id);

                if (await _serverConnection!.NewFolderAsync($"{(path != String.Empty ? $"{path}\\" : String.Empty)}{newFolderWindow.FolderName}", my, id))
                    await RefreshFilesAsync();
                else
                    NewMessageBox("Couldn't create the folder.", ButtonContent.OK, true);
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private UIElement CreateTreeViewItemHeader(bool folder, string name, long? fileSize = null, bool user = false)
        {
            var fileSizeNotNull = fileSize is not null;
            CheckBox checkBox = new()
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            checkBox.Checked += CheckBoxChanged;
            checkBox.Unchecked += CheckBoxChanged;

            return new Grid()
            {
                Height = 19,

                ColumnDefinitions =
                {
                    new ColumnDefinition() { Width = GridLength.Auto },
                    new ColumnDefinition() { Width = GridLength.Auto },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition() { Width = GridLength.Auto },
                },

                Children =
                {
                    SetColumnAndReturnTheSameElement(new Image()
                    {
                        Source = folder ? _folderIcon : _fileIcon,
                        VerticalAlignment = VerticalAlignment.Center
                    }, 0),

                    SetColumnAndReturnTheSameElement(new TextBlock()
                    {
                        Text = name,
                        Margin = new Thickness(8, 0, 0, 0),
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center
                    }, 2),

                    SetColumnAndReturnTheSameElement(new TextBlock()
                    {
                        Text = fileSizeNotNull ? $"{fileSize} Bytes" : String.Empty,
                        Margin = new Thickness(fileSizeNotNull ? 20 : 0, 0, 0, 0),
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center
                    }, 3),

                    SetColumnAndReturnTheSameElement(checkBox, 1)
                }
            };
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = TreeViewFiles.Items
                .Cast<TreeViewItem>()
                .ToArray();
                var units = GetAllUnits(items);
                bool check = true;

                if (units.Length > 0)
                {
                    foreach (var unit in units)
                    {
                        if (!await _serverConnection!.DeleteAsync(unit))
                            check = false;
                    }

                    if (!check)
                        NewMessageBox("Couldn't delete some of the selected items.", ButtonContent.OK, true);

                    await RefreshFilesAsync();
                }
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        public static T SetColumnAndReturnTheSameElement<T>(T element, int column) where T : UIElement
        {
            Grid.SetColumn(element, column);

            return element;
        }

        private void CloseIfServerDisconnected()
        {
            if (!(_serverConnection?.IsConnected ?? false))
                AppHelper.CloseAppForServer();
            else
                NewMessageBox("An error occured.", ButtonContent.OK, true);
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var isChecked = checkBox.IsChecked ?? false;
            var treeViewItem = (TreeViewItem)((Grid)checkBox.Parent).Parent;
            var items = treeViewItem.Items
                .Cast<TreeViewItem>()
                .ToArray();

            if (isChecked && treeViewItem.IsExpanded)
            {
                foreach (var item in items)
                    ((Grid)item.Header).Children
                        .OfType<CheckBox>()
                        .First().IsChecked = isChecked;
            }
            else if (!isChecked)
                UncheckParents(treeViewItem);
        }

        private string GetPath(TreeViewItem item, out bool my, out int? id)
        {
            var tag = (TreeNodeTag)item.Tag;
            var path = (tag).Path;
            var indexOfSeparator = path.IndexOf('\\');
            bool containsSeparator = indexOfSeparator != -1;

            id = tag.id;

            my = (containsSeparator ? path.Substring(0, indexOfSeparator) : path) == _serverConnection?.UserInfo?.User.Username;

            return my ? (containsSeparator ? path.Substring(indexOfSeparator + 1) : String.Empty) : path;
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            bool my, check = true, encrypted;
            string[]? files;
            List<FileInfo> fileInfos = [];
            var item = (TreeViewItem?)TreeViewFiles.SelectedItem;

            try
            {
                if (item is null)
                    return;

                if ((files = FileDialogs.SelectFiles()) is null)
                    return;

                _lastPercentage = -1;
                if (_files is null)
                    _files = [];
                _curFile = -1;
                _files.Clear();
                ConnectionHandler.FilePartTransported -= OnFilePartTransported;
                ConnectionHandler.FilePartTransported += OnFilePartSent;

                Array.ForEach(files, x =>
                {
                    FileInfo fileInfo = new(x);

                    fileInfos.Add(fileInfo);
                    _files.Add(fileInfo.Name, fileInfo.Length);
                });

                encrypted = CheckBoxServerEncryption.IsChecked ?? true;
                _messageBox = null;

                foreach (var fileInfo in fileInfos)
                {
                    _curFile++;

                    if (!await _serverConnection!.UploadAsync(GetPath(item, out my, out int? id), my, encrypted, fileInfo, id))
                        check = false;
                }

                if (check)
                    await RefreshFilesAsync();
                else
                    NewMessageBox("Couldn't upload all of the selected files.", ButtonContent.OK, true);
            }
            catch
            {
                _messageBox?.ClosedOnPurpose = true;
                _messageBox?.Close();

                _messageBox = null;

                CloseIfServerDisconnected();
            }
            finally
            {
                ConnectionHandler.FilePartTransported += OnFilePartTransported;
                ConnectionHandler.FilePartTransported -= OnFilePartSent;

                _lastPercentage = -1;
            }
        }

        private FileUnit[] GetAllUnits(TreeViewItem[] items)
        {
            List<FileUnit> output = [];

            foreach (var item in items)
                output.AddRange(GetUnits(item));

            return output.ToArray();
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            ConnectionHandler.FilePartTransported -= OnFilePartTransported;
            ConnectionHandler.FilePartTransported += OnFilePartReceived;

            try
            {
                var units = GetAllUnits(TreeViewFiles.Items
                .Cast<TreeViewItem>()
                .ToArray());
                var names = new string[units.Length];
                var folder = FileDialogs.SelectFolder();
                var originalMessage = $"Saved to {@folder}:\n";
                var message = originalMessage;
                bool check = true;

                if (folder is null)
                    return;

                for (var i = 0; i < units.Length; i++)
                {
                    names[i] = GetFileNameFromPath(units[i].Path);
                }

                _files = new(names.Select(x => new KeyValuePair<string, long>(x, 0)));
                _curFile = -1;
                _lastPercentage = -1;

                foreach (var unit in units)
                {
                    _curFile++;

                    var downloadedFile = await _serverConnection!.DownloadAsync(unit, folder, CheckBoxServerEncryption.IsChecked ?? true);

                    if (downloadedFile is null)
                        check = false;
                    else
                        message += $"{downloadedFile}\n";
                }

                if (!check)
                {
                    if (message == originalMessage)
                        message = "Couldn't download any of the files.";
                    else
                        message += "Couldn't download some of the files.";
                }
                else
                {
                    message = message.Trim();
                }

                NewMessageBox(message, ButtonContent.OK, true);
            }
            catch
            {
                _messageBox?.ClosedOnPurpose = true;
                _messageBox?.Close();
                _messageBox = null;

                CloseIfServerDisconnected();
            }
            finally
            {
                ConnectionHandler.FilePartTransported += OnFilePartTransported;
                ConnectionHandler.FilePartTransported -= OnFilePartReceived;

                _lastPercentage = -1;
            }
        }

        private FileUnit[] GetUnits(TreeViewItem item)
        {
            List<FileUnit> output = [];
            var header = (Grid)item.Header;


            if ((header.Children.OfType<CheckBox>().First().IsChecked ?? false) && header.Children.OfType<TextBlock>().All(x => x.Text.Trim() != _serverConnection?.UserInfo?.User.Username && _serverConnection!.UserInfo!.Users.All(y => y.Username != x.Text.Trim())))
            {
                var path = GetPath(item, out bool my, out int? id);

                output.Add(new()
                {
                    Path = path,
                    My = my,
                    Unit = header.Children
                                .OfType<Image>()
                                .First().Source == _fileIcon ? Unit.File : Unit.Directory,
                    ID = id
                });

                return output.ToArray();
            }

            foreach (var subItem in item.Items.Cast<TreeViewItem>())
                output.AddRange(GetUnits(subItem));

            return output.ToArray();
        }

        private void OnFilePartSent(object? sender, FilePartTransportedEventArgs e)
        {
            KeyValuePair<string, long> file;

            if (_curFile == _files?.Count - 1 && e.Part == 100)
            {
                _messageBox?.ClosedOnPurpose = true;
                _messageBox?.Close();
                _messageBox = null;
                return;
            }

            file = _files!.ElementAt(_curFile);

            if (_messageBox is null)
                NewMessageBox(e, file, ButtonContent.None);
            else
            {
                if (_lastPercentage != e.Part) _messageBox.ChangeContent(e, file);
            }

            _lastPercentage = e.Part;
        }

        private async void Share_Click(object sender, RoutedEventArgs e)
        {
            bool success = true;

            try
            {
                var item = (TreeViewItem?)TreeViewFiles.SelectedItem;
                if (item is null)
                    return;

                var grid = (Grid)item.Header;
                Dir curDir = _serverConnection!.UserInfo!.MyDir;
                TreeNodeTag tag = (TreeNodeTag)item.Tag;
                string[] pathParts = tag.Path.Split('\\');
                bool my;

                if (tag.Owner != _serverConnection?.UserInfo?.User.Username)
                {
                    NewMessageBox("You can share only your files.", ButtonContent.OK, true);
                    return;
                }

                if (pathParts.Length == 1)
                {
                    NewMessageBox("You can't share your root folder.", ButtonContent.OK, true);
                    return;
                }

                var unit = grid.Children.OfType<Image>().First().Source == _fileIcon ? Unit.File : Unit.Directory;
                for (var i = 1; i < pathParts.Length - 1; i++)
                {
                    curDir = curDir.Dirs!.First(x => x.Name == pathParts[i]);
                }

                _sharingWindow = new(unit, grid.Children.OfType<TextBlock>().First().Text, _serverConnection.UserInfo.Users, _serverConnection.UserInfo.UserGroups, GetShares(_serverConnection.UserInfo, pathParts, unit));

                if (_sharingWindow.DidErrorOccur)
                {
                    return;
                }

                _sharingWindow.ShowDialog();

                if (_sharingWindow.DidErrorOccur || !_sharingWindow.Changed)
                {
                    _sharingWindow = null;

                    return;
                }

                if (!await _serverConnection.EditSharesAsync(_sharingWindow.Shares!, GetPath(item, out my, out _), my, unit))
                    success = false;
                else
                    await RefreshFilesAsync();
            }
            catch
            {
                CloseIfServerDisconnected();

                return;
            }

            NewMessageBox(success ? "Shares changed." : "Couldn't change shares.", ButtonContent.OK, true);
        }

        private void OnFilePartReceived(object? sender, FilePartTransportedEventArgs e)
        {
            KeyValuePair<string, long> file;

            e.AmountOfFiles = _files!.Count;
            e.CurrentFile = _curFile + 1;

            if (_curFile == _files?.Count - 1 && e.Part == 100)
            {
                _messageBox?.ClosedOnPurpose = true;
                _messageBox?.Close();
                _messageBox = null;
                return;
            }

            file = _files!.ElementAt(_curFile);

            if (_messageBox is null)
                NewMessageBox(e, file, ButtonContent.None);
            else
            {
                if (_lastPercentage != e.Part) _messageBox.ChangeContent(e, file);
            }

            _lastPercentage = e.Part;
        }

        private void ManageGroups_Click(object sender, RoutedEventArgs e)
        {
            new GroupManagementWindow(_serverConnection!).ShowDialog();

            RefreshTreeView(_serverConnection?.UserInfo!);
        }

        private async void Rename_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (TreeViewItem?)TreeViewFiles.SelectedItem;
                var check = true;

                if (item is not null)
                {
                    var path = GetPath(item, out bool my, out int? id);
                    if (!String.IsNullOrEmpty(path) && _serverConnection!.UserInfo!.Users.All(x => x.Username != path) && _serverConnection.UserInfo.User.Username != path)
                    {
                        var pathParts = path.Split('\\');
                        NewFolderWindow newNameWindow = new($"Rename \"{pathParts.Last()}\" to:")
                        {
                            BadNameMessage = "File name contains invalid character(s)."
                        };

                        newNameWindow.ShowDialog();

                        if (newNameWindow.FolderName is not null)
                        {
                            if (await _serverConnection!.RenameAsync(path, string.Join('\\', pathParts[0..(pathParts.Length - 1)].Concat([newNameWindow.FolderName])), ((Grid)item.Header).Children.OfType<Image>().First().Source == _fileIcon ? Unit.File : Unit.Directory, my, id))
                            {
                                await RefreshFilesAsync();
                            }
                            else
                            {
                                check = false;
                            }
                        }
                    }
                    else
                    {
                        check = false;
                    }

                    if (!check)
                        NewMessageBox("Couldn't rename the item.", ButtonContent.OK, true);
                }
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private void UncheckParents(TreeViewItem item)
        {
            if (item.Parent is TreeViewItem parent)
            {
                ((Grid)parent.Header).Children
                    .OfType<CheckBox>()
                    .First().IsChecked = false;

                UncheckParents(parent);
            }
        }

        private string GetFileNameFromPath(string path)
        {
            var indexOfSeparator = path.LastIndexOf('\\');

            return indexOfSeparator != -1 ? path.Substring(indexOfSeparator + 1) : path;
        }

        private Share[]? GetShares(AllUserInfo userInfo, string[] pathParts, Unit unit)
        {
            Dir curDir = userInfo.MyDir;
            var isFile = unit == Unit.File;

            for (var i = 1; i < (isFile ? pathParts.Length - 1 : pathParts.Length); i++)
            {
                curDir.Dirs?.ForEach(x =>
                {
                    if (x.Name == pathParts[i])
                        curDir = x;
                });
            }

            if (isFile)
                return curDir.Fils?.First(x => x.Name == pathParts.Last()).Shares;
            else
                return curDir.Shares;
        }
    }
}