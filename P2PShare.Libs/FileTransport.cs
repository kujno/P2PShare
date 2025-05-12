using P2PShare.Libs.Models;
using P2PShare.Models;
using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace P2PShare.Libs
{
    public class FileTransport
    {
        public static event EventHandler<string?>? InviteReceived;
        public static event EventHandler<FilesBeingTransportedEventArgs>? FilesBeingTransported;
        public static event EventHandler<int>? FilePartTransported;
        public static int BufferSize { get; } = 8192;
        private static int AesKeySize { get; } = 32;
        public static byte[] Ack { get; } = Encoding.UTF8.GetBytes("y");
        private static char FileLengthSymbol { get; } = '<';
        public static char FileSeparator { get; } = '|';

        public static async Task<bool> SendFile(TcpClient[] clients, FileInfo[] fileInfos)
        {
            int modulusLength;
            int exponentLength;
            int? rsaKeyLength = EncryptionAsymmetrical.GetPublicKeyLength(out modulusLength, out exponentLength);

            if (rsaKeyLength is null)
            {
                return false;
            }

            NetworkStream[] streams = new NetworkStream[2];
            byte[] inviteBytes = createInvite(fileInfos);
            byte[] buffer = new byte[Ack.Length];
            byte[] rsaKey = new byte[(int)rsaKeyLength];
            bool response;

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
                response = await ack(streams[0]);

                if (!response)
                {
                    return false;
                }

                EncryptorAsymmetrical encryptorAsymmetrical;
                byte[] modulus = new byte[modulusLength];
                byte[] exponent = new byte[exponentLength];

                await SendAck(streams[0]);

                await streams[0].ReadAsync(rsaKey, 0, rsaKey.Length);

                Array.Copy(rsaKey, 0, modulus, 0, modulusLength);
                Array.Copy(rsaKey, modulusLength, exponent, 0, exponentLength);
                encryptorAsymmetrical = new(modulus, exponent);

                onFilesBeingTransported(new(fileInfos, Receive_Send.Send));

                for (int i = 0; i < fileInfos.Length; i++)
                {
                    int bytesRead;
                    int bytesSent = 0;
                    using FileStream fileStream = new FileStream(fileInfos[i].FullName, FileMode.Open, FileAccess.Read);
                    byte[] aesKey = new byte[AesKeySize];
                    byte[] aesKeyEncrypted;
                    byte[] buffer2;
                    EncryptionSymmetrical cryptographySymmetrical;

                    RandomNumberGenerator.Fill(aesKey);
                    cryptographySymmetrical = new(aesKey);

                    aesKeyEncrypted = encryptorAsymmetrical.Encrypt(aesKey);

                    await streams[0].WriteAsync(aesKeyEncrypted, 0, aesKeyEncrypted.Length);

                    await ack(streams[0]);

                    while ((bytesRead = await fileStream.ReadAsync(buffer2 = new byte[Math.Min(BufferSize, fileInfos[i].Length - bytesSent)], 0, buffer2.Length)) > 0)
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
                        OnFilePartTransported(FileHandling.CalculatePercentage(fileInfos[i].Length, bytesSent));
                    }
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
            if (!ConnectionClient.AreClientsConnected(clients))
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

        public static async Task<FileInfo[]?> ReceiveFile(TcpClient client, string[] filePaths, int[] fileLengths, DecryptorAsymmetrical decryptor)
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

            FileInfo[] fileInfos = new FileInfo[filePaths.Length];

            try
            {
                stream = client.GetStream();

                for (int i = 0; i < fileInfos.Length; i++)
                {
                    fileInfos[i] = new FileInfo(filePaths[i]);
                }

                onFilesBeingTransported(new(fileInfos, Receive_Send.Receive));

                for (int i = 0; i < fileInfos.Length; i++)
                {
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                    aesKey = decryptor.Decrypt(buffer);
                    EncryptionSymmetrical encryption = new(aesKey);

                    await FileHandling.CreateFile(stream, filePaths[i], fileLengths[i], encryption);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return fileInfos;
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

        private static byte[] createInvite(FileInfo[] fileInfos)
        {
            string invite = string.Empty;

            for (int i = 0; i < fileInfos.Length; i++)
            {
                invite += $"{fileInfos[i].Name} {FileLengthSymbol}{fileInfos[i].Length}B>";

                if (fileInfos.Length > 1 && i != fileInfos.Length - 1)
                {
                    invite += FileSeparator;
                }
            }
            return Encoding.UTF8.GetBytes(invite);
        }

        private static void onInviteReceived(string? invite)
        {
            InviteReceived?.Invoke(null, invite);
        }

        public static int[] GetLenghtsFromFiles(string[] files)
        {
            int[] lengths = new int[files.Length];

            for (int i = 0; i < lengths.Length; i++)
            {
                int separatorIndex = files[i].IndexOf(FileLengthSymbol);
                lengths[i] = int.Parse(files[i].Substring(separatorIndex + 1, files[i].LastIndexOf('B') - separatorIndex - 1));
            }

            return lengths;
        }

        public static string[] GetNamesFromFiles(string[] files)
        {
            string[] names = new string[files.Length];

            for (int i = 0; i < names.Length; i++)
            {
                names[i] = files[i].Substring(0, files[i].IndexOf($" {FileLengthSymbol}"));
            }

            return names;
        }

        private static void onFilesBeingTransported(FilesBeingTransportedEventArgs filesBeingTransportedEventArgs)
        {
            FilesBeingTransported?.Invoke(null, filesBeingTransportedEventArgs);
        }

        public static void OnFilePartTransported(int percentage)
        {
            FilePartTransported?.Invoke(null, percentage);
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
