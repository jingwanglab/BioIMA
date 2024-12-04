using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

public class LineDragThumb : Thumb
{
    public LineDragThumb()
    {
        base.Background = Brushes.AliceBlue;
        base.Width = 5;
        base.Height = 5;
    }
}
public class LinesAdorner : Adorner
{
    private readonly Line line;
    private readonly VisualCollection visuals;
    private readonly LineDragThumb startThumb;
    private readonly LineDragThumb endThumb;

    public LinesAdorner(Line adornedLine) : base(adornedLine)
    {
        this.line = adornedLine;
        this.visuals = new VisualCollection(this);

        this.startThumb = new LineDragThumb();
        this.startThumb.DragDelta += StartThumb_DragDelta;

        this.endThumb = new LineDragThumb();
        this.endThumb.DragDelta += EndThumb_DragDelta;

        this.visuals.Add(this.startThumb);
        this.visuals.Add(this.endThumb);

        PositionThumbs();
    }

    private void PositionThumbs()
    {
        SetThumbPosition(this.startThumb, this.line.X1, this.line.Y1);
        SetThumbPosition(this.endThumb, this.line.X2, this.line.Y2);
    }

    private void SetThumbPosition(Thumb thumb, double x, double y)
    {
        Canvas.SetLeft(thumb, x - thumb.Width / 2);
        Canvas.SetTop(thumb, y - thumb.Height / 2);
    }

    private void StartThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        this.line.X1 += e.HorizontalChange;
        this.line.Y1 += e.VerticalChange;
        PositionThumbs();
    }

    private void EndThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        this.line.X2 += e.HorizontalChange;
        this.line.Y2 += e.VerticalChange;
        PositionThumbs();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        this.startThumb.Arrange(new Rect(new Point(this.line.X1 - this.startThumb.Width / 2, this.line.Y1 - this.startThumb.Height / 2), this.startThumb.RenderSize));
        this.endThumb.Arrange(new Rect(new Point(this.line.X2 - this.endThumb.Width / 2, this.line.Y2 - this.endThumb.Height / 2), this.endThumb.RenderSize));

        return finalSize;
    }

    protected override int VisualChildrenCount => this.visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        return this.visuals[index];
    }
}
