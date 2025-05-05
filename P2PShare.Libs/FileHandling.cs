using System.Net.Sockets;

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
                    await networkStream.ReadAsync(nonce, 0, FileTransport.NonceSize);

                    byte[] buffer = new byte[FileTransport.BufferSize + SymmetricCryptography.TagSize];
                    byte[] decryptedBuffer;

                    await networkStream.ReadAsync(buffer, 0, Math.Min(FileTransport.BufferSize, fileLength - totalBytesRead));

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
