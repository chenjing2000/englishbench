namespace EnglishBench.Exercises;

public sealed record ExerciseOption(string Key, string Text);

public sealed record ExerciseQuestion(int Number, string Prompt, string Cue,
    IReadOnlyList<ExerciseOption> Options);

public sealed record ExerciseContent(string Type, IReadOnlyList<ExerciseQuestion> Questions,
    IReadOnlyList<ExerciseOption> SharedOptions);
