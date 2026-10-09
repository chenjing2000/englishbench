using ReadArticles.Models;
using ReadArticles.Infrastructure;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace ReadArticles.Services;
public sealed class VocabularyRepository
{
    public VocabularySnapshot Load(string path)
    {
        if (!File.Exists(path)) return new VocabularySnapshot(new VocabularyData(), null, true);
        byte[] bytes = File.ReadAllBytes(path);
        string content = System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object || JsonFiles.Text(root, "filetype") != "vocabulary")
            throw new InvalidDataException("filetype 必须为 vocabulary。");
        foreach (var entry in JsonFiles.Array(root, "words").EnumerateArray())
        {
            JsonFiles.Text(entry, "word");
            JsonFiles.Text(entry, "phonetic_uk", false);
            JsonFiles.Text(entry, "phonetic_us", false);
            foreach (var meaning in JsonFiles.Array(entry, "meanings").EnumerateArray())
            { JsonFiles.Text(meaning, "pos", false); JsonFiles.Text(meaning, "meaning", false); }
            if (!entry.TryGetProperty("audio", out var audio)) throw new InvalidDataException("词条缺少 audio。");
            JsonFiles.Text(audio, "uk"); JsonFiles.Text(audio, "us");
        }
        var data = JsonSerializer.Deserialize<VocabularyData>(content)!;
        Validate(data);
        return new VocabularySnapshot(data, JsonFiles.Fingerprint(bytes), true);
    }

    public string Save(string path, VocabularyData data, string? expectedFingerprint)
    {
        Validate(data);
        // Deny ordinary in-place writers while preparing the replacement. Release the
        // read handle immediately before rename, which Windows requires for overwrite.
        using var guard = File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete) : null;
        void CheckCurrent()
        {
            string? actual = File.Exists(path) ? JsonFiles.Fingerprint(path) : null;
            if (actual != expectedFingerprint) throw new InvalidOperationException("词汇表已被其他程序修改，请重新打开文章后再编辑。");
        }
        CheckCurrent();
        return JsonFiles.WriteAtomic(path, data, () => { CheckCurrent(); guard?.Dispose(); });
    }

    public static VocabularyEntry NewEntry(string text)
    {
        string normalized = Regex.Replace(text, @"\s+", " ").Trim();
        if (normalized.Length == 0) throw new InvalidDataException("选词不能为空。");
        string stem = AudioStem(normalized);
        return new VocabularyEntry
        {
            Word = normalized,
            Audio = new AudioPaths($"audio_vocabulary/{stem}_uk.mp3", $"audio_vocabulary/{stem}_us.mp3")
        };
    }

    public static string AudioStem(string word)
    {
        string stem = Regex.Replace(word.Trim().ToLowerInvariant(), @"\s+", "_");
        stem = Regex.Replace(stem, "[<>:\"/\\\\|?*]+", "_");
        stem = Regex.Replace(stem, "_+", "_").Trim('_');
        if (stem.Length == 0) throw new InvalidDataException("无法生成词条的音频文件名。");
        return stem;
    }

    public static void Validate(VocabularyData data)
    {
        if (data.FileType != "vocabulary" || data.Words is null) throw new InvalidDataException("词汇表结构无效。");
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in data.Words)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Word) || !words.Add(Regex.Replace(entry.Word, @"\s+", " ").Trim()))
                throw new InvalidDataException("词条为空或重复。");
            string stem = AudioStem(entry.Word);
            if (!stems.Add(stem)) throw new InvalidDataException($"词条音频文件名冲突：{entry.Word}");
            if (entry.Audio is null || entry.Audio.Uk != $"audio_vocabulary/{stem}_uk.mp3" || entry.Audio.Us != $"audio_vocabulary/{stem}_us.mp3")
                throw new InvalidDataException($"词条音频路径无效：{entry.Word}");
            if (entry.Meanings is null || entry.PhoneticUk is null || entry.PhoneticUs is null || entry.Meanings.Any(m => m is null || m.Pos is null || m.Text is null))
                throw new InvalidDataException($"词条音标或释义无效：{entry.Word}");
        }
    }
}
