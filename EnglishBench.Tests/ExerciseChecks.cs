using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EnglishBench.Exercises;
using EnglishBench.Infrastructure;
using EnglishBench.Services;

internal static class ExerciseChecks
{
    private static string SourceFolder => Path.GetFullPath("EnglishBench.Tests/Fixtures/Exercises/Week 5 Monday");
    private const string Title = "Unitree Robots at the Spring Festival Gala";
    private static readonly string[] Types = { "article_choice", "article_answer", "article_cloze", "article_cloze_words", "article_cloze_sentences" };

    internal static void Run()
    {
        RunCase("exercise schemas validate all five types, blanks and duplicate numbers/options", root =>
        {
            foreach (string type in Types)
            {
                string path = WriteExercise(root, type);
                var content = new ExerciseRepository().Load(path, Passage(type));
                Program.Check(content.Type == type && content.Questions.Count == 2);
                Reject(() => new ExerciseRepository().Load(path, Passage(type is "article_choice" or "article_answer" ? "article_cloze" : "article_choice")));
                var invalid = JsonNode.Parse(File.ReadAllText(path))!;
                invalid[type == "article_choice" || type == "article_answer" ? "questions" : "items"]![1]!["number"] = 1;
                File.WriteAllText(path, invalid.ToJsonString());
                Reject(() => new ExerciseRepository().Load(path, Passage(type)));
            }
            string choice = WriteExercise(root, "article_choice");
            var duplicate = JsonNode.Parse(File.ReadAllText(choice))!;
            duplicate["questions"]![0]!["options"]![1]!["key"] = "A";
            File.WriteAllText(choice, duplicate.ToJsonString());
            Reject(() => new ExerciseRepository().Load(choice, Passage("article_choice")));
        });
        RunCase("manual saves mirror xiaoxin/book paths and overwrite exactly one current answer", root =>
        {
            string book = Path.Combine(root, "Book name"), directory = Path.Combine(book, "Week", "Monday");
            Directory.CreateDirectory(directory);
            var content = new ExerciseRepository().Load(WriteExercise(directory, "article_choice"), Passage("article_choice"));
            var store = new ExerciseAnswers();
            string path = store.GetPath(root, book, Path.Combine(directory, "Sample.exercise.json"));
            Program.Check(path == Path.Combine(root, "userdata", "xiaoxin", "Book name", "Week", "Monday", "Sample.exercise.json"));
            var session = new ExerciseSession(content, path);
            session.SetAnswer(1, "A");
            Program.Check(session.IsDirty && !File.Exists(path));
            session.Save();
            Program.Check(!session.IsDirty && store.Load(path, content)[1] == "A");
            session.SetAnswer(1, "B");
            Program.Check(store.Load(path, content)[1] == "A");
            session.Save();
            using (var json = JsonDocument.Parse(File.ReadAllText(path))) Program.Check(json.RootElement.GetProperty("answers").GetArrayLength() == 2);
            var restored = new ExerciseSession(content, path, store.Load(path, content));
            Program.Check(restored.Answer(1) == "B" && !restored.IsDirty);
            restored.Reset();
            Program.Check(restored.IsDirty && store.Load(path, content)[1] == "B");
            restored.Save();
            Program.Check(store.Load(path, content).Values.All(answer => answer == "") && Directory.GetFiles(Path.GetDirectoryName(path)!).Length == 1);
            Reject(() => store.GetPath(root, book, Path.Combine(root, "outside.exercise.json")));
            var invalid = JsonNode.Parse(File.ReadAllText(path))!;
            invalid["answers"]![0]!["answer"] = "Z";
            File.WriteAllText(path, invalid.ToJsonString());
            Reject(() => store.Load(path, content));
            invalid["answers"]![0]!["answer"] = "A";
            invalid["answers"]![1]!["number"] = 1;
            File.WriteAllText(path, invalid.ToJsonString());
            Reject(() => store.Load(path, content));
            File.WriteAllText(path, "{}");
            Reject(() => store.Load(path, content));
            string blocked = Path.Combine(root, "blocked");
            File.WriteAllText(blocked, "a file cannot be an answer directory");
            var unsaved = new ExerciseSession(content, Path.Combine(blocked, "answers.json"));
            unsaved.SetAnswer(1, "A");
            var errorView = new ExerciseView();
            errorView.ShowExercise(unsaved);
            string warning = "";
            errorView.Message += message => warning = message;
            Program.Check(!errorView.SaveAnswers() && unsaved.IsDirty && unsaved.Answer(1) == "A" && warning.Contains("回答无法保存"));
            errorView.ShowExercise(null);
        });
        RunCase("WPF exercise container renders five types and only save/reset actions", root =>
        {
            var view = new ExerciseView { FontSize = 16 };
            var window = new Window { Content = view, Width = 580, Height = 700, Left = -10000, Top = -10000, ShowActivated = false };
            try
            {
                window.Show();
                foreach (string type in Types)
                {
                    var content = new ExerciseRepository().Load(WriteExercise(root, type), Passage(type));
                    var session = new ExerciseSession(content, Path.Combine(root, type + ".answers.json"));
                    view.ShowExercise(session); WpfTestHelpers.Pump(30);
                    var buttons = WpfTestHelpers.Descendants(view).OfType<Button>().ToArray();
                    Program.Check(buttons.Select(button => (string)button.Content).SequenceEqual(new[] { "保存回答", "重置回答" }));
                    if (type == "article_choice" || type == "article_cloze")
                    {
                        var radios = WpfTestHelpers.Descendants(view).OfType<RadioButton>().ToArray();
                        Program.Check(radios.Length == 4);
                        radios[0].IsChecked = true;
                        Program.Check(session.Answer(1) == "A");
                        radios[1].IsChecked = true;
                        Program.Check(session.Answer(1) == "B");
                    }
                    else
                    {
                        var inputs = WpfTestHelpers.Descendants(view).OfType<TextBox>().ToArray();
                        Program.Check(inputs.Length == 2 && inputs.All(input => input.FontSize == 16));
                        Program.Check(inputs[0].AcceptsReturn == (type == "article_answer"));
                        if (type == "article_cloze_sentences") Program.Check(inputs[0].MaxLength == 1 && inputs[0].CharacterCasing == CharacterCasing.Upper);
                        inputs[0].Text = type == "article_cloze_sentences" ? "B" : "my answer";
                    }
                    Click(buttons[0]); Program.Check(!session.IsDirty && File.Exists(session.AnswerPath));
                    Click(buttons[1]); Program.Check(session.IsDirty && content.Questions.All(question => session.Answer(question.Number) == ""));
                }
                view.ShowExercise(null); Program.Check(view.Visibility == Visibility.Collapsed);
            }
            finally { window.Close(); }
        });
        RunCase("Week 5 Monday reads below passage and restores only explicitly saved test answers", root =>
        {
            string article = CopySample(root);
            var window = new EnglishBench.MainWindow(false) { Left = -10000, Top = -10000, ShowActivated = false };
            try
            {
                window.Show(); Program.Check(window.ViewModel.OpenLibrary(root) && window.SelectPassage(article)); WpfTestHelpers.Pump(30);
                var view = (ExerciseView)window.FindName("ExercisePanel");
                var body = (StackPanel)window.FindName("ParagraphHost");
                Program.Check(view.Session!.Content.Questions.Count == 4 && !body.IsAncestorOf(view));
                Program.Check(((StackPanel)view.Parent).Children.IndexOf(view) > ((StackPanel)view.Parent).Children.IndexOf(body));
                Program.Check(view.Session.AnswerPath == Path.Combine(root, "userdata", "xiaoxin", "英语时文阅读理解（七年级）", "Week 5 Monday", Title + ".exercise.json"));
                view.Session.SetAnswer(1, "A");
                Program.Check(!File.Exists(view.Session.AnswerPath) && view.SaveAnswers());
                view.Session.SetAnswer(1, "B"); window.ViewModel.CanLeaveArticle = () => false;
                Program.Check(!window.ViewModel.OpenLibrary(root) && !window.ViewModel.OpenArticle(article));
                Program.Check(view.Session.Answer(1) == "B"); window.ViewModel.CanLeaveArticle = () => true;
                Program.Check(window.ViewModel.OpenArticle(article));
                Program.Check(view.Session!.Answer(1) == "A" && !view.Session.IsDirty);
                var snapshotView = new ExerciseView { Background = Brushes.White, FontSize = view.FontSize, Padding = new Thickness(12) };
                var snapshotWindow = new Window { Content = snapshotView, Background = Brushes.White, Width = 600, Height = 950, Left = -10000, Top = -10000, ShowActivated = false };
                try
                {
                    snapshotView.ShowExercise(view.Session);
                    snapshotWindow.Show(); WpfTestHelpers.Pump(30);
                    var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(snapshotView.ActualWidth), (int)Math.Ceiling(snapshotView.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(snapshotView);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                    using (var image = File.Create(Path.GetFullPath("artifacts/exercise-sample.png"))) encoder.Save(image);
                }
                finally { snapshotView.ShowExercise(null); snapshotWindow.Close(); }
                File.WriteAllText(Path.ChangeExtension(article, ".exercise.json"), "not JSON");
                Program.Check(window.ViewModel.OpenArticle(article) && window.ViewModel.Article != null);
                Program.Check(view.Visibility == Visibility.Collapsed && window.ViewModel.Status.Contains("练习无法加载"));
            }
            finally { ((ExerciseView)window.FindName("ExercisePanel")).ShowExercise(null); window.Close(); WpfTestHelpers.Pump(); }
        });
        RunCase("exercise inputs keep space and arrows for editing while other controls keep audio keys", root =>
        {
            string article = CopySample(root);
            File.Copy(WriteExercise(root, "article_answer"), Path.ChangeExtension(article, ".exercise.json"), true);
            File.Copy(Path.Combine(Path.GetDirectoryName(TestData.ArticlePath)!, "audio_segments", "s001_uk.mp3"), Path.Combine(Path.GetDirectoryName(article)!, "reading.mp3"));
            var window = new EnglishBench.MainWindow(false) { Left = -10000, Top = -10000, ShowActivated = false };
            try
            {
                window.Show(); Program.Check(window.ViewModel.OpenLibrary(root) && window.SelectPassage(article)); WpfTestHelpers.Pump(30);
                var view = (ExerciseView)window.FindName("ExercisePanel");
                var input = WpfTestHelpers.Descendants(view).OfType<TextBox>().First();
                window.ViewModel.ToggleArticleAudio(); WpfTestHelpers.Wait(() => window.ViewModel.Playback.CanSeekArticle);
                window.ViewModel.ToggleArticleAudio(); window.ViewModel.Playback.SeekArticle(1);
                foreach (var key in new[] { Key.Space, Key.Left, Key.Right })
                {
                    SendKey(input, key);
                    Program.Check(window.ViewModel.Playback.IsArticlePaused && Math.Abs(window.ViewModel.Playback.ArticlePosition - 1) < 0.05);
                }
                SendKey((Button)window.FindName("ReadAllButton"), Key.Space);
                Program.Check(window.ViewModel.Playback.IsArticlePlaying);
                input.Text = "a full answer";
                Program.Check(view.Session!.Answer(1) == "a full answer" && !File.Exists(view.Session.AnswerPath));
                Program.Check(view.SaveAnswers()); window.ViewModel.Stop();
            }
            finally { ((ExerciseView)window.FindName("ExercisePanel")).ShowExercise(null); window.Close(); WpfTestHelpers.Pump(); }
        });
    }
    private static EnglishBench.Models.Article Passage(string type)
    {
        if (type.StartsWith("article_cloze"))
            return new EnglishBench.Models.ArticleBlank(new[] { new[] { new EnglishBench.Models.Segment("s001", "Text [[1]] and [[2]].") } }, 2);
        return new EnglishBench.Models.Article(new[] { new[] { new EnglishBench.Models.Segment("s001", "Complete passage.", TestData.Paths("s001")) } }, 2);
    }
    private static string WriteExercise(string root, string type)
    {
        var options = new[] { new { key = "A", text = "First option" }, new { key = "B", text = "Second option" } };
        var json = new JsonObject { ["filetype"] = "exercise", ["type"] = type };
        var questions = new JsonArray();
        for (int number = 1; number <= 2; number++)
        {
            var item = new JsonObject { ["number"] = number };
            if (type == "article_choice" || type == "article_answer") item["prompt"] = "Question " + number;
            if (type == "article_choice" || type == "article_cloze") item["options"] = JsonSerializer.SerializeToNode(options);
            if (type == "article_cloze_words") item["cue"] = "cue";
            questions.Add(item);
        }
        json[type == "article_choice" || type == "article_answer" ? "questions" : "items"] = questions;
        if (type == "article_cloze_sentences") json["options"] = JsonSerializer.SerializeToNode(options);
        string path = Path.Combine(root, "Sample.exercise.json"); File.WriteAllText(path, json.ToJsonString()); return path;
    }
    private static string CopySample(string root)
    {
        string book = Path.Combine(root, "英语时文阅读理解（七年级）"), directory = Path.Combine(book, "Week 5 Monday");
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(book, "book.json"), "{}");
        foreach (string suffix in new[] { ".json", ".exercise.json", ".vocabulary.json" }) File.Copy(Path.Combine(SourceFolder, Title + suffix), Path.Combine(directory, Title + suffix));
        return Path.Combine(directory, Title + ".json");
    }
    private static void RunCase(string name, Action<string> action)
    {
        Program.Run(name, () =>
        {
            string root = Path.GetFullPath(Path.Combine("artifacts", "exercise-tests", Guid.NewGuid().ToString("N"))); Directory.CreateDirectory(root);
            var sourceFiles = new[] { ".json", ".exercise.json", ".vocabulary.json" }.Select(suffix => Path.Combine(SourceFolder, Title + suffix)).ToArray();
            var hashes = sourceFiles.Select(JsonFiles.Fingerprint).ToArray();
            try { action(root); Program.Check(sourceFiles.Select(JsonFiles.Fingerprint).SequenceEqual(hashes)); }
            finally { Directory.Delete(root, true); }
        });
    }
    private static void Reject(Action action)
    {
        try { action(); } catch (Exception error) when (error is InvalidDataException or ArgumentException) { return; }
        throw new Exception("Invalid exercise data accepted.");
    }
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void SendKey(UIElement target, Key key) => target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target), 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
}
