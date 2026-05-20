namespace BioIMA.Avalonia.Models;

public class MeasurementRecord
{
    public string Id { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string LabelName { get; set; } = "";
    public string ShapeType { get; set; } = "";

    public string Area { get; set; } = "NA";
    public string AreaPx { get; set; } = "NA";
    public string Perimeter { get; set; } = "NA";
    public string Width { get; set; } = "NA";
    public string Height { get; set; } = "NA";
    public string AspectRatio { get; set; } = "NA";
    public string Circularity { get; set; } = "NA";
    public string EquivalentDiameter { get; set; } = "NA";
    public string LineLength { get; set; } = "NA";
    public string LinePx { get; set; } = "NA";
    public string AngleDegree { get; set; } = "NA";
    public string MeanRgb { get; set; } = "NA";
    public string HexColor { get; set; } = "NA";
    public string Lab { get; set; } = "NA";

    public string MeanR { get; set; } = "NA";
    public string MeanG { get; set; } = "NA";
    public string MeanB { get; set; } = "NA";

    public string LabL { get; set; } = "NA";
    public string LabA { get; set; } = "NA";
    public string LabB { get; set; } = "NA";


    public string ColorClusterK { get; set; } = "NA";
    public string DominantColorHex { get; set; } = string.Empty;
    public string ColorDistribution { get; set; } = string.Empty;
}