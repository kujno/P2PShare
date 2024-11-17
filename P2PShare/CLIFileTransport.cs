using P2PShare.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PShare.CLI
{
    public class CLIFileTransport
    {
        private static void receiveInviteLoop(TcpClient client)
        {
            while (true)
            {
                string? invite = FileTransport.ReceiveInvite(client).Result;

                if (invite is null)
                {
                    continue;
                }

                bool accepted = CLIHelp.GetBool(invite.Substring(0, invite.IndexOf('#')));

                if (!accepted)
                {
                    Console.WriteLine("The file was not accepted\n");

                    return;
                }

                int indexOfHashtag = invite.IndexOf('#') + 1;
                int fileLength = int.Parse(invite.Substring(indexOfHashtag, invite.LastIndexOf('#') - indexOfHashtag));
                string filePath = CLIHelp.GetNewFileInfo("Insert the file path where to save the file: ").FullName;
                FileInfo? fileInfo;

                Console.Clear();
                Console.WriteLine("The file will be received in a while...");

                fileInfo = FileTransport.ReceiveFile(client, fileLength, filePath);

                if (fileInfo is null)
                {
                    Console.WriteLine("The file transfer failed\n");

                    return;
                }

                Console.Clear();
                Console.WriteLine($"The file {fileInfo.Name} was received successfully\n");

                CLIHelp.PrintFileInfo(fileInfo);
            }
        }

        public static void Sharing(int? port, NetworkInterface @interface)
        {
            TcpClient client = CLIConnection.GetClient(port, @interface).Result;
            bool sent;


            switch (CLIHelp.GetBool("Would you like to send[y] or receive[n]: "))
            {
                case true:
                    FileInfo fileInfo = CLIHelp.GetExistingFileInfo("Insert the file path to send the file: ");

                    sent = FileTransport.SendFile(client, fileInfo);

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
    }
}
