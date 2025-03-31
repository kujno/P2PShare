using System.Net.Sockets;
using System.Text;

namespace P2PShare.Libs
{
    public class FileTransport
    {
        public static event EventHandler<string?>? InviteReceived;
        public static event EventHandler? FileBeingReceived;
        public static event EventHandler? TransferFailed;
        public static event EventHandler? FileBeingSent;
        public static event EventHandler<int>? FilePartReceived;
        public static event EventHandler<int>? FilePartSent;

        public static async Task<bool> SendFile(TcpClient[] clients, FileInfo fileInfo)
        {
            NetworkStream[] stream = new NetworkStream[2];

            try
            {
                for (int i = 0; i < stream.Length; i++)
                {
                    stream[i] = clients[i].GetStream();
                }
            }
            catch
            {
                return false;
            }

            byte[] inviteBytes = createInvite(fileInfo);
            byte[] buffer = new byte[Encoding.UTF8.GetBytes("y").Length];

            try
            {
                await stream[1].WriteAsync(inviteBytes, 0, inviteBytes.Length);
            }
            catch
            {
                return false;
            }
            
            try
            {
                await stream[1].ReadAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                return false;
            }
            
            if (Encoding.UTF8.GetString(buffer) != "y")
            {
                return false;
            }

            try
            {
                int bytesRead;
                int bytesSent = 0;
                byte[] buffer2 = new byte[8192];
                using FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
                
                onFileBeingSent();

                while ((bytesRead = await fileStream.ReadAsync(buffer2, 0, buffer2.Length)) > 0)
                {
                    await stream[0].WriteAsync(buffer2, 0, bytesRead);

                    bytesSent += bytesRead;
                    OnFilePartSent(FileHandling.CalculatePercentage(fileInfo.Length, bytesSent));
                }
            }
            catch
            {
                onTransferFailed();

                return false;
            }

            return true;
        }

        public static async Task ReceiveInvite(TcpClient client)
        {
            NetworkStream stream;

            try
            {
                stream = client.GetStream();
            }
            catch
            {
                return;
            }

            byte[] buffer = new byte[1024];
            int bytesRead;
            try
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch
            {                
                return;
            }

            onInviteReceived(Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }

        public static async Task<FileInfo?> ReceiveFile(TcpClient client, int fileLength, string filePath)
        {
            NetworkStream networkStream;

            try
            {
                networkStream = client.GetStream();
            }
            catch
            {
                return null;
            }

            onFileBeingReceived();

            try
            {
                await FileHandling.CreateFile(networkStream, filePath, fileLength);
            }
            catch
            {
                onTransferFailed();

                return null;
            }

            return new FileInfo(filePath);
        }

        public static async Task Reply(TcpClient client, bool accepted)
        {
            NetworkStream stream;

            try
            {
                stream = client.GetStream();
            }
            catch
            {
                return;
            }

            string reply;

            switch (accepted)
            {
                case true:
                    reply = "y";

                    break;
                case false:
                    reply = "n";
                    
                    break;
            }
            
            byte[] replyBytes = Encoding.UTF8.GetBytes(reply);

            await stream.FlushAsync();

            try
            {
                await stream.WriteAsync(replyBytes, 0, replyBytes.Length);
            }
            catch
            {
            }
        }

        private static byte[] createInvite(FileInfo fileInfo)
        {
            return Encoding.UTF8.GetBytes($"{fileInfo.Name} ({fileInfo.Length}B)\nAccept?");
        }

        private static void onInviteReceived(string? invite)
        {
            InviteReceived?.Invoke(null, invite);
        }

        public static int GetFileLenghtFromInvite(string invite)
        {
            return int.Parse(invite.Substring(invite.IndexOf('(') + 1, invite.LastIndexOf('B') - invite.IndexOf('(') - 1));
        }

        private static void onFileBeingReceived()
        {
            FileBeingReceived?.Invoke(null, EventArgs.Empty);
        }

        private static void onTransferFailed()
        {
            TransferFailed?.Invoke(null, EventArgs.Empty);
        }

        private static void onFileBeingSent()
        {
            FileBeingSent?.Invoke(null, EventArgs.Empty);
        }

        public static void OnFilePartReceived(int percentage)
        {
            FilePartReceived?.Invoke(null, percentage);
        }

        public static void OnFilePartSent(int percentage)
        {
            FilePartSent?.Invoke(null, percentage);
        }
    }
}
