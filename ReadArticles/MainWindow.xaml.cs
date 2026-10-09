using System.Windows;
using ReadArticles.Infrastructure;
using ReadArticles.Services;
using ReadArticles.ViewModels;

namespace ReadArticles;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(bool persistSettings = true)
    {
        this.persistSettings = persistSettings;
        settings = persistSettings ? ReaderSettings.Load() : new ReaderSettings();
        ViewModel = new MainViewModel(new PlaybackController(player), new PlaybackController(pronunciationPlayer));

        InitializeComponent();
        DataContext = ViewModel;
        InitializeWindowSettings();
        ViewModel.HighlightsVisible = false;

        ViewModel.ArticleChanged += RenderArticle;
        ViewModel.VocabularyChanged += RefreshVocabularyHighlights;
        InitializePlayback();
        InitializeStatus();
        Closing += WindowClosing;
        Closed += WindowClosed;

        RenderArticle();
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        double volume = player.Volume;
        CloseStatus();
        ClosePlayback();
        SaveSettings(volume);
    }
}
