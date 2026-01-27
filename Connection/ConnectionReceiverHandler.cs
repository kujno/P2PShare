using P2PShare.Libs;
using System.Net;

namespace P2PShare.Connection
{
    public class ConnectionReceiverHandler(IPAddress ipLocal, CancellationToken cancellationToken) : ConnectionHandler(ipLocal, cancellationToken)
    {
        private Dictionary<string, long>? _filesAndSizes;
        private bool _encrypted;

        public async Task<Dictionary<string, long>> ReceiveInviteAsync()
        {
            try
            {
                Client = await ReceiveTcpClientAsync(_ipLocal!, _initialPort);

                _encrypted = await YNReceiveAsync(false);

                if (_encrypted) await SendEncryptionKeyAsync();

                _filesAndSizes = await ReceiveInviteAsync<Dictionary<string, long>>(_encrypted);
            }
            catch (Exception ex)
            {
                throw (ex is OperationCanceledException) ? ex : new Exception(InviteErrorMessage, ex);
            }

            return _filesAndSizes;
        }

        public async Task<string[]> ReceiveFilesAsync(string dictionaryPath)
        {
            int port;

            await YNSendAsync(_encrypted);

            port = await ReceivePortAsync(_encrypted);

            Client.Dispose();
            using (Client = await ReceiveTcpClientAsync(_ipLocal!, port)) return await ReceiveFilesAsync(_filesAndSizes!, dictionaryPath, _encrypted);
        }

        public async Task RejectFilesAsync()
        {
            using (Client) await YNSendAsync(_encrypted, false);
        }
    }
}
