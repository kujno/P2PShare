using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PShare.Libs
{
    public class FileTransport
    {
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

            byte[] fileBytes;

            try
            {
                fileBytes = File.ReadAllBytes(fileInfo.FullName);
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
                stream.Write(fileBytes, 0, fileBytes.Length);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static async Task<string?> ReceiveInvite(TcpClient client)
        {
            NetworkStream stream;

            try
            {
                stream = client.GetStream();
            }
            catch
            {
                return null;
            }

            byte[] buffer = new byte[1024];
            int bytesRead;
            try
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                return null;
            }

            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        public static FileInfo? ReceiveFile(TcpClient client, int fileLength, string filePath)
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

            try
            {
                FileHandling.CreateFile(networkStream, filePath, fileLength);
            }
            catch
            {
                return null;
            }

            return new FileInfo(filePath);
        }

        public static void Reply(TcpClient client, bool accepted)
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
                stream.Write(replyBytes, 0, replyBytes.Length);
            }
            catch
            {
                return;
            }
        }

        private static byte[] createInvite(FileInfo fileInfo)
        {
            return Encoding.UTF8.GetBytes($"File: {fileInfo.Name} ({fileInfo.Length} bytes)\nDo you want to accept it? [y/n]: ");
        }
    }
}
