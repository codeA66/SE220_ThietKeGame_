using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Tools.Editor
{
    public static class PrisonNormalMapGenerator
    {
        private const string SpriteDirectory = "Assets/_Project/Art/Environment/Prison/Sprites";
        private const string NormalDirectory = "Assets/_Project/Art/Environment/Prison/Normals";
        private const string NormalSecondaryTextureName = "_NormalMap";
        private const float SobelStrength = 1.6f;
        private const float AlphaCutoff = 0.05f;

        public static readonly string[] CoreArchitectureSpriteNames =
        {
            "backWall_0_00.png",
            "backWall_1_00.png",
            "backWall_2_00.png",
            "backWall_3_00.png",
            "backWall_4_00.png",
            "backWall_5_00.png",
            "backWall_6_00.png",
            "backWall_7_00.png",
            "backWall_8_00.png",
            "backWall_9_00.png",
            "floorStamp_0_00.png",
            "floorStamp_1_00.png",
            "floorStamp_2_00.png",
            "floorStamp_3_00.png",
            "floorStamp_4_00.png",
            "floorStamp_5_00.png",
            "floorStamp_6_00.png",
            "floorStamp_7_00.png",
            "floorStamp_8_00.png",
            "floorStamp_9_00.png",
            "alcovePillar_00.png",
            "archeSmall_00.png",
            "prisonBigAlcove_00.png",
            "woodenPlatform_left_00.png",
            "woodenPlatform_0_00.png",
            "woodenPlatform_1_00.png",
            "woodenPlatform_2_00.png",
            "woodenPlatform_3_00.png",
            "woodenPlatform_4_00.png",
            "woodenPlatform_right_00.png",
            "exitDoor_00.png",
            "prionSmallWindow_00.png",
            "alccoveParalaxeA_00.png",
            "alccoveParalaxeB_00.png",
            "bannerBig_00.png",
            "stoneStamp_0_00.png"
        };

        [MenuItem("Tools/DeadCells/Visual/Generate Core Prison Normal Maps")]
        public static void GenerateCoreArchitectureNormals()
        {
            Directory.CreateDirectory(NormalDirectory);

            int writtenCount = 0;
            int normalReimportCount = 0;
            int spriteBindCount = 0;

            for (int i = 0; i < CoreArchitectureSpriteNames.Length; i++)
            {
                string fileName = CoreArchitectureSpriteNames[i];
                string spritePath = $"{SpriteDirectory}/{fileName}";
                string stem = Path.GetFileNameWithoutExtension(fileName);
                string normalPath = $"{NormalDirectory}/{stem}_n.png";

                if (!File.Exists(spritePath))
                    throw new FileNotFoundException($"Missing core Prison sprite '{spritePath}'.", spritePath);

                byte[] pngBytes = GenerateSobelNormalPng(spritePath);
                if (WriteBytesIfChanged(normalPath, pngBytes))
                    writtenCount++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            for (int i = 0; i < CoreArchitectureSpriteNames.Length; i++)
            {
                string fileName = CoreArchitectureSpriteNames[i];
                string spritePath = $"{SpriteDirectory}/{fileName}";
                string stem = Path.GetFileNameWithoutExtension(fileName);
                string normalPath = $"{NormalDirectory}/{stem}_n.png";

                if (ConfigureNormalImporter(normalPath))
                    normalReimportCount++;

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normalTex == null)
                    throw new InvalidOperationException($"Failed to load imported normal map '{normalPath}'.");

                if (BindSecondaryNormalTexture(spritePath, normalTex))
                    spriteBindCount++;
            }

            Debug.Log(
                $"[PrisonNormalMapGenerator] Processed {CoreArchitectureSpriteNames.Length}/{CoreArchitectureSpriteNames.Length} sprites " +
                $"(pngWritten: {writtenCount}, normalReimported: {normalReimportCount}, secondaryBound: {spriteBindCount}).");
        }

        public static void GenerateAndBindNormalForSprite(string spritePath)
        {
            if (string.IsNullOrWhiteSpace(spritePath) || !File.Exists(spritePath))
                throw new FileNotFoundException($"Missing source Prison sprite '{spritePath}'.", spritePath);

            Directory.CreateDirectory(NormalDirectory);

            string stem = Path.GetFileNameWithoutExtension(spritePath);
            string normalPath = $"{NormalDirectory}/{stem}_n.png";

            byte[] pngBytes = GenerateSobelNormalPng(spritePath);
            bool written = WriteBytesIfChanged(normalPath, pngBytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            bool normalReimported = ConfigureNormalImporter(normalPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex == null)
                throw new InvalidOperationException($"Failed to load imported normal map '{normalPath}'.");

            bool bound = BindSecondaryNormalTexture(spritePath, normalTex);

            Debug.Log(
                $"[PrisonNormalMapGenerator] Processed '{spritePath}' -> '{normalPath}' " +
                $"(pngWritten: {written}, normalReimported: {normalReimported}, secondaryBound: {bound}).");
        }

        private static byte[] GenerateSobelNormalPng(string spritePath)
        {
            byte[] srcBytes = File.ReadAllBytes(spritePath);
            Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            Texture2D outTex = null;

            try
            {
                if (!ImageConversion.LoadImage(srcTex, srcBytes, markNonReadable: false))
                    throw new InvalidOperationException($"Failed to decode PNG '{spritePath}'.");

                int width = srcTex.width;
                int height = srcTex.height;
                Color[] srcPixels = srcTex.GetPixels();
                float[] heights = new float[srcPixels.Length];

                for (int i = 0; i < srcPixels.Length; i++)
                {
                    Color c = srcPixels[i];
                    heights[i] = (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) * c.a;
                }

                Color[] normalPixels = new Color[srcPixels.Length];
                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int idx = rowOffset + x;
                        if (srcPixels[idx].a < AlphaCutoff)
                        {
                            normalPixels[idx] = new Color(0.5f, 0.5f, 1.0f, 1.0f);
                            continue;
                        }

                        float tl = SampleHeight(heights, width, height, x - 1, y + 1);
                        float tc = SampleHeight(heights, width, height, x, y + 1);
                        float tr = SampleHeight(heights, width, height, x + 1, y + 1);
                        float ml = SampleHeight(heights, width, height, x - 1, y);
                        float mr = SampleHeight(heights, width, height, x + 1, y);
                        float bl = SampleHeight(heights, width, height, x - 1, y - 1);
                        float bc = SampleHeight(heights, width, height, x, y - 1);
                        float br = SampleHeight(heights, width, height, x + 1, y - 1);

                        float dx = (tr + 2f * mr + br) - (tl + 2f * ml + bl);
                        float dy = (tl + 2f * tc + tr) - (bl + 2f * bc + br);

                        Vector3 n = new Vector3(-dx * SobelStrength, -dy * SobelStrength, 1f).normalized;
                        normalPixels[idx] = new Color(
                            n.x * 0.5f + 0.5f,
                            n.y * 0.5f + 0.5f,
                            n.z * 0.5f + 0.5f,
                            1f);
                    }
                }

                outTex = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: true);
                outTex.SetPixels(normalPixels);
                outTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                return ImageConversion.EncodeToPNG(outTex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(srcTex);
                if (outTex != null)
                    UnityEngine.Object.DestroyImmediate(outTex);
            }
        }

        private static float SampleHeight(float[] heights, int width, int height, int x, int y)
        {
            int cx = Mathf.Clamp(x, 0, width - 1);
            int cy = Mathf.Clamp(y, 0, height - 1);
            return heights[cy * width + cx];
        }

        private static bool WriteBytesIfChanged(string path, byte[] bytes)
        {
            if (File.Exists(path))
            {
                byte[] existing = File.ReadAllBytes(path);
                if (BytesEqual(existing, bytes))
                    return false;
            }

            File.WriteAllBytes(path, bytes);
            return true;
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        private static bool ConfigureNormalImporter(string normalPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing TextureImporter for '{normalPath}'.");

            bool dirty =
                importer.textureType != TextureImporterType.NormalMap ||
                importer.convertToNormalmap ||
                importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.npotScale != TextureImporterNPOTScale.None;

            if (!dirty)
                return false;

            importer.textureType = TextureImporterType.NormalMap;
            importer.convertToNormalmap = false;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return true;
        }

        private static bool BindSecondaryNormalTexture(string spritePath, Texture2D normalTex)
        {
            TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing TextureImporter for source sprite '{spritePath}'.");

            SecondarySpriteTexture[] existing = importer.secondarySpriteTextures ?? Array.Empty<SecondarySpriteTexture>();
            List<SecondarySpriteTexture> updated = new List<SecondarySpriteTexture>(existing.Length + 1);
            bool alreadyExact = false;

            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].name == NormalSecondaryTextureName)
                {
                    if (existing[i].texture == normalTex)
                        alreadyExact = true;
                    continue;
                }

                updated.Add(existing[i]);
            }

            if (alreadyExact)
            {
                // Still collapse duplicate _NormalMap entries if any existed
                int count = 0;
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i].name == NormalSecondaryTextureName)
                        count++;
                }

                if (count == 1)
                    return false;
            }

            updated.Add(new SecondarySpriteTexture
            {
                name = NormalSecondaryTextureName,
                texture = normalTex
            });

            importer.secondarySpriteTextures = updated.ToArray();
            importer.SaveAndReimport();
            return true;
        }
    }
}
