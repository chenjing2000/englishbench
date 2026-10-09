using System.IO;
namespace ReadArticles.Infrastructure;

public static class ResourcePaths
{
    public static string Resolve(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(':'))
            throw new ArgumentException("Resource path must be a relative path.");
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(fullRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Resource path escapes the passage directory.");
        // Do not follow junctions/symlinks out of the expected root.
        string? current = path;
        while (current is not null)
        {
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Resource path traverses a reparse point.");
            current = Path.GetDirectoryName(current);
        }
        return path;
    }
}
