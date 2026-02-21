using System.Windows;

namespace P2PShare.Utils
{
    public static class AppHelper
    {
        public static void CloseAppForServer(Window window)
        {
            new CustomMessageBox("Disconnected from server.\nRestart the application.", Models.ButtonContent.OK, window).ShowDialog();

            Application.Current.Shutdown();
        }
    }
}
