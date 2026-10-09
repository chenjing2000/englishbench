using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

internal static class WindowChecks
{
    internal static void Run()
    {
        Program.Run("loaded paragraph hit-testing preserves each owning SID", () =>
        {
            WithReader(reader =>
            {
                foreach (var range in reader.Ranges)
                {
                    var character = reader.PositionAt(range, 3);
                    Rect rect = character.GetCharacterRect(LogicalDirection.Forward);
                    Program.Check(!rect.IsEmpty && rect.Height > 0);
                    var pointer = reader.GetPositionFromPoint(new Point(rect.Left + 1, rect.Top + rect.Height / 2), false);
                    Program.Check(pointer != null && reader.FindSegment(pointer)?.Segment.Sid == range.Segment.Sid);
                }
            });
        });
        Program.Run("loaded selection action emits text and hides on invalid selection", () =>
        {
            WithReader(reader =>
            {
                var range = reader.Ranges[0];
                reader.Selection.Select(reader.PositionAt(range, 0), reader.PositionAt(range, 6));
                WpfTestHelpers.Pump();
                Program.Check(reader.IsSelectionAddVisible);
                ReadArticles.Models.PassageSelection? received = null;
                reader.SelectionRequested += selection => received = selection;
                Program.Check(reader.RequestSelectionAdd() && received?.Text == "public");
                reader.Selection.Select(reader.Ranges[0].Start, reader.Ranges[1].End);
                WpfTestHelpers.Pump();
                Program.Check(!reader.IsSelectionAddVisible);
            });
        });
        Program.Run("reader status occupies space only while a message is present and expires at six seconds", () =>
        {
            var window = new ReadArticles.MainWindow(false) { Left = -10000, Top = -10000, ShowActivated = false };
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

    private static void WithReader(Action<ReadArticles.Rendering.ParagraphReader> check)
    {
        var reader = TestData.Reader();
        var window = new Window
        {
            Content = reader, Width = 600, Height = 300,
            Left = -10000, Top = -10000, ShowActivated = false
        };
        try
        {
            window.Show();
            window.UpdateLayout();
            WpfTestHelpers.Pump();
            check(reader);
        }
        finally { window.Close(); WpfTestHelpers.Pump(); }
    }
}
