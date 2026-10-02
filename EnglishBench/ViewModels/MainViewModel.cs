using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using EnglishBench.Models;
using EnglishBench.Services;

namespace EnglishBench.ViewModels;

public sealed partial class MainViewModel : INotifyPropertyChanged
{
    private readonly ReaderWorkspace workspace = new ReaderWorkspace();
    private readonly ReaderPlayback playback;
    private string status = "";
    private string accent = "uk";
    private bool highlightsVisible;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? ArticleChanged;
    public Func<bool>? CanLeaveArticle { get; set; }
    public event Action? VocabularyChanged;
    public event Action? PlaybackChanged;
    public ObservableCollection<NavigationNode> Books { get; } = new ObservableCollection<NavigationNode>();
    public ObservableCollection<VocabularyEntryViewModel> Words { get; } = new ObservableCollection<VocabularyEntryViewModel>();
    public LoadedArticle? Article => workspace.Article;
    public string? LibraryRoot => workspace.Library?.Root;
    public string ArticleTitle => Article?.Title ?? "";
    public ReaderPlayback Playback => playback;
    public string? PlayingSid => playback.CurrentSid;
    public string Owner => playback.Owner;
    public PlaybackState PlaybackState => playback.State;
    public bool CanWriteVocabulary => Article?.Vocabulary.CanWrite == true;
    public string CurrentUser => "User: xiaoxin";
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
            playback.StopPronunciation();
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
    public MainViewModel(PlaybackController articlePlayer, PlaybackController pronunciationPlayer)
    {
        playback = new ReaderPlayback(articlePlayer, pronunciationPlayer);
        playback.Message += message => Status = message;
        playback.Changed += () =>
        {
            PlaybackChanged?.Invoke();
            CommandManager.InvalidateRequerySuggested();
        };
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
