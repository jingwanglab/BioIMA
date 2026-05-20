using System;                    // Math 在这里
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BioIMA.Avalonia.Models;

namespace BioIMA.Avalonia.Views;

public partial class AverageColorWindow : Window
{
    public bool Confirmed { get; private set; }
    public ColorAnalysisResult CurrentResult { get; private set; }

    private readonly string? _imagePath;
    private readonly Func<int, ColorAnalysisResult?>? _recalculate;

    public AverageColorWindow(
        ColorAnalysisResult result,
        string? imagePath = null,
        Bitmap? previewBitmap = null,
        Func<int, ColorAnalysisResult?>? recalculate = null,
        int initialClusterCount = 5)
    {
        InitializeComponent();

        _imagePath = imagePath;
        _recalculate = recalculate;
        CurrentResult = result;

        //ClusterCountBox.Value = initialClusterCount;
        ClusterCountComboBox.SelectedIndex = Math.Clamp(initialClusterCount - 2, 0, 10);

        //  如果有 previewBitmap 优先用它，否则从 imagePath 加载
        if (previewBitmap is not null)
        {
            OverviewImage.Source = previewBitmap;
            OverviewPanel.IsVisible = true;
        }
        else if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            OverviewImage.Source = new Bitmap(imagePath);
            OverviewPanel.IsVisible = true;
        }

        ApplyResult(result);

        RecalculateButton.Click += (_, _) =>
        {
            RecalculateColorDistribution();
        };

        SaveButton.Click += (_, _) =>
        {
            Confirmed = true;
            Close();
        };

        CancelButton.Click += (_, _) =>
        {
            Confirmed = false;
            Close();
        };

        SaveReportButton.Click += async (_, _) =>
        {
            await SaveReportPngAsync();
        };
    }

    // 重新计算颜色分布，k 为聚类数
    private void RecalculateColorDistribution()
    {
        if (_recalculate is null)
            return;

        var selectedItem = ClusterCountComboBox.SelectedItem as ComboBoxItem;
        var kText = selectedItem?.Content?.ToString();

        int k = 5;

        if (!string.IsNullOrWhiteSpace(kText) && int.TryParse(kText, out var parsed))
            k = parsed;

        k = Math.Clamp(k, 2, 12);

        var newResult = _recalculate(k);

        if (newResult is null)
            return;

        CurrentResult = newResult;
        ApplyResult(newResult);
    }

    // 把 result 数据刷新到 UI 上
    private void ApplyResult(ColorAnalysisResult result)
    {
        RgbTextBlock.Text = $"RGB: {result.MeanRgbText}";
        HexTextBlock.Text = $"Hex: {result.MeanHex}";
        LabTextBlock.Text = $"Lab: {result.LabText}";
        ColorPreview.Background = new SolidColorBrush(result.MeanColor);

        // 先清空再赋值，强制 ItemsControl 刷新
        ColorClusterItems.ItemsSource = null;
        ColorClusterItems.ItemsSource = result.Clusters;
    }

    private async Task SaveReportPngAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        // 临时--------看看传进来的是什么
        //Console.WriteLine($"[DEBUG] _imagePath = '{_imagePath}'");

        var baseName = string.IsNullOrEmpty(_imagePath)
            ? "color_report"
            : Path.GetFileNameWithoutExtension(_imagePath);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save color analysis report",
                SuggestedFileName = $"{baseName}_color_report.png",
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG image")
                    {
                        Patterns = ["*.png"]
                    }
                ]
            });

        if (file is null)
            return;

        var width = Math.Max(1, (int)Math.Ceiling(ReportRoot.Bounds.Width));
        var height = Math.Max(1, (int)Math.Ceiling(ReportRoot.Bounds.Height));

        var bitmap = new RenderTargetBitmap(
            new PixelSize(width, height),
            new Vector(96, 96));

        bitmap.Render(ReportRoot);

        await using var stream = await file.OpenWriteAsync();
        bitmap.Save(stream);
    }
}