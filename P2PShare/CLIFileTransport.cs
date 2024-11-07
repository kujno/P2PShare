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
        private static async void receiveInviteLoop(TcpClient client)
        {
            while (true)
            {
                string? invite = await FileTransport.ReceiveInvite(client);

                if (invite is null)
                {
                    Console.WriteLine("The connection was lost\n");

                    continue;
                }

                bool accepted = CLIHelp.GetBool(invite);

                if (!accepted)
                {
                    Console.WriteLine("The file was not accepted\n");

                    continue;
                }

                int indexOfClosure = invite.IndexOf('(') + 1;
                int fileLength = int.Parse(invite.Substring(indexOfClosure, invite.IndexOf(')') - indexOfClosure));
                string filePath = CLIHelp.GetFileInfo("Insert the file path to save the file: ").FullName;
                FileInfo? fileInfo;

                Console.Clear();
                Console.WriteLine("The file will be received in a while...");

                fileInfo = FileTransport.ReceiveFile(client, fileLength, filePath);

                if (fileInfo is null)
                {
                    Console.WriteLine("The file transfer failed\n");

                    continue;
                }

                Console.Clear();
                Console.WriteLine("The file was received successfully\n");

                CLIHelp.PrintFileInfo(fileInfo);
            }
        }

        public static void Sharing(int? port, NetworkInterfaceType interfaceType)
        {
            TcpClient client = CLIConnection.GetClient(port, interfaceType).Result;
            bool sent;

            receiveInviteLoop(client);

            sent = FileTransport.SendFile(client, CLIHelp.GetFileInfo("Insert the file path to send the file or wait for a file transfer invite: "));

            switch (sent)
            {
                case true:
                    Console.WriteLine("The file was sent successfully\n");

                    break;

                case false:
                    Console.WriteLine("The file transfer failed\n");

                    break;
            }
        }
    }
}
