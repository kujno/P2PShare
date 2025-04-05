using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class FileHandling
    {
        public static async Task CreateFile(NetworkStream networkStream, string filePath, int fileLength)
        {
            using (FileStream fileStream = new FileStream(filePath, getFileMode(filePath)))
            {
                int totalBytesRead = 0;

                while (totalBytesRead < fileLength)
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead = await networkStream.ReadAsync(buffer, 0, Math.Min(buffer.Length, fileLength - totalBytesRead));

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;

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
