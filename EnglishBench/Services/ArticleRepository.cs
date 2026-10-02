using EnglishBench.Models;
using EnglishBench.Infrastructure;
using System.IO;
using System.Text.Json;
namespace EnglishBench.Services;
public sealed class ArticleRepository
{
    public LoadedArticle Load(string path)
    {
        string absolute = Path.GetFullPath(path);
        string title = Path.GetFileNameWithoutExtension(absolute);
        using var json = JsonDocument.Parse(File.ReadAllText(absolute));
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object || JsonFiles.Text(root, "filetype") != "passage")
            throw new InvalidDataException("filetype 必须为 passage。");
        if (!root.TryGetProperty("next_sid", out var next) || next.ValueKind != JsonValueKind.Number || !next.TryGetInt32(out int nextSid) || nextSid < 1 || nextSid > 1000)
            throw new InvalidDataException("next_sid 必须是 1–1000 的整数。");
        var paragraphs = new List<Segment[]>();
        foreach (var paragraph in JsonFiles.Array(root, "paragraphs", true).EnumerateArray())
        {
            var segments = new List<Segment>();
            foreach (var item in JsonFiles.Array(paragraph, "paragraph", true).EnumerateArray())
            {
                string sid = JsonFiles.Text(item, "sid"), text = JsonFiles.Text(item, "text");
                AudioPaths? audio = null;
                if (item.TryGetProperty("audio", out var rawAudio))
                    audio = new AudioPaths(JsonFiles.Text(rawAudio, "uk"), JsonFiles.Text(rawAudio, "us"));
                segments.Add(new Segment(sid, text, audio));
            }
            paragraphs.Add(segments.ToArray());
        }
        bool blank = paragraphs.Any(paragraph => paragraph.Any(segment => segment.Text.Contains("[[")));
        Article content = blank ? new ArticleBlank(paragraphs, nextSid) : new Article(paragraphs, nextSid);
        string vocabularyPath = Path.ChangeExtension(absolute, ".vocabulary.json");
        var warnings = new List<string>();
        VocabularySnapshot vocabulary;
        try { vocabulary = new VocabularyRepository().Load(vocabularyPath); }
        catch (Exception error) when (error is JsonException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            warnings.Add("词汇表无法加载：" + error.Message);
            vocabulary = new VocabularySnapshot(new VocabularyData(), null, false, error.Message);
        }
        var resources = new AudioResources();
        int missing = 0;
        foreach (var segment in content.Segments.Where(s => s.Audio is not null))
            foreach (string accent in new[] { "uk", "us" })
            {
                var asset = resources.Segment(Path.GetDirectoryName(absolute)!, segment, accent);
                if (!AudioResources.IsAvailable(asset)) missing++;
            }
        if (missing > 0) warnings.Add($"缺失 {missing} 个正文音频；正文仍可阅读。");
        return new LoadedArticle(absolute, title, content, vocabularyPath, vocabulary, warnings);
    }
}
