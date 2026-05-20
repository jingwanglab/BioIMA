using Avalonia.Media;
using System;
using System.Linq;
using System.Collections.Generic;
namespace BioIMA.Avalonia.Models;

public class ColorAnalysisResult
{
    public int ClusterCount { get; set; } = 5; //kmeans color cluster 的 K 字段
    
    public long PixelCount { get; set; }

    public double MeanR { get; set; }

    public double MeanG { get; set; }

    public double MeanB { get; set; }

    public byte MeanRByte => (byte)Math.Clamp(Math.Round(MeanR), 0, 255);

    public byte MeanGByte => (byte)Math.Clamp(Math.Round(MeanG), 0, 255);

    public byte MeanBByte => (byte)Math.Clamp(Math.Round(MeanB), 0, 255);

    public string MeanRgbText => $"{MeanR:F0}, {MeanG:F0}, {MeanB:F0}";

    public string MeanHex { get; set; } = "NA";

    public double LabL { get; set; }

    public double LabA { get; set; }

    public double LabB { get; set; }

    public string LabText => $"{LabL:F2}, {LabA:F2}, {LabB:F2}";

    public Color MeanColor => Color.FromRgb(MeanRByte, MeanGByte, MeanBByte);

    public List<ColorClusterRecord> Clusters { get; set; } = new();

    public ColorClusterRecord? DominantCluster => Clusters.Count > 0 ? Clusters[0] : null;

    public string DominantColorHex => DominantCluster?.Hex ?? "NA";

    public string ColorDistributionText =>
        Clusters.Count == 0
            ? "NA"
            : string.Join("; ", Clusters.Select(c => $"{c.Hex}:{c.Percentage:F1}%"));
}