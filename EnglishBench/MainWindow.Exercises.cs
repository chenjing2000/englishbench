using System.IO;
using System.Text.Json;
using System.Windows;
using EnglishBench.Exercises;

namespace EnglishBench;

public partial class MainWindow
{
    private void InitializeExercises()
    {
        ExercisePanel.Message += ExerciseMessage;
        ViewModel.CanLeaveArticle = ConfirmExerciseLeave;
    }

    private void LoadExercise()
    {
        ExercisePanel.ShowExercise(null);
        var article = ViewModel.Article;
        if (article == null) return;
        string path = Path.ChangeExtension(article.FilePath, ".exercise.json");
        if (!File.Exists(path)) return;
        try
        {
            var content = new ExerciseRepository().Load(path, article.Content);
            var store = new ExerciseAnswers();
            string? answerPath = FindAnswerPath(path, store);
            Dictionary<int, string>? restored = null;
            if (answerPath != null)
            {
                try { restored = store.Load(answerPath, content); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
                {
                    ExerciseMessage("回答未恢复：" + error.Message);
                }
            }
            else ExerciseMessage("找不到 book.json，练习可显示但无法保存回答。");
            ExercisePanel.ShowExercise(new ExerciseSession(content, answerPath, restored));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException)
        {
            ExerciseMessage("练习无法加载：" + error.Message);
        }
    }

    private string? FindAnswerPath(string exercisePath, ExerciseAnswers store)
    {
        string? book = Path.GetDirectoryName(exercisePath);
        while (book != null)
        {
            if (File.Exists(Path.Combine(book, "book.json")))
            {
                string library = ViewModel.LibraryRoot ?? Path.GetDirectoryName(book)!;
                return store.GetPath(library, book, exercisePath);
            }
            book = Path.GetDirectoryName(book);
        }
        return null;
    }

    private bool ConfirmExerciseLeave()
    {
        if (ExercisePanel.Session?.IsDirty != true) return true;
        var result = MessageBox.Show(this, "当前练习回答尚未保存。是否保存后离开？\n选择“否”将放弃未保存的修改。",
            "保存回答", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel) return false;
        return result == MessageBoxResult.No || ExercisePanel.SaveAnswers();
    }

    private void ExerciseMessage(string message) => ViewModel.Status =
        string.IsNullOrWhiteSpace(ViewModel.Status) ? message : ViewModel.Status + "  " + message;
}
