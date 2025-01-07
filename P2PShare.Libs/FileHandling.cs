using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class FileHandling
    {
        public static event EventHandler<int>? FilePartReceived;
        
        public static async Task CreateFile(NetworkStream networkStream, string filePath, int fileLength)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                int totalBytesRead = 0;

                while (totalBytesRead < fileLength)
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead = await networkStream.ReadAsync(buffer, 0, Math.Min(buffer.Length, fileLength - totalBytesRead));

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;

                    onFilePartReceived(calculatePercentage(fileLength, totalBytesRead));
                }
            }
        }

        private static void onFilePartReceived(int percentage)
        {
            FilePartReceived?.Invoke(null, percentage);
        }

        private static int calculatePercentage(int total, int part)
        {
            return part / (total / 100);
        }
    }
}
