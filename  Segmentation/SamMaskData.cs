using System.Collections.Generic;

namespace BioIMA.Avalonia.Segmentation;

public sealed class SamMaskData
{
    public List<float> Mask { get; set; } = new();
    public int[] Shape { get; set; } = new int[0];
    public List<float> IoU { get; set; } = new();
}