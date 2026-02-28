using P2PShare.Libs;
using P2PShare.Libs.Models.FileSytem;
using P2PShare.Libs.Models.Requests;
using P2PShare.Models;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace P2PShare.Connection
{
    public class ConnectionToServerHandler : ConnectionHandler
    {
        public AllUserInfo? UserInfo { get; set; } = null;
        public bool IsConnected { get => Client is not null; }

        public required IPAddress IPServer
        {
            get
            {
                return _ipRemote
                    ?? throw new ArgumentNullException();
            }

            init => _ipRemote = value;
        }

        private Task? _monitoring;

        public async Task ConnectAsync()
        {
            int port;

            using (Client = await ConnectAsync(_initialServerPort, true))
            {
                await SendEncryptionKeyAsync();

                port = await ReceivePortAsync(true);
            }

            Client = await ConnectAsync(port, true);

            _monitoring = MonitorConnection();
        }

        public async Task MonitorConnection()
        {
            try
            {
                while (!(Client.Client.Poll(0, SelectMode.SelectRead) && Client.Client.Available == 0) && !CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(1000, CancellationToken);
                    }
                    catch (OperationCanceledException) { }
                }
            }
            catch { }

            Dispose();
            _client = null;
        }

        public async Task<bool> LogInAsync(string username, string password)
        {
            var successful = await SendRequestYNAsync(new Request()
            {
                Tag = Tag.Login,
                Username = username,
                Password = password
            }.ToJSON());

            if (successful)
                await GetUserInfoAsync();

            return successful;
        }

        public async Task GetUserInfoAsync() => UserInfo = AllUserInfo.Deserialize(await ReceiveInfoAsync());

        public async Task GetAsync()
        {
            await SendInfoAsync(new Request()
            {
                Tag = Tag.Get
            }.ToJSON());

            await GetUserInfoAsync();
        }

        public async Task<bool> NewFolderAsync(string folderName, bool my)
        {
            return await SendRequestYNAsync(new Request()
            {
                Tag = Tag.AddFolder,
                FileName = folderName,
                My = my,
                Unit = Unit.Directory
            }.ToJSON());
        }

        public async Task<bool> DeleteAsync(FileUnit unit)
        {
            return await SendRequestYNAsync(new Request()
            {
                Tag = Tag.DeleteFile,
                FileName = unit.Path,
                My = unit.My,
                Unit = unit.Unit
            }.ToJSON());
        }

        public async Task<bool> UploadAsync(string path, bool my, bool encrypted, FileInfo file)
        {
            var fileName = $"{(path != String.Empty ? $"{path}\\" : String.Empty)}{file.Name}";
            var check = await SendRequestYNAsync(new Request()
            {
                Tag = Tag.Upload,
                FileName = fileName,
                My = my,
                FileSize = file.Length,
                Encrypted = encrypted
            }.ToJSON());

            if (check)
                await SendFilesAsync([file], encrypted);

            return check;
        }

        public async Task<string?> DownloadAsync(FileUnit unit, string dirPath, bool encrypted)
        {
            var check = await SendRequestYNAsync(new Request()
            {
                Tag = Tag.Download,
                FileName = unit.Path,
                My = unit.My,
                FileSize = unit.Size,
                Encrypted = encrypted,
                Unit = unit.Unit
            }.ToJSON());
            var indexOfSeparator = unit.Path.LastIndexOf('\\');
            string? downloadedFile = null;

            if (check)
            {
                var fileToDownload = await ReceiveInviteAsync<Dictionary<string, long>>(true);

                await YNSendAsync(true);

                downloadedFile = (await ReceiveFilesAsync(fileToDownload, dirPath, encrypted)).First();
            }

            return downloadedFile;
        }

        public async Task<bool> EditSharesAsync(Share[] shares, string path, bool my, Unit unit)
        {
            return await SendRequestYNAsync(new Request()
            {
                Tag = Tag.ChangeSharing,
                Shares = shares,
                FileName = path,
                My = my,
                Unit = unit
            }.ToJSON());
        }

        public async Task<bool> CreateGroupAsync(string name)
        {
            return await SendRequestYNAsync(new Request()
            {
                Tag = Tag.CreateGroup,
                Name = name
            }.ToJSON());
        }

        public async Task<bool> DeleteGroupAsync(Group group)
        {
            return await SendRequestYNAsync(new Request()
            {
                Tag = Tag.DeleteGroup,
                Group = group
            }.ToJSON());
        }

        public async Task<bool> EditGroupAsync(Group group)
        {
            return await SendRequestYNAsync(new Request()
            {
                Tag = Tag.EditGroup,
                Group = group
            }.ToJSON());
        }
    }
}
