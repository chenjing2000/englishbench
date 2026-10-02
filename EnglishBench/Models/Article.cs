using System.IO;
using System.Text.RegularExpressions;

namespace EnglishBench.Models;

public class Article
{
    public int NextSid { get; }
    public IReadOnlyList<Segment[]> Paragraphs { get; }
    public IEnumerable<Segment> Segments => Paragraphs.SelectMany(paragraph => paragraph);

    public Article(IReadOnlyList<Segment[]> paragraphs, int nextSid) : this(paragraphs, nextSid, false) { }

    protected Article(IReadOnlyList<Segment[]> paragraphs, int nextSid, bool hasBlanks)
    {
        if (paragraphs.Count == 0 || paragraphs.Any(paragraph => paragraph.Length == 0))
            throw new InvalidDataException("正文及段落不能为空。");
        if (nextSid < 1 || nextSid > 1000) throw new InvalidDataException("next_sid 必须是 1–1000 的整数。");
        var ids = new HashSet<string>();
        int highestSid = 0;
        foreach (var paragraph in paragraphs)
        {
            foreach (var segment in paragraph)
            {
                if (!Regex.IsMatch(segment.Sid, @"^s[0-9]{3}$") || !ids.Add(segment.Sid))
                    throw new InvalidDataException("非法或重复 SID：" + segment.Sid);
                int sid = int.Parse(segment.Sid.Substring(1));
                if (sid == 0) throw new InvalidDataException("非法 SID：" + segment.Sid);
                highestSid = Math.Max(highestSid, sid);
                if (string.IsNullOrEmpty(segment.Text) || segment.Text != segment.Text.Trim())
                    throw new InvalidDataException(segment.Sid + " 正文为空或含首尾空白。");
                if (!hasBlanks && (segment.Text.Contains("[[") || segment.Text.Contains("]]") ||
                    segment.Audio?.Uk != $"audio_segments/{segment.Sid}_uk.mp3" ||
                    segment.Audio?.Us != $"audio_segments/{segment.Sid}_us.mp3"))
                    throw new InvalidDataException(segment.Sid + " 完整文章音频路径或正文结构无效。");
            }
        }
        if (nextSid <= highestSid) throw new InvalidDataException("next_sid 不大于已有 SID。");
        Paragraphs = paragraphs;
        NextSid = nextSid;
    }
}
