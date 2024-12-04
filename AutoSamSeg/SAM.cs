﻿using Emgu.CV;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;


namespace wpf522
{
    class SAM
    {
        public static SAM theSingleton = null;
        InferenceSession mEncoder;
        InferenceSession mDecoder;
        public float mask_threshold = 0.0f;
        bool mReady = false;
        protected SAM()
        {

        }
        public static SAM Instance()
        {
            if (null == theSingleton)
            {
                theSingleton = new SAM();
            }
            return theSingleton;
        }
        /// <summary>
        /// 加载Segment Anything模型
        /// </summary>
        public void LoadONNXModel()
        {
            // 清理旧的模型会话
            if (this.mEncoder != null)
                this.mEncoder.Dispose();

            if (this.mDecoder != null)
                this.mDecoder.Dispose();

            // 设置 ONNX Runtime 的 Session 选项
            var options = new SessionOptions
            {
                EnableMemoryPattern = false,
                EnableCpuMemArena = false
            };

            // 获取可执行文件的目录
            string exePath = AppDomain.CurrentDomain.BaseDirectory;  // 确保路径正确
            string modelFolder = Path.Combine(exePath, "SAMmodel");  // 拼接模型文件夹路径

            // 定义编码器模型的路径
            string encodeModelPath = Path.Combine(modelFolder, "encoder-quant.onnx");
            if (!File.Exists(encodeModelPath))
            {
                MessageBox.Show(encodeModelPath + " not exist!");
                return;
            }
            this.mEncoder = new InferenceSession(encodeModelPath, options);

            // 定义解码器模型的路径
            string decodeModelPath = Path.Combine(modelFolder, "decoder-quant.onnx");
            if (!File.Exists(decodeModelPath))
            {
                MessageBox.Show(decodeModelPath + " not exist!");
                return;
            }
            this.mDecoder = new InferenceSession(decodeModelPath, options);
        }

        /// <summary>
        /// Segment Anything对图像进行编码
        /// </summary>
        public float[] Encode(OpenCvSharp.Mat image, int orgWid, int orgHei)
        {
            Transforms tranform = new Transforms(1024);

            float[] img = tranform.ApplyImage(image, orgWid, orgHei);
            var tensor = new DenseTensor<float>(img, new[] { 1, 3, 1024, 1024 });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("x", tensor)
            };

            var results = this.mEncoder.Run(inputs);
            var embedding = results.First().AsTensor<float>().ToArray();
            this.mReady = true;

            return embedding;
        }

        /// <summary>
        /// Segment Anything提示信息解码
        /// </summary>
        public MaskData Decode(List<Promotion> promotions, float[] embedding, int orgWid, int orgHei)
        {
            if (this.mReady == false)
            {
                MessageBox.Show("Image Embedding is not done!");
                return null;
            }

            var embedding_tensor = new DenseTensor<float>(embedding, new[] { 1, 256, 64, 64 });

            var bpmos = promotions.FindAll(e => e.mType == PromotionType.Box);
            var pproms = promotions.FindAll(e => e.mType == PromotionType.Point);
            int boxCount = promotions.FindAll(e => e.mType == PromotionType.Box).Count();
            int pointCount = promotions.FindAll(e => e.mType == PromotionType.Point).Count();
            float[] promotion = new float[2 * (boxCount * 2 + pointCount)];
            float[] label = new float[boxCount * 2 + pointCount];
            for (int i = 0; i < boxCount; i++)
            {
                var input = bpmos[i].GetInput();
                for (int j = 0; j < input.Count(); j++)
                {
                    promotion[4 * i + j] = input[j];
                }
                var la = bpmos[i].GetLable();
                for (int j = 0; j < la.Count(); j++)
                {
                    label[2 * i + j] = la[j];
                }
            }
            for (int i = 0; i < pointCount; i++)
            {
                var p = pproms[i].GetInput();
                for (int j = 0; j < p.Count(); j++)
                {
                    promotion[boxCount * 4 + 2 * i + j] = p[j];
                }
                var la = pproms[i].GetLable();
                for (int j = 0; j < la.Count(); j++)
                {
                    label[boxCount * 2 + i + j] = la[j];
                }
            }

            var point_coords_tensor = new DenseTensor<float>(promotion, new[] { 1, boxCount * 2 + pointCount, 2 });

            var point_label_tensor = new DenseTensor<float>(label, new[] { 1, boxCount * 2 + pointCount });

            float[] mask = new float[256 * 256];
            for (int i = 0; i < mask.Count(); i++)
            {
                mask[i] = 0;
            }
            var mask_tensor = new DenseTensor<float>(mask, new[] { 1, 1, 256, 256 });

            float[] hasMaskValues = new float[1] { 0 };
            var hasMaskValues_tensor = new DenseTensor<float>(hasMaskValues, new[] { 1 });

            float[] orig_im_size_values = { (float)orgHei, (float)orgWid };
            var orig_im_size_values_tensor = new DenseTensor<float>(orig_im_size_values, new[] { 2 });

            var decode_inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("image_embeddings", embedding_tensor),
                NamedOnnxValue.CreateFromTensor("point_coords", point_coords_tensor),
                NamedOnnxValue.CreateFromTensor("point_labels", point_label_tensor),
                NamedOnnxValue.CreateFromTensor("mask_input", mask_tensor),
                NamedOnnxValue.CreateFromTensor("has_mask_input", hasMaskValues_tensor),
                NamedOnnxValue.CreateFromTensor("orig_im_size", orig_im_size_values_tensor)
            };
            MaskData md = new MaskData();
            var segmask = this.mDecoder.Run(decode_inputs).ToList();
            md.mMask = segmask[0].AsTensor<float>().ToArray().ToList();
            md.mShape = segmask[0].AsTensor<float>().Dimensions.ToArray();
            md.mIoU = segmask[1].AsTensor<float>().ToList();
            return md;

        }

    }

}