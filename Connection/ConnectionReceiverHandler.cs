using P2PShare.Libs;

namespace P2PShare.Connection
{
    public class ConnectionReceiverHandler : ConnectionHandler
    {
        private Dictionary<string, long>? _filesAndSizes;

        public bool Encrypted { get; private set; }

        public async Task<Dictionary<string, long>> ReceiveInviteAsync()
        {
            try
            {
                Client = await ReceiveTcpClientAsync(_initialPort);

                Encrypted = await YNReceiveAsync(false);

                if (Encrypted) await SendEncryptionKeyAsync();

                _filesAndSizes = await ReceiveInviteAsync<Dictionary<string, long>>(Encrypted);
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

            await YNSendAsync(Encrypted);

            port = await ReceivePortAsync(Encrypted);

            Client.Dispose();
            using (Client = await ReceiveTcpClientAsync(port)) return await ReceiveFilesAsync(_filesAndSizes!, dictionaryPath, Encrypted);
        }

        public async Task RejectFilesAsync()
        {
            using (Client) await YNSendAsync(Encrypted, false);
        }
    }
}
