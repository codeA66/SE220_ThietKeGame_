using System.IO;
using DeadCells.Tools.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public sealed class VisualShaderAndNormalTests
    {
        private const string PaletteSwapLitShaderName = "DeadCells/PaletteSwapLit2D";
        private const string MobFleshSwapLitShaderName = "DeadCells/MobFleshSwapLit2D";

        private const string PaletteSwapLitShaderPath = "Assets/_Project/Art/Shaders/PaletteSwapLit2D.shader";
        private const string MobFleshSwapLitShaderPath = "Assets/_Project/Art/Shaders/MobFleshSwapLit2D.shader";

        private const string PlayerMaterialPath = "Assets/_Project/Art/Characters/Player/Player_PaletteSwap.mat";

        private static readonly string[] MobMaterialPaths =
        {
            "Assets/_Project/Features/Enemies/Materials/Mob_zombie.mat",
            "Assets/_Project/Features/Enemies/Materials/Mob_zombieShield.mat",
            "Assets/_Project/Features/Enemies/Materials/Mob_deadArcher.mat",
            "Assets/_Project/Features/Enemies/Materials/Mob_grenadier.mat",
            "Assets/_Project/Features/Enemies/Materials/Mob_Rat.mat"
        };

        [Test]
        public void CharacterLitHybridShaders_CompileCleanlyAndBindPreservedProperties()
        {
            Shader paletteLit = Shader.Find(PaletteSwapLitShaderName);
            Assert.IsNotNull(paletteLit, $"Shader '{PaletteSwapLitShaderName}' could not be found.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(paletteLit), $"Shader '{PaletteSwapLitShaderName}' has compile errors.");

            Shader mobLit = Shader.Find(MobFleshSwapLitShaderName);
            Assert.IsNotNull(mobLit, $"Shader '{MobFleshSwapLitShaderName}' could not be found.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(mobLit), $"Shader '{MobFleshSwapLitShaderName}' has compile errors.");

            AssertShaderHasRequiredPasses(PaletteSwapLitShaderPath);
            AssertShaderHasRequiredPasses(MobFleshSwapLitShaderPath);

            Assert.AreNotEqual(-1, paletteLit.FindPropertyIndex("_SelfLitWeight"));
            Assert.AreNotEqual(-1, paletteLit.FindPropertyIndex("_Flash"));
            Assert.AreNotEqual(-1, paletteLit.FindPropertyIndex("_MaskTex"));
            Assert.AreNotEqual(-1, paletteLit.FindPropertyIndex("_NormalMap"));

            Assert.AreNotEqual(-1, mobLit.FindPropertyIndex("_SelfLitWeight"));
            Assert.AreNotEqual(-1, mobLit.FindPropertyIndex("_Flash"));
            Assert.AreNotEqual(-1, mobLit.FindPropertyIndex("_FlashColor"));
            Assert.AreNotEqual(-1, mobLit.FindPropertyIndex("_MaskTex"));
            Assert.AreNotEqual(-1, mobLit.FindPropertyIndex("_NormalMap"));

            Material playerMat = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
            Assert.IsNotNull(playerMat, $"Missing player material at '{PlayerMaterialPath}'.");
            Assert.IsNotNull(playerMat.shader);
            Assert.AreEqual(PaletteSwapLitShaderName, playerMat.shader.name);
            Assert.IsNotNull(playerMat.GetTexture("_PaletteTex"), "Player material must preserve _PaletteTex.");
            Assert.That(playerMat.GetFloat("_SelfLitWeight"), Is.InRange(0.35f, 0.55f),
                "Player material _SelfLitWeight must remain within [0.35, 0.55].");
            Assert.GreaterOrEqual(playerMat.GetFloat("_PaletteRows"), 1f,
                "Player material must preserve _PaletteRows >= 1.");

            foreach (string matPath in MobMaterialPaths)
            {
                Material mobMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Assert.IsNotNull(mobMat, $"Missing enemy material at '{matPath}'.");
                Assert.IsNotNull(mobMat.shader, $"Material '{matPath}' has null shader.");
                Assert.AreEqual(MobFleshSwapLitShaderName, mobMat.shader.name,
                    $"Material '{matPath}' must use '{MobFleshSwapLitShaderName}'.");
                Assert.That(mobMat.GetFloat("_SelfLitWeight"), Is.InRange(0.35f, 0.55f),
                    $"Material '{matPath}' _SelfLitWeight must remain within [0.35, 0.55].");
                Assert.AreNotEqual(new Color(1f, 0f, 1f, 1f), mobMat.GetColor("_FleshColor"),
                    $"Material '{matPath}' must preserve its custom _FleshColor rather than reverting to default magenta.");
            }
        }

        [Test]
        public void EnvironmentShadersAndMaterials_CompileCleanlyWithValidHdrAndParameters()
        {
            var expectedShaders = new (string shaderName, string assetPath, string[] requiredProps, bool requiresNormalsPass)[]
            {
                (
                    "DeadCells/ParallaxGradientMap2D",
                    "Assets/_Project/Art/Shaders/ParallaxGradientMap2D.shader",
                    new[] { "_MainTex", "_ColorDeep", "_ColorMidDark", "_ColorMidLight", "_ColorHighlight", "_ScrollSpeed", "_LitBlend", "_MaskTex", "_NormalMap" },
                    false
                ),
                (
                    "DeadCells/EmissiveHdr2D",
                    "Assets/_Project/Art/Shaders/EmissiveHdr2D.shader",
                    new[] { "_MainTex", "_EmissionColor", "_PulseSpeed", "_PulseAmount", "_DstBlend" },
                    false
                ),
                (
                    "DeadCells/AdditiveGodRay2D",
                    "Assets/_Project/Art/Shaders/AdditiveGodRay2D.shader",
                    new[] { "_MainTex", "_Color", "_ScrollSpeed", "_ShimmerStrength" },
                    false
                ),
                (
                    "DeadCells/VertexSway2D",
                    "Assets/_Project/Art/Shaders/VertexSway2D.shader",
                    new[] { "_MainTex", "_MaskTex", "_NormalMap", "_Color", "_SwayAmplitude", "_SwayFrequency", "_SwayPhaseScale", "_UseWorldYPin", "_PinTopWorldY", "_SwaySpanY" },
                    true
                ),
                (
                    "DeadCells/GroundMist2D",
                    "Assets/_Project/Art/Shaders/GroundMist2D.shader",
                    new[] { "_MainTex", "_MistColor", "_ScrollSpeed", "_NoiseScale" },
                    false
                )
            };

            foreach (var spec in expectedShaders)
            {
                Shader shader = Shader.Find(spec.shaderName);
                Assert.IsNotNull(shader, $"Environment shader '{spec.shaderName}' could not be found.");
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), $"Environment shader '{spec.shaderName}' has compile errors.");

                Assert.IsTrue(File.Exists(spec.assetPath), $"Shader file does not exist at '{spec.assetPath}'.");
                if (spec.requiresNormalsPass)
                {
                    AssertShaderHasRequiredPasses(spec.assetPath);
                }
                else
                {
                    string source = File.ReadAllText(spec.assetPath);
                    StringAssert.Contains("\"LightMode\" = \"Universal2D\"", source,
                        $"Shader '{spec.assetPath}' must define a Universal2D pass.");
                }

                foreach (string prop in spec.requiredProps)
                {
                    Assert.AreNotEqual(-1, shader.FindPropertyIndex(prop),
                        $"Shader '{spec.shaderName}' is missing expected property '{prop}'.");
                }
            }

            const string envMatDir = "Assets/_Project/Art/Materials/Environment";
            var expectedMaterials = new (string fileName, string shaderName)[]
            {
                ("Mat_ParallaxFar_Prison.mat", "DeadCells/ParallaxGradientMap2D"),
                ("Mat_ParallaxMid_Prison.mat", "DeadCells/ParallaxGradientMap2D"),
                ("Mat_GodRay_Additive.mat", "DeadCells/AdditiveGodRay2D"),
                ("Mat_Banner_Sway.mat", "DeadCells/VertexSway2D"),
                ("Mat_Vine_Sway.mat", "DeadCells/VertexSway2D"),
                ("Mat_Emissive_Flame.mat", "DeadCells/EmissiveHdr2D"),
                ("Mat_Emissive_Mushroom.mat", "DeadCells/EmissiveHdr2D"),
                ("Mat_Emissive_Scroll.mat", "DeadCells/EmissiveHdr2D"),
                ("Mat_Emissive_Exit.mat", "DeadCells/EmissiveHdr2D"),
                ("Mat_GroundMist_Prison.mat", "DeadCells/GroundMist2D"),
                ("Mat_RoomBanner_Sway.mat", "DeadCells/VertexSway2D"),
                ("Mat_Emissive_MushroomHalo.mat", "DeadCells/EmissiveHdr2D")
            };

            foreach (var entry in expectedMaterials)
            {
                string matPath = $"{envMatDir}/{entry.fileName}";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Assert.IsNotNull(mat, $"Missing environment material at '{matPath}'.");
                Assert.IsNotNull(mat.shader, $"Environment material '{matPath}' has null shader.");
                Assert.AreEqual(entry.shaderName, mat.shader.name,
                    $"Environment material '{matPath}' must use shader '{entry.shaderName}'.");
            }

            Material parallaxFar = AssetDatabase.LoadAssetAtPath<Material>($"{envMatDir}/Mat_ParallaxFar_Prison.mat");
            Assert.AreEqual(0.0f, parallaxFar.GetFloat("_LitBlend"), 1e-4f);

            Material parallaxMid = AssetDatabase.LoadAssetAtPath<Material>($"{envMatDir}/Mat_ParallaxMid_Prison.mat");
            Assert.AreEqual(0.25f, parallaxMid.GetFloat("_LitBlend"), 1e-4f);

            Material godRay = AssetDatabase.LoadAssetAtPath<Material>($"{envMatDir}/Mat_GodRay_Additive.mat");
            Assert.AreEqual(0.14f, godRay.GetFloat("_ScrollSpeed"), 1e-4f);
            Assert.AreEqual(0.20f, godRay.GetFloat("_ShimmerStrength"), 1e-4f);

            Material bannerSway = AssetDatabase.LoadAssetAtPath<Material>($"{envMatDir}/Mat_Banner_Sway.mat");
            Assert.AreEqual(0.09f, bannerSway.GetFloat("_SwayAmplitude"), 1e-4f);
            Assert.AreEqual(1.6f, bannerSway.GetFloat("_SwayFrequency"), 1e-4f);
            Assert.AreEqual(0.7f, bannerSway.GetFloat("_SwayPhaseScale"), 1e-4f);

            Material vineSway = AssetDatabase.LoadAssetAtPath<Material>($"{envMatDir}/Mat_Vine_Sway.mat");
            Assert.AreEqual(0.12f, vineSway.GetFloat("_SwayAmplitude"), 1e-4f);
            Assert.AreEqual(1.2f, vineSway.GetFloat("_SwayFrequency"), 1e-4f);
            Assert.AreEqual(0.5f, vineSway.GetFloat("_SwayPhaseScale"), 1e-4f);

            Material groundMist = AssetDatabase.LoadAssetAtPath<Material>($"{envMatDir}/Mat_GroundMist_Prison.mat");
            Assert.AreEqual(0.04f, groundMist.GetFloat("_ScrollSpeed"), 1e-4f);
            Assert.AreEqual(3.5f, groundMist.GetFloat("_NoiseScale"), 1e-4f);
            Assert.GreaterOrEqual(groundMist.GetColor("_MistColor").a, 0.30f,
                "Mat_GroundMist_Prison _MistColor alpha must be >= 0.30 so ground mist reads on GPU.");

            Material roomBannerSway = AssetDatabase.LoadAssetAtPath<Material>($"{envMatDir}/Mat_RoomBanner_Sway.mat");
            Assert.AreEqual(0.09f, roomBannerSway.GetFloat("_SwayAmplitude"), 1e-4f);
            Assert.AreEqual(1.6f, roomBannerSway.GetFloat("_SwayFrequency"), 1e-4f);
            Assert.AreEqual(0.7f, roomBannerSway.GetFloat("_SwayPhaseScale"), 1e-4f);
            Assert.AreEqual(1.0f, roomBannerSway.GetFloat("_UseWorldYPin"), 1e-4f);
            Assert.AreEqual(7.6f, roomBannerSway.GetFloat("_PinTopWorldY"), 1e-4f);
            Assert.AreEqual(4.0f, roomBannerSway.GetFloat("_SwaySpanY"), 1e-4f);

            var emissiveMaterials = new (string fileName, float pulseSpeed, float pulseAmount)[]
            {
                ("Mat_Emissive_Flame.mat", 3.5f, 0.12f),
                ("Mat_Emissive_Mushroom.mat", 1.4f, 0.15f),
                ("Mat_Emissive_Scroll.mat", 2.2f, 0.18f),
                ("Mat_Emissive_Exit.mat", 1.2f, 0.10f)
            };

            foreach (var emissive in emissiveMaterials)
            {
                string matPath = $"{envMatDir}/{emissive.fileName}";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Color emission = mat.GetColor("_EmissionColor");
                float maxChannel = Mathf.Max(emission.r, Mathf.Max(emission.g, emission.b));
                Assert.That(maxChannel, Is.InRange(1.5f, 3.0f),
                    $"Material '{matPath}' HDR _EmissionColor max linear channel ({maxChannel}) must be in [1.5, 3.0] above Bloom threshold (1.05).");
                Assert.AreEqual(emissive.pulseSpeed, mat.GetFloat("_PulseSpeed"), 1e-4f,
                    $"Material '{matPath}' _PulseSpeed mismatch.");
                Assert.AreEqual(emissive.pulseAmount, mat.GetFloat("_PulseAmount"), 1e-4f,
                    $"Material '{matPath}' _PulseAmount mismatch.");
                Assert.AreEqual(10f, mat.GetFloat("_DstBlend"), 1e-4f,
                    $"Material '{matPath}' must preserve backwards-compatible _DstBlend == 10 (OneMinusSrcAlpha).");
            }

            Material haloMat = AssetDatabase.LoadAssetAtPath<Material>($"{envMatDir}/Mat_Emissive_MushroomHalo.mat");
            Assert.IsNotNull(haloMat, "Missing Mat_Emissive_MushroomHalo.mat.");
            Assert.AreEqual(1f, haloMat.GetFloat("_DstBlend"), 1e-4f,
                "Mat_Emissive_MushroomHalo must use additive _DstBlend == 1 (BlendMode.One).");
            Color haloEm = haloMat.GetColor("_EmissionColor");
            float haloMax = Mathf.Max(haloEm.r, Mathf.Max(haloEm.g, haloEm.b));
            Assert.That(haloMax, Is.InRange(0.5f, 1.0f),
                "Mat_Emissive_MushroomHalo must stay strictly below Bloom threshold (1.05) to avoid opaque sticker orbs.");
            Assert.AreEqual(1.4f, haloMat.GetFloat("_PulseSpeed"), 1e-4f);
            Assert.AreEqual(0.15f, haloMat.GetFloat("_PulseAmount"), 1e-4f);
        }

        private static void AssertShaderHasRequiredPasses(string assetPath)
        {
            Assert.IsTrue(File.Exists(assetPath), $"Shader file does not exist at '{assetPath}'.");
            string source = File.ReadAllText(assetPath);
            StringAssert.Contains("\"LightMode\" = \"Universal2D\"", source,
                $"Shader '{assetPath}' must define a Universal2D pass.");
            StringAssert.Contains("\"LightMode\" = \"NormalsRendering\"", source,
                $"Shader '{assetPath}' must define a NormalsRendering pass.");
        }

        [Test]
        public void PrisonCoreArchitectureSprites_HaveNormalMapsGeneratedAndBoundAsSecondaryTextures()
        {
            const string spriteDir = "Assets/_Project/Art/Environment/Prison/Sprites";
            const string normalDir = "Assets/_Project/Art/Environment/Prison/Normals";

            string[] spriteNames = PrisonNormalMapGenerator.CoreArchitectureSpriteNames;
            Assert.AreEqual(36, spriteNames.Length, "Expected exactly 36 core Prison architecture sprites.");

            for (int i = 0; i < spriteNames.Length; i++)
            {
                string fileName = spriteNames[i];
                string stem = Path.GetFileNameWithoutExtension(fileName);
                string spritePath = $"{spriteDir}/{fileName}";
                string normalPath = $"{normalDir}/{stem}_n.png";

                Assert.IsTrue(File.Exists(spritePath), $"Source sprite does not exist at '{spritePath}'.");
                Assert.IsTrue(File.Exists(normalPath), $"Generated normal map does not exist at '{normalPath}'.");

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                Assert.IsNotNull(normalTex, $"Failed to load normal map Texture2D at '{normalPath}'.");

                TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                Assert.IsNotNull(normalImporter, $"Missing TextureImporter for normal map '{normalPath}'.");
                Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType,
                    $"Normal map '{normalPath}' must use TextureImporterType.NormalMap.");
                Assert.AreEqual(FilterMode.Point, normalImporter.filterMode,
                    $"Normal map '{normalPath}' must use FilterMode.Point.");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, normalImporter.textureCompression,
                    $"Normal map '{normalPath}' must be Uncompressed.");
                Assert.IsFalse(normalImporter.mipmapEnabled,
                    $"Normal map '{normalPath}' must have mipmapEnabled == false.");

                TextureImporter spriteImporter = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                Assert.IsNotNull(spriteImporter, $"Missing TextureImporter for source sprite '{spritePath}'.");

                SecondarySpriteTexture[] secondaryTextures = spriteImporter.secondarySpriteTextures;
                Assert.IsNotNull(secondaryTextures, $"secondarySpriteTextures is null on '{spritePath}'.");

                Texture2D boundNormal = null;
                for (int j = 0; j < secondaryTextures.Length; j++)
                {
                    if (secondaryTextures[j].name == "_NormalMap")
                    {
                        boundNormal = secondaryTextures[j].texture;
                        break;
                    }
                }

                Assert.AreSame(normalTex, boundNormal,
                    $"Source sprite '{spritePath}' must bind '{normalPath}' under secondary texture '_NormalMap'.");
            }
        }

        [Test]
        public void PrisonGeneratedProps_Are24PpuPointUncompressedWithBoundNormalMaps()
        {
            var expectedProps = new (string path, int width, int height, string normalPath)[]
            {
                (PrisonPropSpriteGenerator.WallLanternAssetPath, 20, 30,
                    "Assets/_Project/Art/Environment/Prison/Normals/prisonWallLantern_00_n.png"),
                (PrisonPropSpriteGenerator.ArchSillAssetPath, 24, 8,
                    "Assets/_Project/Art/Environment/Prison/Normals/prisonArchSill_00_n.png"),
                (PrisonPropSpriteGenerator.ArchJambAssetPath, 8, 24,
                    "Assets/_Project/Art/Environment/Prison/Normals/prisonArchJamb_00_n.png"),
            };

            Assert.AreEqual(0.125f, PrisonPropSpriteGenerator.WallLanternFlameOffsetLocalY, 1e-4f);

            foreach (var spec in expectedProps)
            {
                Assert.IsTrue(File.Exists(spec.path), $"Missing generated prop sprite '{spec.path}'.");
                Assert.IsTrue(File.Exists(spec.normalPath), $"Missing generated prop normal '{spec.normalPath}'.");

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.path);
                Assert.IsNotNull(sprite, $"Failed to load Sprite '{spec.path}'.");
                Assert.AreEqual(24f, sprite.pixelsPerUnit, 1e-4f, $"'{spec.path}' must use 24 PPU.");
                Assert.AreEqual(spec.width, Mathf.RoundToInt(sprite.rect.width), $"'{spec.path}' width mismatch.");
                Assert.AreEqual(spec.height, Mathf.RoundToInt(sprite.rect.height), $"'{spec.path}' height mismatch.");

                TextureImporter importer = AssetImporter.GetAtPath(spec.path) as TextureImporter;
                Assert.IsNotNull(importer);
                Assert.AreEqual(FilterMode.Point, importer.filterMode);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                Assert.IsFalse(importer.mipmapEnabled);

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.normalPath);
                Assert.IsNotNull(normalTex);
                TextureImporter normalImporter = AssetImporter.GetAtPath(spec.normalPath) as TextureImporter;
                Assert.IsNotNull(normalImporter);
                Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType);
                Assert.AreEqual(FilterMode.Point, normalImporter.filterMode);

                SecondarySpriteTexture[] secondary = importer.secondarySpriteTextures;
                Assert.IsTrue(
                    secondary != null && System.Array.Exists(secondary, s => s.name == "_NormalMap" && s.texture == normalTex),
                    $"'{spec.path}' must bind '{spec.normalPath}' as secondary texture '_NormalMap'.");
            }
        }

        [Test]
        public void ParallaxFarBackdrop_IsSeamlessGrayscalePixelArtWithLowInternalContrast()
        {
            const string assetPath = "Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png";
            Assert.IsTrue(File.Exists(assetPath), $"Expected ParallaxFar grayscale backdrop PNG at '{assetPath}'.");

            Assert.IsFalse(Path.IsPathRooted(ParallaxTextureProcessor.DefaultRawInputPath),
                "ParallaxTextureProcessor.DefaultRawInputPath must be a project-relative path (no hardcoded machine path).");
            StringAssert.DoesNotContain("Users/", ParallaxTextureProcessor.DefaultRawInputPath,
                "ParallaxTextureProcessor.DefaultRawInputPath must not contain personal user directory paths.");

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Assert.IsNotNull(sprite, $"Failed to load Sprite at '{assetPath}'.");
            Assert.AreEqual(24f, sprite.pixelsPerUnit, 1e-4f, "ParallaxFar sprite must use 24 PPU.");
            Assert.AreEqual(768f, sprite.rect.width, 1e-4f, "ParallaxFar sprite width must be 768.");
            Assert.AreEqual(512f, sprite.rect.height, 1e-4f, "ParallaxFar sprite height must be 512 (square 1:1 pixels).");

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.IsNotNull(importer, $"Missing TextureImporter for '{assetPath}'.");
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.AreEqual(24f, importer.spritePixelsPerUnit, 1e-4f);
            Assert.AreEqual(FilterMode.Point, importer.filterMode);
            Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapModeU);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeV);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);

            byte[] rawBytes = File.ReadAllBytes(assetPath);
            Texture2D rawTex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: true);
            try
            {
                Assert.IsTrue(ImageConversion.LoadImage(rawTex, rawBytes, markNonReadable: false),
                    $"Failed to decode PNG bytes from '{assetPath}'.");
                Assert.AreEqual(768, rawTex.width);
                Assert.AreEqual(512, rawTex.height);

                Color32[] pixels = rawTex.GetPixels32();
                int width = rawTex.width;
                int height = rawTex.height;
                int count = pixels.Length;

                float maxRgDelta = 0f;
                float maxGbDelta = 0f;
                float minLum = float.MaxValue;
                float maxLum = float.MinValue;
                double sumLum = 0.0;
                double sumSqLum = 0.0;

                for (int i = 0; i < count; i++)
                {
                    Color32 p = pixels[i];
                    float r = p.r / 255f;
                    float g = p.g / 255f;
                    float b = p.b / 255f;

                    float rg = Mathf.Abs(r - g);
                    float gb = Mathf.Abs(g - b);
                    if (rg > maxRgDelta) maxRgDelta = rg;
                    if (gb > maxGbDelta) maxGbDelta = gb;

                    float lum = 0.299f * r + 0.587f * g + 0.114f * b;
                    if (lum < minLum) minLum = lum;
                    if (lum > maxLum) maxLum = lum;
                    sumLum += lum;
                    sumSqLum += lum * lum;
                }

                double meanLum = sumLum / count;
                double variance = System.Math.Max(0.0, (sumSqLum / count) - (meanLum * meanLum));
                float stdDevLum = (float)System.Math.Sqrt(variance);

                double seamDiffSum = 0.0;
                for (int y = 0; y < height; y++)
                {
                    int rowStart = y * width;
                    Color32 left = pixels[rowStart];
                    Color32 right = pixels[rowStart + width - 1];
                    float lumLeft = (0.299f * left.r + 0.587f * left.g + 0.114f * left.b) / 255f;
                    float lumRight = (0.299f * right.r + 0.587f * right.g + 0.114f * right.b) / 255f;
                    seamDiffSum += Mathf.Abs(lumLeft - lumRight);
                }
                float meanSeamDiff = (float)(seamDiffSum / height);

                Debug.Log(
                    $"[ParallaxFarStats] min={minLum:F4}, max={maxLum:F4}, mean={meanLum:F4}, stddev={stdDevLum:F4}, " +
                    $"seamDiff={meanSeamDiff:F4}, maxRG={maxRgDelta:F5}, maxGB={maxGbDelta:F5}");

                Assert.LessOrEqual(maxRgDelta, 2f / 255f, "ParallaxFar backdrop must be pure grayscale (|R - G| <= 2/255).");
                Assert.LessOrEqual(maxGbDelta, 2f / 255f, "ParallaxFar backdrop must be pure grayscale (|G - B| <= 2/255).");
                Assert.LessOrEqual(stdDevLum, 0.24f, "ParallaxFar backdrop internal contrast (stddev) must be <= 0.24.");
                Assert.LessOrEqual(maxLum, 0.82f, "ParallaxFar backdrop max luminance must be <= 0.82.");
                Assert.GreaterOrEqual(minLum, 0.06f, "ParallaxFar backdrop min luminance must be >= 0.06.");
                Assert.LessOrEqual(meanSeamDiff, 0.06f, "ParallaxFar backdrop horizontal seam continuity must be <= 0.06.");
            }
            finally
            {
                Object.DestroyImmediate(rawTex);
            }
        }

        [Test]
        public void PrisonChromakeyProps_HaveZeroRawMagentaPixels()
        {
            PrisonPropSpriteGenerator.EnsureAll();

            string[] chromakeyAuditPaths =
            {
                "Assets/_Project/Art/Environment/Prison/Sprites/exitDoorSewerShort_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/exitDoorPrisonCourtyard_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/switchCopper_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/tpStoneUp_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/tpStoneDown_00.png",
                "Assets/_Project/Art/Environment/Prison/Sprites/alchemyBibli_00.png",
            };

            foreach (string assetPath in chromakeyAuditPaths)
            {
                Assert.IsTrue(File.Exists(assetPath), $"Expected sprite PNG at '{assetPath}'.");
                byte[] rawBytes = File.ReadAllBytes(assetPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: true);
                try
                {
                    Assert.IsTrue(ImageConversion.LoadImage(tex, rawBytes, markNonReadable: false),
                        $"Failed to decode PNG at '{assetPath}'.");
                    Color32[] pixels = tex.GetPixels32();
                    int magentaCount = 0;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color32 c = pixels[i];
                        if (c.a > 16 && c.r >= 200 && c.b >= 190 && c.g <= 175 && (c.r - c.g) >= 45 && (c.b - c.g) >= 35)
                        {
                            magentaCount++;
                        }
                    }

                    Assert.AreEqual(0, magentaCount,
                        $"Sprite '{assetPath}' still contains {magentaCount} unbaked hot-magenta chromakey pixels.");
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                }
            }
        }
    }
}
