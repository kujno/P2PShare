using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace P2PShare.Libs
{
    public class FileTransport
    {
        public static event EventHandler<string?>? InviteReceived;
        public static event EventHandler? FileBeingReceived;
        public static event EventHandler? TransferFailed;
        public static event EventHandler? FileBeingSent;
        public static event EventHandler<int>? FilePartReceived;
        public static event EventHandler<int>? FilePartSent;
        public static int BufferSize { get; } = 8192;
        private static int AesKeySize { get; } = 32;
        public static int NonceSize { get; } = 12;
        public static byte[] Ack { get; } = Encoding.UTF8.GetBytes("y");

        public static async Task<bool> SendFile(TcpClient[] clients, FileInfo fileInfo)
        {
            int yLength = Encoding.UTF8.GetBytes("y").Length;
            int modulusLength;
            int exponentLength;
            int? rsaKeyLength = AsymmetricCryptography.GetKeyLength(false, out modulusLength, out exponentLength);

            if (rsaKeyLength is null)
            {
                return false;
            }

            NetworkStream[] streams = new NetworkStream[2];
            int rsaKeyLengthNoNull = (int)rsaKeyLength;
            byte[] inviteBytes = createInvite(fileInfo);
            byte[] buffer = new byte[yLength + rsaKeyLengthNoNull];
            byte[] rsaKey = new byte[rsaKeyLengthNoNull];

            try
            {
                streams = ClientHandling.GetStreamsFromTcpClients(clients);

                await streams[1].WriteAsync(inviteBytes, 0, inviteBytes.Length);
                await streams[0].ReadAsync(buffer, 0, buffer.Length);

                if (Encoding.UTF8.GetString(buffer) == "n")
                {
                    return false;
                }

                int bytesRead;
                int bytesSent = 0;
                RSAParameters rsaParameters = new();
                using FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
                byte[] aesKey = new byte[AesKeySize];
                byte[] aesKeyEncrypted;
                byte[] buffer2 = new byte[BufferSize];
                byte[] oldNonce = new byte[NonceSize];

                RandomNumberGenerator.Fill(aesKey);

                rsaKey = getKeyFromBuffer(buffer, yLength, rsaKeyLengthNoNull);

                rsaParameters.Modulus = new byte[modulusLength];
                rsaParameters.Exponent = new byte[exponentLength];
                Array.Copy(rsaKey, 0, rsaParameters.Modulus, 0, modulusLength);
                Array.Copy(rsaKey, modulusLength, rsaParameters.Exponent, 0, exponentLength);

                aesKeyEncrypted = AsymmetricCryptography.Encrypt(aesKey, rsaParameters);

                onFileBeingSent();

                await streams[0].WriteAsync(aesKeyEncrypted, 0, aesKeyEncrypted.Length);

                while ((bytesRead = await fileStream.ReadAsync(buffer2, 0, buffer2.Length)) > 0)
                {
                    byte[] ackBuffer = new byte[Ack.Length];

                    do
                    {
                        byte[] encryptedData;
                        byte[] nonce = new byte[NonceSize];

                        do
                        {
                            RandomNumberGenerator.Fill(nonce);
                        }
                        while (nonce == oldNonce);

                        // send nonce
                        await streams[0].WriteAsync(nonce, 0, NonceSize);

                        encryptedData = SymmetricCryptography.Encrypt(buffer2, aesKey, nonce);

                        // ack nonce
                        await streams[0].ReadAsync(ackBuffer, 0, Ack.Length);

                        // send chunk
                        await streams[0].WriteAsync(encryptedData, 0, encryptedData.Length);

                        // ack
                        await streams[0].ReadAsync(ackBuffer, 0, Ack.Length);

                        oldNonce = nonce;
                    }
                    while (!ackBuffer.SequenceEqual(Ack));
                    
                    bytesSent += bytesRead;
                    OnFilePartSent(FileHandling.CalculatePercentage(fileInfo.Length, bytesSent));
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public static async Task ReceiveInvite(TcpClient client)
        {
            NetworkStream stream;
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                stream = client.GetStream();

                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                return;
            }

            onInviteReceived(Encoding.UTF8.GetString(buffer, 0, bytesRead));
        }

        public static async Task<FileInfo?> ReceiveFile(TcpClient client, int fileLength, string filePath, RSAParameters rsaParameters)
        {
            NetworkStream stream;
            byte[] aesKey = new byte[AesKeySize];
            byte[] buffer;
            
            RandomNumberGenerator.Fill(aesKey);
            buffer = new byte[AsymmetricCryptography.Encrypt(aesKey, AsymmetricCryptography.GenerateKeys()[0]).Length];

            try
            {
                stream = client.GetStream();

                onFileBeingReceived();

                await stream.ReadAsync(buffer, 0, buffer.Length);
                aesKey = AsymmetricCryptography.Decrypt(buffer, rsaParameters);

                await FileHandling.CreateFile(stream, filePath, fileLength, aesKey);
            }
            catch (Exception)
            {
                return null;
            }

            return new FileInfo(filePath);
        }

        public static async Task Reply(TcpClient client, bool accepted, RSAParameters rsaParameters)
        {
            NetworkStream stream;
            string reply;
            byte[] replyBytes;

            try
            {
                stream = client.GetStream();

                switch (accepted)
                {
                    case true:
                        reply = "y";

                        break;
                    case false:
                        reply = "n";

                        break;
                }
                replyBytes = Encoding.UTF8.GetBytes(reply);

                if (accepted && rsaParameters.Modulus is not null && rsaParameters.Exponent is not null)
                {
                    replyBytes = replyBytes.Concat(rsaParameters.Modulus).Concat(rsaParameters.Exponent).ToArray();
                }

                await stream.WriteAsync(replyBytes, 0, replyBytes.Length);
            }
            catch
            {
            }
        }

        private static byte[] createInvite(FileInfo fileInfo)
        {
            return Encoding.UTF8.GetBytes($"{fileInfo.Name} ({fileInfo.Length}B)\nAccept?");
        }

        private static void onInviteReceived(string? invite)
        {
            InviteReceived?.Invoke(null, invite);
        }

        public static int GetFileLenghtFromInvite(string invite)
        {
            return int.Parse(invite.Substring(invite.IndexOf('(') + 1, invite.LastIndexOf('B') - invite.IndexOf('(') - 1));
        }

        public static string GetFileNameFromInvite(string invite)
        {
            return invite.Substring(0, invite.IndexOf(" ("));
        }

        private static void onFileBeingReceived()
        {
            FileBeingReceived?.Invoke(null, EventArgs.Empty);
        }

        private static void onTransferFailed()
        {
            TransferFailed?.Invoke(null, EventArgs.Empty);
        }

        private static void onFileBeingSent()
        {
            FileBeingSent?.Invoke(null, EventArgs.Empty);
        }

        public static void OnFilePartReceived(int percentage)
        {
            FilePartReceived?.Invoke(null, percentage);
        }

        public static void OnFilePartSent(int percentage)
        {
            FilePartSent?.Invoke(null, percentage);
        }

        private static byte[] getKeyFromBuffer(byte[] buffer, int yLength, int keyLength)
        {
            byte[] output = new byte[keyLength];
            
            Array.Copy(buffer, yLength, output, 0, keyLength);

            return output;
        }
    }
}
