using Microsoft.Win32;

namespace P2PShare.Utils
{
    public class FileDialogs
    {
        public static string? SelectFolder()
        {
            OpenFolderDialog dialog = new();

            dialog.Multiselect = false;
            dialog.Title = "Select a folder";

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        public static string[]? SelectFiles()
        {
            OpenFileDialog dialog = new();
            dialog.Multiselect = true;
            bool? selected = dialog.ShowDialog();

            return selected == true ? dialog.FileNames : null;
        }
    }
}
