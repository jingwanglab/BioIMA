using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

public class GridLinesAdorner : Adorner
{
    public GridLinesAdorner(UIElement adornedElement) : base(adornedElement)
    {
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var adornedElement = AdornedElement as FrameworkElement;
        if (adornedElement == null) return;

        // 获取单元格的边界
        var cellBounds = new Rect(0, 0, adornedElement.ActualWidth, adornedElement.ActualHeight);
        drawingContext.DrawRectangle(null, new Pen(Brushes.LightGray, 1), cellBounds);
    }
}
