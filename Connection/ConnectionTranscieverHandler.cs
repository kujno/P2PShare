using P2PShare.Libs;
using P2PShare.Libs.Models.Exceptions;
using System.IO;
using System.Net;

namespace P2PShare.Connection
{
    public class ConnectionTranscieverHandler : ConnectionHandler
    {
        public static event EventHandler<IPAddress>? Contacted;

        public required IPAddress IPRemote
        {
            get => _ipRemote!;
            init => _ipRemote = value;
        }

        private void OnContacted(IPAddress ip) => Contacted?.Invoke(this, ip);

        public async override Task SendFilesAsync(FileInfo[] files, bool encrypted)
        {
            try
            {
                int port;

                if (!files.All(x => x.Exists)) throw new FileNotFoundException("One or more files to send were not found.");

                OnContacted(_ipRemote!);
                using (Client = await ConnectAsync(_initialPort, false))
                {
                    await YNSendAsync(false, encrypted);
                    if (encrypted) await ReceiveEncryptionKeyAsync();

                    if (!await SendInviteAsync(files, encrypted)) throw new FileTransportDeniedException("File transport was denied.");

                    port = await SendPortAsync(encrypted);
                }

                using (Client = await ConnectAsync(port, false)) await base.SendFilesAsync(files, encrypted);
            }
            catch (Exception ex)
            {
                throw (ex is OperationCanceledException ||
                    ex is FileNotFoundException ||
                    ex is FileTransportDeniedException ||
                    ex is ConnectionFailedException ||
                    ex is CouldNotOpenFileException) ? ex : new Exception("Sending file(s) failed.", ex);
            }
        }
    }
}
