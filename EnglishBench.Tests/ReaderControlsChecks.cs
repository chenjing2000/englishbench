using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using EnglishBench.Infrastructure;
using EnglishBench.Services;

internal static class ReaderControlsChecks
{
    internal static void Run()
    {
        Program.Run("folder selection discovers names without opening any JSON or content", () =>
        {
            var window = CreateWindow();
            string root = Path.GetFullPath(Path.Combine("artifacts", "controls-tests", Guid.NewGuid().ToString("N")));
            try
            {
                Directory.CreateDirectory(root);
                window.Show();
                Program.Check(window.ViewModel.OpenArticle(TestData.ArticlePath));
                string book = Path.Combine(root, "Book"); Directory.CreateDirectory(book);
                File.WriteAllText(Path.Combine(book, "book.json"), "marker");
                string path = Path.Combine(book, "Locked.json"); File.WriteAllText(path, "not JSON");
                using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                Program.Check(window.ViewModel.OpenLibrary(root));
                Program.Check(window.ViewModel.Books.Count == 1 && MainLeaves(window).Single().Name == "Locked");
                Program.Check(window.ViewModel.Article is null && window.ViewModel.Words.Count == 0 && window.Readers.Count == 0);
            }
            finally
            {
                try { window.Close(); WpfTestHelpers.Pump(); }
                finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
            }
        });
        Program.Run("bottom circular settings offers upward font/accent menus and shared sizing", () =>
        {
            var window = CreateWindow();
            try
            {
                Directory.CreateDirectory(Path.GetFullPath("artifacts"));
                window.Show();
                Program.Check(window.ViewModel.OpenLibrary(TestData.LibraryRoot));
                Program.Check(window.SelectPassage(TestData.ArticlePath)); WpfTestHelpers.Pump(30);
                var button = (Button)window.FindName("SettingsButton");
                window.Left = SystemParameters.WorkArea.Left + 40;
                window.Top = SystemParameters.WorkArea.Top + 40;
                Program.Check(button.ContextMenu.Placement == PlacementMode.Top);
                Program.Check(Math.Abs(button.ContextMenu.FontSize - 10.0 * 96 / 72) < 0.001);
                Program.Check(button.ContextMenu.Items.Count == 2 && window.FindName("AccentSelector") is null);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Program.Check(button.ContextMenu.IsOpen); WpfTestHelpers.Pump(30);
                var surface = System.Windows.Media.VisualTreeHelper.GetChild(button.ContextMenu, 0) as Border;
                Program.Check(surface?.CornerRadius == new CornerRadius(10));
                var fonts = (MenuItem)button.ContextMenu.Items[0];
                Program.Check(fonts.HorizontalContentAlignment == HorizontalAlignment.Left);
                fonts.IsSubmenuOpen = true; WpfTestHelpers.Pump(30);
                var popup = (Popup)fonts.Template.FindName("PART_Popup", fonts);
                Program.Check(popup.IsOpen && popup.Child is Border { CornerRadius.TopLeft: 10 });
                var origin = surface!.PointToScreen(new Point(0, surface.ActualHeight));
                var buttonEdge = button.PointToScreen(new Point(button.ActualWidth, 0));
                var scale = System.Windows.PresentationSource.FromVisual(button)!.CompositionTarget!.TransformToDevice;
                Console.WriteLine($"  Settings panels: {surface.ActualWidth}, {((Border)popup.Child).ActualWidth}; gaps: {(origin.X - buttonEdge.X) / scale.M11}, {(buttonEdge.Y - origin.Y) / scale.M22}");
                Program.Check(Math.Abs(surface.ActualWidth / 134 - 0.6) < 0.02);
                Program.Check(Math.Abs(((Border)popup.Child).ActualWidth / 134 - 0.6) < 0.02);
                Program.Check(Math.Abs((origin.X - buttonEdge.X) / scale.M11 - 7) < 1);
                Program.Check(Math.Abs((buttonEdge.Y - origin.Y) / scale.M22 - 4) < 1);
                foreach (var item in button.ContextMenu.Items.Cast<MenuItem>().Concat(button.ContextMenu.Items.Cast<MenuItem>().SelectMany(item => item.Items.Cast<MenuItem>())))
                    Program.Check(Math.Abs(item.FontSize - 10.0 * 96 / 72) < 0.001);
                var menuImage = new System.Windows.Media.Imaging.RenderTargetBitmap((int)button.ContextMenu.ActualWidth, (int)button.ContextMenu.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                menuImage.Render(button.ContextMenu);
                var menuEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                menuEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(menuImage));
                using (var menuFile = File.Create(Path.GetFullPath("artifacts/settings-menu.png"))) menuEncoder.Save(menuFile);
                var submenu = (Border)popup.Child;
                var submenuImage = new System.Windows.Media.Imaging.RenderTargetBitmap((int)(submenu.ActualWidth + submenu.Margin.Left + submenu.Margin.Right), (int)(submenu.ActualHeight + submenu.Margin.Top + submenu.Margin.Bottom), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                submenuImage.Render(submenu);
                var submenuEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                submenuEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(submenuImage));
                using (var submenuFile = File.Create(Path.GetFullPath("artifacts/settings-submenu.png"))) submenuEncoder.Save(submenuFile);
                fonts.IsSubmenuOpen = false; button.ContextMenu.IsOpen = false;
                window.Left = -10000; window.Top = -10000;
                foreach (int points in new[] { 11, 12, 13 })
                {
                    var option = (MenuItem)((MenuItem)button.ContextMenu.Items[0]).Items[points - 11];
                    option.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); WpfTestHelpers.Pump(30);
                    double expected = points * 96.0 / 72;
                    Program.Check(window.Readers.All(r => Math.Abs(r.FontSize - expected) < 0.001));
                    Program.Check(Math.Abs(((ListBox)window.FindName("VocabularyList")).FontSize - expected) < 0.001);
                    CheckIndentedParagraphs(window);
                }
                var accents = (MenuItem)button.ContextMenu.Items[1];
                ((MenuItem)accents.Items[1]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Program.Check(window.ViewModel.Accent == "us");
                ((MenuItem)accents.Items[0]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Program.Check(window.ViewModel.Accent == "uk");
            }
            finally
            {
                window.Close(); WpfTestHelpers.Pump();
            }
        });
        Program.Run("native segment and vocabulary playback show seekable progress; stop resets it", () =>
        {
            var window = CreateWindow();
            try
            {
                Directory.CreateDirectory(Path.GetFullPath("artifacts"));
                window.Show();
                LoadArticle(window);
                var player = (WpfAudioPlayer)typeof(EnglishBench.MainWindow).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
                player.Volume = 0;
                var slider = (Slider)window.FindName("PlaybackProgress");
                var settingsButton = (Button)window.FindName("SettingsButton");
                ((MenuItem)((MenuItem)settingsButton.ContextMenu.Items[0]).Items[0]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                var reader = window.Readers[0];
                Program.Check(reader.RequestSegmentPlayback(reader.PositionAt(reader.Ranges[0], 0)));
                WpfTestHelpers.Wait(() => slider.IsEnabled && slider.Maximum > 1);
                Program.Check(slider.Visibility == Visibility.Visible);
                player.Pause();
                slider.RaiseEvent(new DragStartedEventArgs(0, 0) { RoutedEvent = Thumb.DragStartedEvent });
                slider.Value = slider.Maximum / 2; WpfTestHelpers.Pump(40);
                slider.RaiseEvent(new DragCompletedEventArgs(0, 0, false) { RoutedEvent = Thumb.DragCompletedEvent });
                Program.Check(Math.Abs(player.Position.TotalSeconds - slider.Maximum / 2) < 0.2);
                window.UpdateLayout();
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using (var image = File.Create(Path.GetFullPath("artifacts/reader-controls.png"))) encoder.Save(image);
                window.ViewModel.Stop();
                Program.Check(slider.Visibility == Visibility.Collapsed && slider.Value == 0);
                window.ViewModel.PlayWord(window.ViewModel.Words[0].Entry, "uk");
                WpfTestHelpers.Wait(() => slider.IsEnabled);
                Program.Check(slider.Visibility == Visibility.Visible);
                window.ViewModel.Stop();
            }
            finally
            {
                window.Close(); WpfTestHelpers.Pump();
            }
        });
        Program.Run("bottom play uses the first sibling MP3, pauses only that target and global stop clears audio", () =>
        {
            var window = CreateWindow();
            string root = Path.GetFullPath(Path.Combine("artifacts", "controls-tests", Guid.NewGuid().ToString("N")));
            try
            {
                Directory.CreateDirectory(root);
                window.Show();
                LoadArticle(window);
                var fixtureWord = window.ViewModel.Words[0].Entry;
                string path = Path.Combine(root, "Short.json");
                File.Copy(TestData.ArticlePath, path);
                string audioRoot = Path.GetDirectoryName(TestData.ArticlePath)!;
                string sourceMp3 = Path.Combine(audioRoot, "audio_segments", "s001_uk.mp3");
                // Create the later name first so enumeration order cannot choose the file.
                File.Copy(sourceMp3, Path.Combine(root, "Z-last.mp3"));
                File.Copy(sourceMp3, Path.Combine(root, "a-first.MP3"));
                string wordPath = Path.Combine(root, fixtureWord.Audio.Uk);
                Directory.CreateDirectory(Path.GetDirectoryName(wordPath)!);
                File.Copy(Path.Combine(audioRoot, fixtureWord.Audio.Uk), wordPath);
                string nested = Path.Combine(root, "nested"); Directory.CreateDirectory(nested);
                File.Copy(sourceMp3, Path.Combine(nested, "00-ignored.mp3"));
                File.WriteAllText(Path.Combine(root, "00-ignored.wav"), "ignored");
                if (!window.ViewModel.OpenArticle(path)) throw new Exception(window.ViewModel.Status);
                var playButton = (Button)window.FindName("ReadAllButton");
                var slider = (Slider)window.FindName("PlaybackProgress");
                var player = (WpfAudioPlayer)typeof(EnglishBench.MainWindow).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
                player.Volume = 0;
                window.ViewModel.PlayWord(fixtureWord, "uk");
                WpfTestHelpers.Wait(() => slider.IsEnabled);
                playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                WpfTestHelpers.Wait(() => slider.IsEnabled);
                Program.Check(window.ViewModel.Owner == "passage" && window.ViewModel.PlayingSid is null);
                var media = (System.Windows.Media.MediaPlayer)typeof(WpfAudioPlayer).GetField("media", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player)!;
                Program.Check(media.Source.LocalPath == Path.Combine(root, "a-first.MP3"));
                Program.Check(WpfTestHelpers.IsIcon(((Image)playButton.Content).Source, UiIcons.Pause));
                playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Program.Check(window.ViewModel.PlaybackState == PlaybackState.Paused);
                slider.Value = slider.Maximum / 2; WpfTestHelpers.Pump(100);
                Program.Check(window.ViewModel.PlaybackState == PlaybackState.Paused && Math.Abs(player.Position.TotalSeconds - slider.Maximum / 2) < 0.2);
                window.ViewModel.Accent = "us";
                Program.Check(window.ViewModel.PlaybackState == PlaybackState.Paused);
                playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Program.Check(window.ViewModel.PlaybackState == PlaybackState.Playing && media.Source.LocalPath == Path.Combine(root, "a-first.MP3"));
                ((Button)window.FindName("StopButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Program.Check(window.ViewModel.PlaybackState == PlaybackState.Stopped && slider.Visibility == Visibility.Collapsed);
                playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                WpfTestHelpers.Wait(() => slider.IsEnabled);
                slider.Value = slider.Maximum - 0.1;
                WpfTestHelpers.Wait(() => window.ViewModel.PlaybackState == PlaybackState.Stopped);
                Program.Check(window.ViewModel.PlayingSid is null && slider.Visibility == Visibility.Collapsed);
                // A playing word is not a pause target for the bottom button.
                Program.Check(window.ViewModel.OpenArticle(TestData.ArticlePath));
                playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Program.Check(window.ViewModel.PlaybackState == PlaybackState.Stopped &&
                    window.ViewModel.Status.Contains("同级目录") && !player.IsOpen);
                window.ViewModel.PlayWord(window.ViewModel.Words[0].Entry, "uk");
                WpfTestHelpers.Wait(() => slider.IsEnabled);
                playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Program.Check(window.ViewModel.PlaybackState == PlaybackState.Playing && window.ViewModel.Owner.StartsWith("word:") && window.ViewModel.Status.Contains("MP3"));
                window.ViewModel.PlayWord(window.ViewModel.Words[0].Entry, "uk");
                WpfTestHelpers.Wait(() => slider.IsEnabled);
                ((Button)window.FindName("StopButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Program.Check(window.ViewModel.PlaybackState == PlaybackState.Stopped && slider.Visibility == Visibility.Collapsed);
            }
            finally
            {
                try { window.Close(); WpfTestHelpers.Pump(); }
                finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
            }
        });
    }
    private static EnglishBench.MainWindow CreateWindow() => new EnglishBench.MainWindow(false)
    {
        ShowActivated = false, Left = -10000, Top = -10000
    };
    private static void LoadArticle(EnglishBench.MainWindow window)
    {
        Program.Check(window.ViewModel.OpenLibrary(TestData.LibraryRoot));
        Program.Check(window.SelectPassage(TestData.ArticlePath));
        WpfTestHelpers.Pump(30);
    }
    private static IEnumerable<EnglishBench.Models.NavigationNode> MainLeaves(EnglishBench.MainWindow window) => EnglishBench.ViewModels.MainViewModel.AllLeaves(window.ViewModel.Books);
    private static void CheckIndentedParagraphs(EnglishBench.MainWindow window)
    {
        var host = (StackPanel)window.FindName("ParagraphHost");
        Program.Check(host.Children.Cast<object>().All(c => c is EnglishBench.Rendering.ParagraphReader));
        Program.Check(!WpfTestHelpers.Descendants(window).OfType<Button>().Any(b => Equals(b.Content, "导出")));
        foreach (var reader in window.Readers)
        {
            var paragraph = (System.Windows.Documents.Paragraph)reader.Document.Blocks.FirstBlock!;
            Program.Check(Math.Abs(paragraph.TextIndent - reader.FontSize * 2) < 0.001);
            reader.SetVocabulary(["new test term"], true);
            paragraph = (System.Windows.Documents.Paragraph)reader.Document.Blocks.FirstBlock!;
            Program.Check(Math.Abs(paragraph.TextIndent - reader.FontSize * 2) < 0.001);
        }
    }
}
