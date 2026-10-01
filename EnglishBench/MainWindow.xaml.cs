using System.Windows;
using EnglishBench.Infrastructure;
using EnglishBench.Services;
using EnglishBench.ViewModels;

namespace EnglishBench;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(bool persistSettings = true)
    {
        this.persistSettings = persistSettings;
        settings = persistSettings ? ReaderSettings.Load() : new ReaderSettings();
        ViewModel = new MainViewModel(new PlaybackController(player));

        InitializeComponent();
        DataContext = ViewModel;
        InitializeWindowSettings();
        ViewModel.HighlightsVisible = false;

        ViewModel.ArticleChanged += RenderArticle;
        ViewModel.VocabularyChanged += RefreshVocabularyHighlights;
        ViewModel.PlaybackChanged += RefreshPlayback;
        ViewModel.PropertyChanged += ViewModelPropertyChanged;
        statusTimer.Tick += StatusTimerTick;
        player.Opened += PlayerOpened;
        progressTimer.Tick += ProgressTimerTick;
        Closing += WindowClosing;
        Closed += WindowClosed;

        RestartStatusTimer();
        RenderArticle();
    }
}
