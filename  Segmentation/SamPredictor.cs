using Avalonia.Media.Imaging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BioIMA.Avalonia.Segmentation;

public sealed class SamPredictor : IDisposable
{
    private InferenceSession? _encoder;
    private InferenceSession? _decoder;

    public float MaskThreshold { get; set; } = 0.0f;
    public bool IsReady { get; private set; }

    public void LoadOnnxModel(string modelFolder)
    {
        _encoder?.Dispose();
        _decoder?.Dispose();

    // 把模型复制到用户可写目录，绕开 macOS app bundle 的只读/权限限制
        modelFolder = EnsureModelsReady(modelFolder);

        var options = new SessionOptions
        {
            EnableMemoryPattern = false,
            EnableCpuMemArena = false
        };

        string encoderPath = Path.Combine(modelFolder, "encoder-quant.onnx");
        string decoderPath = Path.Combine(modelFolder, "decoder-quant.onnx");

        if (!File.Exists(encoderPath))
            throw new FileNotFoundException($"Encoder model not found: {encoderPath}");

        if (!File.Exists(decoderPath))
            throw new FileNotFoundException($"Decoder model not found: {decoderPath}");

        _encoder = new InferenceSession(encoderPath, options);
        _decoder = new InferenceSession(decoderPath, options);
        IsReady = true;
    }
    private static string EnsureModelsReady(string sourceFolder)
    {
        // macOS: ~/Library/Application Support/BioIMA/SAMmodel
        var targetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BioIMA", "SAMmodel");
        Directory.CreateDirectory(targetDir);

        foreach (var name in new[] { "encoder-quant.onnx", "decoder-quant.onnx" })
        {
            var src = Path.Combine(sourceFolder, name);
            var dst = Path.Combine(targetDir, name);
            // 不存在或大小不一致zai复制，避免每次启动都重复拷贝
            if (File.Exists(src) &&
                (!File.Exists(dst) || new FileInfo(dst).Length != new FileInfo(src).Length))
            {
                File.Copy(src, dst, overwrite: true);
            }
        }
        return targetDir;
    }

    public float[] Encode(Bitmap image)
    {
        if (_encoder is null)
            throw new InvalidOperationException("SAM encoder is not loaded.");

        var transform = new SamTransforms(1024);

        float[] img = transform.ApplyImage(image);
        var tensor = new DenseTensor<float>(img, new[] { 1, 3, 1024, 1024 });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("x", tensor)
        };

        using var results = _encoder.Run(inputs);
        return results.First().AsTensor<float>().ToArray();
    }

    public SamMaskData Decode(List<SamPromotion> promotions, float[] embedding, int orgWid, int orgHei)
    {
        if (!IsReady || _decoder is null)
            throw new InvalidOperationException("SAM decoder is not loaded.");

        var embeddingTensor = new DenseTensor<float>(embedding, new[] { 1, 256, 64, 64 });

        var boxPrompts = promotions.Where(e => e.Type == SamPromotionType.Box).ToList();
        var pointPrompts = promotions.Where(e => e.Type == SamPromotionType.Point).ToList();

        int boxCount = boxPrompts.Count;
        int pointCount = pointPrompts.Count;

        float[] promotion = new float[2 * (boxCount * 2 + pointCount)];
        float[] labels = new float[boxCount * 2 + pointCount];

        for (int i = 0; i < boxCount; i++)
        {
            var input = boxPrompts[i].GetInput();
            for (int j = 0; j < input.Length; j++)
                promotion[4 * i + j] = input[j];

            var lab = boxPrompts[i].GetLabel();
            for (int j = 0; j < lab.Length; j++)
                labels[2 * i + j] = lab[j];
        }

        for (int i = 0; i < pointCount; i++)
        {
            var input = pointPrompts[i].GetInput();
            for (int j = 0; j < input.Length; j++)
                promotion[boxCount * 4 + 2 * i + j] = input[j];

            var lab = pointPrompts[i].GetLabel();
            for (int j = 0; j < lab.Length; j++)
                labels[boxCount * 2 + i + j] = lab[j];
        }

        var pointCoordsTensor = new DenseTensor<float>(
            promotion,
            new[] { 1, boxCount * 2 + pointCount, 2 });

        var pointLabelTensor = new DenseTensor<float>(
            labels,
            new[] { 1, boxCount * 2 + pointCount });

        float[] maskInput = new float[256 * 256];
        var maskTensor = new DenseTensor<float>(maskInput, new[] { 1, 1, 256, 256 });

        var hasMaskTensor = new DenseTensor<float>(new float[] { 0 }, new[] { 1 });
        var origSizeTensor = new DenseTensor<float>(new float[] { orgHei, orgWid }, new[] { 2 });

        var decodeInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("image_embeddings", embeddingTensor),
            NamedOnnxValue.CreateFromTensor("point_coords", pointCoordsTensor),
            NamedOnnxValue.CreateFromTensor("point_labels", pointLabelTensor),
            NamedOnnxValue.CreateFromTensor("mask_input", maskTensor),
            NamedOnnxValue.CreateFromTensor("has_mask_input", hasMaskTensor),
            NamedOnnxValue.CreateFromTensor("orig_im_size", origSizeTensor)
        };

        using var segmask = _decoder.Run(decodeInputs);
        var list = segmask.ToList();

        return new SamMaskData
        {
            Mask = list[0].AsTensor<float>().ToArray().ToList(),
            Shape = list[0].AsTensor<float>().Dimensions.ToArray(),
            IoU = list[1].AsTensor<float>().ToList()
        };
    }

    public void Dispose()
    {
        _encoder?.Dispose();
        _decoder?.Dispose();
    }
}