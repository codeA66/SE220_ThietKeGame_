using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Tools.Editor
{
    public static class ParallaxTextureProcessor
    {
        public const string DefaultRawInputPath =
            "SourceAssets/Parallax/raw_prison_parallax_far.jpg";

        public const string OutputAssetPath =
            "Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png";

        public const int WorkWidth = 384;
        public const int WorkHeight = 256;
        public const int TargetWidth = 768;
        public const int TargetHeight = 512;

        private const float CropVMin = 0.04f;
        private const float CropVMax = 0.96f;
        private const float TargetLumMin = 0.12f;
        private const float TargetLumMax = 0.78f;
        private const int SeamBlendWidth = 24;
        private const int QuantizeLevels = 16;
        private const float BayerDitherStrength = 0.35f;

        private static readonly float[,] Bayer4x4 =
        {
            {  0f,  8f,  2f, 10f },
            { 12f,  4f, 14f,  6f },
            {  3f, 11f,  1f,  9f },
            { 15f,  7f, 13f,  5f }
        };

        [MenuItem("Tools/DeadCells/Visual/Normalize Prison ParallaxFar Backdrop")]
        public static void NormalizeParallaxFarFromDefault()
        {
            string envPath = Environment.GetEnvironmentVariable("PARALLAX_RAW_INPUT");
            string rawPath = string.IsNullOrWhiteSpace(envPath) ? DefaultRawInputPath : envPath.Trim();
            if (!File.Exists(rawPath))
            {
                throw new FileNotFoundException(
                    $"Raw parallax backdrop image not found at '{rawPath}'. " +
                    $"Provide a source image at '{DefaultRawInputPath}' or set PARALLAX_RAW_INPUT.",
                    rawPath);
            }
            NormalizeParallaxFarTexture(rawPath);
        }

        public static void NormalizeParallaxFarTexture(string rawInputPath)
        {
            if (string.IsNullOrWhiteSpace(rawInputPath))
                throw new ArgumentException("Raw input path cannot be null or empty.", nameof(rawInputPath));

            if (!File.Exists(rawInputPath))
                throw new FileNotFoundException($"Raw parallax backdrop image not found at '{rawInputPath}'.", rawInputPath);

            byte[] pngBytes = BuildNormalizedParallaxPng(rawInputPath, out float minLum, out float maxLum, out float stdDevLum, out float seamDiff);

            string outputDir = Path.GetDirectoryName(OutputAssetPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            bool wroteFile = WriteBytesIfChanged(OutputAssetPath, pngBytes);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            bool reimported = ConfigureSpriteImporter(OutputAssetPath);

            Debug.Log(
                $"[ParallaxTextureProcessor] Normalized '{OutputAssetPath}' from '{rawInputPath}' " +
                $"(wrotePng: {wroteFile}, reimported: {reimported}, min: {minLum:F4}, max: {maxLum:F4}, " +
                $"stddev: {stdDevLum:F4}, seamDiff: {seamDiff:F4}).");
        }

        private static byte[] BuildNormalizedParallaxPng(
            string rawInputPath,
            out float minLum,
            out float maxLum,
            out float stdDevLum,
            out float seamDiff)
        {
            byte[] srcBytes = File.ReadAllBytes(rawInputPath);
            Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: true);
            float[] srcLum;
            int srcW;
            int srcH;

            try
            {
                if (!ImageConversion.LoadImage(srcTex, srcBytes, markNonReadable: false))
                    throw new InvalidOperationException($"Failed to decode raw image '{rawInputPath}'.");

                srcW = srcTex.width;
                srcH = srcTex.height;
                Color32[] srcPixels = srcTex.GetPixels32();
                srcLum = new float[srcPixels.Length];

                for (int i = 0; i < srcPixels.Length; i++)
                {
                    Color32 c = srcPixels[i];
                    srcLum[i] = (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(srcTex);
            }

            int extWidth = WorkWidth + SeamBlendWidth;
            float[] extLum = DownsampleCroppedBand(srcLum, srcW, srcH, extWidth, WorkHeight, CropVMin, CropVMax);

            NormalizePercentiles(extLum, TargetLumMin, TargetLumMax);

            float[] workLum = new float[WorkWidth * WorkHeight];
            ApplyHorizontalWrapBlend(extLum, extWidth, workLum, WorkWidth, WorkHeight, SeamBlendWidth);

            Color32[] outPixels = QuantizeAndScale2x(workLum, WorkWidth, WorkHeight);
            ComputeStatistics(outPixels, TargetWidth, TargetHeight, out minLum, out maxLum, out stdDevLum, out seamDiff);

            Texture2D outTex = new Texture2D(TargetWidth, TargetHeight, TextureFormat.RGBA32, mipChain: false, linear: true);
            try
            {
                outTex.SetPixels32(outPixels);
                outTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                return ImageConversion.EncodeToPNG(outTex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(outTex);
            }
        }

        private static float[] DownsampleCroppedBand(
            float[] srcLum,
            int srcW,
            int srcH,
            int dstW,
            int dstH,
            float vMin,
            float vMax)
        {
            float[] dst = new float[dstW * dstH];
            float vSpan = vMax - vMin;

            for (int y = 0; y < dstH; y++)
            {
                float sy0 = (vMin + (y / (float)dstH) * vSpan) * srcH;
                float sy1 = (vMin + ((y + 1) / (float)dstH) * vSpan) * srcH;
                int iyStart = Mathf.Clamp(Mathf.FloorToInt(sy0), 0, srcH - 1);
                int iyEnd = Mathf.Clamp(Mathf.CeilToInt(sy1) - 1, 0, srcH - 1);

                for (int x = 0; x < dstW; x++)
                {
                    float sx0 = (x / (float)dstW) * srcW;
                    float sx1 = ((x + 1) / (float)dstW) * srcW;
                    int ixStart = Mathf.Clamp(Mathf.FloorToInt(sx0), 0, srcW - 1);
                    int ixEnd = Mathf.Clamp(Mathf.CeilToInt(sx1) - 1, 0, srcW - 1);

                    double accum = 0.0;
                    double weightSum = 0.0;

                    for (int iy = iyStart; iy <= iyEnd; iy++)
                    {
                        float wy = Mathf.Min(sy1, iy + 1f) - Mathf.Max(sy0, iy);
                        if (wy <= 0f) continue;

                        int rowOffset = iy * srcW;
                        for (int ix = ixStart; ix <= ixEnd; ix++)
                        {
                            float wx = Mathf.Min(sx1, ix + 1f) - Mathf.Max(sx0, ix);
                            if (wx <= 0f) continue;

                            float w = wx * wy;
                            accum += srcLum[rowOffset + ix] * w;
                            weightSum += w;
                        }
                    }

                    dst[y * dstW + x] = weightSum > 0.0 ? (float)(accum / weightSum) : 0f;
                }
            }

            return dst;
        }

        private static void NormalizePercentiles(float[] buffer, float outMin, float outMax)
        {
            float[] sorted = new float[buffer.Length];
            Array.Copy(buffer, sorted, buffer.Length);
            Array.Sort(sorted);

            int idxLow = Mathf.Clamp(Mathf.RoundToInt(0.02f * (sorted.Length - 1)), 0, sorted.Length - 1);
            int idxHigh = Mathf.Clamp(Mathf.RoundToInt(0.98f * (sorted.Length - 1)), 0, sorted.Length - 1);
            float pLow = sorted[idxLow];
            float pHigh = sorted[idxHigh];
            float range = Mathf.Max(pHigh - pLow, 1e-4f);
            float outRange = outMax - outMin;

            for (int i = 0; i < buffer.Length; i++)
            {
                float t = Mathf.Clamp01((buffer[i] - pLow) / range);
                buffer[i] = outMin + t * outRange;
            }
        }

        private static void ApplyHorizontalWrapBlend(
            float[] extBuffer,
            int extWidth,
            float[] workBuffer,
            int workWidth,
            int height,
            int blendWidth)
        {
            for (int y = 0; y < height; y++)
            {
                int extRow = y * extWidth;
                int workRow = y * workWidth;

                for (int x = 0; x < workWidth; x++)
                {
                    if (x < blendWidth)
                    {
                        float t = (x + 0.5f) / blendWidth;
                        float w = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t);
                        float rightOverlap = extBuffer[extRow + workWidth + x];
                        float leftMain = extBuffer[extRow + x];
                        workBuffer[workRow + x] = Mathf.Lerp(rightOverlap, leftMain, w);
                    }
                    else
                    {
                        workBuffer[workRow + x] = extBuffer[extRow + x];
                    }
                }
            }
        }

        private static Color32[] QuantizeAndScale2x(float[] workLum, int workW, int workH)
        {
            Color32[] outPixels = new Color32[TargetWidth * TargetHeight];
            float lumRange = TargetLumMax - TargetLumMin;
            float invLevels = 1f / (QuantizeLevels - 1);

            for (int y = 0; y < workH; y++)
            {
                int workRow = y * workW;
                int outRow0 = (2 * y) * TargetWidth;
                int outRow1 = (2 * y + 1) * TargetWidth;

                for (int x = 0; x < workW; x++)
                {
                    float t = Mathf.Clamp01((workLum[workRow + x] - TargetLumMin) / lumRange);
                    float bayer = (Bayer4x4[y & 3, x & 3] / 16f) - 0.5f;
                    float dithered = Mathf.Clamp01(t + bayer * (BayerDitherStrength * invLevels));
                    float q = Mathf.Round(dithered * (QuantizeLevels - 1)) * invLevels;
                    float finalLum = TargetLumMin + q * lumRange;

                    byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(finalLum * 255f), 0, 255);
                    Color32 px = new Color32(g, g, g, 255);

                    int ox = 2 * x;
                    outPixels[outRow0 + ox] = px;
                    outPixels[outRow0 + ox + 1] = px;
                    outPixels[outRow1 + ox] = px;
                    outPixels[outRow1 + ox + 1] = px;
                }
            }

            return outPixels;
        }

        private static void ComputeStatistics(
            Color32[] pixels,
            int width,
            int height,
            out float minLum,
            out float maxLum,
            out float stdDevLum,
            out float seamDiff)
        {
            minLum = float.MaxValue;
            maxLum = float.MinValue;
            double sum = 0.0;
            double sumSq = 0.0;

            for (int i = 0; i < pixels.Length; i++)
            {
                float lum = pixels[i].r / 255f;
                if (lum < minLum) minLum = lum;
                if (lum > maxLum) maxLum = lum;
                sum += lum;
                sumSq += lum * lum;
            }

            double mean = sum / pixels.Length;
            double variance = Math.Max(0.0, (sumSq / pixels.Length) - (mean * mean));
            stdDevLum = (float)Math.Sqrt(variance);

            double seamSum = 0.0;
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                float left = pixels[row].r / 255f;
                float right = pixels[row + width - 1].r / 255f;
                seamSum += Math.Abs(left - right);
            }

            seamDiff = (float)(seamSum / height);
        }

        private static bool ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing TextureImporter for '{assetPath}'.");

            bool dirty =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                Mathf.Abs(importer.spritePixelsPerUnit - 24f) > 1e-4f ||
                importer.filterMode != FilterMode.Point ||
                importer.wrapModeU != TextureWrapMode.Repeat ||
                importer.wrapModeV != TextureWrapMode.Clamp ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.npotScale != TextureImporterNPOTScale.None;

            if (!dirty)
                return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 24f;
            importer.filterMode = FilterMode.Point;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return true;
        }

        private static bool WriteBytesIfChanged(string path, byte[] bytes)
        {
            if (File.Exists(path))
            {
                byte[] existing = File.ReadAllBytes(path);
                if (existing.Length == bytes.Length)
                {
                    bool identical = true;
                    for (int i = 0; i < existing.Length; i++)
                    {
                        if (existing[i] != bytes[i])
                        {
                            identical = false;
                            break;
                        }
                    }

                    if (identical)
                        return false;
                }
            }

            File.WriteAllBytes(path, bytes);
            return true;
        }
    }
}
