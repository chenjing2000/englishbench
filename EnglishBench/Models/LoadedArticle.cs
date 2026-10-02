using System.IO;

namespace EnglishBench.Models;

public sealed record LoadedArticle(string FilePath, string Title, Article Content,
    string VocabularyPath, VocabularySnapshot Vocabulary, IReadOnlyList<string> Warnings)
{
    public string Directory => Path.GetDirectoryName(FilePath)!;
    public IReadOnlyList<Segment[]> Paragraphs => Content.Paragraphs;
    public IEnumerable<Segment> Segments => Content.Segments;
    public bool IsBlank => Content is ArticleBlank;
}
