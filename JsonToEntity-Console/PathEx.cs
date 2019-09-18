
namespace JsonToEntity
{
    public static class PathEx
    {
        public static string GetNormalized(this string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new System.ArgumentNullException("path不允许为空");

            path = path.Replace('/', '\\');
            if (path.IsDirectory())
            {
                if (path.LastIndexOf('\\') != path.Length - 1)
                    path += "\\";
            }

            return path;
        }

        public static bool IsRoot(this string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return System.IO.Path.GetPathRoot(path) == path;
        }

        public static bool IsRelativePath(this string path)
        {
            return string.IsNullOrEmpty(System.IO.Path.GetPathRoot(path));
        }

        public static bool IsDirectory(this string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            System.IO.FileAttributes attr = System.IO.File.GetAttributes(path);
            return (attr & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory;
        }
    }
}
