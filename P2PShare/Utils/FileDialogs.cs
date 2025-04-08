namespace P2PShare.GUI.Utils
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

        public static string? SelectFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            bool? selected = dialog.ShowDialog();

            if (selected == true)
            {
                return dialog.FileName;
            }

            return null;
        }
    }
}
