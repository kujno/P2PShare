using Microsoft.Win32;

namespace P2PShare.Utils
{
    public class FileDialogs
    {
        public static string? SelectFolder(out bool selected)
        {
            OpenFolderDialog dialog = new();

            dialog.Multiselect = false;
            dialog.Title = "Select a folder";

            selected = dialog.ShowDialog() ?? false;

            return selected == true ? dialog.FolderName : null;
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
