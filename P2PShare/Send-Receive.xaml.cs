using System.Windows;
using System.Windows.Input;

namespace P2PShare.GUI
{
    /// <summary>
    /// Interaction logic for Send_Receive.xaml
    /// </summary>
    public partial class Send_Receive : Window
    {
        public Send_Receive()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}
