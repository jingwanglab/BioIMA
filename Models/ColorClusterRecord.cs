using Avalonia.Media;

namespace BioIMA.Avalonia.Models;

public class ColorClusterRecord
{
    public int Rank { get; set; }

    public long PixelCount { get; set; }

    public double Percentage { get; set; }

    public string PercentageText => $"{Percentage:F1}%";

    public byte R { get; set; }

    public byte G { get; set; }

    public byte B { get; set; }

    public string Hex { get; set; } = "NA";

    public double LabL { get; set; }

    public double LabA { get; set; }

    public double LabB { get; set; }

    public IBrush Brush => new SolidColorBrush(Color.FromRgb(R, G, B));
}