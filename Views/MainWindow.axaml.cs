using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.VisualTree;
using Avalonia.Threading;
using BioIMA.Avalonia.ViewModels;
using System; //
using System.ComponentModel;
using BioIMA.Core.Models;
using BioIMA.Core.Enums;

namespace BioIMA.Avalonia.Views;


public partial class MainWindow : Window
{   //构造函数
    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetHostWindow(this);
            AttachVmHandlers(vm);
        }

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel newVm)
            {
                newVm.SetHostWindow(this);
                AttachVmHandlers(newVm);
            }

            UpdateViewportFromCurrentLayout();
        };

        Opened += (_, _) => UpdateViewportFromCurrentLayout();
        SizeChanged += (_, _) => UpdateViewportFromCurrentLayout();

        var overlay = this.FindControl<Control>("OverlayCanvas");
        if (overlay is not null)
            overlay.SizeChanged += (_, _) => UpdateViewportFromCurrentLayout();

        var image = this.FindControl<Image>("MainImage");
        if (image is not null)
            image.SizeChanged += (_, _) => UpdateViewportFromCurrentLayout();
    }
    private MainWindowViewModel? _subscribedVm;

//     private void OverlayCanvas_OnPointerPressed(object? sender, PointerPressedEventArgs e)
//         {
//             if (DataContext is not MainWindowViewModel vm)
//                 return;

//             if (sender is not Control canvas)
//                 return;

//             var imageControl = this.FindControl<Image>("MainImage");
//             if (imageControl is null || vm.CurrentImage is null)
//                 return;

//             var canvasPoint = e.GetPosition(canvas);

//             var imagePixelWidth = vm.CurrentImage.PixelSize.Width;
//             var imagePixelHeight = vm.CurrentImage.PixelSize.Height;

//             var canvasWidth = canvas.Bounds.Width;
//             var canvasHeight = canvas.Bounds.Height;

//             var scale = Math.Min(canvasWidth / imagePixelWidth, canvasHeight / imagePixelHeight);
//             var displayWidth = imagePixelWidth * scale;
//             var displayHeight = imagePixelHeight * scale;

//             var offsetX = (canvasWidth - displayWidth) / 2.0;
//             var offsetY = (canvasHeight - displayHeight) / 2.0;
// //更新视窗
//             vm.UpdateImageViewport(
//                 offsetX,
//                 offsetY, 
//                 displayWidth,
//                 displayHeight,
//                 imagePixelWidth,
//                 imagePixelHeight);

//             // clicked outside actual image area
//             if (canvasPoint.X < offsetX || canvasPoint.X > offsetX + displayWidth ||
//                 canvasPoint.Y < offsetY || canvasPoint.Y > offsetY + displayHeight)
//             {
//                 vm.StatusText = "Clicked outside image area.";
//                 return;
//             }

//             var imageX = (canvasPoint.X - offsetX) / scale;
//             var imageY = (canvasPoint.Y - offsetY) / scale;

//             var isDoubleClick = e.ClickCount >= 2;

//             vm.HandleCanvasPointerPressed(imageX, imageY, isDoubleClick);
//         }
////
        private void AttachVmHandlers(MainWindowViewModel vm)
        {
            if (_subscribedVm is not null)
                _subscribedVm.PropertyChanged -= Vm_PropertyChanged;

            _subscribedVm = vm;
            _subscribedVm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.ViewZoom) ||
                e.PropertyName == nameof(MainWindowViewModel.CurrentImage))
            {
                Dispatcher.UIThread.Post(
                    UpdateViewportFromCurrentLayout,
                    DispatcherPriority.Render);
            }
        }
/// /------------------缩放逻辑的公共方法 ，任何缩放的时候都调用--------------------------
        private void UpdateViewportFromCurrentLayout()
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            var imageControl = this.FindControl<Image>("MainImage");
            var overlay = this.FindControl<Control>("OverlayCanvas");

            if (imageControl is null || overlay is null || vm.CurrentImage is null)
                return;

            var imagePixelWidth = vm.CurrentImage.PixelSize.Width;
            var imagePixelHeight = vm.CurrentImage.PixelSize.Height;

            var canvasWidth = overlay.Bounds.Width;
            var canvasHeight = overlay.Bounds.Height;

            if (canvasWidth <= 0 || canvasHeight <= 0 || imagePixelWidth <= 0 || imagePixelHeight <= 0)
                return;

            var baseScale = Math.Min(canvasWidth / imagePixelWidth, canvasHeight / imagePixelHeight);
            var baseDisplayWidth = imagePixelWidth * baseScale;
            var baseDisplayHeight = imagePixelHeight * baseScale;

            var offsetX = (canvasWidth - baseDisplayWidth) / 2.0;
            var offsetY = (canvasHeight - baseDisplayHeight) / 2.0;

            vm.UpdateImageViewport(
                offsetX,
                offsetY,
                baseDisplayWidth,
                baseDisplayHeight,
                imagePixelWidth,
                imagePixelHeight);
        }
        // 1. 修改签名，添加 async 关键字
       private async void OverlayCanvas_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (sender is not Control canvas)
                return;

            var imageControl = this.FindControl<Image>("MainImage");
            if (imageControl is null || vm.CurrentImage is null)
                return;

            var canvasPoint = e.GetPosition(canvas);

            var imagePixelWidth = vm.CurrentImage.PixelSize.Width;
            var imagePixelHeight = vm.CurrentImage.PixelSize.Height;

            var canvasWidth = canvas.Bounds.Width;
            var canvasHeight = canvas.Bounds.Height;

            var scale = Math.Min(canvasWidth / imagePixelWidth, canvasHeight / imagePixelHeight);
            var displayWidth = imagePixelWidth * scale;
            var displayHeight = imagePixelHeight * scale;

            var offsetX = (canvasWidth - displayWidth) / 2.0;
            var offsetY = (canvasHeight - displayHeight) / 2.0;

            vm.UpdateImageViewport(
                offsetX,
                offsetY,
                displayWidth,
                displayHeight,
                imagePixelWidth,
                imagePixelHeight);

            if (vm.TryBeginDragVertex(canvasPoint.X, canvasPoint.Y))
            {
                e.Pointer.Capture(canvas);
                return;
            }

            if (canvasPoint.X < offsetX || canvasPoint.X > offsetX + displayWidth ||
                canvasPoint.Y < offsetY || canvasPoint.Y > offsetY + displayHeight)
            {
                vm.StatusText = "Clicked outside image area.";
                return;
            }

            var imageX = (canvasPoint.X - offsetX) / scale;
            var imageY = (canvasPoint.Y - offsetY) / scale;

            var props = e.GetCurrentPoint(canvas).Properties;

            if (vm.CurrentTool == ToolMode.SegmentBox && props.IsRightButtonPressed)
            {
                vm.BeginSamBox(imageX, imageY);
                e.Pointer.Capture(canvas);
                return;
            }

            var isDoubleClick = e.ClickCount >= 2;
            vm.HandleCanvasPointerPressed(imageX, imageY, isDoubleClick);

            if (vm.ScaleDialogPending)
            {
                vm.ScaleDialogPending = false;

                var dlg = new SetScaleWindow(vm.LastRulerPixelLength);
                await dlg.ShowDialog(this);

                if (dlg.Confirmed)
                {
                    vm.ApplyScale(vm.LastRulerPixelLength, dlg.KnownLength, dlg.UnitName);
                }
            }
        }
        private void OverlayCanvas_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (sender is not Control canvas)
                return;

            var p = e.GetPosition(canvas);

            if (vm.CurrentTool == ToolMode.SegmentBox && vm.IsSamBoxDragging)
            {
                var imageX = vm.CanvasToImageX(p.X);
                var imageY = vm.CanvasToImageY(p.Y);
                vm.UpdateSamBox(imageX, imageY);
                return;
            }

            if (!vm.IsDraggingVertex)
                return;

            vm.DragSelectedVertex(p.X, p.Y);
        }
        private async void OverlayCanvas_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (vm.CurrentTool == ToolMode.SegmentBox && vm.IsSamBoxDragging)
            {
                await vm.EndSamBoxAsync();
                e.Pointer.Capture(null);
                return;
            }

            vm.EndDragVertex();
            e.Pointer.Capture(null);
        }
        private async void LabelsListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (vm.SelectedAnnotationLabel is null)
                return;

            var dlg = new EditLabelWindow(vm.SelectedAnnotationLabel);
            var result = await dlg.ShowDialog<bool?>(this);

            if (result != true)
                return;

            if (dlg.ShouldDelete)
            {
                vm.DeleteSelectedLabel();
                return;
            }

            vm.SelectedAnnotationLabel.Name = dlg.EditedName;
            vm.SelectedAnnotationLabel.StrokeColor = dlg.EditedStrokeColor;
            vm.SelectedAnnotationLabel.FillColor = dlg.EditedFillColor;

            vm.RefreshOverlayAfterLabelEdit();
        }
        private bool TryMapCanvasPointToImagePoint(
            double canvasX,
            double canvasY,
            double canvasWidth,
            double canvasHeight,
            double imageControlWidth,
            double imageControlHeight,
            double bitmapPixelWidth,
            double bitmapPixelHeight,
            out double imageX,
            out double imageY)
        {
            imageX = 0;
            imageY = 0;

            if (bitmapPixelWidth <= 0 || bitmapPixelHeight <= 0)
                return false;

            // Uniform scaling
            var scale = Math.Min(imageControlWidth / bitmapPixelWidth, imageControlHeight / bitmapPixelHeight);

            var displayedWidth = bitmapPixelWidth * scale;
            var displayedHeight = bitmapPixelHeight * scale;

            var offsetX = (canvasWidth - displayedWidth) / 2.0;
            var offsetY = (canvasHeight - displayedHeight) / 2.0;

            // clicked outside actual image display area
            if (canvasX < offsetX || canvasX > offsetX + displayedWidth ||
                canvasY < offsetY || canvasY > offsetY + displayedHeight)
            {
                return false;
            }

            imageX = (canvasX - offsetX) / scale;
            imageY = (canvasY - offsetY) / scale;

            return true;
        }
}
