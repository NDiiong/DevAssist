namespace DevAssist
{
    public static class FileNameUtilities
    {
        public const char DirectorySeparatorChar = '\\';
        public const char AltDirectorySeparatorChar = '/';
        public const char VolumeSeparatorChar = ':';

        public static bool IsFileName(string path)
        {
            return IndexOfFileName(path) == 0;
        }

        public static int IndexOfExtension(string path) =>
            path is null
                ? -1
                : IndexOfExtension(path.AsSpan());

        public static int IndexOfExtension(ReadOnlySpan<char> path)
        {
            int length = path.Length;
            int i = length;

            while (--i >= 0)
            {
                char c = path[i];
                if (c == '.')
                {
                    if (i != length - 1)
                    {
                        return i;
                    }

                    return -1;
                }

                if (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar || c == VolumeSeparatorChar)
                {
                    break;
                }
            }

            return -1;
        }

        public static string GetExtension(string path)
        {
            if (path == null)
            {
                return null;
            }

            int index = IndexOfExtension(path);
            return (index >= 0) ? path.Substring(index) : string.Empty;
        }

        public static ReadOnlyMemory<char> GetExtension(ReadOnlyMemory<char> path)
        {
            int index = IndexOfExtension(path.Span);
            return (index >= 0) ? path.Slice(index) : default;
        }

        public static string RemoveExtension(string path)
        {
            if (path == null)
            {
                return null;
            }

            int index = IndexOfExtension(path);
            if (index >= 0)
            {
                return path.Substring(0, index);
            }

            if (path.Length > 0 && path[path.Length - 1] == '.')
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }

        public static string ChangeExtension(string path, string extension)
        {
            if (path == null)
            {
                return null;
            }

            var pathWithoutExtension = RemoveExtension(path);
            if (extension == null || path.Length == 0)
            {
                return pathWithoutExtension;
            }

            if (extension.Length == 0 || extension[0] != '.')
            {
                return pathWithoutExtension + "." + extension;
            }

            return pathWithoutExtension + extension;
        }

        public static int IndexOfFileName(string path)
        {
            if (path == null)
            {
                return -1;
            }

            for (int i = path.Length - 1; i >= 0; i--)
            {
                char ch = path[i];
                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        public static string GetFileName(string path, bool includeExtension = true)
        {
            int fileNameStart = IndexOfFileName(path);
            var fileName = (fileNameStart <= 0) ? path : path.Substring(fileNameStart);
            return includeExtension ? fileName : RemoveExtension(fileName);
        }
    }
}