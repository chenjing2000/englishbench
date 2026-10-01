using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EnglishBench.Infrastructure;

namespace EnglishBench;

public partial class MainWindow
{
    private readonly ReaderSettings settings;
    private readonly bool persistSettings;
    private readonly DispatcherTimer statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };

    private void InitializeWindowSettings()
    {
        Resources[SystemParameters.VerticalScrollBarWidthKey] = SystemParameters.VerticalScrollBarWidth / 2;
        var workArea = SystemParameters.WorkArea;
        Width = workArea.Width * 0.7;
        Height = workArea.Height * 0.7;
        MinWidth = Math.Min(1050, Width);
        MinHeight = Math.Min(620, Height);
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;

        // Keep all three columns within the smaller startup window.
        double contentWidth = Math.Max(0, Width - 40);
        double panelScale = Math.Min(1, contentWidth / 906);
        LeftColumn.MinWidth = 200 * panelScale;
        CenterColumn.MinWidth = 410 * panelScale;
        RightColumn.MinWidth = 280 * panelScale;
        LeftColumn.Width = new GridLength(Finite(settings.LeftWidth, 310, LeftColumn.MinWidth, Math.Max(LeftColumn.MinWidth, contentWidth * 0.22)));
        RightColumn.Width = new GridLength(Finite(settings.RightWidth, 390, RightColumn.MinWidth, Math.Max(RightColumn.MinWidth, contentWidth * 0.28)));

        int fontPoints = 11;
        if (double.IsFinite(settings.FontSize))
        {
            fontPoints = (int)Math.Clamp(Math.Round(settings.FontSize * 72 / 96), 11, 13);
        }
        ApplyReadingFont(fontPoints);
        player.Volume = double.IsFinite(settings.Volume) ? Math.Clamp(settings.Volume, 0, 1) : 0.65;
        ViewModel.Accent = settings.Accent;
        RefreshSettingsChecks();
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Status)) RestartStatusTimer();
    }

    private void StatusTimerTick(object? sender, EventArgs e)
    {
        statusTimer.Stop();
        ViewModel.Status = "";
    }

    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (!persistSettings) return;
        settings.Width = RestoreBounds.Width;
        settings.Height = RestoreBounds.Height;
        settings.LeftWidth = LeftColumn.ActualWidth;
        settings.RightWidth = RightColumn.ActualWidth;
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        double volume = player.Volume;
        progressTimer.Stop();
        statusTimer.Stop();
        ViewModel.Stop();
        player.Dispose();
        if (!persistSettings) return;

        settings.Volume = volume;
        settings.Accent = ViewModel.Accent;
        settings.HighlightsVisible = ViewModel.HighlightsVisible;
        settings.Library = ViewModel.LibraryRoot;
        settings.Passage = ViewModel.Article?.FilePath;
        try
        {
            settings.Save();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Trace.WriteLine(error.Message);
        }
    }

    private void AccentClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            ViewModel.Accent = (string)item.Tag;
            RefreshSettingsChecks();
        }
    }

    private void RestartStatusTimer()
    {
        statusTimer.Stop();
        if (!string.IsNullOrEmpty(ViewModel.Status)) statusTimer.Start();
    }

    private void SettingsClicked(object sender, RoutedEventArgs e)
    {
        SettingsButton.ContextMenu.PlacementTarget = SettingsButton;
        SettingsButton.ContextMenu.IsOpen = true;
    }

    private void FontClicked(object sender, RoutedEventArgs e) => ApplyReadingFont(int.Parse((string)((MenuItem)sender).Tag));

    private void ApplyReadingFont(int points)
    {
        settings.FontSize = points * 96.0 / 72;
        foreach (var reader in readers) reader.FontSize = settings.FontSize;
        VocabularyList.FontSize = settings.FontSize;
        RefreshSettingsChecks();
    }

    private void RefreshSettingsChecks()
    {
        Font11Option.IsChecked = Math.Abs(settings.FontSize - 11.0 * 96 / 72) < 0.01;
        Font12Option.IsChecked = Math.Abs(settings.FontSize - 12.0 * 96 / 72) < 0.01;
        Font13Option.IsChecked = Math.Abs(settings.FontSize - 13.0 * 96 / 72) < 0.01;
        AccentUkOption.IsChecked = ViewModel.Accent == "uk";
        AccentUsOption.IsChecked = ViewModel.Accent == "us";
    }

    private static double Finite(double value, double fallback, double min, double max) => double.IsFinite(value) ? Math.Clamp(value, min, Math.Max(min, max)) : fallback;
}
