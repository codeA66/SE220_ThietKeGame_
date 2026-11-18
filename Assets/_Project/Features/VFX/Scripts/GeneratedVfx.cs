using UnityEngine;
using Random = System.Random;

namespace DeadCells.VFX
{
    // Pixel-art sprite sequences drawn in code, played through the normal VfxSpawner like the imported
    // Dead Cells effects: 32 pixels per unit, point filtered, 30 fps, white-based so they read on any floor.
    public static class GeneratedVfx
    {
        private const int PixelsPerUnit = 32;
        private const float Fps = 30f;

        private static readonly Color32 White = new Color32(255, 255, 255, 255);
        private static readonly Color32 Cream = new Color32(255, 240, 205, 255);
        // Same red as the enemy health bar fill.
        private static readonly Color32 Blood = new Color32(184, 23, 31, 255);
        private static readonly Color32 Dust = new Color32(232, 222, 204, 255);
        private static readonly Color32 DustShade = new Color32(168, 156, 140, 255);
        private static readonly Color32 Smoke = new Color32(214, 210, 205, 255);
        private static readonly Color32 SmokeShade = new Color32(120, 112, 115, 255);

        private static VfxDefinition s_hitSpark;
        private static VfxDefinition s_deathPuff;
        private static VfxDefinition s_runDust;
        private static VfxDefinition s_rollDust;

        public static VfxDefinition HitSpark
        {
            get
            {
                if (s_hitSpark == null)
                    s_hitSpark = VfxDefinition.Create("Vfx_HitSpark", BuildHitSpark(), Fps, 1f, 3, 4);
                return s_hitSpark;
            }
        }

        public static VfxDefinition DeathPuff
        {
            get
            {
                if (s_deathPuff == null)
                    s_deathPuff = VfxDefinition.Create("Vfx_DeathPuff", BuildDeathPuff(), Fps, 1f, 2, 3);
                return s_deathPuff;
            }
        }

        // Puffs trail to the left of the feet; flip the effect when running left.
        public static VfxDefinition RunDust
        {
            get
            {
                if (s_runDust == null)
                    s_runDust = VfxDefinition.Create("Vfx_RunDust", BuildDust(), Fps, 1f, 1, 4);
                return s_runDust;
            }
        }

        public static VfxDefinition RollDust
        {
            get
            {
                if (s_rollDust == null)
                    s_rollDust = VfxDefinition.Create("Vfx_RollDust", BuildDust(), Fps, 1.6f, 1, 2);
                return s_rollDust;
            }
        }

        // Radial white streaks that shoot out and thin, a quick centre flash and a few blood droplets.
        private static Sprite[] BuildHitSpark()
        {
            const int size = 32;
            const int frameCount = 5;
            var random = new Random(7);
            float[] angles = new float[8];
            float[] lengths = new float[8];
            for (int i = 0; i < angles.Length; i++)
            {
                angles[i] = (i / (float)angles.Length + (float)(random.NextDouble() - 0.5) * 0.06f) * Mathf.PI * 2f;
                lengths[i] = 5f + (float)random.NextDouble() * 3f;
            }
            float[] dropAngles = new float[4];
            for (int i = 0; i < dropAngles.Length; i++)
                dropAngles[i] = (float)random.NextDouble() * Mathf.PI * 2f;

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                var pixels = new Pixels(size, size);
                float c = (size - 1) * 0.5f;

                float flash = 5f - 1.75f * f;
                if (flash > 0f)
                    pixels.Disc(c, c, flash, White);

                float inner = 3f + t * 8f;
                for (int i = 0; i < angles.Length; i++)
                {
                    if (f == frameCount - 1 && i % 2 == 1)
                        continue;
                    float outer = inner + lengths[i] * (1f - t * 0.7f);
                    var color = f < 2 ? White : Cream;
                    pixels.Line(c + Mathf.Cos(angles[i]) * inner, c + Mathf.Sin(angles[i]) * inner,
                        c + Mathf.Cos(angles[i]) * outer, c + Mathf.Sin(angles[i]) * outer, color);
                }

                if (f < frameCount - 1)
                {
                    for (int i = 0; i < dropAngles.Length; i++)
                    {
                        float dist = 4f + t * 11f;
                        float x = c + Mathf.Cos(dropAngles[i]) * dist;
                        float y = c + Mathf.Sin(dropAngles[i]) * dist - t * t * 5f;
                        pixels.Disc(x, y, f < 2 ? 1.2f : 0.7f, Blood);
                    }
                }

                frames[f] = pixels.ToSprite(new Vector2(0.5f, 0.5f));
            }
            return frames;
        }

        // A ring of smoke puffs that balloons outward and fades, with cream sparks rising out of it.
        private static Sprite[] BuildDeathPuff()
        {
            const int size = 48;
            const int frameCount = 9;
            const int puffCount = 8;
            const int sparkCount = 6;
            var random = new Random(21);
            float[] puffAngles = new float[puffCount];
            for (int i = 0; i < puffCount; i++)
                puffAngles[i] = (i / (float)puffCount + (float)(random.NextDouble() - 0.5) * 0.05f) * Mathf.PI * 2f;
            float[] sparkDrift = new float[sparkCount];
            for (int i = 0; i < sparkCount; i++)
                sparkDrift[i] = ((float)random.NextDouble() - 0.5f) * 14f;

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                float alpha = 1f - t * t;
                var pixels = new Pixels(size, size);
                float c = (size - 1) * 0.5f;

                float eased = 1f - (1f - t) * (1f - t);
                float dist = 3f + eased * 15f;
                float radius = 2f + 3.5f * Mathf.Sin(t * Mathf.PI);
                for (int i = 0; i < puffCount; i++)
                {
                    float x = c + Mathf.Cos(puffAngles[i]) * dist;
                    float y = c + Mathf.Sin(puffAngles[i]) * dist * 0.8f;
                    pixels.Disc(x, y, radius, WithAlpha(SmokeShade, alpha));
                    pixels.Disc(x, y, Mathf.Max(0.5f, radius - 1f), WithAlpha(Smoke, alpha));
                }

                float core = 7f * (1f - t);
                if (core > 0.5f)
                    pixels.Disc(c, c, core, WithAlpha(White, alpha));

                for (int i = 0; i < sparkCount; i++)
                {
                    float x = c + sparkDrift[i] * t;
                    float y = c + 3f + t * 14f - t * t * 8f;
                    pixels.Plot(Mathf.RoundToInt(x), Mathf.RoundToInt(y), WithAlpha(Cream, 1f - t * t * t));
                }

                frames[f] = pixels.ToSprite(new Vector2(0.5f, 0.5f));
            }
            return frames;
        }

        // Three dust puffs that kick up behind the feet, drift back and thin out.
        private static Sprite[] BuildDust()
        {
            const int width = 28;
            const int height = 14;
            const int frameCount = 6;
            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                float alpha = 1f - Mathf.Pow(t, 1.5f);
                var pixels = new Pixels(width, height);
                for (int i = 0; i < 3; i++)
                {
                    float x = 22f - i * 6f - t * (3f + i * 2f);
                    float y = 1.5f + t * (3f + i);
                    float radius = 1f + t * (1.5f + 0.4f * i);
                    if (radius >= 2f)
                        pixels.Disc(x, y, radius, WithAlpha(DustShade, alpha));
                    pixels.Disc(x, y, Mathf.Max(0.6f, radius - 0.8f), WithAlpha(Dust, alpha));
                }
                frames[f] = pixels.ToSprite(new Vector2(0.8f, 0f));
            }
            return frames;
        }

        private static Color32 WithAlpha(Color32 color, float alpha) =>
            new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha)));

        private sealed class Pixels
        {
            private readonly int _width;
            private readonly int _height;
            private readonly Color32[] _data;

            public Pixels(int width, int height)
            {
                _width = width;
                _height = height;
                _data = new Color32[width * height];
            }

            // A pixel is only overwritten by a more opaque one, so fading shapes keep their edges.
            public void Plot(int x, int y, Color32 color)
            {
                if (x < 0 || y < 0 || x >= _width || y >= _height)
                    return;
                int index = y * _width + x;
                if (color.a >= _data[index].a)
                    _data[index] = color;
            }

            public void Disc(float cx, float cy, float radius, Color32 color)
            {
                int x0 = Mathf.FloorToInt(cx - radius);
                int x1 = Mathf.CeilToInt(cx + radius);
                int y0 = Mathf.FloorToInt(cy - radius);
                int y1 = Mathf.CeilToInt(cy + radius);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = x - cx;
                        float dy = y - cy;
                        if (dx * dx + dy * dy <= radius * radius)
                            Plot(x, y, color);
                    }
                }
            }

            public void Line(float x0, float y0, float x1, float y1, Color32 color)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 2f));
                for (int i = 0; i <= steps; i++)
                {
                    float k = i / (float)steps;
                    Plot(Mathf.RoundToInt(Mathf.Lerp(x0, x1, k)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, k)), color);
                }
            }

            public Sprite ToSprite(Vector2 pivot)
            {
                var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                texture.SetPixels32(_data);
                texture.Apply();
                var sprite = Sprite.Create(texture, new Rect(0, 0, _width, _height), pivot, PixelsPerUnit);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
        }
    }
}
