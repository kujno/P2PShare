using System.Windows;

namespace P2PShare.Utils
{
    public static class AppHelper
    {
        public static void CloseAppForServer()
        {
            new CustomMessageBox("Disconnected from server.\nRestart the application.", Models.ButtonContent.OK).ShowDialog();

            Application.Current.Shutdown();
        }
    }
}
