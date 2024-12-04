using SkiaSharp;
using Svg.Skia;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class SvgHelper
{
    public static ImageSource LoadSvg(string path)
    {
        try
        {
            using var svg = new SKSvg();
            svg.Load(path);

            var bitmap = new SKBitmap(100, 100);  // 设置输出大小
            using var canvas = new SKCanvas(bitmap);
            canvas.DrawPicture(svg.Picture);

            return bitmap.ToImageSource();
        }
        catch (Exception ex)
        {
            // 可选：处理异常情况，避免程序崩溃
            System.Windows.MessageBox.Show($"加载SVG失败: {ex.Message}");
            return null;
        }
    }
}

// 扩展方法：将 SKBitmap 转换为 ImageSource
public static class BitmapExtensions
{
    public static ImageSource ToImageSource(this SKBitmap bitmap)
    {
        var image = new WriteableBitmap(bitmap.Width, bitmap.Height, 96, 96, PixelFormats.Bgra32, null);
        image.WritePixels(
            new System.Windows.Int32Rect(0, 0, bitmap.Width, bitmap.Height),
            bitmap.GetPixels(),
            bitmap.RowBytes,
            0);
        return image;
    }
}
