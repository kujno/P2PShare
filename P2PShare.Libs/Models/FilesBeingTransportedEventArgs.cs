using P2PShare.Models;

namespace P2PShare.Libs.Models
{
    public class FilesBeingTransportedEventArgs : EventArgs
    {
        public FileInfo[] FileInfos { get; }
        public Receive_Send ReceiveSend { get; }

        public FilesBeingTransportedEventArgs(FileInfo[] fileInfos, Receive_Send receiveSend)
        {
            FileInfos = fileInfos;
            ReceiveSend = receiveSend;
        }
    }
}
