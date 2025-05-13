using P2PShare.Models;

namespace P2PShare.Libs.Models
{
    public class FilesBeingTransportedEventArgs : EventArgs
    {
        public FileInfo[] FileInfos { get; }
        public ReceiveSendEnum ReceiveSend { get; }

        public FilesBeingTransportedEventArgs(FileInfo[] fileInfos, ReceiveSendEnum receiveSend)
        {
            FileInfos = fileInfos;
            ReceiveSend = receiveSend;
        }
    }
}
