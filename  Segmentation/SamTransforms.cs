using Avalonia;
using Avalonia.Media.Imaging;
using System;
using System.Runtime.InteropServices;

namespace BioIMA.Avalonia.Segmentation;

public sealed class SamTransforms
{
    private readonly int _targetLength;

    public SamTransforms(int targetLength)
    {
        _targetLength = targetLength;
    }

    public float[] ApplyImage(Bitmap image)
    {
        int orgw = image.PixelSize.Width;
        int orgh = image.PixelSize.Height;

        int neww = 0;
        int newh = 0;
        GetPreprocessShape(orgw, orgh, _targetLength, ref neww, ref newh);

        using var renderTarget = new RenderTargetBitmap(image.PixelSize, image.Dpi);
        using (var ctx = renderTarget.CreateDrawingContext())
        {
            ctx.DrawImage(image, new Rect(0, 0, orgw, orgh));
        }

        int srcStride = orgw * 4;
        byte[] srcPixels = new byte[srcStride * orgh];

        IntPtr srcBuffer = Marshal.AllocHGlobal(srcPixels.Length);
        try
        {
            renderTarget.CopyPixels(
                new PixelRect(0, 0, orgw, orgh),
                srcBuffer,
                srcPixels.Length,
                srcStride);

            Marshal.Copy(srcBuffer, srcPixels, 0, srcPixels.Length);
        }
        finally
        {
            Marshal.FreeHGlobal(srcBuffer);
        }

        float[] transformed = new float[3 * _targetLength * _targetLength];

        for (int y = 0; y < newh; y++)
        {
            double srcY = (y + 0.5) * orgh / newh - 0.5;
            int y0 = Math.Clamp((int)Math.Floor(srcY), 0, orgh - 1);
            int y1 = Math.Clamp(y0 + 1, 0, orgh - 1);
            double wy = srcY - Math.Floor(srcY);

            for (int x = 0; x < neww; x++)
            {
                double srcX = (x + 0.5) * orgw / neww - 0.5;
                int x0 = Math.Clamp((int)Math.Floor(srcX), 0, orgw - 1);
                int x1 = Math.Clamp(x0 + 1, 0, orgw - 1);
                double wx = srcX - Math.Floor(srcX);

                int idx00 = (y0 * orgw + x0) * 4;
                int idx10 = (y0 * orgw + x1) * 4;
                int idx01 = (y1 * orgw + x0) * 4;
                int idx11 = (y1 * orgw + x1) * 4;

                double r =
                    srcPixels[idx00 + 0] * (1 - wx) * (1 - wy) +
                    srcPixels[idx10 + 0] * wx * (1 - wy) +
                    srcPixels[idx01 + 0] * (1 - wx) * wy +
                    srcPixels[idx11 + 0] * wx * wy;

                double g =
                    srcPixels[idx00 + 1] * (1 - wx) * (1 - wy) +
                    srcPixels[idx10 + 1] * wx * (1 - wy) +
                    srcPixels[idx01 + 1] * (1 - wx) * wy +
                    srcPixels[idx11 + 1] * wx * wy;

                double b =
                    srcPixels[idx00 + 2] * (1 - wx) * (1 - wy) +
                    srcPixels[idx10 + 2] * wx * (1 - wy) +
                    srcPixels[idx01 + 2] * (1 - wx) * wy +
                    srcPixels[idx11 + 2] * wx * wy;

                int outIdx = y * _targetLength + x;

                transformed[outIdx] =
                    ((float)r - 123.675f) / 58.395f;

                transformed[_targetLength * _targetLength + outIdx] =
                    ((float)g - 116.28f) / 57.12f;

                transformed[2 * _targetLength * _targetLength + outIdx] =
                    ((float)b - 103.53f) / 57.375f;
            }
        }

        return transformed;
    }

    public PointPromotion ApplyCoords(PointPromotion orgPoint, int orgw, int orgh)
    {
        int neww = 0;
        int newh = 0;
        GetPreprocessShape(orgw, orgh, _targetLength, ref neww, ref newh);

        float scalx = 1.0f * neww / orgw;
        float scaly = 1.0f * newh / orgh;

        return new PointPromotion(orgPoint.OpType)
        {
            X = (int)(orgPoint.X * scalx),
            Y = (int)(orgPoint.Y * scaly)
        };
    }

    public BoxPromotion ApplyBox(BoxPromotion orgBox, int orgw, int orgh)
    {
        return new BoxPromotion
        {
            LeftUp = ApplyCoords(orgBox.LeftUp, orgw, orgh),
            RightBottom = ApplyCoords(orgBox.RightBottom, orgw, orgh)
        };
    }

    public void GetPreprocessShape(int oldw, int oldh, int longSideLength, ref int neww, ref int newh)
    {
        float scale = longSideLength * 1.0f / Math.Max(oldh, oldw);
        float newht = oldh * scale;
        float newwt = oldw * scale;

        neww = (int)(newwt + 0.5);
        newh = (int)(newht + 0.5);
    }
}