using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class WindowChecks
{
    internal static void Run()
    {
        RunPrototype("prototype P01/P02/P04 actual WPF point hit-testing preserves SID", window =>
        {
            var reader = window.Readers[0];
            foreach (var range in reader.Ranges)
            {
                var character = reader.PositionAt(range, 3);
                Rect rect = character.GetCharacterRect(LogicalDirection.Forward);
                Program.Check(!rect.IsEmpty && rect.Height > 0);
                var pointer = reader.GetPositionFromPoint(new Point(rect.Left + 1, rect.Top + rect.Height / 2), false);
                Program.Check(pointer is not null && reader.FindSegment(pointer)?.Segment.Sid == range.Segment.Sid);
            }
        });
        RunPrototype("prototype P09/P10 loaded valid selection shows plus and adds word", window =>
        {
            var reader = window.Readers[0];
            var range = reader.Ranges[0];
            reader.Selection.Select(reader.PositionAt(range, 10), reader.PositionAt(range, 17));
            WpfTestHelpers.Pump();
            Program.Check(reader.IsSelectionAddVisible && reader.GetSelection()?.Text == "matters");
            Program.Check(reader.RequestSelectionAdd());
            Program.Check(window.ViewModel.Words.Contains("matters") && !reader.IsSelectionAddVisible);
        });
        RunPrototype("prototype P07 invalid loaded selection hides add button", window =>
        {
            var reader = window.Readers[0];
            reader.Selection.Select(reader.Ranges[0].Start, reader.Ranges[1].End);
            WpfTestHelpers.Pump();
            Program.Check(!reader.IsSelectionAddVisible);
        });
        RunPrototype("prototype vocabulary updates preserve multi-Segment selection and SID mapping", window =>
        {
            var reader = window.Readers[0];
            reader.Selection.Select(reader.Ranges[0].Start, reader.Ranges[1].End);
            string selected = reader.Selection.Text;
            reader.SetVocabulary(["character", "public life", "words", "matters"], true);
            Program.Check(reader.Selection.Text == selected && reader.GetSelection() is null);
            Program.Check(reader.FindSegment(reader.Ranges[1].Start)?.Segment.Sid == "s002");
        });
        RunPrototype("prototype native renderer layout and snapshot", window =>
        {
            Program.Check(window.Readers.Count == 4 && window.Readers.All(r => r.ActualHeight > 0 && r.ActualWidth > 0));
            Capture("prototype.png");
            ((System.Windows.Controls.ScrollViewer)window.FindName("PassageScroll")).ScrollToEnd();
            window.UpdateLayout();
            WpfTestHelpers.Pump();
            Capture("prototype-blank.png");

            void Capture(string filename)
            {
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                string output = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", filename);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                using var stream = File.Create(output);
                encoder.Save(stream);
                Console.WriteLine("  Screenshot: " + output);
            }
        });
        Program.Run("reader status occupies space only while a message is present and expires at six seconds", () =>
        {
            var window = new EnglishBench.MainWindow(false) { Left = -10000, Top = -10000, ShowActivated = false };
            try
            {
                window.Show();
                var layout = (System.Windows.Controls.Grid)window.Content;
                var status = layout.Children.OfType<System.Windows.Controls.TextBlock>().Single(child => System.Windows.Controls.Grid.GetRow(child) == 1);
                var panels = layout.Children.OfType<FrameworkElement>().Where(child => System.Windows.Controls.Grid.GetRow(child) == 0 && child is not System.Windows.Controls.GridSplitter).ToArray();
                Program.Check(panels.Length == 3);
                window.ViewModel.Status = "";
                WpfTestHelpers.Pump(); window.UpdateLayout();
                Program.Check(status.Visibility == Visibility.Collapsed && layout.RowDefinitions[1].ActualHeight == 0);
                var expandedHeights = panels.Select(panel => panel.ActualHeight).ToArray();
                window.ViewModel.Status = "first";
                WpfTestHelpers.Pump(); window.UpdateLayout();
                Program.Check(status.Visibility == Visibility.Visible && layout.RowDefinitions[1].ActualHeight > 0);
                var messageHeights = panels.Select(panel => panel.ActualHeight).ToArray();
                Program.Check(messageHeights.Where((height, index) => height < expandedHeights[index]).Count() == 3);
                WpfTestHelpers.Pump(3000);
                window.ViewModel.Status = "second";
                var elapsed = Stopwatch.StartNew();
                WpfTestHelpers.Pump(3300);
                if (elapsed.Elapsed.TotalSeconds < 5.9 && window.ViewModel.Status != "second")
                    throw new Exception($"Status cleared early at {elapsed.Elapsed.TotalSeconds:F2}s: '{window.ViewModel.Status}'");
                while (window.ViewModel.Status == "second" && elapsed.Elapsed.TotalSeconds < 8)
                    WpfTestHelpers.Pump(50);
                if (window.ViewModel.Status != "" || elapsed.Elapsed.TotalSeconds < 5.9)
                    throw new Exception($"Status after {elapsed.Elapsed.TotalSeconds:F2}s: '{window.ViewModel.Status}'");
                WpfTestHelpers.Pump(); window.UpdateLayout();
                Program.Check(status.Visibility == Visibility.Collapsed && layout.RowDefinitions[1].ActualHeight == 0);
                Program.Check(panels.Where((panel, index) => Math.Abs(panel.ActualHeight - expandedHeights[index]) < 0.01).Count() == 3);
            }
            finally
            {
                window.Close();
                WpfTestHelpers.Pump();
            }
        });
    }

    private static void RunPrototype(string name, Action<EnglishBench.PrototypeWindow> check)
    {
        Program.Run(name, () =>
        {
            var window = new EnglishBench.PrototypeWindow
            {
                Left = -10000,
                Top = -10000,
                ShowActivated = false,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize
            };
            try
            {
                window.Show();
                window.UpdateLayout();
                WpfTestHelpers.Pump();
                check(window);
            }
            finally
            {
                window.Close();
                WpfTestHelpers.Pump();
            }
        });
    }
}
