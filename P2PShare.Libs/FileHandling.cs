using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class FileHandling
    {
        public static async Task CreateFile(NetworkStream networkStream, string filePath, int fileLength, EncryptionSymmetrical encryption)
        {
            using (FileStream fileStream = new FileStream(filePath, getFileMode(filePath)))
            {
                int totalBytesRead = 0;

                await FileTransport.SendAck(networkStream);

                while (totalBytesRead < fileLength)
                {
                    byte[] buffer = new byte[Math.Min(FileTransport.BufferSize, fileLength - totalBytesRead) + encryption.TagSize + encryption.NonceSize];
                    byte[]? decryptedBuffer;

                    // get chunk
                    await networkStream.ReadAsync(buffer, 0, buffer.Length);

                    decryptedBuffer = encryption.Decrypt(buffer);

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
