using System.Net.Sockets;

namespace P2PShare.Libs
{
    public class FileHandling
    {
        public static void CreateFile(NetworkStream networkStream, string filePath, int fileLength)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                int totalBytesRead = 0;

                while (totalBytesRead < fileLength)
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead = networkStream.Read(buffer, 0, Math.Min(buffer.Length, fileLength - totalBytesRead));

                    fileStream.Write(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                }
            }
        }
    }
}
