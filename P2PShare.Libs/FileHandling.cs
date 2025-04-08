using System;
using System.Net.Sockets;
using System.Text;

namespace P2PShare.Libs
{
    public class FileHandling
    {
        public static async Task CreateFile(NetworkStream networkStream, string filePath, int fileLength, byte[] aesKey)
        {
            using (FileStream fileStream = new FileStream(filePath, getFileMode(filePath)))
            {
                int totalBytesRead = 0;
                byte[] nonce = new byte[FileTransport.NonceSize];

                while (totalBytesRead < fileLength)
                {
                    byte[] ack = Encoding.UTF8.GetBytes(".");

                    await networkStream.WriteAsync(ack, 0, ack.Length);
                    await networkStream.ReadAsync(nonce, 0, nonce.Length);

                    byte[] buffer = new byte[FileTransport.BufferSize + SymmetricCryptography.TagSize];
                    byte[] decryptedBuffer;

                    await networkStream.WriteAsync(ack, 0, ack.Length);
                    await networkStream.ReadAsync(buffer, 0, Math.Min(buffer.Length, fileLength - totalBytesRead + SymmetricCryptography.TagSize));

                    decryptedBuffer = SymmetricCryptography.Decrypt(buffer, aesKey, nonce);

                    await fileStream.WriteAsync(decryptedBuffer, 0, decryptedBuffer.Length);

                    totalBytesRead += decryptedBuffer.Length;

                    FileTransport.OnFilePartReceived(CalculatePercentage(fileLength, totalBytesRead));
                }
            }
        }

        public static int CalculatePercentage(long total, long part)
        {
            return (int)(part / (total / 100));
        }

        private static FileMode getFileMode(string path)
        {
            if (File.Exists(path))
            {
                return FileMode.Create;
            }

            return FileMode.CreateNew;
        }
    }
}
