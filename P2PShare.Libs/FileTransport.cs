using System.Net.Sockets;
using System.Text;

namespace P2PShare.Libs
{
    public class FileTransport
    {
        public static event EventHandler<string?>? InviteReceived;
        public static event EventHandler? FileBeingReceived;

        public static bool SendFile(TcpClient client, FileInfo fileInfo)
        {
            NetworkStream stream;

            try
            {
                stream = client.GetStream();
            }
            catch
            {
                return false;
            }

            byte[] inviteBytes = createInvite(fileInfo);
            byte[] buffer = new byte[Encoding.UTF8.GetBytes("y").Length];

            try
            {
                stream.Write(inviteBytes, 0, inviteBytes.Length);
            }
            catch
            {
                return false;
            }

            try
            {
                stream.Read(buffer, 0, buffer.Length);
            }
            catch
            {
                return false;
            }
            
            string reply = Encoding.UTF8.GetString(buffer);

            if (reply == "n")
            {
                return false;
            }

            try
            {
                int bytesRead;
                byte[] buffer2 = new byte[8192];
                using FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
                
                while ((bytesRead = fileStream.Read(buffer2, 0, buffer2.Length)) > 0)
                {
                    stream.Write(buffer2, 0, bytesRead);
                }
            }
            catch
            {
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
                onInviteReceived(null);

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
                onInviteReceived(null);

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

            try
            {
                await stream.WriteAsync(replyBytes, 0, replyBytes.Length);
            }
            catch
            {
                return;
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
            return int.Parse(invite.Substring(invite.IndexOf('(') + 1, invite.IndexOf("bytes") - invite.IndexOf('(')));
        }

        private static void onFileBeingReceived()
        {
            FileBeingReceived?.Invoke(null, EventArgs.Empty);
        }
    }
}
