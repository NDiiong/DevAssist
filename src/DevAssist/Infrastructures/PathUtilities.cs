using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DevAssist
{
    public static class PathUtilities
    {
        public const char AltDirectorySeparatorChar = '/';
        public const char VolumeSeparatorChar = ':';
        public const string ParentRelativeDirectory = "..";
        public const string ThisDirectory = ".";
        public static char DirectorySeparatorChar => Path.DirectorySeparatorChar;

        public static string DirectorySeparatorStr = new string(DirectorySeparatorChar, 1);
        public static bool IsUnixLikePlatform => Path.DirectorySeparatorChar == '/';
        public static bool IsWindows => Path.DirectorySeparatorChar == '\\';
        public static bool IsDirectorySeparator(char c) => c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;

        public static bool IsAnyDirectorySeparator(char c) => c == '\\' || c == '/';

        public static string TrimTrailingSeparators(string s)
        {
            int lastSeparator = s.Length;
            while (lastSeparator > 0 && IsDirectorySeparator(s[lastSeparator - 1]))
            {
                lastSeparator -= 1;
            }

            if (lastSeparator != s.Length)
            {
                s = s.Substring(0, lastSeparator);
            }

            return s;
        }

        public static string EnsureTrailingSeparator(string s)
        {
            if (s.Length == 0 || IsAnyDirectorySeparator(s[s.Length - 1]))
            {
                return s;
            }

            bool hasSlash = s.IndexOf('/') >= 0;
            bool hasBackslash = s.IndexOf('\\') >= 0;
            if (hasSlash && !hasBackslash)
            {
                return s + '/';
            }
            else if (!hasSlash && hasBackslash)
            {
                return s + '\\';
            }
            else
            {
                return s + DirectorySeparatorChar;
            }
        }

        public static string GetExtension(string path)
        {
            return FileNameUtilities.GetExtension(path);
        }

        public static ReadOnlyMemory<char> GetExtension(ReadOnlyMemory<char> path)
        {
            return FileNameUtilities.GetExtension(path);
        }

        public static string ChangeExtension(string path, string extension)
        {
            return FileNameUtilities.ChangeExtension(path, extension);
        }

        public static string RemoveExtension(string path)
        {
            return FileNameUtilities.ChangeExtension(path, extension: null);
        }

        public static string GetFileName(string path, bool includeExtension = true)
        {
            return FileNameUtilities.GetFileName(path, includeExtension);
        }

        public static string GetDirectoryName(string path)
        {
            return GetDirectoryName(path, IsUnixLikePlatform);
        }

        internal static string GetDirectoryName(string path, bool isUnixLike)
        {
            if (path != null)
            {
                var rootLength = GetPathRoot(path, isUnixLike).Length;
                if (path.Length > rootLength)
                {
                    var i = path.Length;
                    while (i > rootLength)
                    {
                        i--;
                        if (IsDirectorySeparator(path[i]))
                        {
                            if (i > 0 && IsDirectorySeparator(path[i - 1]))
                            {
                                continue;
                            }

                            break;
                        }
                    }

                    return path.Substring(0, i);
                }
            }

            return null;
        }

        internal static bool IsSameDirectoryOrChildOf(string child, string parent)
        {
            parent = RemoveTrailingDirectorySeparator(parent);
            string currentChild = child;
            while (currentChild != null)
            {
                currentChild = RemoveTrailingDirectorySeparator(currentChild);

                if (currentChild.Equals(parent, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                currentChild = GetDirectoryName(currentChild);
            }

            return false;
        }

        public static string GetPathRoot(string path)
        {
            return GetPathRoot(path, IsUnixLikePlatform);
        }

        private static string GetPathRoot(string path, bool isUnixLike)
        {
            if (path == null)
            {
                return null;
            }

            if (isUnixLike)
            {
                return GetUnixRoot(path);
            }
            else
            {
                return GetWindowsRoot(path);
            }
        }

        private static string GetWindowsRoot(string path)
        {
            int length = path.Length;
            if (length >= 1 && IsDirectorySeparator(path[0]))
            {
                if (length < 2 || !IsDirectorySeparator(path[1]))
                {
                    return path.Substring(0, 1);
                }

                int i = 2;
                i = ConsumeDirectorySeparators(path, length, i);

                bool hitSeparator = false;
                while (true)
                {
                    if (i == length)
                    {
                        return path;
                    }

                    if (!IsDirectorySeparator(path[i]))
                    {
                        i++;
                        continue;
                    }

                    if (!hitSeparator)
                    {
                        hitSeparator = true;
                        i = ConsumeDirectorySeparators(path, length, i);
                        continue;
                    }

                    return path.Substring(0, i);
                }
            }
            else if (length >= 2 && path[1] == VolumeSeparatorChar)
            {
                return length >= 3 && IsDirectorySeparator(path[2])
                    ? path.Substring(0, 3)
                    : path.Substring(0, 2);
            }
            else
            {
                return "";
            }
        }

        private static int ConsumeDirectorySeparators(string path, int length, int i)
        {
            while (i < length && IsDirectorySeparator(path[i]))
            {
                i++;
            }

            return i;
        }

        private static string GetUnixRoot(string path)
        {
            return path.Length > 0 && IsDirectorySeparator(path[0])
                ? path.Substring(0, 1)
                : "";
        }

        public static PathKind GetPathKind(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return PathKind.Empty;
            }

            if (IsAbsolute(path))
            {
                return PathKind.Absolute;
            }

            if (path.Length > 0 && path[0] == '.')
            {
                if (path.Length == 1 || IsDirectorySeparator(path[1]))
                {
                    return PathKind.RelativeToCurrentDirectory;
                }

                if (path[1] == '.')
                {
                    if (path.Length == 2 || IsDirectorySeparator(path[2]))
                    {
                        return PathKind.RelativeToCurrentParent;
                    }
                }
            }

            if (!IsUnixLikePlatform)
            {
                if (path.Length >= 1 && IsDirectorySeparator(path[0]))
                {
                    return PathKind.RelativeToCurrentRoot;
                }

                if (path.Length >= 2 && path[1] == VolumeSeparatorChar && (path.Length <= 2 || !IsDirectorySeparator(path[2])))
                {
                    return PathKind.RelativeToDriveDirectory;
                }
            }

            return PathKind.Relative;
        }

        public static bool IsAbsolute(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (IsUnixLikePlatform)
            {
                return path[0] == DirectorySeparatorChar;
            }

            if (IsDriveRootedAbsolutePath(path))
            {
                return true;
            }

            return path.Length >= 2 &&
                IsDirectorySeparator(path[0]) &&
                IsDirectorySeparator(path[1]);
        }

        private static bool IsDriveRootedAbsolutePath(string path)
        {
            Debug.Assert(!IsUnixLikePlatform);
            return path.Length >= 3 && path[1] == VolumeSeparatorChar && IsDirectorySeparator(path[2]);
        }

        public static string CombineAbsoluteAndRelativePaths(string root, string relativePath)
        {
            Debug.Assert(IsAbsolute(root));

            return CombinePossiblyRelativeAndRelativePaths(root, relativePath);
        }

        public static string CombinePossiblyRelativeAndRelativePaths(string root, string relativePath)
        {
            if (string.IsNullOrEmpty(root))
            {
                return null;
            }

            switch (GetPathKind(relativePath))
            {
                case PathKind.Empty:
                    return root;

                case PathKind.Absolute:
                case PathKind.RelativeToCurrentRoot:
                case PathKind.RelativeToDriveDirectory:
                    return null;
            }

            return CombinePathsUnchecked(root, relativePath);
        }

        public static string CombinePathsUnchecked(string root, string relativePath)
        {
            char c = root[root.Length - 1];
            if (!IsDirectorySeparator(c) && c != VolumeSeparatorChar)
            {
                return root + DirectorySeparatorStr + relativePath;
            }

            return root + relativePath;
        }

        public static string CombinePaths(string root, string path)
        {
            if (string.IsNullOrEmpty(root))
            {
                return path;
            }

            if (string.IsNullOrEmpty(path))
            {
                return root;
            }

            return IsAbsolute(path) ? path : CombinePathsUnchecked(root, path);
        }

        private static string RemoveTrailingDirectorySeparator(string path)
        {
            if (path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]))
            {
                return path.Substring(0, path.Length - 1);
            }
            else
            {
                return path;
            }
        }

        public static bool IsFilePath(string assemblyDisplayNameOrPath)
        {
            string extension = FileNameUtilities.GetExtension(assemblyDisplayNameOrPath);
            return string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
                || assemblyDisplayNameOrPath.IndexOf(DirectorySeparatorChar) != -1
                || assemblyDisplayNameOrPath.IndexOf(AltDirectorySeparatorChar) != -1;
        }

        public static bool ContainsPathComponent(string path, string component, bool ignoreCase)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (path?.IndexOf(component, comparison) >= 0)
            {
                var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

                int count = 0;
                string currentPath = path;
                while (currentPath != null)
                {
                    var currentName = GetFileName(currentPath);
                    if (comparer.Equals(currentName, component))
                    {
                        return true;
                    }

                    currentPath = GetDirectoryName(currentPath);
                    count++;
                }
            }

            return false;
        }

        public static string GetRelativePath(string directory, string fullPath)
        {
            string relativePath = string.Empty;

            directory = TrimTrailingSeparators(directory);
            fullPath = TrimTrailingSeparators(fullPath);

            if (IsChildPath(directory, fullPath))
            {
                return GetRelativeChildPath(directory, fullPath);
            }

            var directoryPathParts = GetPathParts(directory);
            var fullPathParts = GetPathParts(fullPath);

            if (directoryPathParts.Length == 0 || fullPathParts.Length == 0)
            {
                return fullPath;
            }

            int index = 0;

            for (; index < directoryPathParts.Length; index++)
            {
                if (!PathsEqual(directoryPathParts[index], fullPathParts[index]))
                {
                    break;
                }
            }

            if (index == 0)
            {
                return fullPath;
            }

            var remainingParts = directoryPathParts.Length - index;
            if (remainingParts > 0)
            {
                for (int i = 0; i < remainingParts; i++)
                {
                    relativePath = relativePath + ParentRelativeDirectory + DirectorySeparatorStr;
                }
            }

            for (int i = index; i < fullPathParts.Length; i++)
            {
                relativePath = CombinePathsUnchecked(relativePath, fullPathParts[i]);
            }

            return relativePath;
        }

        public static bool IsChildPath(string parentPath, string childPath)
        {
            return parentPath.Length > 0
                && childPath.Length > parentPath.Length
                && PathsEqual(childPath, parentPath, parentPath.Length)
                && (IsDirectorySeparator(parentPath[parentPath.Length - 1]) || IsDirectorySeparator(childPath[parentPath.Length]));
        }

        private static string GetRelativeChildPath(string parentPath, string childPath)
        {
            var relativePath = childPath.Substring(parentPath.Length);

            int start = ConsumeDirectorySeparators(relativePath, relativePath.Length, 0);
            if (start > 0)
            {
                relativePath = relativePath.Substring(start);
            }

            return relativePath;
        }

        private static readonly char[] s_pathChars = new char[] { VolumeSeparatorChar, DirectorySeparatorChar, AltDirectorySeparatorChar };

        private static string[] GetPathParts(string path)
        {
            var pathParts = path.Split(s_pathChars);

            if (pathParts.Contains(ThisDirectory))
            {
                pathParts = pathParts.Where(s => s != ThisDirectory).ToArray();
            }

            return pathParts;
        }

        public static bool PathsEqual(string path1, string path2)
        {
            return PathsEqual(path1, path2, Math.Max(path1.Length, path2.Length));
        }

        private static bool PathsEqual(string path1, string path2, int length)
        {
            if (path1.Length < length || path2.Length < length)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (!PathCharEqual(path1[i], path2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PathCharEqual(char x, char y)
        {
            if (IsDirectorySeparator(x) && IsDirectorySeparator(y))
            {
                return true;
            }

            return IsUnixLikePlatform
                ? x == y
                : char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
        }

        public static string NormalizePathPrefix(string filePath, ImmutableArray<KeyValuePair<string, string>> pathMap)
        {
            if (pathMap.IsDefaultOrEmpty)
            {
                return filePath;
            }

            foreach (var kv in pathMap)
            {
                var oldPrefix = kv.Key;
                if (!(oldPrefix?.Length > 0)) continue;

                if (filePath.StartsWith(oldPrefix, StringComparison.Ordinal))
                {
                    var replacementPrefix = kv.Value;

                    var replacement = replacementPrefix + filePath.Substring(oldPrefix.Length);

                    bool hasSlash = replacementPrefix.IndexOf('/') >= 0;
                    bool hasBackslash = replacementPrefix.IndexOf('\\') >= 0;
                    return
                        (hasSlash && !hasBackslash) ? replacement.Replace('\\', '/') :
                        (hasBackslash && !hasSlash) ? replacement.Replace('/', '\\') :
                        replacement;
                }
            }

            return filePath;
        }

        public static bool IsValidFilePath(string fullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullPath))
                {
                    return false;
                }

                var fileInfo = new FileInfo(fullPath);
                return !string.IsNullOrEmpty(fileInfo.Name);
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is PathTooLongException ||
                ex is NotSupportedException)
            {
                return false;
            }
        }

        public static string NormalizeWithForwardSlash(string p)
            => DirectorySeparatorChar == '/' ? p : p.Replace(DirectorySeparatorChar, '/');
    }

    public enum PathKind
    {
        Empty,
        Relative,
        RelativeToCurrentDirectory,
        RelativeToCurrentParent,
        RelativeToCurrentRoot,
        RelativeToDriveDirectory,
        Absolute,
    }
}