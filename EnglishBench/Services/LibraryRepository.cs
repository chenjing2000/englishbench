using EnglishBench.Models;
using System.IO;
namespace EnglishBench.Services;
public sealed class LibraryRepository
{
    public LibrarySnapshot Load(string root)
    {
        string absolute = Path.GetFullPath(root);
        if (!Directory.Exists(absolute)) throw new DirectoryNotFoundException("图书馆目录不存在。");
        var warnings = new List<string>();
        var books = new List<NavigationNode>();
        foreach (var child in Directory.EnumerateDirectories(absolute).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (Skip(child) || !File.Exists(Path.Combine(child, "book.json"))) continue;
            var children = Scan(child, child, warnings);
            if (children.Count > 0) books.Add(new NavigationNode(Path.GetFileName(child), child, null, children));
            else warnings.Add($"{Path.GetFileName(child)}：没有正文文件。");
        }
        return new LibrarySnapshot(absolute, books, warnings);
    }

    private static List<NavigationNode> Scan(string directory, string bookRoot, List<string> warnings)
    {
        var nodes = new List<NavigationNode>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(file);
                if (Skip(file) || name.Equals("book.json", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".vocabulary.json", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exercise.json", StringComparison.OrdinalIgnoreCase)) continue;
                nodes.Add(new NavigationNode(Path.GetFileNameWithoutExtension(file), bookRoot, Path.GetFullPath(file), Array.Empty<NavigationNode>()));
            }
            foreach (var child in Directory.EnumerateDirectories(directory).Order(StringComparer.OrdinalIgnoreCase))
            {
                if (Skip(child)) continue;
                var children = Scan(child, bookRoot, warnings);
                if (children.Count > 0) nodes.Add(new NavigationNode(Path.GetFileName(child), bookRoot, null, children));
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { warnings.Add(error.Message); }
        return nodes.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase).ThenBy(n => n.IsPassage).ToList();
    }

    private static bool Skip(string path)
    {
        string name = Path.GetFileName(path);
        var attributes = File.GetAttributes(path);
        return name.StartsWith('.') || name.Equals("userdata", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("__pycache__", StringComparison.OrdinalIgnoreCase) ||
            (attributes & (FileAttributes.ReparsePoint | FileAttributes.Hidden)) != 0;
    }
}
