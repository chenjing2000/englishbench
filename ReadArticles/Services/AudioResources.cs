using ReadArticles.Models;
using ReadArticles.Infrastructure;
using System.IO;
namespace ReadArticles.Services;
public sealed class AudioResources
{
    public string ArticleFile(string root)
    {
        string? first = Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .FirstOrDefault();
        if (first == null) throw new FileNotFoundException("文章同级目录没有 MP3 音频文件。");
        return ResourcePaths.Resolve(root, Path.GetFileName(first));
    }

    public string Segment(string root, Segment segment, string accent)
    {
        string relative = segment.Audio?.For(accent) ?? throw new InvalidDataException("ArticleBlank 没有正文音频。");
        return ResourcePaths.Resolve(root, relative);
    }
    public string Vocabulary(string root, VocabularyEntry entry, string accent) => ResourcePaths.Resolve(root, entry.Audio.For(accent));
    public static bool IsAvailable(string path)
    {
        try { return File.Exists(path) && new FileInfo(path).Length > 0; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
