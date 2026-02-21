using P2PShare.Libs;
using System.Net;
using System.Net.Sockets;

namespace P2PShare.Connection
{
    public class ConnectionToServerHandler : ConnectionHandler
    {
        public event EventHandler? Disconnected;

        public required IPAddress IPServer
        {
            get
            {
                return _ipRemote
                    ?? throw new ArgumentNullException();
            }

            set => _ipRemote = value;
        }

        public async Task ConnectAsync() => Client = await ConnectAsync(_initialServerPort, true);

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
