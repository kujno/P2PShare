using System.IO;
using System.Net;

namespace P2PShare
{
    public static class ServerIP
    {
        private static readonly string filePath = "ServerIP.txt";

        public static async Task SetAsync(IPAddress ip) => await File.WriteAllTextAsync(filePath, ip.ToString());

        public static async Task<IPAddress?> GetAsync()
        {
            IPAddress? ip;

            if (File.Exists(filePath)
                && IPAddress.TryParse(await File.ReadAllTextAsync(filePath), out ip))
            {
                return ip;
            }
            else return null;
        }
    }
}
