using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using EnglishBench.Models;
using EnglishBench.Services;
using EnglishBench.Infrastructure;

namespace EnglishBench.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ReaderWorkspace workspace = new ReaderWorkspace();
    private readonly AudioResources resources = new AudioResources();
    private readonly PlaybackController playback;
    private string status = "";
    private string accent = "uk";
    private bool highlightsVisible;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? ArticleChanged;
    public event Action? VocabularyChanged;
    public event Action? PlaybackChanged;
    public ObservableCollection<NavigationNode> Books { get; } = new ObservableCollection<NavigationNode>();
    public ObservableCollection<VocabularyEntryViewModel> Words { get; } = new ObservableCollection<VocabularyEntryViewModel>();
    public LoadedArticle? Article => workspace.Article;
    public string? LibraryRoot => workspace.Library?.Root;
    public string ArticleTitle => Article?.Title ?? "";
    public string? PlayingSid => playback.CurrentSid;
    public string Owner => playback.Owner;
    public PlaybackState PlaybackState => playback.State;
    public bool CanWriteVocabulary => Article?.Vocabulary.CanWrite == true;
    public string CurrentUser { get; private set; } = "User: —";
    public string Status
    {
        get => status;
        set
        {
            status = value;
            Changed();
        }
    }
    public string Accent
    {
        get => accent;
        set
        {
            if ((value != "uk" && value != "us") || value == accent) return;
            if (Owner != "passage") playback.Stop();
            accent = value;
            Changed();
        }
    }
    public bool HighlightsVisible
    {
        get => highlightsVisible;
        set
        {
            highlightsVisible = value;
            Changed();
            VocabularyChanged?.Invoke();
        }
    }
    public MainViewModel(PlaybackController playback)
    {
        this.playback = playback;
        playback.Message += message => Status = message;
        playback.Changed += () =>
        {
            PlaybackChanged?.Invoke();
            CommandManager.InvalidateRequerySuggested();
        };
    }

    public bool OpenLibrary(string root) => Handle(() =>
    {
        workspace.OpenLibrary(root);
        playback.Stop();
        Books.Clear();
        foreach (var book in workspace.Library!.Books) Books.Add(book);
        Words.Clear();
        CurrentUser = "User: —";
        Changed(nameof(CurrentUser));
        Changed(nameof(ArticleTitle));
        ArticleChanged?.Invoke();
        Status = string.Join("  ", workspace.Library.Warnings);
    });

    public bool OpenArticle(string path) => Handle(() =>
    {
        var prepared = new ArticleRepository().Load(path);
        var words = PrepareWords(prepared);
        string user = ReadUser(path);
        workspace.CommitArticle(prepared);
        playback.Stop();
        Words.Clear();
        foreach (var word in words) Words.Add(word);
        CurrentUser = user;
        Changed(nameof(CurrentUser));
        Changed(nameof(ArticleTitle));
        ArticleChanged?.Invoke();
        Status = string.Join("  ", Article!.Warnings);
    });

    private string ReadUser(string path)
    {
        var leaf = AllLeaves(Books).FirstOrDefault(n => string.Equals(n.PassagePath, path, StringComparison.OrdinalIgnoreCase));
        if (leaf is null) return "User: —";
        return new UserRepository().ReadUser(leaf.BookRoot);
    }

    public static IEnumerable<NavigationNode> AllLeaves(IEnumerable<NavigationNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.IsPassage) yield return node;
            else
            {
                foreach (var leaf in AllLeaves(node.Children)) yield return leaf;
            }
        }
    }
    private void RefreshWords()
    {
        Words.Clear();
        if (Article is null) return;
        foreach (var word in PrepareWords(Article)) Words.Add(word);
    }
    private List<VocabularyEntryViewModel> PrepareWords(LoadedArticle article)
    {
        var words = new List<VocabularyEntryViewModel>();
        foreach (var entry in article.Vocabulary.Data.Words)
        {
            bool uk = WordAudioAvailable(article, entry, "uk");
            bool us = WordAudioAvailable(article, entry, "us");
            words.Add(new VocabularyEntryViewModel(entry, this, uk, us));
        }
        return words;
    }
    private bool WordAudioAvailable(LoadedArticle article, VocabularyEntry entry, string wordAccent)
    {
        try
        {
            return AudioResources.IsAvailable(resources.Vocabulary(article.Directory, entry, wordAccent));
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
    public void AddSelection(PassageSelection selection) => Handle(() =>
    {
        var article = Article ?? throw new InvalidOperationException("请先打开文章。");
        var entry = VocabularyRepository.NewEntry(selection.Text);
        if (article.Vocabulary.Data.Words.Any(w => w.Word.Equals(entry.Word, StringComparison.OrdinalIgnoreCase)))
        {
            Status = "词表中已存在：" + entry.Word;
            return;
        }
        var entries = new List<VocabularyEntry>(article.Vocabulary.Data.Words);
        entries.Add(entry);
        CommitWords(entries);
    });
    public bool CanMoveWord(VocabularyEntry entry, int direction)
    {
        if (!CanWriteVocabulary || (direction != -1 && direction != 1)) return false;
        var entries = Article!.Vocabulary.Data.Words;
        int index = entries.IndexOf(entry);
        return index >= 0 && index + direction >= 0 && index + direction < entries.Count;
    }
    public void MoveWord(VocabularyEntry entry, int direction) => Handle(() =>
    {
        if (!CanMoveWord(entry, direction)) return;
        var entries = Article!.Vocabulary.Data.Words.ToList();
        int index = entries.IndexOf(entry), destination = index + direction;
        if (index < 0 || destination < 0 || destination >= entries.Count) return;
        entries.RemoveAt(index);
        entries.Insert(destination, entry);
        CommitWords(entries);
    });
    public void DeleteWord(VocabularyEntry entry) => Handle(() =>
    {
        if (Owner.StartsWith("word:", StringComparison.Ordinal) && Owner.EndsWith(":" + entry.Word, StringComparison.Ordinal)) playback.Stop();
        CommitWords(Article!.Vocabulary.Data.Words.Where(w => !ReferenceEquals(w, entry)).ToList());
    });
    public void ImportVocabulary(string path) => Handle(() =>
    {
        if (!File.Exists(path)) throw new FileNotFoundException("导入文件不存在。");
        var incoming = new VocabularyRepository().Load(path);
        CommitVocabulary(incoming.Data);
    });
    private void CommitWords(List<VocabularyEntry> entries)
    {
        CommitVocabulary(new VocabularyData { Words = entries, Extra = Article!.Vocabulary.Data.Extra });
    }
    private void CommitVocabulary(VocabularyData vocabulary)
    {
        workspace.ReplaceVocabulary(vocabulary);
        RefreshWords();
        VocabularyChanged?.Invoke();
    }
    public AudioItem SegmentAudio(Segment segment) => new AudioItem(resources.Segment(Article!.Directory, segment, Accent), segment.Sid);
    public bool CanPlaySegment(string sid)
    {
        var segment = Article?.Segments.FirstOrDefault(s => s.Sid == sid);
        return segment != null && segment.Audio != null && AudioResources.IsAvailable(SegmentAudio(segment).Path);
    }
    public void PlaySegment(string sid) => HandleAudio(() => playback.Play(new[] { SegmentAudio(Article!.Segments.Single(s => s.Sid == sid)) }, "segment:" + sid));
    public void ToggleArticleAudio() => Handle(() =>
    {
        // Pause/resume applies only to the article file, never a word or segment target.
        if (Owner == "passage" && PlaybackState == PlaybackState.Playing)
        {
            playback.Pause();
            return;
        }
        if (Owner == "passage" && PlaybackState == PlaybackState.Paused)
        {
            playback.Resume();
            return;
        }
        string path = resources.ArticleFile(Article!.Directory);
        playback.Play(new[] { new AudioItem(path) }, "passage");
    });
    public void PlayWord(VocabularyEntry entry, string wordAccent) => HandleAudio(() => playback.Play(new[] { new AudioItem(resources.Vocabulary(Article!.Directory, entry, wordAccent)) }, $"word:{wordAccent}:{entry.Word}"));
    public void Stop() => playback.Stop();
    private void HandleAudio(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
        {
            playback.Stop();
            Status = error.Message;
        }
    }
    private bool Handle(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception error) when (error is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            Status = error.Message;
            return false;
        }
    }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
