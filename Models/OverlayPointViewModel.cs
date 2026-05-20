using BioIMA.Core.Models;

namespace BioIMA.Avalonia.Models;

public class OverlayPointViewModel
{
    public PointD Position { get; set; } = new PointD(0, 0);

    public int VertexIndex { get; set; } = -1;

    public double X
    {
        get => Position.X;
        set => Position.X = value;
    }

    public double Y
    {
        get => Position.Y;
        set => Position.Y = value;
    }

    public double Left => X - 5;
    public double Top => Y - 5;
}