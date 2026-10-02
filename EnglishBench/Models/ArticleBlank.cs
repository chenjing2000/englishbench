using System.IO;
using System.Text.RegularExpressions;

namespace EnglishBench.Models;

public sealed class ArticleBlank : Article
{
    public IReadOnlyList<int> Placeholders { get; }

    public ArticleBlank(IReadOnlyList<Segment[]> paragraphs, int nextSid) : base(paragraphs, nextSid, true)
    {
        var numbers = new List<int>();
        foreach (var segment in Segments)
        {
            if (segment.Audio != null) throw new InvalidDataException("ArticleBlank 不允许 Segment 音频。");
            foreach (Match match in Regex.Matches(segment.Text, @"\[\[([0-9]+)\]\]"))
            {
                if (!int.TryParse(match.Groups[1].Value, out int number) || number < 1)
                    throw new InvalidDataException("占位符编号无效。");
                numbers.Add(number);
            }
            string remaining = Regex.Replace(segment.Text, @"\[\[([0-9]+)\]\]", "");
            if (remaining.Contains("[[") || remaining.Contains("]]"))
                throw new InvalidDataException("ArticleBlank 占位符结构无效。");
        }
        if (numbers.Count == 0 || !numbers.Order().SequenceEqual(Enumerable.Range(1, numbers.Count)))
            throw new InvalidDataException("占位符编号必须唯一、从 1 连续。");
        Placeholders = numbers.ToArray();
    }
}
