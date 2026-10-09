using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ReadArticles.Rendering;

// An ordinary visual in the reader's adorner layer moves with the reader itself.
internal sealed class SelectionAddAdorner : Adorner
{
    private readonly ParagraphReader reader;
    private readonly Button button;
    private AdornerLayer? layer;
    private Point corner;
    private bool tracking;

    public bool IsShown => layer != null && Visibility == Visibility.Visible;

    public SelectionAddAdorner(ParagraphReader reader) : base(reader)
    {
        this.reader = reader;
        Visibility = Visibility.Hidden;
        var pen = new Pen(Brushes.DodgerBlue, 1.3)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null,
            new RectangleGeometry(new Rect(0, 0, 14, 14))));
        drawing.Children.Add(new GeometryDrawing(Brushes.White, pen,
            new EllipseGeometry(new Point(7, 7), 6, 6)));
        drawing.Children.Add(new GeometryDrawing(null, pen,
            new LineGeometry(new Point(4, 7), new Point(10, 7))));
        drawing.Children.Add(new GeometryDrawing(null, pen,
            new LineGeometry(new Point(7, 4), new Point(7, 10))));
        var icon = new DrawingImage(drawing);
        icon.Freeze();
        button = new Button
        {
            Content = new Image { Source = icon, Width = 14, Height = 14 },
            Width = 14, Height = 14, Background = Brushes.Transparent,
            Padding = new Thickness(0), Margin = new Thickness(0), BorderThickness = new Thickness(0),
            Focusable = false, ToolTip = "将选中文字加入词表"
        };
        AddVisualChild(button);
        button.Click += ButtonClicked;
        reader.LayoutUpdated += ReaderLayoutUpdated;
        reader.LostKeyboardFocus += ReaderLostFocus;
        reader.Unloaded += ReaderUnloaded;
    }

    public void Show()
    {
        if (reader.GetSelection() == null || !reader.IsLoaded || !reader.IsVisible)
        {
            Hide();
            return;
        }
        tracking = true;
        if (layer == null)
        {
            layer = AdornerLayer.GetAdornerLayer(reader);
            if (layer == null) return;
            layer.Add(this);
        }
        Position();
    }

    public void Hide()
    {
        tracking = false;
        Visibility = Visibility.Hidden;
    }

    private void Position()
    {
        if (reader.Selection.IsEmpty || !reader.IsVisible)
        {
            Hide();
            return;
        }
        Rect rectangle = reader.Selection.End.GetCharacterRect(LogicalDirection.Backward);
        bool visible = !rectangle.IsEmpty && rectangle.Height > 0;
        DependencyObject? parent = VisualTreeHelper.GetParent(reader);
        while (visible && parent != null)
        {
            var viewport = parent as ScrollContentPresenter;
            if (viewport != null)
            {
                var bounds = new Rect(viewport.RenderSize);
                visible = bounds.Contains(reader.TranslatePoint(rectangle.BottomRight, viewport));
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        if (visible && corner != rectangle.BottomRight)
        {
            corner = rectangle.BottomRight;
            InvalidateArrange();
        }
    }

    private void ButtonClicked(object sender, RoutedEventArgs e) => reader.RequestSelectionAdd();

    private void ReaderLayoutUpdated(object? sender, EventArgs e)
    {
        if (tracking) Show();
    }

    private void ReaderLostFocus(object sender, KeyboardFocusChangedEventArgs e) => Hide();

    private void ReaderUnloaded(object sender, RoutedEventArgs e)
    {
        Hide();
        if (layer != null) layer.Remove(this);
        layer = null;
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => button;

    protected override Size MeasureOverride(Size constraint)
    {
        button.Measure(new Size(14, 14));
        return AdornedElement.RenderSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        button.Arrange(new Rect(corner, new Size(14, 14)));
        return finalSize;
    }

    // Only the small button intercepts clicks; the rest of the article remains selectable.
    protected override HitTestResult? HitTestCore(PointHitTestParameters parameters) => null;
}
