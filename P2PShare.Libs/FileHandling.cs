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
                    byte[] buffer = new byte[Math.Min(FileTransport.BufferSize, fileLength - totalBytesRead) + SymmetricCryptography.TagSize];
                    byte[]? decryptedBuffer;

                    // get nonce
                    await networkStream.ReadAsync(nonce, 0, FileTransport.NonceSize);

                    await FileTransport.SendAck(networkStream);

                    // get chunk
                    await networkStream.ReadAsync(buffer, 0, buffer.Length);

                    decryptedBuffer = SymmetricCryptography.Decrypt(buffer, aesKey, nonce);

                    if (decryptedBuffer is not null)
                    {
                        await FileTransport.SendAck(networkStream, true);

                        await fileStream.WriteAsync(decryptedBuffer, 0, decryptedBuffer.Length);

                        totalBytesRead += decryptedBuffer.Length;

                        FileTransport.OnFilePartReceived(CalculatePercentage(fileLength, totalBytesRead));

                        continue;
                    }

                    await FileTransport.SendAck(networkStream, false);
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
