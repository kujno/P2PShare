using P2PShare.Libs;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PShare.CLI
{
    public class CLIFileTransport
    {
        private static async void receiveInviteLoop(TcpClient client)
        {
            while (true)
            {
                CancellationTokenSource cts = new CancellationTokenSource();

                string? invite = null;
                
                FileTransport.ReceiveInvite(client, cts.Token);

                if (invite is null)
                {
                    continue;
                }

                bool accepted = CLIHelp.GetBool(invite);

                FileTransport.Reply(client, accepted);

                if (!accepted)
                {
                    Console.WriteLine("The file was not accepted\n");

                    return;
                }

                int indexOfBracket = invite.IndexOf('(') + 1;
                int indexOfColon = invite.IndexOf(':') + 2;
                int fileLength = int.Parse(invite.Substring(indexOfBracket, invite.IndexOf("bytes") - indexOfBracket - 1));
                string filePath = CLIHelp.GetDirectoryInfo("Insert the directory file path where to save the file: ").FullName + "\\" + invite.Substring(indexOfColon, invite.IndexOf('(') - 1 - indexOfColon);
                FileInfo? fileInfo;

                Console.Clear();
                Console.WriteLine("The file will be received in a while...");

                fileInfo = await FileTransport.ReceiveFile(client, fileLength, filePath);

                if (fileInfo is null)
                {
                    Console.WriteLine("The file transfer failed\n");

                    return;
                }

                Console.Clear();
                Console.WriteLine($"The file {fileInfo.Name} was received successfully\n");

                CLIHelp.PrintFileInfo(fileInfo);

                return;
            }
        }

        public static async void Sharing(NetworkInterface @interface)
        {
            int? port;
            TcpClient client;
            bool sent;

            port = CLIHelp.GetNullablePortInt("If you would like to wait for a connection type a port number\nIf not press [Enter] key\n\nType a port number: ", @interface);
            Console.WriteLine();

            client = CLIConnection.GetClient(port, @interface).Result;

            switch (CLIHelp.GetBool("Would you like to send[y] or receive[n]: "))
            {
                case true:
                    FileInfo fileInfo = CLIHelp.GetFileInfo("Insert the file path to send the file: ");

                    sent = await FileTransport.SendFile(client, fileInfo);

                    switch (sent)
                    {
                        case true:
                            Console.WriteLine($"The file {fileInfo.Name} was sent successfully\n");

                            break;

                        case false:
                            Console.WriteLine("The file transfer failed\n");

                            break;
                    }

                    break;

                case false:
                    
                    
                    Console.WriteLine("Waiting for file share invite...\n");
                    
                    receiveInviteLoop(client);

                    break;
            }
        }

        public static void SharingLoop()
        {
            int i = 0;

            do
            {
                Sharing(CLIInterfaceHandling.GetInterface(i));

                i++;
            }
            while (CLIHelp.GetBool("Would you like to send/receive any other file? [y/n]: "));
        }
    }
}
