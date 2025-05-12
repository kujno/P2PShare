using P2PShare.Models;
using P2PShare.Utils;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace P2PShare
{
    /// <summary>
    /// Interaction logic for Send_Receive.xaml
    /// </summary>
    public partial class Send_Receive : Window
    {
        private Receive_Send _receiveSend;
        private int _filesCount;
        private int _i;
        private FileInfo[] _fileInfos;
        public bool Done { get; }
        
        public Send_Receive(Receive_Send receiveSend, FileInfo[] fileInfos)
        {
            InitializeComponent();
            _receiveSend = receiveSend;
            _i = 1;
            _fileInfos = fileInfos;
            Done = false;
            _filesCount = _fileInfos.Length;

            ChangeText(0);
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        public void ChangeText(int percentage)
        {
            if (percentage >= 100)
            {
                if (_i == _filesCount)
                {
                    Close();
                }

                _i++;
                percentage = 0;
            }
            
            string text = $"File: {_fileInfos[_i - 1].Name}";

            if (_filesCount > 1)
            {
                text += $" {_i}/{_filesCount}";
            }
            Text.Text = text + $"\n{Elements.Received_Sent(_receiveSend)}: {percentage}%";
        }
    }
}
