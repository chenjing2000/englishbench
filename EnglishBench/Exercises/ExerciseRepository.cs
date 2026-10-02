using System.IO;
using System.Text.Json;
using EnglishBench.Infrastructure;
using EnglishBench.Models;

namespace EnglishBench.Exercises;

public sealed class ExerciseRepository
{
    public ExerciseContent Load(string path, Article passage)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var root = json.RootElement;
        if (JsonFiles.Text(root, "filetype") != "exercise") throw new InvalidDataException("filetype 必须为 exercise。");
        string type = JsonFiles.Text(root, "type");
        bool complete = type == "article_choice" || type == "article_answer";
        bool cloze = type == "article_cloze" || type == "article_cloze_words" || type == "article_cloze_sentences";
        if (!complete && !cloze) throw new InvalidDataException("不支持的练习题型：" + type);
        bool hasBlanks = passage is ArticleBlank;
        if ((complete && hasBlanks) || (cloze && !hasBlanks))
            throw new InvalidDataException("练习题型与正文类型不一致。");
        var shared = type == "article_cloze_sentences" ? ReadOptions(root, true) : new List<ExerciseOption>();
        var questions = new List<ExerciseQuestion>();
        var numbers = new HashSet<int>();
        foreach (var item in JsonFiles.Array(root, complete ? "questions" : "items", true).EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("number", out var raw) || raw.ValueKind != JsonValueKind.Number || !raw.TryGetInt32(out int number) || number < 1 || !numbers.Add(number))
                throw new InvalidDataException("练习题号必须是唯一的正整数。");
            string prompt = complete ? JsonFiles.Text(item, "prompt") : "";
            if (complete && (string.IsNullOrWhiteSpace(prompt) || prompt.Contains("[[") || prompt.Contains("]]")))
                throw new InvalidDataException("题目正文为空或包含填空占位符。");
            string cue = type == "article_cloze_words" ? JsonFiles.Text(item, "cue") : "";
            var options = type == "article_choice" || type == "article_cloze" ? ReadOptions(item, false) : new List<ExerciseOption>();
            questions.Add(new ExerciseQuestion(number, prompt, cue, options));
        }
        if (passage is ArticleBlank blank && !blank.Placeholders.Order().SequenceEqual(numbers.Order()))
            throw new InvalidDataException("练习题号与正文填空不一致。");
        return new ExerciseContent(type, questions, shared);
    }

    private static List<ExerciseOption> ReadOptions(JsonElement item, bool sentencePool)
    {
        var options = new List<ExerciseOption>();
        var keys = new HashSet<string>();
        foreach (var option in JsonFiles.Array(item, "options", true).EnumerateArray())
        {
            string key = JsonFiles.Text(option, "key"), text = JsonFiles.Text(option, "text");
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(text) || !keys.Add(key) ||
                sentencePool && (key.Length != 1 || key[0] < 'A' || key[0] > 'Z'))
                throw new InvalidDataException("练习选项键或文本无效。");
            options.Add(new ExerciseOption(key, text));
        }
        if (options.Count < 2) throw new InvalidDataException("练习至少需要两个选项。");
        return options;
    }
}
