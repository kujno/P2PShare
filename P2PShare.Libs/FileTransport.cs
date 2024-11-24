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

            byte[] inviteBytes = Encoding.UTF8.GetBytes($"File: {fileInfo.Name} ({fileInfo.Length} bytes)\nDo you want to accept it? [y/n]: ");
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
            NetworkStream stream;

            try
            {
                stream = client.GetStream();
            }
            catch
            {
                return null;
            }

            byte[] reply = Encoding.UTF8.GetBytes("y");

            try
            {
                stream.Write(reply, 0, reply.Length);
            }
            catch
            {
                return null;
            }

            try
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Create);
                int totalBytesRead = 0;

                while (totalBytesRead < fileLength)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, fileLength - totalBytesRead));

                    fileStream.Write(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                }
            }
            catch
            {
                return null;
            }

            return new FileInfo(filePath);
        }
    }
}
