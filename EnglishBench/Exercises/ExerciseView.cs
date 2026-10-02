using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EnglishBench.Exercises;

public sealed class ExerciseView : UserControl
{
    private sealed class QuestionRow
    {
        public ExerciseQuestion Question { get; }
        public List<RadioButton> Options { get; } = new List<RadioButton>();
        public TextBox? Input { get; set; }
        public QuestionRow(ExerciseQuestion question) => Question = question;
    }

    private readonly StackPanel body = new StackPanel();
    private readonly List<QuestionRow> rows = new List<QuestionRow>();
    private readonly Button saveButton = new Button { Content = "保存回答" };
    private readonly Button resetButton = new Button { Content = "重置回答" };
    private bool syncing;
    public ExerciseSession? Session { get; private set; }
    public event Action<string>? Message;

    public ExerciseView()
    {
        Content = body;
        Visibility = Visibility.Collapsed;
        saveButton.Click += (_, _) => SaveAnswers();
        resetButton.Click += (_, _) => Session?.Reset();
    }

    public void ShowExercise(ExerciseSession? session)
    {
        if (Session != null) Session.Changed -= Refresh;
        Session = session;
        foreach (var button in new[] { saveButton, resetButton })
            if (button.Parent is Panel parent) parent.Children.Remove(button);
        body.Children.Clear();
        rows.Clear();
        Visibility = session == null ? Visibility.Collapsed : Visibility.Visible;
        if (session == null) return;
        if (session.Content.SharedOptions.Count > 0)
        {
            foreach (var option in session.Content.SharedOptions)
                body.Children.Add(Text(option.Key + ". " + option.Text, new Thickness(0, 4, 0, 4)));
        }
        foreach (var question in session.Content.Questions) AddQuestion(question);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 4) };
        foreach (var button in new[] { saveButton, resetButton })
        {
            button.Margin = new Thickness(4, 3, 4, 3);
            button.Padding = new Thickness(8, 6, 8, 6);
            actions.Children.Add(button);
        }
        body.Children.Add(actions);
        session.Changed += Refresh;
        Refresh();
    }

    private void AddQuestion(ExerciseQuestion question)
    {
        var row = new QuestionRow(question);
        rows.Add(row);
        var block = new StackPanel();
        block.Children.Add(Text(question.Number + ". " + question.Prompt, new Thickness(0, 8, 0, 8)));
        if (question.Options.Count > 0)
        {
            Panel options = Session!.Content.Type == "article_cloze" ? new WrapPanel() : new StackPanel();
            foreach (var option in question.Options)
            {
                var radio = new RadioButton
                {
                    Content = Text(option.Key + ". " + option.Text, new Thickness(0)),
                    Tag = option.Key,
                    GroupName = "exercise-" + GetHashCode() + "-" + question.Number,
                    Margin = new Thickness(16, 5, 12, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                radio.Checked += (_, _) => { if (!syncing) Session!.SetAnswer(question.Number, option.Key); };
                row.Options.Add(radio);
                options.Children.Add(radio);
            }
            block.Children.Add(options);
        }
        else
        {
            bool multiline = Session!.Content.Type == "article_answer";
            row.Input = new TextBox
            {
                AcceptsReturn = multiline,
                TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                Padding = new Thickness(5),
                MinHeight = 30,
                Margin = new Thickness(16, 0, 0, 4),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            if (!multiline)
            {
                row.Input.Width = Session.Content.Type == "article_cloze_sentences" ? 45 : 180;
                row.Input.HorizontalAlignment = HorizontalAlignment.Left;
            }
            if (Session.Content.Type == "article_cloze_sentences")
            {
                row.Input.MaxLength = 1;
                row.Input.CharacterCasing = CharacterCasing.Upper;
            }
            if (question.Cue != "") block.Children.Add(Text("(" + question.Cue + ")", new Thickness(16, 0, 0, 6)));
            row.Input.TextChanged += (_, _) => { if (!syncing) Session!.SetAnswer(question.Number, row.Input.Text); };
            block.Children.Add(row.Input);
        }
        block.Children.Add(new Border { Height = 1, Background = Brushes.Gainsboro, Margin = new Thickness(0, 12, 0, 12) });
        body.Children.Add(block);
    }

    private void Refresh()
    {
        if (Session == null) return;
        syncing = true;
        try
        {
            foreach (var row in rows)
            {
                string answer = Session.Answer(row.Question.Number);
                foreach (var radio in row.Options)
                {
                    radio.IsChecked = (string)radio.Tag == answer;
                }
                if (row.Input != null && row.Input.Text != answer) row.Input.Text = answer;
            }
            saveButton.IsEnabled = Session.AnswerPath != null;
        }
        finally { syncing = false; }
    }

    public bool SaveAnswers()
    {
        try
        {
            Session?.Save();
            return Session != null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            Message?.Invoke("回答无法保存：" + error.Message);
            return false;
        }
    }

    private static TextBlock Text(string value, Thickness margin) =>
        new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, Margin = margin };
}
