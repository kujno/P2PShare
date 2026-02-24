using P2PShare.Libs;
using P2PShare.Libs.Models.FileSytem;
using P2PShare.Libs.Models.Requests;
using System.Net;
using System.Net.Sockets;

namespace P2PShare.Connection
{
    public class ConnectionToServerHandler : ConnectionHandler
    {
        public static event EventHandler? Disconnected;

        public AllUserInfo? UserInfo { get; private set; } = null;
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

            Disconnected?.Invoke(this, EventArgs.Empty);
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
    }
}
