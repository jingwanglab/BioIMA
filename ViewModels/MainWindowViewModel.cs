using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Magick = ImageMagick;
// Avalonia 基础引用
using global::Avalonia;
using global::Avalonia.Collections;
using global::Avalonia.Controls;
using global::Avalonia.Media;
using global::Avalonia.Media.Imaging;
using global::Avalonia.Platform.Storage;
using global::Avalonia.Threading;
using global::Avalonia.Platform;
// MVVM 工具
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
// 项目内部引用
using BioIMA.Core.Enums;
using BioIMA.Core.Models;
using BioIMA.Avalonia.Models;
using BioIMA.Avalonia.Views;
using BioIMA.Avalonia.Segmentation;
using BioIMA.Avalonia.Services;
//确定简称
using Point = Avalonia.Point; 
using Window = Avalonia.Controls.Window;

namespace BioIMA.Avalonia.ViewModels;

// // ────────────────────────────────────────────────────────────────────────────
// // Fix 1: OverlayPointViewModel 需要有 Left / Top 属性
// //         XAML 里 Canvas.Left="{Binding Left}" 绑定的这里
// //         偏移 -5 让 10×10 的椭圆中心对准坐标点
// // ────────────────────────────────────────────────────────────────────────────
// public class OverlayPointViewModel
// {
//     public double X { get; set; }
//     public double Y { get; set; }

//     public double Left => X - 5;
//     public double Top  => Y - 5;
// }

// ────────────────────────────────────────────────────────────────────────────
// OverlayShapeViewModel — 已完成形状的显示模型
// ────────────────────────────────────────────────────────────────────────────
// public class OverlayShapeViewModel
// {
//     public string ShapeType { get; set; } = string.Empty;
//     public string Stroke    { get; set; } = "#FFFFFF";
//     public string Fill      { get; set; } = "#00000000";

//     public List<OverlayPointViewModel> Points { get; set; } = new();

//     // XAML 里 Polygon / Polyline 的 Points 属性需要 "x1,y1 x2,y2 ..." 格式
//     public string PolygonPoints  => string.Join(" ", Points.Select(p => $"{p.X},{p.Y}"));
//     public string PolylinePoints => PolygonPoints;
// }

// ────────────────────────────────────────────────────────────────────────────
// MainWindowViewModel
// ────────────────────────────────────────────────────────────────────────────
public partial class MainWindowViewModel : ViewModelBase
{
    private Window? _hostWindow;

    // label 自动编号
    private int _labelCounter = 1;

    // Polyline 的点列表复用同一个对象，避免每次 get 重新创建
    ///private readonly AvaloniaList<Point> _polylinePoints = new();

    // ── 1. 构造函数 ──────────────────────────────────────────────────────────
    public MainWindowViewModel()
    {
        Labels = new ObservableCollection<LabelItem>();
        Labels.Add(new LabelItem { Name = "label1" });
        Labels.Add(new LabelItem { Name = "label2" });
        Labels.Add(new LabelItem { Name = "auto-mask-1" });
    }

    public void SetHostWindow(Window window)
    {
        _hostWindow = window;
    }

    // ── 工具方法 ─────────────────────────────────────────────────────────────
    private static bool IsSupportedImageFile(string filePath)
    {
        var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff",".heic", ".heif" };
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return supportedExtensions.Contains(extension);
    }
    //------------导出结果辅助方法-----------------
    private static string CsvEscape(string? value)
    {
        value ??= "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ObservableProperty — UI 绑定的基础属性
    // ════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private string    statusText        = "Ready";
    [ObservableProperty] private string    lengthText        = "-";
    [ObservableProperty] private string    areaText          = "-";
    [ObservableProperty] private string    pixelText         = "-";
    [ObservableProperty] private string    currentLabelText  = "None";
    [ObservableProperty] private string    currentToolText   = "None";
    [ObservableProperty] private string    currentFolderText = "No folder selected";
    [ObservableProperty] private double scaleKnownLength = 1.0;
    [ObservableProperty] private Bitmap?   currentImage;
    [ObservableProperty] private string? currentImagePath;  //file path

    [ObservableProperty] private ToolMode  currentTool       = ToolMode.None;

    [ObservableProperty] private Bitmap? segmentationMaskBitmap;
    [ObservableProperty] private bool hasSegmentationMask = false;
    [ObservableProperty] private bool isSamModelLoaded = false;
    [ObservableProperty] private bool isSamEmbeddingReady = false;

    [ObservableProperty] private ImageFileItem?   selectedImageFile;
    [ObservableProperty] private AnnotationLabel? selectedAnnotationLabel;
    [ObservableProperty] private LabelItem?       selectedLabel;


    //自动分割面板属性
    [ObservableProperty] private bool isSegmentationPanelVisible = false;
    [ObservableProperty] private string selectedSegmentationModel = "";
    [ObservableProperty] private bool isSegmentationModelChosen = false;

    [ObservableProperty] private bool isSamEncoding = false;
    [ObservableProperty] private double samEncodingProgress = 0;
    [ObservableProperty] private string samEncodingText = "";
    [ObservableProperty] private SamOpType currentSamPointMode = SamOpType.Add;
    [ObservableProperty] private bool isSamBoxDragging = false;

    // 图像在 Canvas 中的显示参数（由 code-behind 在每次点击时更新）
    [ObservableProperty] private double imageDisplayOffsetX;
    [ObservableProperty] private double imageDisplayOffsetY;
    [ObservableProperty] private double imageDisplayWidth;
    [ObservableProperty] private double imageDisplayHeight;
    [ObservableProperty] private double imagePixelWidth;
    [ObservableProperty] private double imagePixelHeight;
    //全局的缩放
    [ObservableProperty] private double viewZoom = 1.0;
    //全局的Ruler
    [ObservableProperty] private AnnotationShape? currentRulerShape;
    [ObservableProperty] private double pixelsPerUnit = 1.0;
    [ObservableProperty] private string rulerUnit = "mm";
    [ObservableProperty] private bool hasValidScale = false;
    [ObservableProperty] private double lastRulerPixelLength;
    [ObservableProperty] private bool scaleDialogPending;

    //颜色的默认k-means。k。每次改 K 并 Save 后，可把这个 K 记住，下一次窗口默认用上一次的 K
    [ObservableProperty] private int colorClusterCount = 5;
    
    // ════════════════════════════════════════════════════════════════════════
    // 集合属性 yu 字段区
    // ════════════════════════════════════════════════════════════════════════

    public ObservableCollection<ImageFileItem>   ImageFiles       { get; } = new();
    public ObservableCollection<LabelItem>       Labels           { get; }
    public ObservableCollection<AnnotationLabel> AnnotationLabels { get; } = new();
    public ObservableCollection<MeasurementRow> MeasurementRows { get; } = new();
    public string StrokeColor { get; set; } = "#00BCD4";
    public string FillColor   { get; set; } = "#5500BCD4";

    private AnnotationShape? _draggingShape;
    private int _draggingVertexIndex = -1;

    // private wpf522.SAM? _sam;
    // private float[]? _samEmbedding;
    // private List<wpf522.Promotion> _samPromotions = new();
    private SamPredictor? _sam;
    private float[]? _samEmbedding;
    private List<SamPromotion> _samPromotions = new();

    private PointD? _samBoxStartImagePoint;
    private PointD? _samBoxEndImagePoint;
    //计算属性
    public bool IsDraggingVertex => _draggingShape is not null && _draggingVertexIndex >= 0;
    
    public bool IsDeleteBoxVisible => CurrentTool == ToolMode.DeleteBox && CurrentDrawingPoints.Count >= 2;
    
    public bool ShowDeleteBoxHint => CurrentTool == ToolMode.DeleteBox;

    public bool ShowSegmentationModelChooser => IsSegmentationPanelVisible && !IsSegmentationModelChosen;
    public bool ShowSegmentationPromptTools => IsSegmentationPanelVisible && IsSegmentationModelChosen;
    public string DeleteBoxHintText => "Click two corners to draw a delete box.";

    //segment mask 字段
    private float[]? _lastSamMask;
    private int _lastSamMaskWidth;
    private int _lastSamMaskHeight;

    private readonly Dictionary<string, MaskLabelData> _maskLabelDataByLabelId = new();

    private sealed class MaskLabelData
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public byte[] Mask { get; set; } = Array.Empty<byte>();

        // 只用于 preview / overlay，不用于测量
        public List<PointD> DisplayContour { get; set; } = new();
    }

    //本次颜色结果字段
    private (double R, double G, double B, string Hex, double L, double A, double B2)? _pendingColorMeasurement;
    // 当前图片的标注文档（切换图片时重置）
    public ImageAnnotationDocument CurrentAnnotationDocument { get; private set; } = new();

    // 正在绘制中的点（存图像坐标）
    public ObservableCollection<PointD> CurrentDrawingPoints { get; } = new();

    // ════════════════════════════════════════════════════════════════════════
    // 计算属性 — 供 XAML 绑定的显示坐标
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>是否显示占位提示（无图片时）</summary>
    public bool ShowPlaceholder => CurrentImage == null;
   ///Sam的提示点集合
    public ObservableCollection<SamPromptPointViewModel> SamPromptPoints { get; } = new();
    ///Sam提示框显示属性
   public bool IsSamBoxPreviewVisible =>
    CurrentTool == ToolMode.SegmentBox &&
    IsSamBoxDragging &&
    _samBoxStartImagePoint is not null &&
    _samBoxEndImagePoint is not null;

    public double SamBoxPreviewLeft
    {
        get
        {
            if (_samBoxStartImagePoint is null || _samBoxEndImagePoint is null) return 0;
            var x1 = ImageToCanvasX(_samBoxStartImagePoint.X);
            var x2 = ImageToCanvasX(_samBoxEndImagePoint.X);
            return Math.Min(x1, x2);
        }
    }

    public double SamBoxPreviewTop
    {
        get
        {
            if (_samBoxStartImagePoint is null || _samBoxEndImagePoint is null) return 0;
            var y1 = ImageToCanvasY(_samBoxStartImagePoint.Y);
            var y2 = ImageToCanvasY(_samBoxEndImagePoint.Y);
            return Math.Min(y1, y2);
        }
    }

    public double SamBoxPreviewWidth
    {
        get
        {
            if (_samBoxStartImagePoint is null || _samBoxEndImagePoint is null) return 0;
            var x1 = ImageToCanvasX(_samBoxStartImagePoint.X);
            var x2 = ImageToCanvasX(_samBoxEndImagePoint.X);
            return Math.Abs(x2 - x1);
        }
    }

    public double SamBoxPreviewHeight
    {
        get
        {
            if (_samBoxStartImagePoint is null || _samBoxEndImagePoint is null) return 0;
            var y1 = ImageToCanvasY(_samBoxStartImagePoint.Y);
            var y2 = ImageToCanvasY(_samBoxEndImagePoint.Y);
            return Math.Abs(y2 - y1);
        }
    }
    ///刷新box view方法
    private void RefreshSamBoxPreview()
    {
        OnPropertyChanged(nameof(IsSamBoxPreviewVisible));
        OnPropertyChanged(nameof(SamBoxPreviewLeft));
        OnPropertyChanged(nameof(SamBoxPreviewTop));
        OnPropertyChanged(nameof(SamBoxPreviewWidth));
        OnPropertyChanged(nameof(SamBoxPreviewHeight));
    }
    /// <summary>当前绘制折线是否可见（≥2个点才显示）</summary>
    //public bool IsCurrentDrawingVisible => CurrentDrawingPoints.Count >= 2;
    /// SegmentBox 可以单独画自己的预览矩形 
        public bool IsCurrentDrawingVisible =>
            (CurrentTool == ToolMode.Polygon ||
            CurrentTool == ToolMode.Line ||
            CurrentTool == ToolMode.Angle ||
            CurrentTool == ToolMode.Ruler) &&
            CurrentDrawingPoints.Count >= 2;

    /// <summary>当前绘制折线的 Canvas 坐标点列表，供 Polyline 绑定</summary>
    // public AvaloniaList<Point> CurrentDrawingPolylinePoints
    // {
    //     get
    //     {
    //         // 复用同一对象，避免重新创建触发额外通知
    //         _polylinePoints.Clear();
    //         foreach (var p in CurrentDrawingPoints)
    //             _polylinePoints.Add(new Point(ImageToCanvasX(p.X), ImageToCanvasY(p.Y)));
    //         return _polylinePoints;
    //     }
    // }
        public AvaloniaList<Point> CurrentDrawingPolylinePoints
        {
            get
            {
                return new AvaloniaList<Point>(
                    CurrentDrawingPoints.Select(p =>
                        new Point(ImageToCanvasX(p.X), ImageToCanvasY(p.Y)))
                );
            }
        }
        //delete box用的points
        public AvaloniaList<Point> CurrentDeleteBoxPoints
        {
            get
            {
                if (CurrentTool != ToolMode.DeleteBox || CurrentDrawingPoints.Count < 2)
                    return new AvaloniaList<Point>();

                var p1 = CurrentDrawingPoints[0];
                var p2 = CurrentDrawingPoints[1];

                var x1 = ImageToCanvasX(Math.Min(p1.X, p2.X));
                var y1 = ImageToCanvasY(Math.Min(p1.Y, p2.Y));
                var x2 = ImageToCanvasX(Math.Max(p1.X, p2.X));
                var y2 = ImageToCanvasY(Math.Max(p1.Y, p2.Y));

                return new AvaloniaList<Point>
                {
                    new Point(x1, y1),
                    new Point(x2, y1),
                    new Point(x2, y2),
                    new Point(x1, y2),
                    new Point(x1, y1)
                };
            }
        }
    /// <summary>当前绘制中各点的显示坐标，供 ItemsControl 绑定</summary>
    public ObservableCollection<OverlayPointViewModel> DisplayCurrentDrawingPoints
    {
        get
        {
            var points = new ObservableCollection<OverlayPointViewModel>();
            foreach (var p in CurrentDrawingPoints)
                points.Add(new OverlayPointViewModel
                {
                    X = ImageToCanvasX(p.X),
                    Y = ImageToCanvasY(p.Y)
                });
            return points;
        }
    }

    /// <summary>所有已完成形状的显示模型，供 ItemsControl 绑定</summary>
        public ObservableCollection<OverlayShapeViewModel> DisplayOverlayShapes
    {
        get
        {
            var result = new ObservableCollection<OverlayShapeViewModel>();

            foreach (var shape in CurrentAnnotationDocument.Shapes)
            {
                var label = CurrentAnnotationDocument.Labels
                    .FirstOrDefault(l => l.Id == shape.LabelId);

                var stroke = label?.StrokeColor ?? (shape.ShapeType switch
                {
                    "polygon" => "#00BCD4",
                    "rectangle" => "#FF9800",
                    "line" => "#00BCD4",
                    "angle" => "#00BCD4",
                    _ => "#FFFFFF"
                });

                var fill = label?.FillColor ?? (
                    shape.ShapeType == "polygon" || shape.ShapeType == "rectangle"
                        ? "#5500BCD4"
                        : "#00000000"
                );

                var vm = new OverlayShapeViewModel
                {
                    ShapeType = shape.ShapeType,
                    Stroke = stroke,
                    Fill = fill,
                    Points = shape.Points.Select(p => new OverlayPointViewModel
                    {
                        X = ImageToCanvasX(p.X),
                        Y = ImageToCanvasY(p.Y)
                    }).ToList()
                };

                if (shape.ShapeType == "line" && shape.Points.Count >= 2)
                {
                    var p1 = shape.Points[0];
                    var p2 = shape.Points[1];
                    var dx = p2.X - p1.X;
                    var dy = p2.Y - p1.Y;
                    var lengthPx = Math.Sqrt(dx * dx + dy * dy);

                    vm.MeasureText = HasValidScale && PixelsPerUnit > 0
                        ? $"{lengthPx / PixelsPerUnit:F2} {RulerUnit}"
                        : $"{lengthPx:F1} px";
                }

                if (shape.ShapeType == "angle" && shape.Points.Count >= 3)
                {
                    var p1 = shape.Points[0];
                    var p2 = shape.Points[1];
                    var p3 = shape.Points[2];

                    var v1x = p1.X - p2.X;
                    var v1y = p1.Y - p2.Y;
                    var v2x = p3.X - p2.X;
                    var v2y = p3.Y - p2.Y;

                    var dot = v1x * v2x + v1y * v2y;
                    var mag1 = Math.Sqrt(v1x * v1x + v1y * v1y);
                    var mag2 = Math.Sqrt(v2x * v2x + v2y * v2y);

                    double angleDeg = 0;
                    if (mag1 > 0 && mag2 > 0)
                    {
                        var cosTheta = dot / (mag1 * mag2);
                        cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));
                        angleDeg = Math.Acos(cosTheta) * 180.0 / Math.PI;
                    }

                    vm.AngleText = $"{angleDeg:F2}°";
                }

                result.Add(vm);
            }

            return result;
        }
    }

    /// <summary>所有已完成形状的端点显示坐标，供端点圆圈的 ItemsControl 绑定</summary>
   public ObservableCollection<OverlayPointViewModel> DisplayCompletedPoints
    {
        get
        {
            var result = new ObservableCollection<OverlayPointViewModel>();

            if (SelectedAnnotationLabel is null)
                return result;

            var shape = CurrentAnnotationDocument.Shapes
                .FirstOrDefault(s => s.LabelId == SelectedAnnotationLabel.Id);

            if (shape is null || shape.ShapeType != "polygon")
                return result;

            for (int i = 0; i < shape.Points.Count; i++)
            {
                var p = shape.Points[i];
                result.Add(new OverlayPointViewModel
                {
                    VertexIndex = i,
                    X = ImageToCanvasX(p.X),
                    Y = ImageToCanvasY(p.Y)
                });
            }

            return result;
        }
    }
    //单独显示ruler
    public OverlayShapeViewModel? DisplayRulerShape
    {
        get
        {
            if (CurrentRulerShape is null)
                return null;

            return new OverlayShapeViewModel
            {
                ShapeType = "ruler",
                Stroke = "#FFD54F",
                Fill = "#00000000",
                Points = CurrentRulerShape.Points.Select(p => new OverlayPointViewModel
                {
                    X = ImageToCanvasX(p.X),
                    Y = ImageToCanvasY(p.Y)
                }).ToList()
            };
        }
    }
    // ════════════════════════════════════════════════════════════════════════
    // 属性变更回调
    // ════════════════════════════════════════════════════════════════════════

    partial void OnSelectedImageFileChanged(ImageFileItem? value)
    {
        if (value is not null)
            _ = LoadImageFromPathAsync(value.FilePath);
    }

    partial void OnCurrentImageChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(ShowPlaceholder));
    }

    partial void OnSelectedAnnotationLabelChanged(AnnotationLabel? value)
    {
        RefreshOverlayBindings();
        RefreshSelectedLabelProperties();
    }
    // ════════════════════════════════════════════════════════════════════════
    // 图像视口更新（由 code-behind 在每次点击前调用）
    // ════════════════════════════════════════════════════════════════════════

    public void UpdateImageViewport(
        double offsetX,
        double offsetY,
        double displayWidth,
        double displayHeight,
        double pixelWidth,
        double pixelHeight)
    {
        ImageDisplayOffsetX = offsetX;
        ImageDisplayOffsetY = offsetY;
        ImageDisplayWidth   = displayWidth;
        ImageDisplayHeight  = displayHeight;
        ImagePixelWidth     = pixelWidth;
        ImagePixelHeight    = pixelHeight;

        RefreshOverlayBindings();
    }

    // ════════════════════════════════════════════════════════════════════════
    // 坐标转换
    // ════════════════════════════════════════════════════════════════════════

    private double ImageToCanvasX(double imageX)
    {
        if (ImagePixelWidth <= 0 || ImageDisplayWidth <= 0)
            return imageX;

        return ImageDisplayOffsetX + imageX * (ImageDisplayWidth / ImagePixelWidth);
    }

    private double ImageToCanvasY(double imageY)
    {
        if (ImagePixelHeight <= 0 || ImageDisplayHeight <= 0)
            return imageY;

        return ImageDisplayOffsetY + imageY * (ImageDisplayHeight / ImagePixelHeight);
    }

    public double CanvasToImageX(double canvasX)
    {
        if (ImageDisplayWidth <= 0 || ImagePixelWidth <= 0 || ViewZoom <= 0)
            return canvasX;

        return (canvasX - ImageDisplayOffsetX) * (ImagePixelWidth / ImageDisplayWidth);
    }

    public double CanvasToImageY(double canvasY)
    {
        if (ImageDisplayHeight <= 0 || ImagePixelHeight <= 0 || ViewZoom <= 0)
            return canvasY;

        return (canvasY - ImageDisplayOffsetY) * (ImagePixelHeight / ImageDisplayHeight);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Canvas 点击处理入口
    // ════════════════════════════════════════════════════════════════════════

    public void HandleCanvasPointerPressed(double x, double y, bool isDoubleClick)
    {
        switch (CurrentTool)
        {
            case ToolMode.Polygon:
                HandlePolygonClick(x, y, isDoubleClick);
                break;

            case ToolMode.Rectangle:
                HandleRectangleClick(x, y, isDoubleClick);
                break;

            case ToolMode.Ruler:
                HandleRulerClick(x, y, isDoubleClick);
                break;

            case ToolMode.Line:
                HandleLineClick(x, y, isDoubleClick);
                break;

            case ToolMode.Angle:
                HandleAngleClick(x, y, isDoubleClick);
                break;
            
            case ToolMode.DeleteBox:
                HandleDeleteBoxClick(x, y, isDoubleClick);
                break;
            
            case ToolMode.ColorBox:
                HandleColorBoxClick(x, y, isDoubleClick);
                break;
            
            case ToolMode.SegmentPoint:
                _ = RunSamPointSegmentationAsync(x, y);
                break;

            case ToolMode.SegmentBox:
                StatusText = $"SAM box prompt click at ({x:F1}, {y:F1})";
                break;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Fix 2: Polygon 单击加点，双击完成
    // ！！！Avalonia 双击时会先触发一次单击(ClickCount=1)再触发双击(ClickCount=2)
    // 双击时点已经在第一次单击时加过了，双击只做 finalize
    // ────────────────────────────────────────────────────────────────────────
    private void HandlePolygonClick(double x, double y, bool isDoubleClick)
    {
        if (isDoubleClick)
        {
            StatusText = $"Double click — finalizing polygon ({CurrentDrawingPoints.Count} pts)";
            if (CurrentDrawingPoints.Count >= 3)
                FinalizePolygon();
        }
        else
        {
            CurrentDrawingPoints.Add(new PointD(x, y));
            RefreshDrawingBindings();
            StatusText = $"Polygon point added ({CurrentDrawingPoints.Count} pts): ({x:F1}, {y:F1})";
        }
    }
    // ────────────────────────────────────────────────────────────────────────
    // LINE：单击两次（忽略双击）
    // ────────────────────────────────────────────────────────────────────────
    private void HandleLineClick(double x, double y, bool isDoubleClick)
    {
        if (isDoubleClick) return;

        CurrentDrawingPoints.Add(new PointD(x, y));
        RefreshDrawingBindings();

        if (CurrentDrawingPoints.Count == 2)
            FinalizeLine();
    }
    // ────────────────────────────────────────────────────────────────────────
    // ANGLE：三个点 画的时候显示两条边
    // ────────────────────────────────────────────────────────────────────────
    private void HandleAngleClick(double x, double y, bool isDoubleClick)
    {
        if (isDoubleClick) return;

        CurrentDrawingPoints.Add(new PointD(x, y));
        RefreshDrawingBindings();

        if (CurrentDrawingPoints.Count == 3)
            FinalizeAngle();
    }
    // ────────────────────────────────────────────────────────────────────────
    // Rectangle：单击两次，自动完成矩形（忽略双击）
    // ────────────────────────────────────────────────────────────────────────
    private void HandleRectangleClick(double x, double y, bool isDoubleClick)
    {
        if (isDoubleClick) return;

        CurrentDrawingPoints.Add(new PointD(x, y));
        RefreshDrawingBindings();
        StatusText = $"Rectangle point added ({CurrentDrawingPoints.Count}/2): ({x:F1}, {y:F1})";

        if (CurrentDrawingPoints.Count == 2)
            FinalizeRectangle();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Ruler：单击两次，自动完成标尺（忽略双击）
    // ────────────────────────────────────────────────────────────────────────
    private void HandleRulerClick(double x, double y, bool isDoubleClick)
    {
        if (isDoubleClick) return;

        CurrentDrawingPoints.Add(new PointD(x, y));
        RefreshDrawingBindings();
        StatusText = $"Ruler point added ({CurrentDrawingPoints.Count}/2): ({x:F1}, {y:F1})";

        if (CurrentDrawingPoints.Count == 2)
            FinalizeRuler();
    }
     // ────────────────────────────────────────────────────────────────────────
    // color box的两次点击
    // ────────────────────────────────────────────────────────────────────────
    private void HandleColorBoxClick(double x, double y, bool isDoubleClick)
    {
        if (isDoubleClick) return;

        CurrentDrawingPoints.Add(new PointD(x, y));
        RefreshDrawingBindings();
        StatusText = $"Color-box point added ({CurrentDrawingPoints.Count}/2): ({x:F1}, {y:F1})";

        if (CurrentDrawingPoints.Count == 2)
            FinalizeColorBox();
    }
     // 颜色框的预览虚线框
    public AvaloniaList<Point> CurrentColorBoxPoints
    {
        get
        {
            if (CurrentTool != ToolMode.ColorBox || CurrentDrawingPoints.Count < 2)
                return new AvaloniaList<Point>();

            var p1 = CurrentDrawingPoints[0];
            var p2 = CurrentDrawingPoints[1];

            var x1 = ImageToCanvasX(Math.Min(p1.X, p2.X));
            var y1 = ImageToCanvasY(Math.Min(p1.Y, p2.Y));
            var x2 = ImageToCanvasX(Math.Max(p1.X, p2.X));
            var y2 = ImageToCanvasY(Math.Max(p1.Y, p2.Y));

            return new AvaloniaList<Point>
            {
                new Point(x1, y1),
                new Point(x2, y1),
                new Point(x2, y2),
                new Point(x1, y2),
                new Point(x1, y1)
            };
        }
    }

    public bool IsColorBoxVisible => CurrentTool == ToolMode.ColorBox && CurrentDrawingPoints.Count >= 2;
    // ────────────────────────────────────────────────────────────────────────
    //删除box，两次点击确定一个框，第二次点击就删除
    // ────────────────────────────────────────────────────────────────────────
    private void HandleDeleteBoxClick(double x, double y, bool isDoubleClick)
    {
        if (isDoubleClick) return;

        CurrentDrawingPoints.Add(new PointD(x, y));
        RefreshDrawingBindings();
        StatusText = $"Delete-box point added ({CurrentDrawingPoints.Count}/2): ({x:F1}, {y:F1})";

        if (CurrentDrawingPoints.Count == 2)
            FinalizeDeleteBox();
    }
    //实现删除框
    private void FinalizeDeleteBox()
    {
        var p1 = CurrentDrawingPoints[0];
        var p2 = CurrentDrawingPoints[1];

        var minX = Math.Min(p1.X, p2.X);
        var maxX = Math.Max(p1.X, p2.X);
        var minY = Math.Min(p1.Y, p2.Y);
        var maxY = Math.Max(p1.Y, p2.Y);

        bool PointInside(PointD p) =>
            p.X >= minX && p.X <= maxX &&
            p.Y >= minY && p.Y <= maxY;

        var shapesToDelete = CurrentAnnotationDocument.Shapes
            .Where(shape => shape.ShapeType != "ruler" &&
                            shape.Points.Any(PointInside))
            .ToList();

        if (shapesToDelete.Count == 0)
        {
            CurrentDrawingPoints.Clear();
            RefreshDrawingBindings();
            StatusText = "No shapes found in delete box.";
            return;
        }

        var labelIdsToDelete = shapesToDelete
            .Select(s => s.LabelId)
            .Distinct()
            .ToHashSet();

        foreach (var shape in shapesToDelete)
            CurrentAnnotationDocument.Shapes.Remove(shape);

        var labelsToDelete = CurrentAnnotationDocument.Labels
            .Where(l => labelIdsToDelete.Contains(l.Id))
            .ToList();

        foreach (var label in labelsToDelete)
            CurrentAnnotationDocument.Labels.Remove(label);

        var annotationLabelsToDelete = AnnotationLabels
            .Where(l => labelIdsToDelete.Contains(l.Id))
            .ToList();

        foreach (var label in annotationLabelsToDelete)
            AnnotationLabels.Remove(label);

        if (SelectedAnnotationLabel is not null && labelIdsToDelete.Contains(SelectedAnnotationLabel.Id))
            SelectedAnnotationLabel = null;

        CurrentDrawingPoints.Clear();
        RefreshOverlayBindings();
        RefreshLabelLists();

        StatusText = $"Deleted {shapesToDelete.Count} shape(s).";
    }
    //label面板只显示标注类型///
    public ObservableCollection<AnnotationLabel> DisplayAnnotationLabels
    {
        get
        {
            return new ObservableCollection<AnnotationLabel>(
                AnnotationLabels.Where(x =>
                    x.Type == "polygon" || x.Type == "rectangle")
            );
        }
    }
    private void RefreshLabelLists()
    {
        OnPropertyChanged(nameof(DisplayAnnotationLabels));
    }

    public void ApplyScale(double pixelLength, double knownLength, string unitName)
    {
        if (knownLength <= 0)
            return;

        PixelsPerUnit = pixelLength / knownLength;
        ScaleKnownLength = knownLength;
        RulerUnit = unitName;
        HasValidScale = true;

        StatusText = $"Scale set: {PixelsPerUnit:F4} px/{RulerUnit}";
        RefreshOverlayBindings();
    }
    public bool ShowRulerLabel => CurrentRulerShape is not null && HasValidScale;

    public string RulerLabelText => $"{ScaleKnownLength:G} {RulerUnit}";

    public double RulerLabelLeft
    {
        get
        {
            if (CurrentRulerShape is null || CurrentRulerShape.Points.Count < 2)
                return 0;

            var p1 = CurrentRulerShape.Points[0];
            var p2 = CurrentRulerShape.Points[1];

            var midX = (p1.X + p2.X) / 2.0;
            return ImageToCanvasX(midX) + 8;
        }
    }

    public double RulerLabelTop
    {
        get
        {
            if (CurrentRulerShape is null || CurrentRulerShape.Points.Count < 2)
                return 0;

            var p1 = CurrentRulerShape.Points[0];
            var p2 = CurrentRulerShape.Points[1];

            var midY = (p1.Y + p2.Y) / 2.0;
            return ImageToCanvasY(midY) - 26;
        }
    }
    // ════════════════════════════════════════════════════════════════════════
    // Finalize — 完成并保存标注
    // ════════════════════════════════════════════════════════════════════════

    private void FinalizePolygon()
    {
        var label = new AnnotationLabel
        {
            Name = $"label{_labelCounter++}",
            Type = "polygon"
        };

        var shape = new AnnotationShape
        {
            LabelId   = label.Id,
            ShapeType = "polygon",
            Points    = CurrentDrawingPoints.Select(p => new PointD(p.X, p.Y)).ToList()
        };

        CurrentAnnotationDocument.Labels.Add(label);
        CurrentAnnotationDocument.Shapes.Add(shape);
        AnnotationLabels.Add(label);

        SelectedAnnotationLabel = label;
        CurrentLabelText = label.Name;

        CurrentDrawingPoints.Clear();
        RefreshOverlayBindings();
        RefreshLabelLists();
        StatusText = $"Polygon '{label.Name}' finalized — labels={CurrentAnnotationDocument.Labels.Count}, shapes={CurrentAnnotationDocument.Shapes.Count}";
    }

    private void FinalizeRectangle()
    {
        var p1 = CurrentDrawingPoints[0];
        var p2 = CurrentDrawingPoints[1];

        var x1 = Math.Min(p1.X, p2.X);
        var y1 = Math.Min(p1.Y, p2.Y);
        var x2 = Math.Max(p1.X, p2.X);
        var y2 = Math.Max(p1.Y, p2.Y);

        var label = new AnnotationLabel
        {
            Name = $"label{_labelCounter++}",
            Type = "rectangle"
        };

        var shape = new AnnotationShape
        {
            LabelId   = label.Id,
            ShapeType = "rectangle",
            Points    = new List<PointD>
            {
                new(x1, y1), new(x2, y1),
                new(x2, y2), new(x1, y2)
            }
        };

        CurrentAnnotationDocument.Labels.Add(label);
        CurrentAnnotationDocument.Shapes.Add(shape);
        AnnotationLabels.Add(label);

        SelectedAnnotationLabel = label;
        CurrentLabelText = label.Name;

        CurrentDrawingPoints.Clear();
        RefreshOverlayBindings();
        RefreshLabelLists();
        StatusText = $"Rectangle '{label.Name}' finalized";
    }
    private void FinalizeLine()
    {
        var p1 = CurrentDrawingPoints[0];
        var p2 = CurrentDrawingPoints[1];

        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        var lengthPx = Math.Sqrt(dx * dx + dy * dy);

        var label = new AnnotationLabel
        {
            Name = $"line{_labelCounter++}",
            Type = "line"
        };

        var shape = new AnnotationShape
        {
            LabelId = label.Id,
            ShapeType = "line",
            Points = new List<PointD>
            {
                new(p1.X, p1.Y),
                new(p2.X, p2.Y)
            }
        };

        CurrentAnnotationDocument.Labels.Add(label);
        CurrentAnnotationDocument.Shapes.Add(shape);
        AnnotationLabels.Add(label);

        SelectedAnnotationLabel = label;
        CurrentLabelText = label.Name;

        var lineValue = HasValidScale && PixelsPerUnit > 0
            ? $"{lengthPx / PixelsPerUnit:F2} {RulerUnit}"
            : $"{lengthPx:F2} px";

        AddMeasurementRecord(
            labelName: label.Name,
            shapeType: "line",
            lineLength: lineValue
        );
        // 更新 UI 的逻辑
        if (HasValidScale && PixelsPerUnit > 0)
        {
            var realLength = lengthPx / PixelsPerUnit;
            LengthText = $"{realLength:F2} {RulerUnit}";
        }
        else
        {
            LengthText = $"{lengthPx:F1} px";
        }

        CurrentDrawingPoints.Clear();
        RefreshOverlayBindings();
        RefreshLabelLists();

        StatusText = $"Line '{label.Name}' finalized";
    }

    private void FinalizeAngle()
    {
        // 三点定义角度：
        // p1 = 第一条边端点
        // p2 = 顶点
        // p3 = 第二条边端点
        var p1 = CurrentDrawingPoints[0];
        var p2 = CurrentDrawingPoints[1];
        var p3 = CurrentDrawingPoints[2];

        var v1x = p1.X - p2.X;
        var v1y = p1.Y - p2.Y;
        var v2x = p3.X - p2.X;
        var v2y = p3.Y - p2.Y;

        var dot = v1x * v2x + v1y * v2y;
        var mag1 = Math.Sqrt(v1x * v1x + v1y * v1y);
        var mag2 = Math.Sqrt(v2x * v2x + v2y * v2y);

        double angleDeg = 0;
        if (mag1 > 0 && mag2 > 0)
        {
            var cosTheta = dot / (mag1 * mag2);
            cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));
            angleDeg = Math.Acos(cosTheta) * 180.0 / Math.PI;
        }

        var label = new AnnotationLabel
        {
            Name = $"angle{_labelCounter++}",
            Type = "angle"
        };

        var shape = new AnnotationShape
        {
            LabelId = label.Id,
            ShapeType = "angle",
            Points = new List<PointD>
            {
                new(p1.X, p1.Y),
                new(p2.X, p2.Y),
                new(p3.X, p3.Y)
            }
        };

        CurrentAnnotationDocument.Labels.Add(label);
        CurrentAnnotationDocument.Shapes.Add(shape);
        AnnotationLabels.Add(label);

        SelectedAnnotationLabel = label;
        CurrentLabelText = label.Name;

        LengthText = $"{angleDeg:F2}°";
        // ---  SetMeasurementRows ---
        AddMeasurementRecord(
            labelName: label.Name,
            shapeType: "angle",
            angleDegree: $"{angleDeg:F2}°"
        );
        // ------------------------------------
        CurrentDrawingPoints.Clear();
        RefreshOverlayBindings();
        RefreshLabelLists();

        StatusText = $"Angle '{label.Name}' finalized: {angleDeg:F2}°";
    }
    // private void FinalizeRuler()
    // {
    //     var p1 = CurrentDrawingPoints[0];
    //     var p2 = CurrentDrawingPoints[1];

    //     var dx       = p2.X - p1.X;
    //     var dy       = p2.Y - p1.Y;
    //     var lengthPx = Math.Sqrt(dx * dx + dy * dy);

    //     var label = new AnnotationLabel
    //     {
    //         Name = $"ruler{_labelCounter++}",
    //         Type = "ruler"
    //     };

    //     var shape = new AnnotationShape
    //     {
    //         LabelId   = label.Id,
    //         ShapeType = "ruler",
    //         Points    = new List<PointD>
    //         {
    //             new(p1.X, p1.Y),
    //             new(p2.X, p2.Y)
    //         }
    //     };

    //     CurrentAnnotationDocument.Labels.Add(label);
    //     CurrentAnnotationDocument.Shapes.Add(shape);
    //     AnnotationLabels.Add(label);

    //     SelectedAnnotationLabel = label;
    //     CurrentLabelText = label.Name;
    //     LengthText = $"{lengthPx:F1} px";

    //     CurrentDrawingPoints.Clear();
    //     RefreshOverlayBindings();
    //     RefreshLabelLists();
    //     StatusText = $"Ruler '{label.Name}' finalized — length: {lengthPx:F1} px";
    // }
    private void FinalizeRuler()
    {   //计算
        var p1 = CurrentDrawingPoints[0];
        var p2 = CurrentDrawingPoints[1];

        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        var lengthPx = Math.Sqrt(dx * dx + dy * dy);
        // 在清空原始点位前，记录下最后一次测量的像素长度
        LastRulerPixelLength = lengthPx;
        // --- 整合新单位转换逻辑 ---
        if (HasValidScale && PixelsPerUnit > 0)
        {
            // 计算物理长度：像素 / (像素每单位)
            var realLength = lengthPx / PixelsPerUnit;
            LengthText = $"{realLength:F2} {RulerUnit}";
        }
        else
        {
            // 还没有比例尺时显示像素
            LengthText = $"{lengthPx:F1} px";
        }
        //UI对象创建
        CurrentRulerShape = new AnnotationShape
        {
            LabelId = "GLOBAL_RULER",
            ShapeType = "ruler",
            Points = new List<PointD>
            {
                new(p1.X, p1.Y),
                new(p2.X, p2.Y)
            }
        };

        ////////////LengthText = $"{lengthPx:F1} px";
        //临时数据清理
        CurrentDrawingPoints.Clear();
        RefreshOverlayBindings();
        
        // StatusText = $"Ruler set: {lengthPx:F1} px";
        StatusText = $"Ruler set: {LengthText}";
        // 5. 最后触发对话框逻辑 确保当对话框弹出时，后台的像素值(LastRulerPixelLength)已更新完毕
        ScaleDialogPending = true;
    }
    //--------------------------------------------
    //拖动端点改多边形方法
    public bool TryBeginDragVertex(double canvasX, double canvasY)
    {
        if (SelectedAnnotationLabel is null)
            return false;

        var shape = CurrentAnnotationDocument.Shapes
            .FirstOrDefault(s => s.LabelId == SelectedAnnotationLabel.Id);

        if (shape is null || shape.ShapeType != "polygon")
            return false;

        for (int i = 0; i < shape.Points.Count; i++)
        {
            var px = ImageToCanvasX(shape.Points[i].X);
            var py = ImageToCanvasY(shape.Points[i].Y);

            var dx = canvasX - px;
            var dy = canvasY - py;

            if (dx * dx + dy * dy <= 8 * 8)
            {
                _draggingShape = shape;
                _draggingVertexIndex = i;
                StatusText = $"Dragging vertex {i}";
                return true;
            }
        }

        return false;
    }
    private void RefreshSelectedLabelProperties()
    {
        // 我先留个空，后需要同步颜色/名字显示再加逻辑
    }
    
    public void DragSelectedVertex(double canvasX, double canvasY)
    {
        if (_draggingShape is null || _draggingVertexIndex < 0)
            return;

        var imageX = CanvasToImageX(canvasX);
        var imageY = CanvasToImageY(canvasY);

        _draggingShape.Points[_draggingVertexIndex] = new PointD(imageX, imageY);

        RefreshOverlayBindings();
    }

    public void EndDragVertex()
    {
        _draggingShape = null;
        _draggingVertexIndex = -1;
    }
    //----------------------
    //测量多边形和举行mask
    //-----------------------
    private static double PolygonArea(IReadOnlyList<PointD> pts)
    {
        if (pts.Count < 3) return 0;

        double sum = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var p1 = pts[i];
            var p2 = pts[(i + 1) % pts.Count];
            sum += p1.X * p2.Y - p2.X * p1.Y;
        }

        return Math.Abs(sum) * 0.5;
    }
    //周长
    private static double PolygonPerimeter(IReadOnlyList<PointD> pts)
    {
        if (pts.Count < 2) return 0;

        double sum = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var p1 = pts[i];
            var p2 = pts[(i + 1) % pts.Count];

            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            sum += Math.Sqrt(dx * dx + dy * dy);
        }

        return sum;
    }
    //包围盒宽高
    private static (double width, double height) BoundingBoxSize(IReadOnlyList<PointD> pts)
    {
        if (pts.Count == 0) return (0, 0);

        var minX = pts.Min(p => p.X);
        var maxX = pts.Max(p => p.X);
        var minY = pts.Min(p => p.Y);
        var maxY = pts.Max(p => p.Y);

        return (maxX - minX, maxY - minY);
    }
    //主测量函数
    private async Task MeasurePolygonLike(AnnotationLabel label, AnnotationShape shape)
    {
        var pts = shape.Points;
        if (pts.Count < 3)
        {
            StatusText = "Polygon needs at least 3 points.";
            return;
        }

        var areaPx = PolygonArea(pts);
        var perimeterPx = PolygonPerimeter(pts);
        var (widthPx, heightPx) = BoundingBoxSize(pts);

        var aspectRatio = heightPx > 0 ? widthPx / heightPx : 0;
        var circularity = perimeterPx > 0 ? 4.0 * Math.PI * areaPx / (perimeterPx * perimeterPx) : 0;
        var eqDiameterPx = areaPx > 0 ? Math.Sqrt(4.0 * areaPx / Math.PI) : 0;

        // var previewBitmap = BuildColorAnalysisPreview(pts);
        // var dlg = new AverageColorWindow(colorResult, CurrentImagePath, previewBitmap);
        // await dlg.ShowDialog(_hostWindow);

        string area = "NA";
        string perimeter = "NA";
        string width = "NA";
        string height = "NA";
        string aspect = "NA";
        string circular = "NA";
        string eqDiameter = "NA";
        string meanRgb = "NA";
        string hexColor = "NA";
        string lab = "NA";
        string meanRStr = "NA";
        string meanGStr = "NA";
        string meanBStr = "NA";
        string labLStr = "NA";
        string labAStr = "NA";
        string labBStr = "NA";
        string colorClusterK = "NA";
        string dominantColorHex = "NA";
        string colorDistribution = "NA";

        
        // if (TryComputeMeanColorInShape(pts, out var meanR, out var meanG, out var meanB,
        //     out var hex, out var labL, out var labA, out var labB))
        //     {
        //         meanRgb = $"{meanR:F0}, {meanG:F0}, {meanB:F0}";
        //         hexColor = hex;
        //         lab = $"{labL:F2}, {labA:F2}, {labB:F2}";

        //         meanRStr = $"{meanR:F2}";
        //         meanGStr = $"{meanG:F2}";
        //         meanBStr = $"{meanB:F2}";
        //         labLStr = $"{labL:F2}";
        //         labAStr = $"{labA:F2}";
        //         labBStr = $"{labB:F2}";
        //     }

        ///////////////
        // var colorResult = CurrentImage is not null
        //     ? ColorAnalysisService.AnalyzePolygon(CurrentImage, pts, clusterCount: 5)
        //     : null;
        var colorResult = CurrentImage is not null
            ? ColorAnalysisService.AnalyzePolygon(CurrentImage, pts, clusterCount: ColorClusterCount)
            : null;

        if (colorResult is not null)
        {
            meanRgb = colorResult.MeanRgbText;
            hexColor = colorResult.MeanHex;
            lab = colorResult.LabText;

            meanRStr = $"{colorResult.MeanR:F2}";
            meanGStr = $"{colorResult.MeanG:F2}";
            meanBStr = $"{colorResult.MeanB:F2}";

            labLStr = $"{colorResult.LabL:F2}";
            labAStr = $"{colorResult.LabA:F2}";
            labBStr = $"{colorResult.LabB:F2}";

            dominantColorHex = colorResult.DominantColorHex;
            colorDistribution = colorResult.ColorDistributionText;

            if (_hostWindow is not null)
            {
              //Console.WriteLine($"[DEBUG] passing to dialog: '{CurrentImagePath}'");
                var ptsCopy = pts.ToList(); 
                var previewBitmap = BuildColorAnalysisPreview(ptsCopy);  // 
                var dlg = new AverageColorWindow(
                    colorResult,
                    imagePath: CurrentImagePath,
                    previewBitmap: previewBitmap,
                    recalculate: k => CurrentImage is null ? null
                        : ColorAnalysisService.AnalyzePolygon(CurrentImage, ptsCopy, clusterCount: k),
                    initialClusterCount: ColorClusterCount);  // 加上 previewBitmap
                await dlg.ShowDialog(_hostWindow);

                if (!dlg.Confirmed)
                {
                    StatusText = "Polygon color analysis cancelled.";
                    return;
                }
                 //  同步最终结果
                var finalResult = dlg.CurrentResult;
                ColorClusterCount = finalResult.ClusterCount;
                colorClusterK = finalResult.ClusterCount.ToString();

                meanRgb = finalResult.MeanRgbText;
                hexColor = finalResult.MeanHex;
                lab = finalResult.LabText;
                meanRStr = $"{finalResult.MeanR:F2}";
                meanGStr = $"{finalResult.MeanG:F2}";
                meanBStr = $"{finalResult.MeanB:F2}";
                labLStr = $"{finalResult.LabL:F2}";
                labAStr = $"{finalResult.LabA:F2}";
                labBStr = $"{finalResult.LabB:F2}";
                dominantColorHex = finalResult.DominantColorHex;
                colorDistribution = finalResult.ColorDistributionText;
            }
        }
        if (HasValidScale && PixelsPerUnit > 0)
        {
            var areaReal = areaPx / (PixelsPerUnit * PixelsPerUnit);
            var perimeterReal = perimeterPx / PixelsPerUnit;
            var widthReal = widthPx / PixelsPerUnit;
            var heightReal = heightPx / PixelsPerUnit;
            var eqDiameterReal = eqDiameterPx / PixelsPerUnit;

            area = $"{areaReal:F2} {RulerUnit}²";
            perimeter = $"{perimeterReal:F2} {RulerUnit}";
            width = $"{widthReal:F2} {RulerUnit}";
            height = $"{heightReal:F2} {RulerUnit}";
            eqDiameter = $"{eqDiameterReal:F2} {RulerUnit}";
        }
        else
        {
            area = $"{areaPx:F2} px²";
            perimeter = $"{perimeterPx:F2} px";
            width = $"{widthPx:F2} px";
            height = $"{heightPx:F2} px";
            eqDiameter = $"{eqDiameterPx:F2} px";
        }

        aspect = $"{aspectRatio:F4}";
        circular = $"{circularity:F4}";

        AddMeasurementRecord(
            labelName: label.Name,
            shapeType: shape.ShapeType,
            area: area,
            areaPx: $"{areaPx:F2}",
            perimeter: perimeter,
            width: width,
            height: height,
            aspectRatio: aspect,
            circularity: circular,
            equivalentDiameter: eqDiameter,
            lineLength: "NA",
            linePx: "NA",
            angleDegree: "NA",
            meanRgb: meanRgb,
            hexColor: hexColor,
            lab: lab,
            meanR: meanRStr,
            meanG: meanGStr,
            meanB: meanBStr,
            labL: labLStr,
            labA: labAStr,
            labB: labBStr,
            colorClusterK: colorClusterK, 
            dominantColorHex: dominantColorHex,
            colorDistribution: colorDistribution    
        );

        StatusText = $"Measured {label.Name}";
    }
    private void MeasureLine(AnnotationLabel label, AnnotationShape shape)
    {
        if (shape.Points.Count < 2)
        {
            StatusText = "Line needs 2 points.";
            return;
        }

        var p1 = shape.Points[0];
        var p2 = shape.Points[1];

        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        var lengthPx = Math.Sqrt(dx * dx + dy * dy);

        var linePx = $"{lengthPx:F2}";
        var lineLength = HasValidScale && PixelsPerUnit > 0
            ? $"{lengthPx / PixelsPerUnit:F2} {RulerUnit}"
            : $"{lengthPx:F2} px";

        AddMeasurementRecord(
            labelName: label.Name,
            shapeType: "line",
            area: "NA",
            areaPx: "NA",
            perimeter: "NA",
            width: "NA",
            height: "NA",
            aspectRatio: "NA",
            circularity: "NA",
            equivalentDiameter: "NA",
            lineLength: lineLength,
            linePx: linePx,
            angleDegree: "NA"
        );

        StatusText = $"Measured {label.Name}";
    }
    //自动测量平均颜色：是否在mask内 ，那些点
    private static bool IsPointInPolygon(double x, double y, IReadOnlyList<PointD> polygon)
    {
        bool inside = false;
        int n = polygon.Count;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var xi = polygon[i].X;
            var yi = polygon[i].Y;
            var xj = polygon[j].X;
            var yj = polygon[j].Y;

            bool intersect = ((yi > y) != (yj > y)) &&
                            (x < (xj - xi) * (y - yi) / ((yj - yi) + 1e-12) + xi);

            if (intersect)
                inside = !inside;
        }

        return inside;
    }
    //算mask里面的颜色p 
    private bool TryComputeMeanColorInShape(
    IReadOnlyList<PointD> pts,
    out double meanR,
    out double meanG,
    out double meanB,
    out string hex,
    out double labL,
    out double labA,
    out double labB2)
    {
        meanR = meanG = meanB = 0;
        hex = "NA";
        labL = labA = labB2 = 0;

        if (CurrentImage is null || pts.Count < 3)
            return false;

        var minX = (int)Math.Floor(pts.Min(p => p.X));
        var maxX = (int)Math.Ceiling(pts.Max(p => p.X));
        var minY = (int)Math.Floor(pts.Min(p => p.Y));
        var maxY = (int)Math.Ceiling(pts.Max(p => p.Y));

        minX = Math.Max(0, minX);
        minY = Math.Max(0, minY);
        maxX = Math.Min(CurrentImage.PixelSize.Width - 1, maxX);
        maxY = Math.Min(CurrentImage.PixelSize.Height - 1, maxY);

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        if (width <= 0 || height <= 0)
            return false;

        var stride = width * 4;
        var bufferSize = stride * height;
        var managedBuffer = new byte[bufferSize];
        IntPtr unmanagedBuffer = IntPtr.Zero;

        try
        {
            unmanagedBuffer = Marshal.AllocHGlobal(bufferSize);

            CurrentImage.CopyPixels(
                new PixelRect(minX, minY, width, height),
                unmanagedBuffer,
                bufferSize,
                stride);

            Marshal.Copy(unmanagedBuffer, managedBuffer, 0, bufferSize);
        }
        finally
        {
            if (unmanagedBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(unmanagedBuffer);
        }

        double sumR = 0, sumG = 0, sumB = 0;
        long count = 0;

        for (int yy = 0; yy < height; yy++)
        {
            for (int xx = 0; xx < width; xx++)
            {
                // 像素中心坐标，映射回原图坐标
                double px = minX + xx + 0.5;
                double py = minY + yy + 0.5;

                if (!IsPointInPolygon(px, py, pts))
                    continue;

                int i = yy * stride + xx * 4;

                // 前面已验证过当前图像是 RGBA 顺序
                byte r = managedBuffer[i + 0];
                byte g = managedBuffer[i + 1];
                byte b = managedBuffer[i + 2];
                byte a = managedBuffer[i + 3];

                if (a == 0)
                    continue;

                sumR += r;
                sumG += g;
                sumB += b;
                count++;
            }
        }

        if (count == 0)
            return false;

        meanR = sumR / count;
        meanG = sumG / count;
        meanB = sumB / count;

        var rByte = (byte)Math.Round(meanR);
        var gByte = (byte)Math.Round(meanG);
        var bByte = (byte)Math.Round(meanB);

        hex = RgbToHex(rByte, gByte, bByte);
        var lab = RgbToLab(meanR, meanG, meanB);
        labL = lab.L;
        labA = lab.A;
        labB2 = lab.B;

        return true;
    }
    private void MeasureAngle(AnnotationLabel label, AnnotationShape shape)
    {
        if (shape.Points.Count < 3)
        {
            StatusText = "Angle needs 3 points.";
            return;
        }

        var p1 = shape.Points[0];
        var p2 = shape.Points[1];
        var p3 = shape.Points[2];

        var v1x = p1.X - p2.X;
        var v1y = p1.Y - p2.Y;
        var v2x = p3.X - p2.X;
        var v2y = p3.Y - p2.Y;

        var dot = v1x * v2x + v1y * v2y;
        var mag1 = Math.Sqrt(v1x * v1x + v1y * v1y);
        var mag2 = Math.Sqrt(v2x * v2x + v2y * v2y);

        double angleDeg = 0;
        if (mag1 > 0 && mag2 > 0)
        {
            var cosTheta = dot / (mag1 * mag2);
            cosTheta = Math.Max(-1.0, Math.Min(1.0, cosTheta));
            angleDeg = Math.Acos(cosTheta) * 180.0 / Math.PI;
        }

        AddMeasurementRecord(
            labelName: label.Name,
            shapeType: "angle",
            area: "NA",
            areaPx: "NA",
            perimeter: "NA",
            width: "NA",
            height: "NA",
            aspectRatio: "NA",
            circularity: "NA",
            equivalentDiameter: "NA",
            lineLength: "NA",
            linePx: "NA",
            angleDegree: $"{angleDeg:F2}°"
        );

        StatusText = $"Measured {label.Name}";
    }

    //用mask本身计算表型
    private async Task MeasureMaskLabel(
        AnnotationLabel label,
        AnnotationShape shape,
        MaskLabelData maskData)
    {
        if (!TryComputeMaskGeometry(
                maskData.Mask,
                maskData.Width,
                maskData.Height,
                out var areaPx,
                out var perimeterPx,
                out var widthPx,
                out var heightPx))
        {
            StatusText = "Could not measure SAM mask.";
            return;
        }

        var aspectRatio = heightPx > 0 ? widthPx / heightPx : 0;
        var circularity = perimeterPx > 0
            ? 4.0 * Math.PI * areaPx / (perimeterPx * perimeterPx)
            : 0;

        var eqDiameterPx = areaPx > 0
            ? Math.Sqrt(4.0 * areaPx / Math.PI)
            : 0;

        string area;
        string perimeter;
        string width;
        string height;
        string eqDiameter;

        if (HasValidScale && PixelsPerUnit > 0)
        {
            var areaReal = areaPx / (PixelsPerUnit * PixelsPerUnit);
            var perimeterReal = perimeterPx / PixelsPerUnit;
            var widthReal = widthPx / PixelsPerUnit;
            var heightReal = heightPx / PixelsPerUnit;
            var eqDiameterReal = eqDiameterPx / PixelsPerUnit;

            area = $"{areaReal:F2} {RulerUnit}²";
            perimeter = $"{perimeterReal:F2} {RulerUnit}";
            width = $"{widthReal:F2} {RulerUnit}";
            height = $"{heightReal:F2} {RulerUnit}";
            eqDiameter = $"{eqDiameterReal:F2} {RulerUnit}";
        }
        else
        {
            area = $"{areaPx:F2} px²";
            perimeter = $"{perimeterPx:F2} px";
            width = $"{widthPx:F2} px";
            height = $"{heightPx:F2} px";
            eqDiameter = $"{eqDiameterPx:F2} px";
        }

        string meanRgb = "NA";
        string hexColor = "NA";
        string lab = "NA";
        string meanRStr = "NA";
        string meanGStr = "NA";
        string meanBStr = "NA";
        string labLStr = "NA";
        string labAStr = "NA";
        string labBStr = "NA";
        string colorClusterK = "NA";
        string dominantColorHex = "NA";
        string colorDistribution = "NA";

        var colorResult = CurrentImage is not null
            ? ColorAnalysisService.AnalyzeMask(
                CurrentImage,
                maskData.Mask,
                maskData.Width,
                maskData.Height,
                clusterCount: ColorClusterCount)
            : null;

        if (colorResult is not null)
        {
            meanRgb = colorResult.MeanRgbText;
            hexColor = colorResult.MeanHex;
            lab = colorResult.LabText;

            meanRStr = $"{colorResult.MeanR:F2}";
            meanGStr = $"{colorResult.MeanG:F2}";
            meanBStr = $"{colorResult.MeanB:F2}";

            labLStr = $"{colorResult.LabL:F2}";
            labAStr = $"{colorResult.LabA:F2}";
            labBStr = $"{colorResult.LabB:F2}";

            dominantColorHex = colorResult.DominantColorHex;
            colorDistribution = colorResult.ColorDistributionText;

            // if (_hostWindow is not null)
            // {
            //     var contourCopy = maskData.DisplayContour.ToList();
            //     var previewBitmap = BuildColorAnalysisPreview(maskData.DisplayContour);
            //     var dlg = new AverageColorWindow(
            //     colorResult,
            //     imagePath: CurrentImagePath,
            //     previewBitmap: previewBitmap,
            //     recalculate: k => CurrentImage is null ? null
            //         : ColorAnalysisService.AnalyzePolygon(CurrentImage, maskData.DisplayContour, clusterCount: k),
            //     initialClusterCount: ColorClusterCount);
            // await dlg.ShowDialog(_hostWindow);

            //     if (!dlg.Confirmed)
            //     {
            //         StatusText = "SAM mask measurement cancelled.";
            //         return;
            //     }
            if (_hostWindow is not null)
            {
                var contourCopy = maskData.DisplayContour.ToList();  // 安全拷贝，用于 preview
                var previewBitmap = BuildColorAnalysisPreview(contourCopy);
                var dlg = new AverageColorWindow(
                    colorResult,
                    imagePath: CurrentImagePath,
                    previewBitmap: previewBitmap,
                    recalculate: k => CurrentImage is null ? null
                        : ColorAnalysisService.AnalyzeMask(  // ✅ 用精确 mask 重算
                            CurrentImage,
                            maskData.Mask,
                            maskData.Width,
                            maskData.Height,
                            clusterCount: k),
                    initialClusterCount: ColorClusterCount);
                await dlg.ShowDialog(_hostWindow);

                if (!dlg.Confirmed)
                {
                    StatusText = "SAM mask measurement cancelled.";
                    return;
                }
                //  同步最终结果
                var finalResult = dlg.CurrentResult;
                ColorClusterCount = finalResult.ClusterCount;
                colorClusterK = finalResult.ClusterCount.ToString();

                meanRgb = finalResult.MeanRgbText;
                hexColor = finalResult.MeanHex;
                lab = finalResult.LabText;
                meanRStr = $"{finalResult.MeanR:F2}";
                meanGStr = $"{finalResult.MeanG:F2}";
                meanBStr = $"{finalResult.MeanB:F2}";
                labLStr = $"{finalResult.LabL:F2}";
                labAStr = $"{finalResult.LabA:F2}";
                labBStr = $"{finalResult.LabB:F2}";
                dominantColorHex = finalResult.DominantColorHex;
                colorDistribution = finalResult.ColorDistributionText;
            }
        }

        AddMeasurementRecord(
            labelName: label.Name,
            shapeType: "mask",
            area: area,
            areaPx: $"{areaPx:F2}",
            perimeter: perimeter,
            width: width,
            height: height,
            aspectRatio: $"{aspectRatio:F4}",
            circularity: $"{circularity:F4}",
            equivalentDiameter: eqDiameter,
            lineLength: "NA",
            linePx: "NA",
            angleDegree: "NA",
            meanRgb: meanRgb,
            hexColor: hexColor,
            lab: lab,
            meanR: meanRStr,
            meanG: meanGStr,
            meanB: meanBStr,
            labL: labLStr,
            labA: labAStr,
            labB: labBStr,
            colorClusterK: colorClusterK,
            dominantColorHex: dominantColorHex,
            colorDistribution: colorDistribution
        );

        StatusText = $"Measured SAM mask label: {label.Name}";
    }
    //--------------------------------------
    //结果表的方法
    //--------------------------------------
    public ObservableCollection<MeasurementRecord> MeasurementRecords { get; } = new();
    private int _measurementCounter = 1;
    private void AddMeasurementRecord(
        string labelName,
        string shapeType,
        string area = "NA",
        string areaPx = "NA",
        string perimeter = "NA",
        string width = "NA",
        string height = "NA",
        string aspectRatio = "NA",
        string circularity = "NA",
        string equivalentDiameter = "NA",
        string lineLength = "NA",
        string linePx = "NA",
        string angleDegree = "NA",
        string meanRgb = "NA",
        string hexColor = "NA",
        string lab = "NA",
        string meanR = "NA",
        string meanG = "NA",
        string meanB = "NA",
        string labL = "NA",
        string labA = "NA",
        string labB = "NA",
        string colorClusterK = "NA",
        string dominantColorHex = "NA",
        string colorDistribution = "NA"
        )
    {
        
        MeasurementRecords.Add(new MeasurementRecord
        {
            //Id = _measurementCounter.ToString(),
            Id = Path.GetFileNameWithoutExtension(CurrentImagePath ?? ""),
            ImagePath = CurrentAnnotationDocument.ImagePath ?? "",

            LabelName = labelName,
            ShapeType = shapeType,

            Area = area,
            AreaPx = areaPx,
            Perimeter = perimeter,
            Width = width,
            Height = height,
            AspectRatio = aspectRatio,
            Circularity = circularity,
            EquivalentDiameter = equivalentDiameter,
            LineLength = lineLength,
            LinePx = linePx,
            AngleDegree = angleDegree,
            MeanRgb = meanRgb,
            HexColor = hexColor,
            Lab = lab,
            MeanR = meanR,
            MeanG = meanG,
            MeanB = meanB,
            LabL = labL,
            LabA = labA,
            LabB = labB,
            DominantColorHex = dominantColorHex,
            ColorDistribution = colorDistribution
        });

        _measurementCounter++;
    }
    // private void SetMeasurementRows(
    // string area = "NA",
    // string perimeter = "NA",
    // string width = "NA",
    // string height = "NA",
    // string aspectRatio = "NA",
    // string circularity = "NA",
    // string equivalentDiameter = "NA",
    // string lineLength = "NA",
    // string angleDegree = "NA")
    // {
    //     MeasurementRows.Clear();

    //     MeasurementRows.Add(new MeasurementRow { Metric = "Area", Value = area });
    //     MeasurementRows.Add(new MeasurementRow { Metric = "Perimeter", Value = perimeter });
    //     MeasurementRows.Add(new MeasurementRow { Metric = "Width", Value = width });
    //     MeasurementRows.Add(new MeasurementRow { Metric = "Height", Value = height });
    //     MeasurementRows.Add(new MeasurementRow { Metric = "Aspect Ratio", Value = aspectRatio });
    //     MeasurementRows.Add(new MeasurementRow { Metric = "Circularity", Value = circularity });
    //     MeasurementRows.Add(new MeasurementRow { Metric = "Equivalent Diameter", Value = equivalentDiameter });
    //     MeasurementRows.Add(new MeasurementRow { Metric = "Line Length", Value = lineLength });
    //     MeasurementRows.Add(new MeasurementRow { Metric = "Angle Degree", Value = angleDegree });
    // }

    ////--------------生成 preview 的函数----------------------------
    private Bitmap? BuildColorAnalysisPreview(IReadOnlyList<PointD> pts)
    {
        if (CurrentImage is null || pts.Count < 2)
            return null;

        var imageW = CurrentImage.PixelSize.Width;
        var imageH = CurrentImage.PixelSize.Height;

        var minX = pts.Min(p => p.X);
        var maxX = pts.Max(p => p.X);
        var minY = pts.Min(p => p.Y);
        var maxY = pts.Max(p => p.Y);

        var roiW = maxX - minX;
        var roiH = maxY - minY;

        var padding = Math.Max(30, Math.Max(roiW, roiH) * 0.4);

        var cropX = Math.Max(0, minX - padding);
        var cropY = Math.Max(0, minY - padding);
        var cropRight = Math.Min(imageW, maxX + padding);
        var cropBottom = Math.Min(imageH, maxY + padding);

        var cropW = cropRight - cropX;
        var cropH = cropBottom - cropY;

        if (cropW <= 1 || cropH <= 1)
            return null;

        const int previewMaxWidth = 520;
        const int previewMaxHeight = 220;

        var scale = Math.Min(previewMaxWidth / cropW, previewMaxHeight / cropH);
        var previewW = Math.Max(1, (int)Math.Round(cropW * scale));
        var previewH = Math.Max(1, (int)Math.Round(cropH * scale));

        var preview = new RenderTargetBitmap(
            new PixelSize(previewW, previewH),
            new Vector(96, 96));

        using (var ctx = preview.CreateDrawingContext())
        {
            var sourceRect = new Rect(cropX, cropY, cropW, cropH);
            var destRect = new Rect(0, 0, previewW, previewH);

            ctx.DrawImage(CurrentImage, sourceRect, destRect);

            var geometry = new StreamGeometry();

            using (var gctx = geometry.Open())
            {
                var first = pts[0];
                gctx.BeginFigure(
                    new Point((first.X - cropX) * scale, (first.Y - cropY) * scale),
                    isFilled: true);

                for (int i = 1; i < pts.Count; i++)
                {
                    var p = pts[i];
                    gctx.LineTo(
                        new Point((p.X - cropX) * scale, (p.Y - cropY) * scale));
                }

                gctx.EndFigure(isClosed: true);
            }

            var fill = new SolidColorBrush(Color.FromArgb(70, 0, 220, 220));
            var pen = new Pen(Brushes.Red, 3);

            ctx.DrawGeometry(fill, pen, geometry);
        }

        return preview;
    }

    ////////////--------------自动分割的方法------------------------
    partial void OnIsSegmentationPanelVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowSegmentationModelChooser));
        OnPropertyChanged(nameof(ShowSegmentationPromptTools));
    }

    partial void OnIsSegmentationModelChosenChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowSegmentationModelChooser));
        OnPropertyChanged(nameof(ShowSegmentationPromptTools));
    }

    partial void OnSelectedSegmentationModelChanged(string value)
    {
        OnPropertyChanged(nameof(ShowSegmentationModelChooser));
        OnPropertyChanged(nameof(ShowSegmentationPromptTools));
    }
    //encode
    private async Task EnsureSamEmbeddingReadyAsync()
    {
        if (CurrentImage is null)
        {
            StatusText = "No image loaded.";
            return;
        }

        if (_sam is null || !IsSamModelLoaded)
        {
            StatusText = "SAM model is not loaded.";
            return;
        }

        var imagePath = CurrentAnnotationDocument.ImagePath;
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            StatusText = "Current image path is empty.";
            return;
        }

        if (!File.Exists(imagePath))
        {
            StatusText = $"Image file not found: {imagePath}";
            return;
        }

        try
        {
            IsSamEncoding = true;
            SamEncodingProgress = 5;
            SamEncodingText = "Preparing image for SAM...";
            StatusText = $"Preparing SAM embedding for: {Path.GetFileName(imagePath)}";

            await Task.Yield();
            await using var stream = File.OpenRead(imagePath);
            using var bitmap = new Bitmap(stream);
  // ── 大图预缩放 ──────────────────────────────────────────
        // SAM 内部会 resize 到 1024，所以提前把长边压到 1024
        // 小图（长边 ≤ 1024）跳过，不做多余处理
        const int SamInputSize = 1024;
        Bitmap bitmapForSam;

        int origW = bitmap.PixelSize.Width;
        int origH = bitmap.PixelSize.Height;
        int longSide = Math.Max(origW, origH);

        if (longSide > SamInputSize)
        {
            float scale = (float)SamInputSize / longSide;
            int newW = (int)(origW * scale);
            int newH = (int)(origH * scale);

            // 用 Avalonia RenderTargetBitmap 做缩放
            var scaled = new RenderTargetBitmap(
                new PixelSize(newW, newH),
                new Vector(96, 96));

            using (var ctx = scaled.CreateDrawingContext())
            {
                ctx.DrawImage(bitmap,
                    new Rect(0, 0, origW, origH),      // 源
                    new Rect(0, 0, newW, newH));        // 目标
            }

            bitmapForSam = scaled;
            SamEncodingText = $"Resized {origW}×{origH} → {newW}×{newH} for SAM...";
        }
        else
        {
            bitmapForSam = bitmap;
        }
        // ────────────────────────────────────────────────────────
            await Task.Run(() =>
            {
                var embedding = _sam!.Encode(bitmap);

                if (embedding is null)
                    throw new Exception("Encode returned null.");

                if (embedding.Length == 0)
                    throw new Exception("Encode returned empty embedding.");

                _samEmbedding = embedding;
            });
            IsSamEmbeddingReady = _samEmbedding is not null && _samEmbedding.Length > 0;

            if (IsSamEmbeddingReady)
            {
                SamEncodingProgress = 100;
                SamEncodingText = "Embedding ready.";
                StatusText = $"SAM embedding ready. Length={_samEmbedding!.Length}";
            }
            else
            {
                StatusText = "SAM embedding failed: embedding is null or empty.";
            }
        }
        catch (Exception ex)
        {
            _samEmbedding = null;
            IsSamEmbeddingReady = false;
            StatusText = $"SAM encoding failed: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            await Task.Delay(300);
            IsSamEncoding = false;
        }
    }
    [RelayCommand]
    private void SegmentAddPointPrompt()
    {
        CurrentTool = ToolMode.SegmentPoint;
        CurrentToolText = "SegmentPoint";
        CurrentSamPointMode = SamOpType.Add;
        StatusText = "SAM positive point prompt selected.";
    }

    [RelayCommand]
    private void SegmentRemovePointPrompt()
    {
        CurrentTool = ToolMode.SegmentPoint;
        CurrentToolText = "SegmentPoint";
        CurrentSamPointMode = SamOpType.Remove;
        StatusText = "SAM negative point prompt selected.";
    }
    public void BeginSamBox(double imageX, double imageY)
    {
        _samBoxStartImagePoint = new PointD(imageX, imageY);
        _samBoxEndImagePoint = new PointD(imageX, imageY);
        IsSamBoxDragging = true;
        RefreshSamBoxPreview();
        StatusText = $"SAM box started at ({imageX:F1}, {imageY:F1})";
    }

    public void UpdateSamBox(double imageX, double imageY)
    {
        if (!IsSamBoxDragging || _samBoxStartImagePoint is null)
            return;

        _samBoxEndImagePoint = new PointD(imageX, imageY);
        RefreshSamBoxPreview();
    }

    public async Task EndSamBoxAsync()
    {
        if (!IsSamBoxDragging || _samBoxStartImagePoint is null || _samBoxEndImagePoint is null)
            return;

        var start = _samBoxStartImagePoint;
        var end = _samBoxEndImagePoint;

        IsSamBoxDragging = false;
        RefreshSamBoxPreview();

        await RunSamBoxSegmentationAsync(start.X, start.Y, end.X, end.Y);

        _samBoxStartImagePoint = null;
        _samBoxEndImagePoint = null;
        RefreshSamBoxPreview();
    }
    [RelayCommand]
    private async Task ChooseSamModel()
    {
        try
        {
            SelectedSegmentationModel = "SAM";
            IsSegmentationModelChosen = true;
            CurrentTool = ToolMode.SegmentPoint;
            CurrentToolText = "SegmentPoint";
            CurrentDrawingPoints.Clear();
            RefreshDrawingBindings();

            StatusText = "Loading SAM model...";

            await Task.Run(() =>
            {
                _sam ??= new SamPredictor();

                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string modelFolder = Path.Combine(exePath, "SAMmodel");
                _sam.LoadOnnxModel(modelFolder);
            });

            IsSamModelLoaded = true;

            if (CurrentImage is not null && !string.IsNullOrWhiteSpace(CurrentAnnotationDocument.ImagePath))
            {
                await EnsureSamEmbeddingReadyAsync();

                if (IsSamEmbeddingReady)
                {
                    StatusText = "SAM selected. Embedding ready.";
                }
                // 不再覆盖错误信息
            }
            else
            {
                StatusText = "SAM selected. Please open an image.";
            }
        }
        catch (Exception ex)
        {
            IsSamModelLoaded = false;
            StatusText = $"Failed to load SAM: {ex.Message}";
        }
    }
    [RelayCommand]
    private void Segment()
    {
        IsSegmentationPanelVisible = true;
        IsSegmentationModelChosen = false;
        SelectedSegmentationModel = "";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "Segmentation panel opened. Please choose a model.";
    }
    [RelayCommand]
    private void ChooseMobileSamModel()
    {
        SelectedSegmentationModel = "MobileSAM";
        IsSegmentationModelChosen = false;
        StatusText = "MobileSAM is not implemented yet.";
    }

    [RelayCommand]
    private void SegmentPointPrompt()
    {
        CurrentTool = ToolMode.SegmentPoint;
        CurrentToolText = "SegmentPoint";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "SAM Point Prompt selected.";
    }

    [RelayCommand]
    private void SegmentBoxPrompt()
    {
        CurrentTool = ToolMode.SegmentBox;
        CurrentToolText = "SegmentBox";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "SAM Box Prompt selected.";
    }
    //clear的时候清理库存
    [RelayCommand]
    private void ClearSegmentationPrompts()
    {
        CurrentDrawingPoints.Clear();
        SamPromptPoints.Clear();
        _samPromotions.Clear();

        SegmentationMaskBitmap = null;
        HasSegmentationMask = false;

        _lastSamMask = null;
        _lastSamMaskWidth = 0;
        _lastSamMaskHeight = 0;

        RefreshDrawingBindings();
        StatusText = "Segmentation prompts cleared.";
    }

    [RelayCommand]
    private void CloseSegmentationPanel()
    {
        IsSegmentationPanelVisible = false;
        IsSegmentationModelChosen = false;
        SelectedSegmentationModel = "";
        CurrentTool = ToolMode.None;
        CurrentToolText = "None";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "Segmentation panel closed.";
    }

    //sam mask 转成label,but keep exact mask
    [RelayCommand]
    private void ConvertSamMaskToLabel()
    {
        if (_lastSamMask is null || _lastSamMaskWidth <= 0 || _lastSamMaskHeight <= 0)
        {
            StatusText = "No SAM mask to convert.";
            return;
        }

        var binaryMask = ThresholdMask(_lastSamMask, threshold: 0.5f);

        var displayContour = ExtractDisplayContourFromBinaryMask(
            binaryMask,
            _lastSamMaskWidth,
            _lastSamMaskHeight);

        if (displayContour.Count < 3)
        {
            StatusText = "Could not extract a display contour from SAM mask.";
            return;
        }

        // zaici只是显示用，适度简化；不影响测量精度
        var simplifiedDisplayContour = SimplifyClosedPolygon(displayContour, epsilon: 2.0);

        if (simplifiedDisplayContour.Count < 3)
        {
            StatusText = "SAM contour is too small.";
            return;
        }

        var label = new AnnotationLabel
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"sam_mask{CurrentAnnotationDocument.Labels.Count + 1}",
            Type = "mask",
            IsVisible = true,
            StrokeColor = "#FFFF0000",
            FillColor = "#6600FFFF"
        };

        // note：ShapeType 仍然写 polygon，是为了复用现在的 overlay 显示系统
        // 但 Measure 时会优先检查 _maskLabelDataByLabelId，所以不会按 polygon 测
        var shape = new AnnotationShape
        {
            LabelId = label.Id,
            ShapeType = "polygon"
        };

        foreach (var p in simplifiedDisplayContour)
            shape.Points.Add(p);

        CurrentAnnotationDocument.Labels.Add(label);
        CurrentAnnotationDocument.Shapes.Add(shape);

        _maskLabelDataByLabelId[label.Id] = new MaskLabelData
        {
            Width = _lastSamMaskWidth,
            Height = _lastSamMaskHeight,
            Mask = binaryMask,
            DisplayContour = simplifiedDisplayContour
        };

        SelectedAnnotationLabel = label;

        // 转成正式 label 后，隐藏临时 SAM overlay，避免叠在一起
        SegmentationMaskBitmap = null;
        HasSegmentationMask = false;

        RefreshOverlayBindings();

        StatusText = $"SAM mask converted to label: {label.Name}. You can now click Measure.";
    }
    //正点prompt
    private async Task HandleSegmentPointClickAsync(double x, double y)
    {
        if (CurrentImage is null)
        {
            StatusText = "No image loaded.";
            return;
        }

        if (_sam is null || !IsSamModelLoaded)
        {
            StatusText = "SAM model is not loaded.";
            return;
        }

        if (_samEmbedding is null || !IsSamEmbeddingReady)
        {
            StatusText = "SAM embedding is not ready. Encoding now...";
            await EnsureSamEmbeddingReadyAsync();
        }

        if (_samEmbedding is null || !IsSamEmbeddingReady)
        {
            StatusText = "SAM embedding is still not ready.";
            return;
        }

        await RunSamPointSegmentationAsync(x, y);
    }

    private async Task RunSamPointSegmentationAsync(double imageX, double imageY)
    {
        try
        {
            // 1. 【放置点 A】：立即在 UI 上显示点，增强用户反馈感
            // 如果此方法是由 UI 事件触发的，直接 Add 即可，不需要 Post
            SamPromptPoints.Add(new SamPromptPointViewModel
            {
                X = ImageToCanvasX(imageX),
                Y = ImageToCanvasY(imageY),
                IsPositive = CurrentSamPointMode == SamOpType.Add
            });

            if (_sam is null || _samEmbedding is null)
            {
                StatusText = "SAM model or embedding is not ready.";
                return;
            }

            StatusText = $"Running SAM point prompt at ({imageX:F1}, {imageY:F1})...";

            await Task.Run(() =>
            {
                // 2. 【放置点 B】：在后台线程创建用于计算的 Prompt 对象
                var pt = new PointPromotion(CurrentSamPointMode) // 使用当前的模式 (Add/Remove)
                {
                    X = (int)Math.Round(imageX),
                    Y = (int)Math.Round(imageY)
                };

                var ts = new SamTransforms(1024);
                // 将点击坐标转换成模型需要的坐标
                var prompt = ts.ApplyCoords(pt, (int)ImagePixelWidth, (int)ImagePixelHeight);

                _samPromotions.Add(prompt);

                var promotions = _samPromotions.ToList();

                // 执行 SAM 解码
                var md = _sam.Decode(
                    promotions,
                    _samEmbedding,
                    (int)ImagePixelWidth,
                    (int)ImagePixelHeight);

                if (md is null || md.Mask is null || md.Mask.Count == 0)
                    return;

                // 构建掩码位图
                //var bitmap = BuildSegmentationMaskBitmap(md.Mask.ToArray());
                var maskArray = md.Mask.ToArray();
                var bitmap = BuildSegmentationMaskBitmap(maskArray);

                // 3. 将结果推回主线程显示
                Dispatcher.UIThread.Post(() =>
                {
                    _lastSamMask = maskArray;
                    _lastSamMaskWidth = (int)ImagePixelWidth;
                    _lastSamMaskHeight = (int)ImagePixelHeight;

                    SegmentationMaskBitmap = bitmap;
                    HasSegmentationMask = true;
                    StatusText = "SAM mask ready. Click Convert to make it a label.";
                });
                    });        // 关闭 Task.Run 的 lambda
        }              // 关闭 try 块
        catch (Exception ex)
        {
            StatusText = $"SAM point prompt failed: {ex.Message}";
        }
    }
    ///kuang
    private async Task RunSamBoxSegmentationAsync(double x1, double y1, double x2, double y2)
    {
        try
        {
            if (CurrentImage is null)
            {
                StatusText = "No image loaded.";
                return;
            }

            if (_sam is null || !IsSamModelLoaded)
            {
                StatusText = "SAM model is not loaded.";
                return;
            }

            if (_samEmbedding is null || !IsSamEmbeddingReady)
            {
                StatusText = "SAM embedding is not ready. Encoding now...";
                await EnsureSamEmbeddingReadyAsync();
            }

            if (_samEmbedding is null || !IsSamEmbeddingReady)
            {
                StatusText = "SAM embedding is still not ready.";
                return;
            }

            StatusText = "Running SAM box prompt...";

            await Task.Run(() =>
            {
                var box = new BoxPromotion
                {
                    LeftUp = new PointPromotion(SamOpType.Add)
                    {
                        X = (int)Math.Round(Math.Min(x1, x2)),
                        Y = (int)Math.Round(Math.Min(y1, y2))
                    },
                    RightBottom = new PointPromotion(SamOpType.Add)
                    {
                        X = (int)Math.Round(Math.Max(x1, x2)),
                        Y = (int)Math.Round(Math.Max(y1, y2))
                    }
                };

                var ts = new SamTransforms(1024);
                var prompt = ts.ApplyBox(box, (int)ImagePixelWidth, (int)ImagePixelHeight);

                _samPromotions.Add(prompt);

                var promotions = _samPromotions.ToList();

                var md = _sam.Decode(
                    promotions,
                    _samEmbedding,
                    (int)ImagePixelWidth,
                    (int)ImagePixelHeight);

                if (md is null || md.Mask is null || md.Mask.Count == 0)
                    return;

                //var bitmap = BuildSegmentationMaskBitmap(md.Mask.ToArray());
                var maskArray = md.Mask.ToArray();
                var bitmap = BuildSegmentationMaskBitmap(maskArray);

        //         Dispatcher.UIThread.Post(() =>
        //         {
        //             SegmentationMaskBitmap = bitmap;
        //             HasSegmentationMask = true;
        //            //_lastSamMask = md.Mask.ToArray();   // 下面会用到
        //             StatusText = "SAM box mask ready.";
        //         });
        //     });
        // }
                Dispatcher.UIThread.Post(() =>
            {
                _lastSamMask = maskArray;
                _lastSamMaskWidth = (int)ImagePixelWidth;
                _lastSamMaskHeight = (int)ImagePixelHeight;

                SegmentationMaskBitmap = bitmap;
                HasSegmentationMask = true;
                StatusText = "SAM box mask ready. Click Convert to make it a label.";
            });
        });        // 关闭 Task.Run 的 lambda
    }              // 关闭 try 块
        catch (Exception ex)
        {
            StatusText = $"SAM box prompt failed: {ex.Message}";
        }
    }
    private Bitmap BuildSegmentationMaskBitmap(float[] mask)
    {
        int w = (int)ImagePixelWidth;
        int h = (int)ImagePixelHeight;

        var stride = w * 4;
        var pixels = new byte[h * stride];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                if (idx >= mask.Length)
                    continue;

                if (mask[idx] > 0.5f)
                {
                    int p = y * stride + x * 4;

                    // RGBA
                    pixels[p + 0] = 0;
                    pixels[p + 1] = 255;
                    pixels[p + 2] = 180;
                    pixels[p + 3] = 110;
                }
            }
        }

        using var ms = new MemoryStream();
        using (var wb = new WriteableBitmap(
            new PixelSize(w, h),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Unpremul))
        {
            using (var fb = wb.Lock())
            {
                Marshal.Copy(pixels, 0, fb.Address, pixels.Length);
            }

            wb.Save(ms);
        }

        ms.Position = 0;
        return new Bitmap(ms);
    }
    // ════════════════════════════════════════════════════════════════════════
    // 内部辅助：批量通知 UI 刷新
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>仅刷新当前绘制中的线和点（加点时调用）</summary>
    private void RefreshDrawingBindings()
    {
        OnPropertyChanged(nameof(CurrentDrawingPolylinePoints));
        OnPropertyChanged(nameof(DisplayCurrentDrawingPoints));
        OnPropertyChanged(nameof(IsCurrentDrawingVisible));
        OnPropertyChanged(nameof(CurrentDeleteBoxPoints));
        OnPropertyChanged(nameof(ShowDeleteBoxHint));
        OnPropertyChanged(nameof(DeleteBoxHintText));
        OnPropertyChanged(nameof(CurrentColorBoxPoints));
        OnPropertyChanged(nameof(IsColorBoxVisible));
    }

    /// <summary>刷新所有覆盖层（完成形状 + 当前绘制，视口变化或 finalize 时调用）</summary>
    private void RefreshOverlayBindings()
    {
        OnPropertyChanged(nameof(CurrentDrawingPolylinePoints));
        OnPropertyChanged(nameof(DisplayCurrentDrawingPoints));
        OnPropertyChanged(nameof(DisplayOverlayShapes));
        OnPropertyChanged(nameof(DisplayCompletedPoints));
        OnPropertyChanged(nameof(DisplayRulerShape));
        OnPropertyChanged(nameof(IsCurrentDrawingVisible));
        OnPropertyChanged(nameof(IsDeleteBoxVisible));

        OnPropertyChanged(nameof(ShowRulerLabel));
        OnPropertyChanged(nameof(RulerLabelText));
        OnPropertyChanged(nameof(RulerLabelLeft));
        OnPropertyChanged(nameof(RulerLabelTop));
        OnPropertyChanged(nameof(CurrentDeleteBoxPoints));
        OnPropertyChanged(nameof(ShowDeleteBoxHint));
        OnPropertyChanged(nameof(DeleteBoxHintText));

        OnPropertyChanged(nameof(CurrentColorBoxPoints));
        OnPropertyChanged(nameof(IsColorBoxVisible));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 切换图片时重置标注状态
    // ════════════════════════════════════════════════════════════════════════

    private void ResetAnnotationStateForNewImage(string imagePath)
    {
        CurrentAnnotationDocument = new ImageAnnotationDocument
        {
            ImagePath = imagePath
        };

        AnnotationLabels.Clear();
        CurrentDrawingPoints.Clear();
        SelectedAnnotationLabel = null;
        CurrentLabelText = "None";
        LengthText = "-";
        AreaText   = "-";
        PixelText  = "-";
        _labelCounter = 1;
        SegmentationMaskBitmap = null;
        HasSegmentationMask = false;
        _samEmbedding = null;
        _samPromotions.Clear();
        IsSamEmbeddingReady = false;

        RefreshOverlayBindings();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Commands
    // ════════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task OpenFolder()
    {
        if (_hostWindow?.StorageProvider is null)
        {
            StatusText = "Window is not ready.";
            return;
        }

        var folders = await _hostWindow.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title         = "Open Folder",
                AllowMultiple = false
            });

        if (folders.Count == 0)
        {
            StatusText = "Open folder cancelled.";
            return;
        }

        var folder = folders[0];
        CurrentFolderText = folder.Path.LocalPath;

        try
        {
            ImageFiles.Clear();

            var files = Directory.EnumerateFiles(folder.Path.LocalPath)
                .Where(IsSupportedImageFile)
                .OrderBy(x => x);

            foreach (var file in files)
            {
                ImageFiles.Add(new ImageFileItem
                {
                    FilePath    = file,
                    DisplayName = Path.GetFileName(file)
                });
            }

            StatusText = $"Loaded folder: {ImageFiles.Count} image(s)";

            if (ImageFiles.Count > 0)
                SelectedImageFile = ImageFiles[0];
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to open folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenImage()
    {
        if (_hostWindow?.StorageProvider is null)
        {
            StatusText = "Window is not ready.";
            return;
        }

        var files = await _hostWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title         = "Open Image",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.tif", "*.tiff","*.heif","*.heic"]
                    }
                ]
            });

        if (files.Count == 0)
        {
            StatusText = "Open image cancelled.";
            return;
        }

        await LoadImageFromPathAsync(files[0].Path.LocalPath);
    }


//////加载图片辅助方法
    private async Task LoadImageFromPathAsync(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".heic" || extension == ".heif")
            {
                // 转成临时 PNG，确保 SAM 读像素时数据完整
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                try
                {
                    using var magickImage = new Magick.MagickImage(filePath);
                    magickImage.Format = Magick.MagickFormat.Png;
                    await magickImage.WriteAsync(tempPath);

                    await using var stream = File.OpenRead(tempPath);
                    CurrentImage = new Bitmap(stream);
                }
                finally
                {
                    // 加载完删掉临时文件
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
            }
            else
            {
                await using var stream = File.OpenRead(filePath);
                CurrentImage = new Bitmap(stream);
            }

            CurrentImagePath = filePath;  // file path加在这里，两个分支都能覆盖到
            //Console.WriteLine($"[DEBUG] CurrentImagePath set to: '{CurrentImagePath}'");  // debug
            
            ResetAnnotationStateForNewImage(filePath);
            StatusText = $"Loaded: {Path.GetFileName(filePath)}";

            if (SelectedSegmentationModel == "SAM" && IsSamModelLoaded)
            {
                await EnsureSamEmbeddingReadyAsync();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to load image: {ex.Message}";
        }
    }
   
    [RelayCommand]
    private async Task Save()
    {
        if (_hostWindow?.StorageProvider is null)
        {
            StatusText = "Window is not ready.";
            return;
        }

        try
        {
            var file = await _hostWindow.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title             = "Save Annotation",
                    SuggestedFileName = "annotation.json",
                    DefaultExtension  = "json"
                });

            if (file is null)
            {
                StatusText = "Save cancelled.";
                return;
            }

            var json = JsonSerializer.Serialize(CurrentAnnotationDocument, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);

            StatusText = $"Saved: {file.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
        }
    }

    //右边 label 支持删除
    [RelayCommand]
    private void DeleteLabel()
    {
        if (SelectedAnnotationLabel is null)
        {
            StatusText = "No label selected.";
            return;
        }

        var labelId = SelectedAnnotationLabel.Id;

        var shape = CurrentAnnotationDocument.Shapes
            .FirstOrDefault(s => s.LabelId == labelId);

        if (shape is not null)
            CurrentAnnotationDocument.Shapes.Remove(shape);

        CurrentAnnotationDocument.Labels.Remove(SelectedAnnotationLabel);
        AnnotationLabels.Remove(SelectedAnnotationLabel);

        SelectedAnnotationLabel = null;
        CurrentLabelText = "None";

        RefreshOverlayBindings();
        StatusText = "Label deleted.";
    }
    //-------------------加改变label颜色命令-------------//
    [RelayCommand]
    private void SetCyanColor()
    {
        SetSelectedLabelColor("#00BCD4", "#5500BCD4");
    }

    [RelayCommand]
    private void SetRedColor()
    {
        SetSelectedLabelColor("#F44336", "#55F44336");
    }

    [RelayCommand]
    private void SetGreenColor()
    {
        SetSelectedLabelColor("#4CAF50", "#554CAF50");
    }

    private void SetSelectedLabelColor(string stroke, string fill)
    {
        if (SelectedAnnotationLabel is null)
        {
            StatusText = "No label selected.";
            return;
        }

        SelectedAnnotationLabel.StrokeColor = stroke;
        SelectedAnnotationLabel.FillColor = fill;

        RefreshOverlayBindings();
        StatusText = $"Color changed for {SelectedAnnotationLabel.Name}";
    }
    public void DeleteSelectedLabel()
    {
        if (SelectedAnnotationLabel is null)
            return;

        var labelId = SelectedAnnotationLabel.Id;

        var shape = CurrentAnnotationDocument.Shapes
            .FirstOrDefault(s => s.LabelId == labelId);

        if (shape is not null)
            CurrentAnnotationDocument.Shapes.Remove(shape);

        CurrentAnnotationDocument.Labels.Remove(SelectedAnnotationLabel);
        AnnotationLabels.Remove(SelectedAnnotationLabel);

        SelectedAnnotationLabel = null;
        CurrentLabelText = "None";

        RefreshOverlayBindings();
        RefreshLabelLists();
    }

    public void RefreshOverlayAfterLabelEdit()
    {
        RefreshOverlayBindings();
        RefreshLabelLists();
    }

    // ── 工具切换命令 ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void DrawPoint()
    {
        CurrentTool = ToolMode.Polygon;
        CurrentToolText = "Polygon";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "Polygon tool selected";
    }

    [RelayCommand]
    private void Ruler()
    {
        CurrentTool = ToolMode.Ruler;
        CurrentToolText = "Ruler";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "Ruler tool selected";
    }

    [RelayCommand]
    private void Rectangle()
    {
        CurrentTool = ToolMode.Rectangle;
        CurrentToolText = "Rectangle";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "Rectangle tool selected";
    }
    [RelayCommand]
    private void Line()
    {
        CurrentTool = ToolMode.Line;
        CurrentToolText = "Line";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "Line tool selected";
    }

    [RelayCommand]
    private void Angle()
    {
        CurrentTool = ToolMode.Angle;
        CurrentToolText = "Angle";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "Angle tool selected";
    }
    //-------------------测量命令------------------------//
    [RelayCommand]
    private async Task Measure()
    {
        if (SelectedAnnotationLabel is null)
        {
            StatusText = "Please select a shape label to measure.";
            return;
        }

        var label = SelectedAnnotationLabel;

        var shape = CurrentAnnotationDocument.Shapes
            .FirstOrDefault(s => s.LabelId == label.Id);

        if (shape is null)
        {
            StatusText = "Selected label has no shape.";
            return;
        }

        // !!：如果这个 label 背后有 exact mask，就按 mask 测量
        //SAM label 虽然显示为 polygon，但测量时会先进入 MeasureMaskLabel()，不会走 polygon 的面积算法
        if (_maskLabelDataByLabelId.TryGetValue(label.Id, out var maskData))
        {
            await MeasureMaskLabel(label, shape, maskData);
            return;
        }

        switch (shape.ShapeType)
        {
            case "polygon":
            case "rectangle":
                await MeasurePolygonLike(label, shape);
                break;

            case "line":
                MeasureLine(label, shape);
                break;

            case "angle":
                MeasureAngle(label, shape);
                break;

            default:
                StatusText = $"Measurement for '{shape.ShapeType}' is not supported.";
                break;
        }
    }

    //缩放的命令---------------------
    [RelayCommand]
    private void ZoomIn()
    {
        ViewZoom = Math.Min(ViewZoom * 1.2, 20.0);
        //RefreshOverlayBindings();
        StatusText = $"Zoom: {ViewZoom:F2}x";
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ViewZoom = Math.Max(ViewZoom / 1.2, 0.1);
        //RefreshOverlayBindings();
        StatusText = $"Zoom: {ViewZoom:F2}x";
    }

    //-----------reload 清空画面的命令----------------
    [RelayCommand]
    private void Reload()
    {
        CurrentAnnotationDocument = new ImageAnnotationDocument
        {
            ImagePath = CurrentAnnotationDocument.ImagePath
        };

        AnnotationLabels.Clear();
        SelectedAnnotationLabel = null;
        CurrentDrawingPoints.Clear();

        CurrentRulerShape = null;
        ScaleDialogPending = false;
        LastRulerPixelLength = 0;

        CurrentLabelText = "None";
        LengthText = "-";
        AreaText = "-";
        PixelText = "-";

        MeasurementRecords.Clear();

        RefreshOverlayBindings();
        RefreshLabelLists();

        StatusText = "All annotations cleared for current image.";
    }
    //-----------框选删除box的命令---------------
    [RelayCommand]
    private void DeleteBox()
    {
        CurrentTool = ToolMode.DeleteBox;
        CurrentToolText = "DeleteBox";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        StatusText = "Delete-box tool selected,Click two corners to draw a box and delete enclosed shapes.";
    }
    //-----------保存结果的命令-------------------

    [RelayCommand]
    private async Task ExportResults()
    {
        if (_hostWindow?.StorageProvider is null)
        {
            StatusText = "Window is not ready.";
            return;
        }

        if (MeasurementRecords.Count == 0)
        {
            StatusText = "No measurement results to export.";
            return;
        }

        try
        {
            var file = await _hostWindow.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export Results CSV",
                    SuggestedFileName = "measurements.csv",
                    DefaultExtension = "csv",
                    FileTypeChoices =
                    [
                        new FilePickerFileType("CSV File")
                        {
                            Patterns = ["*.csv"]
                        }
                    ]
                });

            if (file is null)
            {
                StatusText = "Export cancelled.";
                return;
            }

            var sb = new StringBuilder();

            // 表头
            sb.AppendLine("Id,ImagePath,LabelName,ShapeType,Area,AreaPx,Perimeter,Width,Height," +
                        "AspectRatio,Circularity,EquivalentDiameter,LineLength,LinePx,AngleDegree," +
                        "MeanRgb,HexColor,Lab,MeanR,MeanG,MeanB,LabL,LabA,LabB," +
                       "ColorClusterK,DominantColorHex,ColorDistribution");

            // 数据行
            foreach (var record in MeasurementRecords)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(record.Id),
                    Csv(record.ImagePath),
                    Csv(record.LabelName),
                    Csv(record.ShapeType),
                    Csv(record.Area),
                    Csv(record.AreaPx),
                    Csv(record.Perimeter),
                    Csv(record.Width),
                    Csv(record.Height),
                    Csv(record.AspectRatio),
                    Csv(record.Circularity),
                    Csv(record.EquivalentDiameter),
                    Csv(record.LineLength),
                    Csv(record.LinePx),
                    Csv(record.AngleDegree),
                    Csv(record.MeanRgb),
                    Csv(record.HexColor),
                    Csv(record.Lab),
                    Csv(record.MeanR),
                    Csv(record.MeanG),
                    Csv(record.MeanB),
                    Csv(record.LabL),
                    Csv(record.LabA),
                    Csv(record.LabB),
                    Csv(record.ColorClusterK),
                    Csv(record.DominantColorHex),
                    Csv(record.ColorDistribution)
                }));
            }

            // 写入文件（加 BOM，Excel 可直接识别 UTF-8）
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await writer.WriteAsync(sb.ToString());

            StatusText = $"Exported {MeasurementRecords.Count} records: {file.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Export failed: {ex.Message}";
        }
    }

    // CSV escape 辅助方法 转义
    private static string Csv(string? value)
    {
        value ??= "";
        if (value.Contains(',') ||
            value.Contains('"') ||
            value.Contains('\n') ||
            value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
    //threshold helper
    private static byte[] ThresholdMask(float[] mask, float threshold)
    {
        var binary = new byte[mask.Length];

        for (int i = 0; i < mask.Length; i++)
            binary[i] = mask[i] > threshold ? (byte)1 : (byte)0;

        return binary;
    }
    
    //segmask几何测量    geometric measurement
    //exact mask pixel count。perimeter 是 raster perimeter，
    // //不是 polygon perimeter。比简化 polygon perimeter 更一致
    private static bool TryComputeMaskGeometry(
        byte[] mask,
        int width,
        int height,
        out double areaPx,
        out double perimeterPx,
        out double bboxWidth,
        out double bboxHeight)
    {
        areaPx = 0;
        perimeterPx = 0;
        bboxWidth = 0;
        bboxHeight = 0;

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;

                if (idx < 0 || idx >= mask.Length || mask[idx] == 0)
                    continue;

                areaPx++;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                // 4-neighbor perimeter：每一条接触背景的边算 1 px
                if (x == 0 || mask[y * width + (x - 1)] == 0)
                    perimeterPx++;

                if (x == width - 1 || mask[y * width + (x + 1)] == 0)
                    perimeterPx++;

                if (y == 0 || mask[(y - 1) * width + x] == 0)
                    perimeterPx++;

                if (y == height - 1 || mask[(y + 1) * width + x] == 0)
                    perimeterPx++;
            }
        }

        if (areaPx <= 0 || maxX < minX || maxY < minY)
            return false;

        bboxWidth = maxX - minX + 1;
        bboxHeight = maxY - minY + 1;

        return true;
    }
    ///// display  contour 提取
    /// //contour 只用于 overlay 和 preview，不参与测量。
    private static List<PointD> ExtractDisplayContourFromBinaryMask(
        byte[] mask,
        int width,
        int height)
    {
        var boundary = new List<PointD>();

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;

                if (idx < 0 || idx >= mask.Length || mask[idx] == 0)
                    continue;

                bool isBoundary =
                    mask[y * width + (x - 1)] == 0 ||
                    mask[y * width + (x + 1)] == 0 ||
                    mask[(y - 1) * width + x] == 0 ||
                    mask[(y + 1) * width + x] == 0;

                if (isBoundary)
                    boundary.Add(new PointD(x, y));
            }
        }

        if (boundary.Count < 3)
            return boundary;

        var cx = boundary.Average(p => p.X);
        var cy = boundary.Average(p => p.Y);

        return boundary
            .OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx))
            .ToList();
    }
    private static List<PointD> SimplifyClosedPolygon(
        IReadOnlyList<PointD> points,
        double epsilon)
    {
        if (points.Count < 4)
            return points.ToList();

        var closed = points.ToList();

        if (Distance(closed[0], closed[^1]) > 1e-6)
            closed.Add(closed[0]);

        var simplified = RamerDouglasPeucker(closed, epsilon);

        if (simplified.Count > 1 && Distance(simplified[0], simplified[^1]) < 1e-6)
            simplified.RemoveAt(simplified.Count - 1);

        return simplified.Count >= 3 ? simplified : points.ToList();
    }

    private static List<PointD> RamerDouglasPeucker(
        IReadOnlyList<PointD> points,
        double epsilon)
    {
        if (points.Count < 3)
            return points.ToList();

        double maxDistance = 0;
        int index = 0;

        for (int i = 1; i < points.Count - 1; i++)
        {
            var distance = PerpendicularDistance(points[i], points[0], points[^1]);

            if (distance > maxDistance)
            {
                index = i;
                maxDistance = distance;
            }
        }

        if (maxDistance > epsilon)
        {
            var left = RamerDouglasPeucker(points.Take(index + 1).ToList(), epsilon);
            var right = RamerDouglasPeucker(points.Skip(index).ToList(), epsilon);

            return left.Take(left.Count - 1).Concat(right).ToList();
        }

        return new List<PointD> { points[0], points[^1] };
    }

    private static double PerpendicularDistance(
        PointD p,
        PointD lineStart,
        PointD lineEnd)
    {
        var dx = lineEnd.X - lineStart.X;
        var dy = lineEnd.Y - lineStart.Y;

        if (Math.Abs(dx) < 1e-12 && Math.Abs(dy) < 1e-12)
            return Distance(p, lineStart);

        var numerator = Math.Abs(
            dy * p.X -
            dx * p.Y +
            lineEnd.X * lineStart.Y -
            lineEnd.Y * lineStart.X);

        var denominator = Math.Sqrt(dx * dx + dy * dy);

        return numerator / denominator;
    }

    private static double Distance(PointD a, PointD b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }
    //-----------颜色测量的命令-------------------
    [RelayCommand]
    private void ColorBox()
    {
        CurrentTool = ToolMode.ColorBox;
        CurrentToolText = "ColorBox";
        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
        
        StatusText = "Color-box tool selected. Click two corners to measure average color.";
    }
    //辅助命令RGB 转 Hex
    private static string RgbToHex(byte r, byte g, byte b)
    {
        return $"#{r:X2}{g:X2}{b:X2}";
    }
    //辅助方法 RGB 转 LAB
    private static (double L, double A, double B) RgbToLab(double r, double g, double b)
    {
        // sRGB -> linear RGB
        double PivotRgb(double n)
        {
            n /= 255.0;
            return n <= 0.04045 ? n / 12.92 : Math.Pow((n + 0.055) / 1.055, 2.4);
        }

        var rl = PivotRgb(r);
        var gl = PivotRgb(g);
        var bl = PivotRgb(b);

        // linear RGB -> XYZ (D65)
        var x = rl * 0.4124564 + gl * 0.3575761 + bl * 0.1804375;
        var y = rl * 0.2126729 + gl * 0.7151522 + bl * 0.0721750;
        var z = rl * 0.0193339 + gl * 0.1191920 + bl * 0.9503041;

        // normalize by D65 reference white
        x /= 0.95047;
        y /= 1.00000;
        z /= 1.08883;

        double PivotXyz(double n)
        {
            return n > 0.008856 ? Math.Pow(n, 1.0 / 3.0) : (7.787 * n) + (16.0 / 116.0);
        }

        var fx = PivotXyz(x);
        var fy = PivotXyz(y);
        var fz = PivotXyz(z);

        var L = (116 * fy) - 16;
        var A = 500 * (fx - fy);
        var B = 200 * (fy - fz);

        return (L, A, B);
    }
  
    // private async void FinalizeColorBox()
    // {
    //     if (CurrentDrawingPoints.Count < 2 || CurrentImage is null || _hostWindow is null)
    //         return;

    //     var p1 = CurrentDrawingPoints[0];
    //     var p2 = CurrentDrawingPoints[1];

    //     var minX = (int)Math.Floor(Math.Min(p1.X, p2.X));
    //     var maxX = (int)Math.Ceiling(Math.Max(p1.X, p2.X));
    //     var minY = (int)Math.Floor(Math.Min(p1.Y, p2.Y));
    //     var maxY = (int)Math.Ceiling(Math.Max(p1.Y, p2.Y));

    //     minX = Math.Max(0, minX);
    //     minY = Math.Max(0, minY);
    //     maxX = Math.Min(CurrentImage.PixelSize.Width - 1, maxX);
    //     maxY = Math.Min(CurrentImage.PixelSize.Height - 1, maxY);

    //     var width = maxX - minX + 1;
    //     var height = maxY - minY + 1;

    //     if (width <= 0 || height <= 0)
    //     {
    //         StatusText = "Invalid color-box region.";
    //         CurrentDrawingPoints.Clear();
    //         RefreshDrawingBindings();
    //         return;
    //     }

    //     // 按常见 BGRA8888 读取
    //     var stride = width * 4;
    //     var bufferSize = stride * height;
    //     var managedBuffer = new byte[bufferSize];

    //     IntPtr unmanagedBuffer = IntPtr.Zero;

    //     try
    //     {
    //         unmanagedBuffer = Marshal.AllocHGlobal(bufferSize);

    //         CurrentImage.CopyPixels(
    //             new PixelRect(minX, minY, width, height),
    //             unmanagedBuffer,
    //             bufferSize,
    //             stride);

    //         Marshal.Copy(unmanagedBuffer, managedBuffer, 0, bufferSize);
    //     }
    //     finally
    //     {
    //         if (unmanagedBuffer != IntPtr.Zero)
    //             Marshal.FreeHGlobal(unmanagedBuffer);
    //     }

    //     double sumR = 0, sumG = 0, sumB = 0;
    //     long count = 0;

    //     for (int i = 0; i < managedBuffer.Length; i += 4)
    //     {
    //        // RGBA
    //         byte r = managedBuffer[i + 0];
    //         byte g = managedBuffer[i + 1];
    //         byte b = managedBuffer[i + 2];
    //         byte a = managedBuffer[i + 3];

    //         if (a == 0)
    //             continue;

    //         sumR += r;
    //         sumG += g;
    //         sumB += b;
    //         count++;
    //     }

    //     if (count == 0)
    //     {
    //         StatusText = "No valid pixels in color-box.";
    //         CurrentDrawingPoints.Clear();
    //         RefreshDrawingBindings();
    //         return;
    //     }

    //     var meanR = sumR / count;
    //     var meanG = sumG / count;
    //     var meanB = sumB / count;

    //     var rByte = (byte)Math.Round(meanR);
    //     var gByte = (byte)Math.Round(meanG);
    //     var bByte = (byte)Math.Round(meanB);

    //     var hex = RgbToHex(rByte, gByte, bByte);
    //     var (labL, labA, labB) = RgbToLab(meanR, meanG, meanB);

    //     var dlg = new AverageColorWindow(
    //         $"{meanR:F0}, {meanG:F0}, {meanB:F0}",
    //         hex,
    //         $"{labL:F2}, {labA:F2}, {labB:F2}",
    //         global::Avalonia.Media.Color.FromRgb(rByte, gByte, bByte));

    //     await dlg.ShowDialog(_hostWindow);

    //     if (dlg.Confirmed)
    //     {
    //         AddMeasurementRecord(
    //             labelName: $"color{_measurementCounter}",
    //             shapeType: "colorbox",
    //             area: "NA",
    //             areaPx: "NA",
    //             perimeter: "NA",
    //             width: "NA",
    //             height: "NA",
    //             aspectRatio: "NA",
    //             circularity: "NA",
    //             equivalentDiameter: "NA",
    //             lineLength: "NA",
    //             linePx: "NA",
    //             angleDegree: "NA",
    //             meanRgb: result.MeanRgbText,
    //             hexColor: result.MeanHex,
    //             lab: result.LabText,
    //             meanR: $"{result.MeanR:F2}",
    //             meanG: $"{result.MeanG:F2}",
    //             meanB: $"{result.MeanB:F2}",
    //             labL: $"{result.LabL:F2}",
    //             labA: $"{result.LabA:F2}",
    //             labB: $"{result.LabB:F2}",
    //             dominantColorHex: result.DominantColorHex,
    //             colorDistribution: result.ColorDistributionText
    //         );

    //         StatusText = "Average color saved.";
    //     }
    //     else
    //     {
    //         StatusText = "Average color cancelled.";
    //     }

    //     CurrentDrawingPoints.Clear();
    //     RefreshDrawingBindings();
    // }
    private async void FinalizeColorBox()
    {
        if (CurrentDrawingPoints.Count < 2 || CurrentImage is null || _hostWindow is null)
            return;

        var p1 = CurrentDrawingPoints[0];
        var p2 = CurrentDrawingPoints[1];

        var minX = (int)Math.Floor(Math.Min(p1.X, p2.X));
        var maxX = (int)Math.Ceiling(Math.Max(p1.X, p2.X));
        var minY = (int)Math.Floor(Math.Min(p1.Y, p2.Y));
        var maxY = (int)Math.Ceiling(Math.Max(p1.Y, p2.Y));

        minX = Math.Max(0, minX);
        minY = Math.Max(0, minY);
        maxX = Math.Min(CurrentImage.PixelSize.Width - 1, maxX);
        maxY = Math.Min(CurrentImage.PixelSize.Height - 1, maxY);

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        if (width <= 0 || height <= 0)
        {
            StatusText = "Invalid color-box region.";
            CurrentDrawingPoints.Clear();
            RefreshDrawingBindings();
            return;
        }

        var result = ColorAnalysisService.AnalyzeBox(
            CurrentImage,
            minX,
            minY,
            maxX,
            maxY,
            clusterCount: ColorClusterCount);

        if (result is null)
        {
            StatusText = "No valid pixels in color-box.";
            CurrentDrawingPoints.Clear();
            RefreshDrawingBindings();
            return;
        }
        // 新增：构建 preview
        var previewPts = new List<PointD>
        {
            new PointD(minX, minY),
            new PointD(maxX, minY),
            new PointD(maxX, maxY),
            new PointD(minX, maxY)
        };
        var previewBitmap = BuildColorAnalysisPreview(previewPts);

        // 传入 previewBitmap
        var dlg = new AverageColorWindow(
        result,
        imagePath: CurrentImagePath,
        previewBitmap: previewBitmap,
        recalculate: k => ColorAnalysisService.AnalyzeBox(
            CurrentImage, minX, minY, maxX, maxY, clusterCount: k),
        initialClusterCount: ColorClusterCount);

        await dlg.ShowDialog(_hostWindow);

        if (dlg.Confirmed)
        {
            var finalResult = dlg.CurrentResult;  // 用最终结果
            ColorClusterCount = finalResult.ClusterCount; 
            AddMeasurementRecord(
                labelName: $"color{_measurementCounter}",
                shapeType: "colorbox",
                area: "NA",
                areaPx: "NA",
                perimeter: "NA",
                width: "NA",
                height: "NA",
                aspectRatio: "NA",
                circularity: "NA",
                equivalentDiameter: "NA",
                lineLength: "NA",
                linePx: "NA",
                angleDegree: "NA",
                meanRgb: finalResult.MeanRgbText,
                hexColor: finalResult.MeanHex,
                lab: finalResult.LabText,
                meanR: $"{finalResult.MeanR:F2}",
                meanG: $"{finalResult.MeanG:F2}",
                meanB: $"{finalResult.MeanB:F2}",
                labL: $"{finalResult.LabL:F2}",
                labA: $"{finalResult.LabA:F2}",
                labB: $"{finalResult.LabB:F2}",
                colorClusterK: finalResult.ClusterCount.ToString(),
                dominantColorHex: finalResult.DominantColorHex,
                colorDistribution: finalResult.ColorDistributionText
            );

            StatusText = "Color analysis saved.";
        }
        else
        {
            StatusText = "Color analysis cancelled.";
        }

        CurrentDrawingPoints.Clear();
        RefreshDrawingBindings();
    }
    // ── 其他命令（待实现）────────────────────────────────────────────────────

    [RelayCommand] private void Undo()     => StatusText = "Undo clicked";
    [RelayCommand] private void Redo()     => StatusText = "Redo clicked";
    [RelayCommand] private void Polygon()  => StatusText = "Polygon tool selected";
    [RelayCommand] private void Option()   => StatusText = "Option clicked";
    [RelayCommand] private void Settings() => StatusText = "Settings clicked";
    [RelayCommand] private void Tools()    => StatusText = "Tools clicked";
    [RelayCommand] private void Image()    => StatusText = "Image clicked";
    [RelayCommand] private void AddLabel() => StatusText = "Add label clicked";
    [RelayCommand] private void Convert()  => StatusText = "Convert clicked";
}