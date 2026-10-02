using System.IO;
using System.Text.Json;
using EnglishBench.Infrastructure;

namespace EnglishBench.Exercises;

public sealed class ExerciseAnswers
{
    public string GetPath(string libraryRoot, string bookRoot, string exercisePath)
    {
        string relative = Path.GetRelativePath(bookRoot, exercisePath);
        ResourcePaths.Resolve(bookRoot, relative);
        string book = Path.GetFileName(Path.TrimEndingDirectorySeparator(bookRoot));
        return ResourcePaths.Resolve(libraryRoot, Path.Combine("userdata", "xiaoxin", book, relative));
    }

    public Dictionary<int, string> Load(string path, ExerciseContent content)
    {
        var result = content.Questions.ToDictionary(question => question.Number, question => "");
        if (!File.Exists(path)) return result;
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var root = json.RootElement;
        if (JsonFiles.Text(root, "filetype") != "exercise_answers" ||
            !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int number) || number != 1 ||
            JsonFiles.Text(root, "type") != content.Type)
            throw new InvalidDataException("已保存回答的格式或题型不匹配，未恢复旧回答。");
        var seen = new HashSet<int>();
        foreach (var item in JsonFiles.Array(root, "answers").EnumerateArray())
        {
            if (!item.TryGetProperty("number", out var raw) || raw.ValueKind != JsonValueKind.Number || !raw.TryGetInt32(out int questionNumber) ||
                !result.ContainsKey(questionNumber) || !seen.Add(questionNumber))
                throw new InvalidDataException("已保存回答包含无效或重复题号。");
            string answer = JsonFiles.Text(item, "answer");
            ValidateAnswer(content, questionNumber, answer);
            result[questionNumber] = answer;
        }
        return result;
    }

    public void Save(string path, ExerciseContent content, IReadOnlyDictionary<int, string> answers)
    {
        foreach (var question in content.Questions) ValidateAnswer(content, question.Number, answers[question.Number]);
        JsonFiles.WriteAtomic(path, new
        {
            filetype = "exercise_answers",
            version = 1,
            type = content.Type,
            answers = content.Questions.Select(question => new { number = question.Number, answer = answers[question.Number] }).ToArray()
        });
    }

    private static void ValidateAnswer(ExerciseContent content, int number, string answer)
    {
        var question = content.Questions.Single(item => item.Number == number);
        var options = content.Type == "article_cloze_sentences" ? content.SharedOptions : question.Options;
        if (answer != "" && options.Count > 0 && !options.Any(option => option.Key == answer))
            throw new InvalidDataException($"第 {number} 题的回答不是有效选项。");
    }
}
