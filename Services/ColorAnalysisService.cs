using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using BioIMA.Avalonia.Models;
using BioIMA.Core.Models;

namespace BioIMA.Avalonia.Services;

public static class ColorAnalysisService
{
    private readonly struct PixelSample
    {
        public PixelSample(byte r, byte g, byte b, double l, double a, double labB)
        {
            R = r;
            G = g;
            B = b;
            L = l;
            A = a;
            LabB = labB;
        }

        public byte R { get; }

        public byte G { get; }

        public byte B { get; }

        public double L { get; }

        public double A { get; }

        public double LabB { get; }
    }

    private struct LabCenter
    {
        public double L;
        public double A;
        public double B;
    }

    private struct ClusterAccumulator
    {
        public long Count;

        public double SumR;
        public double SumG;
        public double SumB;

        public double SumL;
        public double SumA;
        public double SumLabB;
    }

    public static ColorAnalysisResult? AnalyzeBox(
        Bitmap bitmap,
        int minX,
        int minY,
        int maxX,
        int maxY,
        int clusterCount = 5,
        int maxTrainingPixels = 200_000)
    {
        return AnalyzeRegion(
            bitmap,
            minX,
            minY,
            maxX,
            maxY,
            includePixel: null,
            clusterCount: clusterCount,
            maxTrainingPixels: maxTrainingPixels);
    }

    public static ColorAnalysisResult? AnalyzePolygon(
        Bitmap bitmap,
        IReadOnlyList<PointD> points,
        int clusterCount = 5,
        int maxTrainingPixels = 200_000)
    {
        if (points.Count < 3)
            return null;

        var minX = (int)Math.Floor(points.Min(p => p.X));
        var maxX = (int)Math.Ceiling(points.Max(p => p.X));
        var minY = (int)Math.Floor(points.Min(p => p.Y));
        var maxY = (int)Math.Ceiling(points.Max(p => p.Y));

        return AnalyzeRegion(
            bitmap,
            minX,
            minY,
            maxX,
            maxY,
            includePixel: (x, y) => IsPointInPolygon(x, y, points),
            clusterCount: clusterCount,
            maxTrainingPixels: maxTrainingPixels);
    }
    public static ColorAnalysisResult? AnalyzeMask(
        Bitmap bitmap,
        byte[] mask,
        int maskWidth,
        int maskHeight,
        int clusterCount = 5,
        int maxTrainingPixels = 200_000)
    {
        if (mask.Length == 0)
            return null;

        if (bitmap.PixelSize.Width != maskWidth || bitmap.PixelSize.Height != maskHeight)
            return null;

        int minX = maskWidth;
        int minY = maskHeight;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < maskHeight; y++)
        {
            for (int x = 0; x < maskWidth; x++)
            {
                int idx = y * maskWidth + x;

                if (idx >= mask.Length || mask[idx] == 0)
                    continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
            return null;

        return AnalyzeRegion(
            bitmap,
            minX,
            minY,
            maxX,
            maxY,
            includePixel: (x, y) =>
            {
                int ix = (int)Math.Floor(x);
                int iy = (int)Math.Floor(y);

                if (ix < 0 || ix >= maskWidth || iy < 0 || iy >= maskHeight)
                    return false;

                int idx = iy * maskWidth + ix;
                return idx >= 0 && idx < mask.Length && mask[idx] != 0;
            },
            clusterCount: clusterCount,
            maxTrainingPixels: maxTrainingPixels);
    }

    private static ColorAnalysisResult? AnalyzeRegion(
        Bitmap bitmap,
        int minX,
        int minY,
        int maxX,
        int maxY,
        Func<double, double, bool>? includePixel,
        int clusterCount,
        int maxTrainingPixels)
    {
        if (clusterCount < 1)
            clusterCount = 1;

        minX = Math.Max(0, minX);
        minY = Math.Max(0, minY);
        maxX = Math.Min(bitmap.PixelSize.Width - 1, maxX);
        maxY = Math.Min(bitmap.PixelSize.Height - 1, maxY);

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        if (width <= 0 || height <= 0)
            return null;

        var stride = width * 4;
        var bufferSize = stride * height;
        var managedBuffer = new byte[bufferSize];

        IntPtr unmanagedBuffer = IntPtr.Zero;

        try
        {
            unmanagedBuffer = Marshal.AllocHGlobal(bufferSize);

            bitmap.CopyPixels(
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

        // Pass 1: exact mean RGB and valid pixel count.
        double sumR = 0;
        double sumG = 0;
        double sumB = 0;
        long validCount = 0;

        ForEachValidPixel(
            managedBuffer,
            width,
            height,
            stride,
            minX,
            minY,
            includePixel,
            (r, g, b) =>
            {
                sumR += r;
                sumG += g;
                sumB += b;
                validCount++;
            });

        if (validCount == 0)
            return null;

        var meanR = sumR / validCount;
        var meanG = sumG / validCount;
        var meanB = sumB / validCount;

        var meanRByte = (byte)Math.Clamp(Math.Round(meanR), 0, 255);
        var meanGByte = (byte)Math.Clamp(Math.Round(meanG), 0, 255);
        var meanBByte = (byte)Math.Clamp(Math.Round(meanB), 0, 255);

        var meanHex = RgbToHex(meanRByte, meanGByte, meanBByte);
        var meanLab = RgbToLab(meanR, meanG, meanB);

        // Pass 2: collect training pixels for K-means.
        // For very large ROIs, do not train on every pixel.
        var sampleEvery = (int)Math.Max(1, validCount / Math.Max(1, maxTrainingPixels));
        var trainingPixels = new List<PixelSample>();
        long validIndex = 0;

        ForEachValidPixel(
            managedBuffer,
            width,
            height,
            stride,
            minX,
            minY,
            includePixel,
            (r, g, b) =>
            {
                if (validIndex % sampleEvery == 0)
                {
                    var lab = RgbToLab(r, g, b);
                    trainingPixels.Add(new PixelSample(r, g, b, lab.L, lab.A, lab.B));
                }

                validIndex++;
            });

        var k = Math.Min(clusterCount, trainingPixels.Count);
        
        if (trainingPixels.Count == 0)
            return new ColorAnalysisResult
            {
                ClusterCount = k,
                PixelCount = validCount,
                MeanR = meanR,
                MeanG = meanG,
                MeanB = meanB,
                MeanHex = meanHex,
                LabL = meanLab.L,
                LabA = meanLab.A,
                LabB = meanLab.B
            };

        // var k = Math.Min(clusterCount, trainingPixels.Count);
        var centers = RunKMeans(trainingPixels, k, 20);

        // Pass 3: assign all valid pixels to the trained centers.
        // This makes percentages based on all pixels, not only the training sample.
        var accumulators = new ClusterAccumulator[k];

        ForEachValidPixel(
            managedBuffer,
            width,
            height,
            stride,
            minX,
            minY,
            includePixel,
            (r, g, b) =>
            {
                var lab = RgbToLab(r, g, b);
                var clusterIndex = FindNearestCenter(lab.L, lab.A, lab.B, centers);

                accumulators[clusterIndex].Count++;
                accumulators[clusterIndex].SumR += r;
                accumulators[clusterIndex].SumG += g;
                accumulators[clusterIndex].SumB += b;
                accumulators[clusterIndex].SumL += lab.L;
                accumulators[clusterIndex].SumA += lab.A;
                accumulators[clusterIndex].SumLabB += lab.B;
            });

        var clusters = new List<ColorClusterRecord>();

        for (int i = 0; i < accumulators.Length; i++)
        {
            var acc = accumulators[i];

            if (acc.Count == 0)
                continue;

            var clusterR = (byte)Math.Clamp(Math.Round(acc.SumR / acc.Count), 0, 255);
            var clusterG = (byte)Math.Clamp(Math.Round(acc.SumG / acc.Count), 0, 255);
            var clusterB = (byte)Math.Clamp(Math.Round(acc.SumB / acc.Count), 0, 255);

            clusters.Add(new ColorClusterRecord
            {
                PixelCount = acc.Count,
                Percentage = acc.Count * 100.0 / validCount,
                R = clusterR,
                G = clusterG,
                B = clusterB,
                Hex = RgbToHex(clusterR, clusterG, clusterB),
                LabL = acc.SumL / acc.Count,
                LabA = acc.SumA / acc.Count,
                LabB = acc.SumLabB / acc.Count
            });
        }

        clusters = clusters
            .OrderByDescending(c => c.PixelCount)
            .ToList();

        for (int i = 0; i < clusters.Count; i++)
            clusters[i].Rank = i + 1;

        return new ColorAnalysisResult
        {
            ClusterCount = k,
            PixelCount = validCount,
            MeanR = meanR,
            MeanG = meanG,
            MeanB = meanB,
            MeanHex = meanHex,
            LabL = meanLab.L,
            LabA = meanLab.A,
            LabB = meanLab.B,
            Clusters = clusters
        };
    }

    private static void ForEachValidPixel(
        byte[] buffer,
        int width,
        int height,
        int stride,
        int minX,
        int minY,
        Func<double, double, bool>? includePixel,
        Action<byte, byte, byte> action)
    {
        for (int yy = 0; yy < height; yy++)
        {
            for (int xx = 0; xx < width; xx++)
            {
                double imageX = minX + xx + 0.5;
                double imageY = minY + yy + 0.5;

                if (includePixel is not null && !includePixel(imageX, imageY))
                    continue;

                var i = yy * stride + xx * 4;

                // 当前 Avalonia CopyPixels 按 RGBA 读取。
                byte r = buffer[i + 0];
                byte g = buffer[i + 1];
                byte b = buffer[i + 2];
                byte a = buffer[i + 3];

                if (a == 0)
                    continue;

                action(r, g, b);
            }
        }
    }

    private static LabCenter[] RunKMeans(
        IReadOnlyList<PixelSample> samples,
        int k,
        int iterations)
    {
        var centers = InitializeCenters(samples, k);

        for (int iter = 0; iter < iterations; iter++)
        {
            var counts = new int[k];
            var sumL = new double[k];
            var sumA = new double[k];
            var sumB = new double[k];

            for (int i = 0; i < samples.Count; i++)
            {
                var p = samples[i];
                var nearest = FindNearestCenter(p.L, p.A, p.LabB, centers);

                counts[nearest]++;
                sumL[nearest] += p.L;
                sumA[nearest] += p.A;
                sumB[nearest] += p.LabB;
            }

            for (int c = 0; c < k; c++)
            {
                if (counts[c] == 0)
                    continue;

                centers[c].L = sumL[c] / counts[c];
                centers[c].A = sumA[c] / counts[c];
                centers[c].B = sumB[c] / counts[c];
            }
        }

        return centers;
    }

    private static LabCenter[] InitializeCenters(
        IReadOnlyList<PixelSample> samples,
        int k)
    {
        var ordered = samples
            .OrderBy(p => p.L)
            .ThenBy(p => p.A)
            .ThenBy(p => p.LabB)
            .ToList();

        var centers = new LabCenter[k];

        for (int i = 0; i < k; i++)
        {
            var index = (int)Math.Floor((i + 0.5) * ordered.Count / k);
            index = Math.Clamp(index, 0, ordered.Count - 1);

            centers[i] = new LabCenter
            {
                L = ordered[index].L,
                A = ordered[index].A,
                B = ordered[index].LabB
            };
        }

        return centers;
    }

    private static int FindNearestCenter(
        double l,
        double a,
        double b,
        IReadOnlyList<LabCenter> centers)
    {
        var bestIndex = 0;
        var bestDistance = double.MaxValue;

        for (int i = 0; i < centers.Count; i++)
        {
            var dl = l - centers[i].L;
            var da = a - centers[i].A;
            var db = b - centers[i].B;

            var distance = dl * dl + da * da + db * db;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static bool IsPointInPolygon(
        double x,
        double y,
        IReadOnlyList<PointD> polygon)
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

    public static string RgbToHex(byte r, byte g, byte b)
    {
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public static (double L, double A, double B) RgbToLab(
        double r,
        double g,
        double b)
    {
        double PivotRgb(double n)
        {
            n /= 255.0;
            return n <= 0.04045
                ? n / 12.92
                : Math.Pow((n + 0.055) / 1.055, 2.4);
        }

        var rl = PivotRgb(r);
        var gl = PivotRgb(g);
        var bl = PivotRgb(b);

        var x = rl * 0.4124564 + gl * 0.3575761 + bl * 0.1804375;
        var y = rl * 0.2126729 + gl * 0.7151522 + bl * 0.0721750;
        var z = rl * 0.0193339 + gl * 0.1191920 + bl * 0.9503041;

        x /= 0.95047;
        y /= 1.00000;
        z /= 1.08883;

        double PivotXyz(double n)
        {
            return n > 0.008856
                ? Math.Pow(n, 1.0 / 3.0)
                : 7.787 * n + 16.0 / 116.0;
        }

        var fx = PivotXyz(x);
        var fy = PivotXyz(y);
        var fz = PivotXyz(z);

        var labL = 116 * fy - 16;
        var labA = 500 * (fx - fy);
        var labB = 200 * (fy - fz);

        return (labL, labA, labB);
    }
}