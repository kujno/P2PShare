namespace P2PShare.Utils
{
    public class FileDialogs
    {
        public static string? SelectFolder(out bool? selected)
        {
            Microsoft.Win32.OpenFolderDialog dialog = new();

            setSelectFolderDialog(dialog);

            selected = dialog.ShowDialog();

            if (selected == true)
            {
                return dialog.FolderName;
            }

            return null;
        }

        private static Microsoft.Win32.OpenFolderDialog setSelectFolderDialog(Microsoft.Win32.OpenFolderDialog dialog)
        {
            dialog.Multiselect = false;
            dialog.Title = "Select a folder";

            return dialog;
        }

        public static string[]? SelectFiles()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;
            bool? selected = dialog.ShowDialog();

            if (selected == true)
            {
                return dialog.FileNames;
            }

            return null;
        }
    }
}
