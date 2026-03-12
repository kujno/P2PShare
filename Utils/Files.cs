using System.IO;

namespace P2PShare.Utils
{
    public static class Files
    {
        public static bool CheckAccess(FileInfo[] files)
        {
            return files.All(file =>
            {
                try
                {
                    using (var stream = file.Open(FileMode.Open))
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
