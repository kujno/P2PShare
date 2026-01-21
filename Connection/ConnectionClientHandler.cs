using P2PShare.Libs;
using P2PShare.Libs.Models;
using System.IO;
using System.Net;

namespace P2PShare.Connection
{
    public abstract class ConnectionClientHandler : ConnectionHandler
    {
        

        private Dictionary<string, long>? _filesAndSizes;
        private bool _encrypted;

        

        protected ConnectionClientHandler(IPAddress ipLocal, IPAddress ipRemote, CancellationToken cancellationToken) : base(ipLocal, ipRemote, cancellationToken)
        {
        }

        protected ConnectionClientHandler(IPAddress ipLocal, CancellationToken cancellationToken) : base(ipLocal, cancellationToken)
        {
        }

        

        public async Task<Dictionary<string, long>> ReceiveInviteAsync()
        {
            try
            {
                Client = await ReceiveTcpClientAsync(_ipLocal, _initialPort);

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
            var port = await ReceivePortAsync(_encrypted);

            Client.Dispose();
            using (Client = await ReceiveTcpClientAsync(_ipLocal, port))
            {
                return await ReceiveFilesAsync(_filesAndSizes!, dictionaryPath, _encrypted);
            }
        }
    }
}