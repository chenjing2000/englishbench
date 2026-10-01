using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using EnglishBench.Infrastructure;
using EnglishBench.Rendering;
using EnglishBench.Services;

namespace EnglishBench;

public partial class MainWindow
{
    private readonly WpfAudioPlayer player = new WpfAudioPlayer();
    private readonly DispatcherTimer progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    private bool updatingProgress;
    private bool draggingProgress;

    private void PlayerOpened(long request) => UpdateProgress();

    private void ProgressTimerTick(object? sender, EventArgs e) => UpdateProgress();

    private void RefreshPlayback()
    {
        foreach (var reader in readers) reader.SetPlayingSegment(ViewModel.PlayingSid);
        ReadAllButton.IsEnabled = ViewModel.Article != null;
        StopButton.IsEnabled = ViewModel.PlaybackState != PlaybackState.Stopped;
        bool pause = ViewModel.Owner == "passage" && ViewModel.PlaybackState == PlaybackState.Playing;
        if (!ReadAllButton.IsEnabled) ReadAllImage.Source = UiIcons.DisabledPlay;
        else if (pause) ReadAllImage.Source = UiIcons.Pause;
        else ReadAllImage.Source = UiIcons.Play;
        StopImage.Source = StopButton.IsEnabled ? UiIcons.Stop : UiIcons.DisabledStop;
        if (pause)
        {
            ReadAllButton.ToolTip = "暂停文章音频";
        }
        else if (ViewModel.Owner == "passage" && ViewModel.PlaybackState == PlaybackState.Paused)
        {
            ReadAllButton.ToolTip = "继续文章音频";
        }
        else
        {
            ReadAllButton.ToolTip = "播放文章音频";
        }
        System.Windows.Automation.AutomationProperties.SetName(ReadAllButton, (string)ReadAllButton.ToolTip);
        if (ViewModel.PlaybackState == PlaybackState.Stopped)
        {
            progressTimer.Stop();
            draggingProgress = false;
        }
        else
        {
            progressTimer.Start();
        }
        UpdateProgress();
    }

    private void ArticleAudioClicked(object sender, RoutedEventArgs e) => ViewModel.ToggleArticleAudio();

    private void StopClicked(object sender, RoutedEventArgs e) => ViewModel.Stop();

    private void UpdateProgress()
    {
        bool active = ViewModel.PlaybackState != PlaybackState.Stopped;
        PlaybackProgress.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        PlaybackProgress.IsEnabled = active && player.IsOpen && player.Duration.TotalSeconds > 0;
        if (draggingProgress && active) return;
        updatingProgress = true;
        try
        {
            PlaybackProgress.Maximum = PlaybackProgress.IsEnabled ? player.Duration.TotalSeconds : 1;
            PlaybackProgress.Value = PlaybackProgress.IsEnabled ? Math.Clamp(player.Position.TotalSeconds, 0, PlaybackProgress.Maximum) : 0;
        }
        finally
        {
            updatingProgress = false;
        }
    }

    private void ProgressChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (updatingProgress || !player.IsOpen || ViewModel.PlaybackState == PlaybackState.Stopped) return;
        player.Position = TimeSpan.FromSeconds(Math.Clamp(e.NewValue, 0, player.Duration.TotalSeconds));
    }

    private void ProgressDragStarted(object sender, DragStartedEventArgs e) => draggingProgress = true;

    private void ProgressDragCompleted(object sender, DragCompletedEventArgs e)
    {
        draggingProgress = false;
        UpdateProgress();
    }
}
