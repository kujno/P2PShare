using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace P2PShare.Libs
{
    public class FileTransport
    {
        public static event EventHandler<string?>? InviteReceived;
        public static event EventHandler? FileBeingReceived;
        public static event EventHandler? FileBeingSent;
        public static event EventHandler<int>? FilePartReceived;
        public static event EventHandler<int>? FilePartSent;
        public static int BufferSize { get; } = 8192;
        private static int AesKeySize { get; } = 32;
        public static byte[] Ack { get; } = Encoding.UTF8.GetBytes("y");
        private static char Separator { get; } = '<';

        public static async Task<bool> SendFile(TcpClient[] clients, FileInfo fileInfo)
        {
            int yLength = Encoding.UTF8.GetBytes("y").Length;
            int modulusLength;
            int exponentLength;
            int? rsaKeyLength = EncryptionAsymmetrical.GetPublicKeyLength(out modulusLength, out exponentLength);

            if (rsaKeyLength is null)
            {
                return false;
            }

            NetworkStream[] streams = new NetworkStream[2];
            int rsaKeyLengthNoNull = (int)rsaKeyLength;
            byte[] inviteBytes = createInvite(fileInfo);
            byte[] buffer = new byte[Ack.Length];
            byte[] rsaKey = new byte[rsaKeyLengthNoNull];

            try
            {
                streams = ClientHandling.GetStreamsFromTcpClients(clients);

                await streams[1].WriteAsync(inviteBytes, 0, inviteBytes.Length);

                Task delay = Task.Delay(10000);

                if (await Task.WhenAny(ack(streams[0]), delay) == delay)
                {
                    return false;
                }

                // get invite response
                await streams[0].ReadAsync(buffer, 0, Ack.Length);

                if (Encoding.UTF8.GetString(buffer) == "n")
                {
                    return false;
                }

                await SendAck(streams[0]);

                await streams[0].ReadAsync(rsaKey, 0, rsaKey.Length);

                int bytesRead;
                int bytesSent = 0;
                using FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
                byte[] aesKey = new byte[AesKeySize];
                byte[] aesKeyEncrypted;
                byte[] buffer2;
                byte[] modulus = new byte[modulusLength];
                byte[] exponent = new byte[exponentLength];
                EncryptorAsymmetrical encryptorAsymmetrical;
                EncryptionSymmetrical cryptographySymmetrical;

                RandomNumberGenerator.Fill(aesKey);
                cryptographySymmetrical = new(aesKey);

                Array.Copy(rsaKey, 0, modulus, 0, modulusLength);
                Array.Copy(rsaKey, modulusLength, exponent, 0, exponentLength);
                encryptorAsymmetrical = new(modulus, exponent);
                aesKeyEncrypted = encryptorAsymmetrical.Encrypt(aesKey);

                onFileBeingSent();

                await streams[0].WriteAsync(aesKeyEncrypted, 0, aesKeyEncrypted.Length);

                while ((bytesRead = await fileStream.ReadAsync(buffer2 = new byte[Math.Min(BufferSize, fileInfo.Length - bytesSent)], 0, buffer2.Length)) > 0)
                {
                    bool ackBool;

                    do
                    {
                        byte[] encryptedData = cryptographySymmetrical.Encrypt(buffer2);

                        // send chunk
                        await streams[0].WriteAsync(encryptedData, 0, encryptedData.Length);

                        ackBool = await ack(streams[0]);

                        await streams[0].FlushAsync();
                    }
                    while (!ackBool);

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

        public static async Task ReceiveInvite(TcpClient?[] clients)
        {
            if (!ClientConnection.AreClientsConnected(clients))
            {
                return;
            }

            NetworkStream[] streams;
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                streams = ClientHandling.GetStreamsFromTcpClients(clients!);

                bytesRead = await streams[1].ReadAsync(buffer, 0, buffer.Length);

                await SendAck(streams[0]);

                onInviteReceived(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }
            catch
            {
                onInviteReceived(null);
            }
        }

        public static async Task<FileInfo?> ReceiveFile(TcpClient client, int fileLength, string filePath, DecryptorAsymmetrical decryptographer)
        {
            NetworkStream stream;
            byte[] aesKey = new byte[AesKeySize];
            byte[] buffer;
            RSAParameters exampleKey = EncryptionAsymmetrical.GenerateKeys()[0];

            if (EncryptionAsymmetrical.IsPublicKeyNull(exampleKey))
            {
                return null;
            }
            buffer = new byte[exampleKey.Modulus!.Length];

            try
            {
                stream = client.GetStream();

                onFileBeingReceived();

                await stream.ReadAsync(buffer, 0, buffer.Length);
                aesKey = decryptographer.Decrypt(buffer);
                await Task.Delay(10);
                EncryptionSymmetrical encryption = new(aesKey);

                await FileHandling.CreateFile(stream, filePath, fileLength, encryption);
            }
            catch (Exception)
            {
                return null;
            }

            return new FileInfo(filePath);
        }

        public static async Task Reply(TcpClient client, bool accepted)
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

                await stream.WriteAsync(replyBytes, 0, replyBytes.Length);
            }
            catch
            {
            }
        }

        public static async Task SendRSAPublicKey(NetworkStream stream, RSAParameters rsaParameters)
        {
            if (EncryptionAsymmetrical.IsPublicKeyNull(rsaParameters))
            {
                return;
            }

            byte[] buffer = rsaParameters.Modulus!.Concat(rsaParameters.Exponent!).ToArray();

            await ack(stream);

            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private static byte[] createInvite(FileInfo fileInfo)
        {
            return Encoding.UTF8.GetBytes($"{fileInfo.Name} {Separator}{fileInfo.Length}B>\nAccept?");
        }

        private static void onInviteReceived(string? invite)
        {
            InviteReceived?.Invoke(null, invite);
        }

        public static int GetFileLenghtFromInvite(string invite)
        {
            int separatorIndex = invite.IndexOf(Separator);

            return int.Parse(invite.Substring(separatorIndex + 1, invite.LastIndexOf('B') - separatorIndex - 1));
        }

        public static string GetFileNameFromInvite(string invite)
        {
            return invite.Substring(0, invite.IndexOf($" {Separator}"));
        }

        private static void onFileBeingReceived()
        {
            FileBeingReceived?.Invoke(null, EventArgs.Empty);
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

        private static async Task<bool> ack(NetworkStream stream)
        {
            byte[] buffer = new byte[Ack.Length];

            await stream.ReadAsync(buffer, 0, Ack.Length);

            if (buffer.SequenceEqual(Ack))
            {
                return true;
            }

            return false;
        }

        public static async Task SendAck(NetworkStream stream, bool yN)
        {
            byte[] ackBuffer = new byte[Ack.Length];

            switch (yN)
            {
                case true:
                    ackBuffer = Ack;

                    break;

                default:
                    ackBuffer = Encoding.UTF8.GetBytes("n");

                    break;
            }

            await stream.WriteAsync(ackBuffer, 0, Ack.Length);
        }

        public static async Task SendAck(NetworkStream stream)
        {
            await SendAck(stream, true);
        }
    }
}
