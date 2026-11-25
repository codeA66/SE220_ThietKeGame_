using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Tools.Editor
{
    public static class PrisonPropSpriteGenerator
    {
        public const string WallLanternAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonWallLantern_00.png";
        public const string ArchSillAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchSill_00.png";
        public const string ArchSill01AssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchSill_01.png";
        public const string ArchSill02AssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchSill_02.png";
        public const string ArchJambAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchJamb_00.png";
        public const string ArchJamb01AssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchJamb_01.png";
        public const string ArchJamb02AssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchJamb_02.png";
        public const string ArchSpandrelAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchSpandrel_00.png";
        public const string ArchMullionAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonArchMullion_00.png";
        public const string ShelfVial00AssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonShelfVial_00.png";
        public const string ShelfVial01AssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonShelfVial_01.png";
        public const string ShelfVial02AssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonShelfVial_02.png";
        public const string PrisonBellAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonBell_00.png";
        public const string SlabStrataAssetPath = "Assets/_Project/Art/Environment/Prison/Sprites/prisonSlabStrata_00.png";
        public const float WallLanternFlameOffsetLocalY = 0.125f;

        private const string SpriteDirectory = "Assets/_Project/Art/Environment/Prison/Sprites";
        private const string NormalDirectory = "Assets/_Project/Art/Environment/Prison/Normals";
        private const float PixelsPerUnit = 24f;

        [MenuItem("Tools/DeadCells/Visual/Generate 24-PPU Prison Props")]
        public static void EnsureAll()
        {
            Directory.CreateDirectory(SpriteDirectory);

            var specs = new (string assetPath, byte[] pngBytes, TextureWrapMode wrapU, TextureWrapMode wrapV)[]
            {
                (WallLanternAssetPath,  BuildWallLanternPng(),        TextureWrapMode.Clamp,  TextureWrapMode.Clamp),
                (ArchSillAssetPath,     BuildArchMasonryTrimPng(0, vertical: false), TextureWrapMode.Repeat, TextureWrapMode.Clamp),
                (ArchSill01AssetPath,   BuildArchMasonryTrimPng(1, vertical: false), TextureWrapMode.Repeat, TextureWrapMode.Clamp),
                (ArchSill02AssetPath,   BuildArchMasonryTrimPng(2, vertical: false), TextureWrapMode.Repeat, TextureWrapMode.Clamp),
                (ArchJambAssetPath,     BuildArchMasonryTrimPng(0, vertical: true),  TextureWrapMode.Clamp,  TextureWrapMode.Repeat),
                (ArchJamb01AssetPath,   BuildArchMasonryTrimPng(1, vertical: true),  TextureWrapMode.Clamp,  TextureWrapMode.Repeat),
                (ArchJamb02AssetPath,   BuildArchMasonryTrimPng(2, vertical: true),  TextureWrapMode.Clamp,  TextureWrapMode.Repeat),
                (ArchSpandrelAssetPath, BuildArchSpandrelPng(),       TextureWrapMode.Clamp,  TextureWrapMode.Clamp),
                (ArchMullionAssetPath,  BuildArchMullionPng(),        TextureWrapMode.Clamp,  TextureWrapMode.Repeat),
                (ShelfVial00AssetPath,  BuildShelfVialPng(0),         TextureWrapMode.Clamp,  TextureWrapMode.Clamp),
                (ShelfVial01AssetPath,  BuildShelfVialPng(1),         TextureWrapMode.Clamp,  TextureWrapMode.Clamp),
                (ShelfVial02AssetPath,  BuildShelfVialPng(2),         TextureWrapMode.Clamp,  TextureWrapMode.Clamp),
                (PrisonBellAssetPath,   BuildPrisonBellPng(),         TextureWrapMode.Clamp,  TextureWrapMode.Clamp),
                (SlabStrataAssetPath,   BuildSlabStrataPng(),         TextureWrapMode.Clamp,  TextureWrapMode.Clamp),
            };

            int writtenCount = 0;
            for (int i = 0; i < specs.Length; i++)
            {
                if (WriteBytesIfChanged(specs[i].assetPath, specs[i].pngBytes))
                    writtenCount++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            int importedCount = 0;
            for (int i = 0; i < specs.Length; i++)
            {
                if (ConfigureSpriteImporter(specs[i].assetPath, specs[i].wrapU, specs[i].wrapV))
                    importedCount++;
            }

            for (int i = 0; i < specs.Length; i++)
                PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(specs[i].assetPath);

            SanitizeChromakeyPrisonSprites();

            Debug.Log(
                $"[PrisonPropSpriteGenerator] Verified {specs.Length} generated 24-PPU Prison props " +
                $"(written={writtenCount}, reimported={importedCount}).");
        }

        public static bool IsChromakeyMagenta(Color32 c)
        {
            return c.a > 16
                && c.r >= 200
                && c.b >= 190
                && c.g <= 175
                && (c.r - c.g) >= 45
                && (c.b - c.g) >= 35;
        }

        public static void SanitizeChromakeyPrisonSprites()
        {
            var targets = new (string fileName, Color32 replacementRgb)[]
            {
                ("exitDoorSewerShort_00.png",      new Color32(14, 20, 28, 255)),
                ("exitDoorPrisonCourtyard_00.png", new Color32(14, 20, 28, 255)),
                ("switchCopper_00.png",            new Color32(36, 204, 218, 255)),
                ("tpStoneUp_00.png",               new Color32(36, 204, 218, 255)),
                ("tpStoneDown_00.png",             new Color32(36, 204, 218, 255)),
                ("alchemyBibli_00.png",            new Color32(14, 20, 28, 255)),
            };

            foreach (var target in targets)
            {
                string spritePath = $"{SpriteDirectory}/{target.fileName}";
                if (!File.Exists(spritePath))
                {
                    continue;
                }

                byte[] rawBytes = File.ReadAllBytes(spritePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: true);
                try
                {
                    if (!ImageConversion.LoadImage(tex, rawBytes, markNonReadable: false))
                    {
                        continue;
                    }

                    Color32[] pixels = tex.GetPixels32();
                    int modified = 0;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color32 p = pixels[i];
                        if (IsChromakeyMagenta(p))
                        {
                            float detail = Mathf.Clamp01((255f - p.g) / 255f);
                            byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(target.replacementRgb.r * (0.75f + 0.25f * detail)), 0, 255);
                            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(target.replacementRgb.g * (0.75f + 0.25f * detail)), 0, 255);
                            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(target.replacementRgb.b * (0.75f + 0.25f * detail)), 0, 255);
                            pixels[i] = new Color32(r, g, b, p.a);
                            modified++;
                        }
                    }

                    if (modified > 0)
                    {
                        tex.SetPixels32(pixels);
                        tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                        File.WriteAllBytes(spritePath, ImageConversion.EncodeToPNG(tex));
                        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                        string stem = Path.GetFileNameWithoutExtension(target.fileName);
                        string normalPath = $"{NormalDirectory}/{stem}_n.png";
                        if (File.Exists(normalPath))
                        {
                            PrisonNormalMapGenerator.GenerateAndBindNormalForSprite(spritePath);
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(tex);
                }
            }
        }

        private static byte[] BuildWallLanternPng()
        {
            const int width = 20;
            const int height = 30;
            Color32[] pixels = new Color32[width * height];

            Color32 outline = Hex("#1B2128");
            Color32 ironDark = Hex("#28313A");
            Color32 ironFill = Hex("#36404B");
            Color32 ironMid = Hex("#4E5B69");
            Color32 ironHighlight = Hex("#566574");
            Color32 ironRim = Hex("#5E6D7C");
            Color32 boltHighlight = Hex("#7A8C9C");
            Color32 bronzeMullion = Hex("#3B281B");

            Color32 glassDeep = Hex("#9E3E12");
            Color32 glassMid = Hex("#E07E22");
            Color32 glassCore = Hex("#FFD86B");
            Color32 glassHot = Hex("#FFF0B8");

            // 1. Wall sconce backplate (x = 7..12, y = 24..29) bolted flush onto wall (zero dangling chain)
            FillRect(pixels, width, height, 7, 24, 12, 29, outline);
            FillRect(pixels, width, height, 8, 25, 11, 28, ironFill);
            FillRect(pixels, width, height, 8, 28, 11, 28, ironHighlight);
            SetPixel(pixels, width, height, 8, 27, boltHighlight);
            SetPixel(pixels, width, height, 11, 27, boltHighlight);
            SetPixel(pixels, width, height, 8, 26, ironDark);
            SetPixel(pixels, width, height, 11, 26, ironDark);

            // 2. Bracket neck & mounting collar (x = 8..11, y = 21..24)
            FillRect(pixels, width, height, 8, 21, 11, 24, outline);
            FillRect(pixels, width, height, 9, 21, 10, 24, ironFill);
            SetPixel(pixels, width, height, 9, 23, ironMid);
            SetPixel(pixels, width, height, 9, 22, ironMid);
            SetPixel(pixels, width, height, 10, 22, ironMid);
            SetPixel(pixels, width, height, 9, 21, ironRim);
            SetPixel(pixels, width, height, 10, 21, ironMid);

            // 3. Pitched iron lantern hood (y = 18..20, widening from x = 7..12 to x = 4..15)
            // y = 20: x = 7..12
            FillRect(pixels, width, height, 7, 20, 12, 20, outline);
            FillRect(pixels, width, height, 8, 20, 11, 20, ironRim);
            // y = 19: x = 5..14
            FillRect(pixels, width, height, 5, 19, 14, 19, outline);
            FillRect(pixels, width, height, 6, 19, 13, 19, ironMid);
            FillRect(pixels, width, height, 6, 19, 10, 19, ironRim);
            // y = 18: x = 4..15
            FillRect(pixels, width, height, 4, 18, 15, 18, outline);
            FillRect(pixels, width, height, 5, 18, 14, 18, ironFill);
            FillRect(pixels, width, height, 5, 18, 9, 18, ironMid);

            // 4. Glass lantern cage (x = 5..14, y = 6..17, centered at y = 12 => localY offset = +0.125u)
            for (int y = 6; y <= 17; y++)
            {
                SetPixel(pixels, width, height, 5, y, outline);
                SetPixel(pixels, width, height, 14, y, outline);

                for (int x = 6; x <= 13; x++)
                {
                    Color32 glassColor;
                    if (y == 6 || y == 17 || x == 6 || x == 13)
                    {
                        glassColor = glassDeep;
                    }
                    else if (x >= 8 && x <= 11 && y >= 10 && y <= 14)
                    {
                        glassColor = (x >= 9 && x <= 10 && y >= 11 && y <= 13) ? glassHot : glassCore;
                    }
                    else
                    {
                        glassColor = glassMid;
                    }

                    SetPixel(pixels, width, height, x, y, glassColor);
                }
            }

            // Subtle dark bronze center mullion only near top/bottom so flame reads cleanly
            SetPixel(pixels, width, height, 9, 17, bronzeMullion);
            SetPixel(pixels, width, height, 10, 17, bronzeMullion);
            SetPixel(pixels, width, height, 9, 16, bronzeMullion);
            SetPixel(pixels, width, height, 10, 16, bronzeMullion);
            SetPixel(pixels, width, height, 9, 6, bronzeMullion);
            SetPixel(pixels, width, height, 10, 6, bronzeMullion);
            SetPixel(pixels, width, height, 9, 7, bronzeMullion);
            SetPixel(pixels, width, height, 10, 7, bronzeMullion);

            // 5. Lower iron cradle & tapered finial (y = 1..5)
            // y = 5: x = 4..15
            FillRect(pixels, width, height, 4, 5, 15, 5, outline);
            FillRect(pixels, width, height, 5, 5, 14, 5, ironFill);
            FillRect(pixels, width, height, 5, 5, 11, 5, ironMid);
            // y = 4: x = 5..14
            FillRect(pixels, width, height, 5, 4, 14, 4, outline);
            FillRect(pixels, width, height, 6, 4, 13, 4, ironFill);
            SetPixel(pixels, width, height, 6, 4, ironMid);
            SetPixel(pixels, width, height, 7, 4, ironMid);
            // y = 3: x = 7..12
            FillRect(pixels, width, height, 7, 3, 12, 3, outline);
            FillRect(pixels, width, height, 8, 3, 11, 3, ironFill);
            SetPixel(pixels, width, height, 8, 3, ironMid);
            // y = 2: x = 8..11
            FillRect(pixels, width, height, 8, 2, 11, 2, outline);
            SetPixel(pixels, width, height, 9, 2, ironFill);
            SetPixel(pixels, width, height, 10, 2, ironDark);
            // y = 1: x = 9..10
            SetPixel(pixels, width, height, 9, 1, outline);
            SetPixel(pixels, width, height, 10, 1, outline);

            return EncodePixelsToPng(width, height, pixels);
        }

        private static byte[] BuildArchMasonryTrimPng(int variant, bool vertical)
        {
            const int longLength = 24;
            const int shortThickness = 8;
            int width = vertical ? shortThickness : longLength;
            int height = vertical ? longLength : shortThickness;
            Color32[] pixels = new Color32[width * height];

            Color32 bevelShadow = Hex("#1A2322");
            Color32 stoneDark = Hex("#26332F");
            Color32 stoneMid = Hex("#35453E");
            Color32 stoneLight = Hex("#485A50");
            Color32 mossTone = Hex("#3C5442");
            Color32 mossHighlight = Hex("#4E6954");
            Color32 outerSoftDark = WithAlpha(stoneDark, 175);
            Color32 outerSoftMid = WithAlpha(stoneMid, 195);

            for (int along = 0; along < longLength; along++)
            {
                bool isMortar = IsArchMortarLine(along, variant);
                int blockIdx = GetArchBlockIndex(along, variant);
                int outerDepth = GetWeatheredOuterDepth(along, variant, isMortar);

                // Inner edge (across = 7): subtle darkened reveal bevel facing sky opening
                bool skyChip = isMortar || (variant == 2 && along == (vertical ? 15 : 14));
                SetTrimPixel(pixels, width, height, along, 7, vertical, skyChip ? stoneDark : bevelShadow);

                // Sky-facing bevel (across = 6): muted chiseled stone lip
                if (isMortar)
                    SetTrimPixel(pixels, width, height, along, 6, vertical, bevelShadow);
                else if (variant == 1 && ((along >= 1 && along <= 3) || (along >= 11 && along <= 13) || (along >= 19 && along <= 21)))
                    SetTrimPixel(pixels, width, height, along, 6, vertical, mossHighlight);
                else if (((along + variant) & 3) == 1)
                    SetTrimPixel(pixels, width, height, along, 6, vertical, stoneLight);
                else
                    SetTrimPixel(pixels, width, height, along, 6, vertical, stoneMid);

                // Masonry body (across = 2..5)
                for (int across = 2; across <= 5; across++)
                {
                    if (across < outerDepth)
                        continue;

                    if (isMortar)
                    {
                        SetTrimPixel(pixels, width, height, along, across, vertical, across == 2 ? bevelShadow : stoneDark);
                        continue;
                    }

                    bool isCrack = variant == 2 &&
                        ((along == 8 && across == 5) || (along == 9 && across == 4) || (along == 10 && (across == 4 || across == 3)) ||
                         (along == 11 && across == 2) || (along == 16 && across == 5) || (along == 17 && (across == 4 || across == 3)));
                    if (isCrack)
                    {
                        SetTrimPixel(pixels, width, height, along, across, vertical, bevelShadow);
                        continue;
                    }

                    bool isMoss = variant == 1 &&
                        ((across >= 4 && ((along >= 2 && along <= 4) || (along >= 12 && along <= 14))) ||
                         (across == 5 && along >= 19 && along <= 22));
                    if (isMoss)
                    {
                        SetTrimPixel(pixels, width, height, along, across, vertical, ((along + across) & 1) == 0 ? mossTone : mossHighlight);
                        continue;
                    }

                    int h = HashCell(along + variant * 13, across + blockIdx * 7);
                    Color32 bodyColor = across == 5
                        ? ((h % 3 == 0) ? stoneLight : stoneMid)
                        : ((h % 4 == 0) ? stoneDark : stoneMid);
                    SetTrimPixel(pixels, width, height, along, across, vertical, bodyColor);
                }

                // Stepped wall-facing transition (across = 1)
                if (outerDepth <= 1)
                {
                    Color32 transition = outerDepth == 1 ? outerSoftMid : (((along + variant) & 1) == 0 ? stoneDark : stoneMid);
                    SetTrimPixel(pixels, width, height, along, 1, vertical, transition);
                }

                // Chipped interlocking outer edge (across = 0: alpha == 0 in notches; soft blend on protruding stone)
                if (outerDepth == 0)
                    SetTrimPixel(pixels, width, height, along, 0, vertical, outerSoftDark);
            }

            return EncodePixelsToPng(width, height, pixels);
        }

        private static void SetTrimPixel(Color32[] pixels, int width, int height, int along, int across, bool vertical, Color32 color)
        {
            if (vertical)
                SetPixel(pixels, width, height, across, along, color);
            else
                SetPixel(pixels, width, height, along, across, color);
        }

        private static byte[] BuildArchSpandrelPng()
        {
            const int size = 24;
            Color32[] pixels = new Color32[size * size];

            Color32 intradosShadow = Hex("#1A2322");
            Color32 stoneDark = Hex("#26332F");
            Color32 stoneMid = Hex("#35453E");
            Color32 stoneLight = Hex("#485A50");
            Color32 mossTone = Hex("#3C5442");

            const float centerX = 23.5f;
            const float centerY = -0.5f;
            const float radiusX = 16.5f;
            const float radiusY = 19.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Max(0f, (centerX - x) / radiusX);
                    float ny = Mathf.Max(0f, (y - centerY) / radiusY);
                    float metric = Mathf.Pow(nx, 1.85f) + Mathf.Pow(ny, 1.85f);

                    // Curved Gothic arch cutout void remains 100% transparent
                    if (metric < 1.0f)
                        continue;

                    // Subtle organic stone notches strictly outside top-left anchor block (keeps x<=4 && y>=19 solid)
                    if (x == 0 && y < 18 && (y % 5 == 0))
                    {
                        SetPixel(pixels, size, size, x, y, WithAlpha(stoneDark, 175));
                        continue;
                    }
                    if (y == size - 1 && x > 5 && (x % 6 == 0))
                    {
                        SetPixel(pixels, size, size, x, y, WithAlpha(stoneDark, 175));
                        continue;
                    }

                    if (metric < 1.15f)
                    {
                        SetPixel(pixels, size, size, x, y, intradosShadow);
                        continue;
                    }

                    if (metric < 1.46f)
                    {
                        float angle = Mathf.Atan2(y - centerY, centerX - x);
                        bool radialMortar = Mathf.Abs(angle - 0.36f) < 0.045f
                                         || Mathf.Abs(angle - 0.78f) < 0.045f
                                         || Mathf.Abs(angle - 1.18f) < 0.045f;
                        if (radialMortar)
                            SetPixel(pixels, size, size, x, y, stoneDark);
                        else
                            SetPixel(pixels, size, size, x, y, ((x + y) % 3 == 0) ? stoneLight : stoneMid);
                        continue;
                    }

                    // Solid moss-slate spandrel masonry matching backWall_*
                    bool mortarCourse = (y == 12) || (x == 10 && y >= 12) || (x == 4 && y < 12);
                    if (mortarCourse)
                    {
                        SetPixel(pixels, size, size, x, y, stoneDark);
                        continue;
                    }

                    int h = HashCell(x, y);
                    if (h % 11 == 0 && x <= 14 && y >= 13)
                        SetPixel(pixels, size, size, x, y, mossTone);
                    else if (h % 5 == 0)
                        SetPixel(pixels, size, size, x, y, stoneLight);
                    else if (h % 4 == 0)
                        SetPixel(pixels, size, size, x, y, stoneDark);
                    else
                        SetPixel(pixels, size, size, x, y, stoneMid);
                }
            }

            return EncodePixelsToPng(size, size, pixels);
        }

        private static byte[] BuildArchMullionPng()
        {
            const int width = 8;
            const int height = 24;
            Color32[] pixels = new Color32[width * height];

            Color32 ironDark = Hex("#141B20");
            Color32 ironMid = Hex("#27333C");
            Color32 ironLit = Hex("#3E4F5A");
            Color32 rivetGlint = Hex("#526775");

            for (int y = 0; y < height; y++)
            {
                bool isCollarBottom = (y == 5 || y == 17);
                bool isCollarTop = (y == 6 || y == 18);

                if (isCollarBottom || isCollarTop)
                {
                    SetPixel(pixels, width, height, 2, y, isCollarBottom ? ironMid : ironDark);
                    SetPixel(pixels, width, height, 3, y, isCollarBottom ? ironLit : ironMid);
                    SetPixel(pixels, width, height, 4, y, isCollarBottom ? rivetGlint : ironLit);
                    SetPixel(pixels, width, height, 5, y, ironMid);
                    SetPixel(pixels, width, height, 6, y, ironDark);
                    continue;
                }

                // 3px forged wrought-iron vertical mullion bar (x = 3..5)
                SetPixel(pixels, width, height, 3, y, (y % 5 == 0) ? ironMid : ironLit);
                SetPixel(pixels, width, height, 4, y, (y % 7 == 0) ? ironDark : ironMid);
                SetPixel(pixels, width, height, 5, y, ironDark);
            }

            return EncodePixelsToPng(width, height, pixels);
        }

        private static byte[] BuildShelfVialPng(int variant)
        {
            const int width = 10;
            const int height = 14;
            Color32[] pixels = new Color32[width * height];

            Color32 outline = Hex("#141A1E");
            Color32 glassRim = Hex("#8CA8B0");
            Color32 corkDark = Hex("#5E3E28");
            Color32 corkLit = Hex("#9B6E4A");
            Color32 waxDark = Hex("#6E1F22");
            Color32 waxLit = Hex("#B33A38");

            Color32 liqDeep;
            Color32 liqMid;
            Color32 liqBright;
            Color32 glassSpec;

            if (variant == 0)
            {
                // Emerald alchemy vial
                liqDeep = Hex("#146942");
                liqMid = Hex("#22B56B");
                liqBright = Hex("#63EA9C");
                glassSpec = Hex("#D8FFF0");
            }
            else if (variant == 1)
            {
                // Violet alchemy flask
                liqDeep = Hex("#3F1869");
                liqMid = Hex("#7E36C7");
                liqBright = Hex("#C27FFF");
                glassSpec = Hex("#F2DCFF");
            }
            else
            {
                // Amber apothecary bottle
                liqDeep = Hex("#7E3A0F");
                liqMid = Hex("#CF791B");
                liqBright = Hex("#FFC44D");
                glassSpec = Hex("#FFF2C2");
            }

            if (variant == 0)
            {
                // Tall cylindrical emerald vial (x = 2..7, y = 0..9, neck y = 10..11, cork y = 12..13)
                FillRect(pixels, width, height, 2, 0, 7, 9, outline);
                FillRect(pixels, width, height, 3, 1, 6, 8, liqMid);
                FillRect(pixels, width, height, 5, 1, 6, 7, liqDeep);
                FillRect(pixels, width, height, 3, 6, 5, 7, liqBright);
                FillRect(pixels, width, height, 3, 8, 6, 8, glassRim);
                FillRect(pixels, width, height, 3, 10, 6, 11, outline);
                SetPixel(pixels, width, height, 4, 10, glassRim);
                SetPixel(pixels, width, height, 5, 10, glassRim);
                SetPixel(pixels, width, height, 4, 11, glassSpec);
                SetPixel(pixels, width, height, 5, 11, glassRim);
                SetPixel(pixels, width, height, 4, 12, corkLit);
                SetPixel(pixels, width, height, 5, 12, corkDark);
                SetPixel(pixels, width, height, 4, 13, corkLit);
                SetPixel(pixels, width, height, 5, 13, corkDark);
                SetPixel(pixels, width, height, 3, 3, glassSpec);
                SetPixel(pixels, width, height, 3, 4, glassSpec);
                SetPixel(pixels, width, height, 3, 5, glassSpec);
                SetPixel(pixels, width, height, 2, 0, default);
                SetPixel(pixels, width, height, 7, 0, default);
            }
            else if (variant == 1)
            {
                // Bulbous violet round flask (x = 1..8, y = 0..7, neck y = 8..11, cork y = 12..13)
                FillRect(pixels, width, height, 1, 0, 8, 7, outline);
                SetPixel(pixels, width, height, 1, 0, default);
                SetPixel(pixels, width, height, 8, 0, default);
                SetPixel(pixels, width, height, 1, 7, default);
                SetPixel(pixels, width, height, 8, 7, default);
                FillRect(pixels, width, height, 2, 1, 7, 6, liqMid);
                FillRect(pixels, width, height, 5, 1, 7, 5, liqDeep);
                FillRect(pixels, width, height, 3, 4, 5, 5, liqBright);
                SetPixel(pixels, width, height, 2, 6, glassRim);
                SetPixel(pixels, width, height, 7, 6, glassRim);
                FillRect(pixels, width, height, 3, 8, 6, 11, outline);
                SetPixel(pixels, width, height, 2, 10, outline);
                SetPixel(pixels, width, height, 7, 10, outline);
                FillRect(pixels, width, height, 4, 8, 5, 10, glassRim);
                SetPixel(pixels, width, height, 4, 11, glassSpec);
                SetPixel(pixels, width, height, 5, 11, glassRim);
                SetPixel(pixels, width, height, 4, 12, corkLit);
                SetPixel(pixels, width, height, 5, 12, corkDark);
                SetPixel(pixels, width, height, 4, 13, corkLit);
                SetPixel(pixels, width, height, 5, 13, corkDark);
                SetPixel(pixels, width, height, 3, 2, glassSpec);
                SetPixel(pixels, width, height, 3, 3, glassSpec);
                SetPixel(pixels, width, height, 3, 4, glassSpec);
            }
            else
            {
                // Square-shouldered amber apothecary bottle (x = 1..8, y = 0..8, shoulder y = 9, neck y = 10..11)
                FillRect(pixels, width, height, 1, 0, 8, 8, outline);
                SetPixel(pixels, width, height, 1, 0, default);
                SetPixel(pixels, width, height, 8, 0, default);
                FillRect(pixels, width, height, 2, 1, 7, 7, liqMid);
                FillRect(pixels, width, height, 6, 1, 7, 6, liqDeep);
                FillRect(pixels, width, height, 3, 5, 5, 7, liqBright);
                FillRect(pixels, width, height, 2, 9, 7, 9, outline);
                FillRect(pixels, width, height, 3, 9, 6, 9, glassRim);
                FillRect(pixels, width, height, 3, 10, 6, 11, outline);
                SetPixel(pixels, width, height, 4, 10, glassRim);
                SetPixel(pixels, width, height, 5, 10, glassRim);
                SetPixel(pixels, width, height, 4, 11, waxLit);
                SetPixel(pixels, width, height, 5, 11, waxDark);
                SetPixel(pixels, width, height, 4, 12, waxLit);
                SetPixel(pixels, width, height, 5, 12, waxDark);
                SetPixel(pixels, width, height, 4, 13, corkLit);
                SetPixel(pixels, width, height, 5, 13, corkDark);
                SetPixel(pixels, width, height, 3, 2, glassSpec);
                SetPixel(pixels, width, height, 3, 3, glassSpec);
                SetPixel(pixels, width, height, 3, 4, glassSpec);
                SetPixel(pixels, width, height, 3, 6, glassSpec);
            }

            return EncodePixelsToPng(width, height, pixels);
        }

        private static byte[] BuildPrisonBellPng()
        {
            const int width = 48;
            const int height = 56;
            Color32[] pixels = new Color32[width * height];

            Color32 outline = Hex("#12181B");
            Color32 interiorVoid = Hex("#0B0F11");
            Color32 ironDark = Hex("#1B232A");
            Color32 ironMid = Hex("#2B3742");
            Color32 ironLit = Hex("#415261");
            Color32 ironRim = Hex("#647A8C");

            Color32 bronzeShadow = Hex("#242E29");
            Color32 bronzeDark = Hex("#35423A");
            Color32 patinaDeep = Hex("#396B5E");
            Color32 patinaMid = Hex("#4D8A78");
            Color32 patinaLit = Hex("#6BB098");
            Color32 patinaHi = Hex("#8FD1B6");
            Color32 bronzeBandMid = Hex("#8C754A");
            Color32 bronzeBandLit = Hex("#B59862");

            const float cx = 23.5f;

            // 1. Ceiling iron mounting bracket (y = 51..55) & heavy riveted yoke beam (y = 44..51)
            FillRect(pixels, width, height, 20, 51, 27, 55, outline);
            FillRect(pixels, width, height, 21, 51, 26, 54, ironMid);
            FillRect(pixels, width, height, 21, 53, 23, 54, ironLit);

            FillRect(pixels, width, height, 6, 45, 41, 51, outline);
            FillRect(pixels, width, height, 7, 46, 40, 50, ironMid);
            FillRect(pixels, width, height, 7, 49, 28, 50, ironLit);
            FillRect(pixels, width, height, 5, 44, 9, 52, outline);
            FillRect(pixels, width, height, 6, 45, 8, 51, ironLit);
            FillRect(pixels, width, height, 38, 44, 42, 52, outline);
            FillRect(pixels, width, height, 39, 45, 41, 51, ironDark);

            int[] yokeRivets = { 7, 14, 23, 33, 40 };
            for (int i = 0; i < yokeRivets.Length; i++)
            {
                SetPixel(pixels, width, height, yokeRivets[i], 48, bronzeBandLit);
                SetPixel(pixels, width, height, yokeRivets[i], 47, ironDark);
            }

            // 2. Crown iron straps & loops (y = 40..44)
            FillRect(pixels, width, height, 16, 40, 31, 44, outline);
            FillRect(pixels, width, height, 17, 41, 30, 43, ironMid);
            FillRect(pixels, width, height, 17, 42, 22, 43, ironLit);
            FillRect(pixels, width, height, 20, 41, 22, 42, interiorVoid);
            FillRect(pixels, width, height, 25, 41, 27, 42, interiorVoid);

            // 3. Flared Gothic verdigris bronze bell body (y = 12..40)
            for (int y = 12; y <= 40; y++)
            {
                float t = (40 - y) / 28f; // 0 at crown, 1 at soundbow lip
                float halfW = 8.2f + 3.4f * t + 7.6f * Mathf.Pow(t, 2.35f);
                if (y <= 15)
                    halfW += (16 - y) * 0.45f;

                int xMin = Mathf.Max(2, Mathf.FloorToInt(cx - halfW));
                int xMax = Mathf.Min(width - 3, Mathf.CeilToInt(cx + halfW));

                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = x - cx;
                    float adx = Mathf.Abs(dx);
                    if (adx > halfW)
                        continue;

                    if (adx >= halfW - 1.15f || y == 12)
                    {
                        SetPixel(pixels, width, height, x, y, outline);
                        continue;
                    }

                    // Horizontal cast relief bands at y = 33..34 and y = 17..19
                    bool isBandUnderside = (y == 32 || y == 16);
                    bool isReliefBand = (y == 33 || y == 34 || y == 17 || y == 18 || y == 19);
                    float normX = dx / Mathf.Max(1f, halfW);

                    if (isBandUnderside && normX > -0.82f && normX < 0.82f)
                    {
                        SetPixel(pixels, width, height, x, y, outline);
                        continue;
                    }

                    if (isReliefBand)
                    {
                        if (normX < -0.15f)
                            SetPixel(pixels, width, height, x, y, bronzeBandLit);
                        else if (normX < 0.35f)
                            SetPixel(pixels, width, height, x, y, bronzeBandMid);
                        else
                            SetPixel(pixels, width, height, x, y, bronzeDark);
                        continue;
                    }

                    // Directional verdigris patina & bronze curvature shading
                    int texNoise = HashCell(x, y) % 10;
                    bool patinaStreak = ((x + (y / 3)) % 5 <= 2) || y <= 21 || y >= 36;

                    Color32 bellColor;
                    if (normX < -0.68f)
                        bellColor = patinaStreak ? patinaLit : patinaMid;
                    else if (normX < -0.22f)
                        bellColor = (texNoise <= 2) ? patinaHi : patinaLit;
                    else if (normX < 0.18f)
                        bellColor = patinaStreak ? (texNoise == 0 ? patinaLit : patinaMid) : bronzeDark;
                    else if (normX < 0.55f)
                        bellColor = patinaStreak ? patinaDeep : bronzeShadow;
                    else
                        bellColor = (normX > 0.78f && patinaStreak) ? patinaDeep : ironDark;

                    // Subtle weathered pitting / fracture mark on mid waist
                    if ((x == 28 && y >= 24 && y <= 29) || (x == 29 && y >= 22 && y <= 25))
                        bellColor = outline;

                    SetPixel(pixels, width, height, x, y, bellColor);
                }
            }

            // 4. Shadowed inner bell rim & heavy forged iron clapper (y = 3..12)
            FillRect(pixels, width, height, 11, 11, 36, 12, interiorVoid);
            FillRect(pixels, width, height, 13, 10, 34, 11, interiorVoid);
            FillRect(pixels, width, height, 22, 8, 25, 13, outline);
            FillRect(pixels, width, height, 23, 8, 24, 12, ironMid);

            const float clapperCenterY = 5.5f;
            for (int y = 2; y <= 9; y++)
            {
                for (int x = 19; x <= 28; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - clapperCenterY) * (y - clapperCenterY));
                    if (dist > 3.85f)
                        continue;
                    if (dist >= 2.75f)
                        SetPixel(pixels, width, height, x, y, outline);
                    else if (x < cx && y >= clapperCenterY)
                        SetPixel(pixels, width, height, x, y, ironRim);
                    else if (x <= cx)
                        SetPixel(pixels, width, height, x, y, ironLit);
                    else
                        SetPixel(pixels, width, height, x, y, ironMid);
                }
            }

            return EncodePixelsToPng(width, height, pixels);
        }

        private static byte[] BuildSlabStrataPng()
        {
            const int width = 72;
            const int height = 18;
            Color32[] pixels = new Color32[width * height];

            Color32 mortarDark = Hex("#192322");
            Color32 stoneDeep = Hex("#2C3D3A");
            Color32 stoneDark = Hex("#384C49");
            Color32 stoneMid = Hex("#4B625D");
            Color32 stoneLight = Hex("#627B74");
            Color32 stoneSpec = Hex("#738C84");

            for (int x = 0; x < width; x++)
            {
                int lowerMortarY = (x >= 37 && x <= 57) ? 6 : 5;
                int upperMortarY = (x >= 12 && x <= 31) ? 11 : 12;

                bool botSeam = (x == 19 || x == 37 || x == 57);
                bool midSeam = (x == 12 || x == 32 || x == 49 || x == 65);
                bool topSeam = (x == 16 || x == 36 || x == 55);

                int botClip = (botSeam || x <= 1 || x >= width - 2) ? 2 : (((HashCell(x, 101) % 4) == 0) ? 1 : 0);
                int topClip = (topSeam || x <= 2 || x >= width - 3) ? 2 : (((HashCell(x, 202) % 4) == 0) ? 1 : 0);

                for (int y = 0; y < height; y++)
                {
                    if (y < botClip || y > height - 1 - topClip)
                        continue;
                    if (x == 0 && (y <= 2 || y >= 14))
                        continue;
                    if (x == width - 1 && (y <= 3 || y >= 13))
                        continue;

                    if (y == lowerMortarY || y == upperMortarY)
                    {
                        SetPixel(pixels, width, height, x, y, mortarDark);
                        continue;
                    }

                    bool isLowerCourse = y < lowerMortarY;
                    bool isMidCourse = y > lowerMortarY && y < upperMortarY;
                    bool isVerticalMortar = (isLowerCourse && botSeam) || (isMidCourse && midSeam) || (!isLowerCourse && !isMidCourse && topSeam);
                    if (isVerticalMortar)
                    {
                        SetPixel(pixels, width, height, x, y, mortarDark);
                        continue;
                    }

                    bool isCrack =
                        (x == 24 && y >= 8 && y <= 10) || (x == 25 && (y == 7 || y == 8)) ||
                        (x == 59 && y >= 2 && y <= 4) || (x == 60 && y == 2) ||
                        (x == 43 && y >= 13 && y <= 15);
                    if (isCrack)
                    {
                        SetPixel(pixels, width, height, x, y, mortarDark);
                        continue;
                    }

                    int courseTopY = isLowerCourse ? lowerMortarY - 1 : (isMidCourse ? upperMortarY - 1 : height - 1 - topClip);
                    int courseBotY = isLowerCourse ? botClip : (isMidCourse ? lowerMortarY + 1 : upperMortarY + 1);

                    int h = HashCell(x, y);
                    Color32 col;
                    if (y == 0)
                        col = WithAlpha(stoneDeep, 190);
                    else if (y == height - 1)
                        col = WithAlpha(stoneDark, 205);
                    else if (y == courseTopY)
                        col = (h % 5 == 0) ? stoneSpec : stoneLight;
                    else if (y == courseTopY - 1 && (h % 3 == 0))
                        col = stoneLight;
                    else if (y == courseBotY)
                        col = (h % 3 == 0) ? stoneDeep : stoneDark;
                    else if (h % 6 == 0)
                        col = stoneLight;
                    else if (h % 4 == 0)
                        col = stoneDark;
                    else
                        col = stoneMid;

                    SetPixel(pixels, width, height, x, y, col);
                }
            }

            return EncodePixelsToPng(width, height, pixels);
        }

        private static bool IsArchMortarLine(int coord, int variant)
        {
            return variant switch
            {
                0 => coord == 7 || coord == 16,
                1 => coord == 9 || coord == 19,
                _ => coord == 5 || coord == 13 || coord == 21
            };
        }

        private static int GetArchBlockIndex(int coord, int variant)
        {
            return variant switch
            {
                0 => coord < 7 ? 0 : (coord < 16 ? 1 : 2),
                1 => coord < 9 ? 0 : (coord < 19 ? 1 : 2),
                _ => coord < 5 ? 0 : (coord < 13 ? 1 : (coord < 21 ? 2 : 3))
            };
        }

        private static int GetWeatheredOuterDepth(int coord, int variant, bool isMortar)
        {
            if (isMortar)
                return 2;
            if (((coord + variant * 5) % 4 == 0) || ((coord * 7 + variant * 3) % 11 <= 2))
                return 1;
            return 0;
        }

        private static int HashCell(int x, int y)
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) & 0x7FFFFFFF;
        }

        private static Color32 WithAlpha(Color32 c, byte alpha)
        {
            return new Color32(c.r, c.g, c.b, alpha);
        }

        private static bool ConfigureSpriteImporter(string assetPath, TextureWrapMode wrapModeU, TextureWrapMode wrapModeV = TextureWrapMode.Clamp)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing TextureImporter for '{assetPath}'.");

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            bool dirty =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                Mathf.Abs(importer.spritePixelsPerUnit - PixelsPerUnit) > 1e-4f ||
                importer.filterMode != FilterMode.Point ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.mipmapEnabled ||
                importer.npotScale != TextureImporterNPOTScale.None ||
                importer.wrapModeU != wrapModeU ||
                importer.wrapModeV != wrapModeV ||
                settings.spriteAlignment != (int)SpriteAlignment.Center ||
                Vector2.SqrMagnitude(settings.spritePivot - new Vector2(0.5f, 0.5f)) > 1e-6f;

            if (!dirty)
                return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapModeU = wrapModeU;
            importer.wrapModeV = wrapModeV;

            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            return true;
        }

        private static byte[] EncodePixelsToPng(int width, int height, Color32[] pixels)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false);
            try
            {
                tex.SetPixels32(pixels);
                tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                return ImageConversion.EncodeToPNG(tex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        private static void FillRect(Color32[] pixels, int width, int height, int xMin, int yMin, int xMax, int yMax, Color32 color)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                    SetPixel(pixels, width, height, x, y, color);
            }
        }

        private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;
            pixels[y * width + x] = color;
        }

        private static Color32 Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color c))
                throw new ArgumentException($"Invalid hex color '{hex}'.", nameof(hex));
            return c;
        }

        private static bool WriteBytesIfChanged(string path, byte[] bytes)
        {
            if (File.Exists(path))
            {
                byte[] existing = File.ReadAllBytes(path);
                if (existing.Length == bytes.Length)
                {
                    bool same = true;
                    for (int i = 0; i < existing.Length; i++)
                    {
                        if (existing[i] != bytes[i])
                        {
                            same = false;
                            break;
                        }
                    }
                    if (same)
                        return false;
                }
            }

            File.WriteAllBytes(path, bytes);
            return true;
        }
    }
}
