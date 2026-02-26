using P2PShare.Connection;
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
        private BitmapImage _fileIcon = new(new Uri(Path.Combine(AppContext.BaseDirectory, "images/file.png"), UriKind.Absolute)), _folderIcon = new(new Uri(Path.Combine(AppContext.BaseDirectory, "images/folder.png"), UriKind.Absolute));

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

            _auth = AuthAsync();
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
                            await RefreshFilesAsync();
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

            if (_messageBox is null) NewMessageBox(e, file, false, ButtonContent.Cancel);
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
            _messageBox = new(content, buttonContent, this, modal);
            ShowMessageBox(modal);
        }

        private void NewMessageBox(FilePartTransportedEventArgs transportInfo, KeyValuePair<string, long> file, bool modal, ButtonContent buttonContent)
        {
            _messageBox = new(transportInfo, file, buttonContent, this);
            ShowMessageBox(modal);
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

                TreeViewFiles.Items.Clear();

                TreeViewFiles.Items.Add(CreateFromDir(_serverConnection.UserInfo!.MyDir));
                Array.ForEach(CreateFromSharedDirsAndFiles(_serverConnection.UserInfo.SharedDirs ?? [], _serverConnection.UserInfo.SharedFils ?? []), x => TreeViewFiles.Items.Add(x));
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private sealed record TreeNodeTag(string Owner, string Path);

        private TreeViewItem CreateFromDir(Dir dir, string? previousName = null)
        {
            var curDirName = previousName is null ? dir.Name : $"{previousName}\\{dir.Name}";

            TreeViewItem item = new()
            {
                Header = CreateTreeViewItemHeader(true, dir.Name),
                Tag = new TreeNodeTag(dir.Owner, curDirName),
                Foreground = _textColor
            };

            if (dir.Dirs is not null)
            {
                foreach (var subDir in dir.Dirs)
                    item.Items.Add(CreateFromDir(subDir, curDirName));
            }

            if (dir.Fils is not null)
            {
                foreach (var fil in dir.Fils)
                    item.Items.Add(new TreeViewItem()
                    {
                        Header = CreateTreeViewItemHeader(false, fil.Name, fil.Size),
                        Tag = new TreeNodeTag(fil.Owner, $"{curDirName}\\{fil.Name}"),
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
                CreateTreeViewItemIfOwnerNotThere(ref items, dir.Owner);

                items.First(x => ((TreeNodeTag)x.Tag).Owner == dir.Owner).Items.Add(CreateFromDir(dir));
            }

            foreach (var fil in sharedFils)
            {
                CreateTreeViewItemIfOwnerNotThere(ref items, fil.Owner);

                var item = items.First(x => ((TreeNodeTag)x.Tag).Owner == fil.Owner);

                item.Items.Add(new TreeViewItem()
                {
                    Header = CreateTreeViewItemHeader(false, fil.Name, fil.Size),
                    Tag = new TreeNodeTag(fil.Owner, $"{item.Name}\\{fil.Name}"),
                    Foreground = _textColor,
                });
            }

            return items.ToArray();
        }

        private void CreateTreeViewItemIfOwnerNotThere(ref List<TreeViewItem> items, string owner)
        {
            if (items.All(x => ((TreeNodeTag)x.Tag).Owner == owner))
                items.Add(new() { Header = CreateTreeViewItemHeader(true, owner) });
        }

        private async void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            NewFolderWindow newFolderWindow = new();
            bool my;

            try
            {
                newFolderWindow.ShowDialog();
                if (newFolderWindow.FolderName is null)
                    return;

                var path = GetPath((TreeViewItem)TreeViewFiles.SelectedItem, out my);

                if (await _serverConnection!.NewFolderAsync($"{(path != String.Empty ? $"{path}\\" : String.Empty)}{newFolderWindow.FolderName}", my))
                    await RefreshFilesAsync();
                else
                    NewMessageBox("Couldn't create the folder.", ButtonContent.OK, true);
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private UIElement CreateTreeViewItemHeader(bool folder, string name, long? fileSize = null)
        {
            var fileSizeNotNull = fileSize is not null;
            CheckBox checkBox = new()
            {
                Margin = new Thickness(20, 0, 0, 0),
            };

            checkBox.Checked += CheckBoxChanged;
            checkBox.Unchecked += CheckBoxChanged;

            return new Grid()
            {
                Height = 19,

                ColumnDefinitions =
                {
                    new ColumnDefinition() { Width = GridLength.Auto },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition() { Width = GridLength.Auto },
                    new ColumnDefinition() { Width = GridLength.Auto },
                },

                Children =
                {
                    SetColumnAndReturnTheSameElement(new Image()
                    {
                        Source = folder ? _folderIcon : _fileIcon,
                    }, 0),

                    SetColumnAndReturnTheSameElement(new TextBlock()
                    {
                        Text = name,
                        Margin = new Thickness(10, 0, 0, 0),
                    }, 1),

                    SetColumnAndReturnTheSameElement(new TextBlock()
                    {
                        Text = fileSizeNotNull ? $"{fileSize} Bytes" : String.Empty,
                        Margin = new Thickness(fileSizeNotNull ? 20 : 0, 0, 0, 0),
                    }, 2),

                    SetColumnAndReturnTheSameElement(checkBox, 3)
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

                foreach (var unit in units)
                {
                    if (!await _serverConnection!.DeleteAsync(unit))
                        check = false;
                }

                if (check)
                    await RefreshFilesAsync();
                else
                    NewMessageBox("Couldn't delete all of the selected items.", ButtonContent.OK, true);
            }
            catch
            {
                CloseIfServerDisconnected();
            }
        }

        private T SetColumnAndReturnTheSameElement<T>(T element, int column) where T : UIElement
        {
            Grid.SetColumn(element, column);

            return element;
        }

        private void CloseIfServerDisconnected()
        {
            if (!(_serverConnection?.IsConnected ?? false))
                AppHelper.CloseAppForServer(this);
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

        private string GetPath(TreeViewItem item, out bool my)
        {
            var path = ((TreeNodeTag)item.Tag).Path;
            var indexOfSeparator = path.IndexOf('\\');
            bool containsSeparator = indexOfSeparator != -1;

            my = (containsSeparator ? path.Substring(0, indexOfSeparator) : path) == _serverConnection?.UserInfo?.User.Username;

            return my ? (containsSeparator ? path.Substring(indexOfSeparator + 1) : String.Empty) : path;
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            bool my, check = true, encrypted;
            string[]? files;
            List<FileInfo> fileInfos = [];

            try
            {
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

                    if (!await _serverConnection!.UploadAsync(GetPath((TreeViewItem)TreeViewFiles.SelectedItem, out my), my, encrypted, fileInfo))
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

                    await RefreshFilesAsync();
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


            if ((header.Children.OfType<CheckBox>().First().IsChecked ?? false) && header.Children.OfType<TextBlock>().All(x => x.Text.Trim() != _serverConnection?.UserInfo?.User.Username))
            {
                var path = GetPath(item, out bool my);

                output.Add(new()
                {
                    Path = path,
                    My = my,
                    Unit = header.Children
                                .OfType<Image>()
                                .First().Source == _fileIcon ? Unit.File : Unit.Directory
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
                NewMessageBox(e, file, false, ButtonContent.None);
            else
            {
                if (_lastPercentage != e.Part) _messageBox.ChangeContent(e, file);
            }

            _lastPercentage = e.Part;
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
                NewMessageBox(e, file, false, ButtonContent.None);
            else
            {
                if (_lastPercentage != e.Part) _messageBox.ChangeContent(e, file);
            }

            _lastPercentage = e.Part;
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
    }
}