using System.Windows;
using System.Windows.Controls;
using EnglishBench.Infrastructure;
using EnglishBench.Rendering;
using EnglishBench.Services;
using EnglishBench.ViewModels;
using Microsoft.Win32;

namespace EnglishBench;

public partial class PrototypeWindow : Window
{
    private readonly WpfAudioPlayer player;
    public PrototypeViewModel ViewModel { get; }
    public IReadOnlyList<ParagraphReader> Readers => readers;
    private readonly List<ParagraphReader> readers = [];

    public PrototypeWindow()
    {
        player = new WpfAudioPlayer();
        ViewModel = new(new PlaybackController(player));
        InitializeComponent();
        DataContext = ViewModel;
        player.Volume = VolumeSlider.Value;
        ViewModel.VocabularyChanged += () =>
        {
            foreach (var reader in readers) reader.SetVocabulary(ViewModel.Words, ViewModel.HighlightsVisible);
        };
        ViewModel.PlaybackChanged += () =>
        {
            foreach (var reader in readers) reader.SetPlayingSegment(ViewModel.PlayingSid);
        };
        for (int index = 0; index < ViewModel.Paragraphs.Count; index++)
        {
            int paragraphIndex = index;
            var heading = new DockPanel { Margin = new Thickness(0, 6, 0, 2) };
            var play = new Button { Content = "▶ Paragraph " + (index + 1), FontSize = 12,
                Padding = new Thickness(8, 3, 8, 3), HorizontalAlignment = HorizontalAlignment.Left };
            play.Click += (_, _) => ViewModel.PlayParagraph(paragraphIndex);
            heading.Children.Add(play);
            ParagraphHost.Children.Add(heading);
            AddReader(ViewModel.Paragraphs[index]);
        }
        ParagraphHost.Children.Add(new Separator { Margin = new Thickness(0, 18, 0, 18) });
        ParagraphHost.Children.Add(new TextBlock { Text = "ArticleBlank · 只读占位符", FontSize = 21, FontWeight = FontWeights.SemiBold });
        ParagraphHost.Children.Add(new TextBlock { Text = "正文不播放音频；答案输入属于 Exercise 区。", Margin = new Thickness(0, 6, 0, 12) });
        AddReader(ViewModel.BlankParagraph);
        Closed += (_, _) => { ViewModel.Stop(); player.Dispose(); };
    }

    private void AddReader(IEnumerable<Models.Segment> segments)
    {
        var reader = new ParagraphReader(segments, ViewModel.Words) { Margin = new Thickness(0, 2, 0, 10) };
        reader.SelectionRequested += ViewModel.AddSelection;
        reader.SegmentPlaybackRequested += ViewModel.PlaySegment;
        reader.HoverChanged += sid =>
        {
            if (sid is not null) ViewModel.Status = $"Segment：{sid} · 右键播放 {ViewModel.Accent.ToUpperInvariant()}";
        };
        readers.Add(reader);
        ParagraphHost.Children.Add(reader);
    }
    private void AccentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not null && AccentSelector.SelectedItem is ComboBoxItem item)
            ViewModel.Accent = (string)item.Tag;
    }
    private void ReadAllClicked(object sender, RoutedEventArgs e) => ViewModel.PlayAll();
    private void PauseClicked(object sender, RoutedEventArgs e) => ViewModel.Pause();
    private void ResumeClicked(object sender, RoutedEventArgs e) => ViewModel.Resume();
    private void StopClicked(object sender, RoutedEventArgs e) => ViewModel.Stop();
    private void VolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (player is not null) player.Volume = e.NewValue; }
    private void AudioFolderClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "选择包含 audio_segments 的目录" };
        if (dialog.ShowDialog(this) == true) ViewModel.AudioRoot = dialog.FolderName;
    }
    private void LocalFileClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "选择已有本地 MP3", Filter = "MP3 files|*.mp3", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) ViewModel.PlayExistingFile(dialog.FileName);
    }
}

