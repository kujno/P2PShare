using P2PShare.Libs;
using System.Net;
using System.Net.Sockets;

namespace P2PShare.Connection
{
    public class ConnectionToServerHandler : ConnectionHandler
    {
        public static event EventHandler? Disconnected;

        private Task? _monitoring;

        public required IPAddress IPServer
        {
            get
            {
                return _ipRemote
                    ?? throw new ArgumentNullException();
            }

            set => _ipRemote = value;
        }

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
            while (!(Client.Client.Poll(0, SelectMode.SelectRead) && Client.Client.Available == 0) && !CancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, CancellationToken);
                }
                catch (OperationCanceledException) { }
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
