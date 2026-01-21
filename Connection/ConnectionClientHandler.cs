using P2PShare.Libs;
using P2PShare.Libs.Models;
using System.IO;
using System.Net;

namespace P2PShare.Connection
{
    public class ConnectionClientHandler(IPAddress ipRemote, IPAddress ipLocal, CancellationToken cancellationToken) : ConnectionHandler(ipRemote, ipLocal, cancellationToken)
    {
        public static event EventHandler<IPAddress>? Contacted;

        private Dictionary<string, long>? _filesAndSizes;
        private bool _encrypted;

        private void OnContacted(IPAddress ip) => Contacted?.Invoke(this, ip);

        public async Task SendAsync(FileInfo[] files, bool encrypted)
        {
            try
            {
                int port;

                if (!files.All(x => x.Exists)) throw new FileNotFoundException("One or more files to send were not found.");

                OnContacted(_ipRemote);
                using (Client = await ConnectAsync(_initialPort, false))
                {
                    await YNSendAsync(false, encrypted);
                    if (encrypted) await ReceiveEncryptionKeyAsync();

                    if (!await SendInviteAsync(files, encrypted)) throw new FileTransportDeniedException("File transport was denied.");

                    port = await SendPortAsync(encrypted);
                }

                using (Client = await ConnectAsync(port, false))
                {
                    await SendFilesAsync(files, encrypted);
                }
            }
            catch (Exception ex)
            {
                throw (ex is OperationCanceledException ||
                    ex is FileNotFoundException ||
                    ex is FileTransportDeniedException ||
                    ex is ConnectionFailedException) ? ex : new Exception("Sending file(s) failed.", ex);
            }
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