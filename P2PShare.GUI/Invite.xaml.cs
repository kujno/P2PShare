using System.Windows;

namespace P2PShare.GUI
{
    /// <summary>
    /// Interaction logic for Invite.xaml
    /// </summary>
    public partial class Invite : Window
    {
        private bool _accepted;
        
        public bool Accepted 
        {
            get 
            { 
                return _accepted; 
            }
        }

        public Invite()
        {
            InitializeComponent();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            _accepted = false;

            Close();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            _accepted = true;

            Close();
        }
    }
}
