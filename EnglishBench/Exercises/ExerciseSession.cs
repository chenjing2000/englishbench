namespace EnglishBench.Exercises;

public sealed class ExerciseSession
{
    private readonly Dictionary<int, string> answers;
    private Dictionary<int, string> saved;
    public ExerciseContent Content { get; }
    public string? AnswerPath { get; }
    public bool IsDirty => answers.Any(pair => pair.Value != saved[pair.Key]);
    public event Action? Changed;

    public ExerciseSession(ExerciseContent content, string? answerPath, IReadOnlyDictionary<int, string>? restored = null)
    {
        Content = content;
        AnswerPath = answerPath;
        answers = content.Questions.ToDictionary(question => question.Number,
            question => restored != null && restored.TryGetValue(question.Number, out string? value) ? value : "");
        saved = new Dictionary<int, string>(answers);
    }

    public string Answer(int number) => answers[number];

    public void SetAnswer(int number, string answer)
    {
        if (answers[number] == answer) return;
        answers[number] = answer;
        Changed?.Invoke();
    }

    public void Reset()
    {
        foreach (var question in Content.Questions) answers[question.Number] = "";
        Changed?.Invoke();
    }

    public void Save()
    {
        if (AnswerPath == null) throw new InvalidOperationException("无法确定回答保存目录，请从图书馆打开文章。");
        new ExerciseAnswers().Save(AnswerPath, Content, answers);
        saved = new Dictionary<int, string>(answers);
        Changed?.Invoke();
    }
}
