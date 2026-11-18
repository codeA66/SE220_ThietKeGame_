using System.Collections.Generic;
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
        private static readonly Color32 Ember = new Color32(255, 160, 60, 255);
        // Crimson, matching the blood drops.
        private static readonly Color32 Blood = new Color32(165, 12, 45, 255);
        private static readonly Color32 Dust = new Color32(232, 222, 204, 255);
        private static readonly Color32 DustShade = new Color32(168, 156, 140, 255);
        private static readonly Color32 Smoke = new Color32(214, 210, 205, 255);
        private static readonly Color32 SmokeShade = new Color32(150, 144, 150, 255);
        private static readonly Color32 SmokeLight = new Color32(244, 242, 238, 255);
        private static readonly Color32 Gold = new Color32(255, 214, 90, 255);
        private static readonly Color32 BloodDark = new Color32(95, 5, 28, 255);
        private static readonly Color32 Rubble = new Color32(96, 86, 80, 255);

        // The big centred effects are flattened below their centre so they sit on the floor instead of
        // digging into it. These are how far (in units) each one still reaches below its centre.
        private const float LowerSquash = 0.6f;
        private const int HitSparkSize = 96;
        private const int CritSparkSize = 128;
        // Measured from the art: the lowest spark ends about 42 px (crit 55 px) below the contact point.
        public const float HitSparkExtentBelow = 42f / PixelsPerUnit * LowerSquash;
        public const float CritSparkExtentBelow = 55f / PixelsPerUnit * LowerSquash;
        public const float DeathPuffExtentBelow = 48f / PixelsPerUnit * LowerSquash;

        // How much of the lower reach is allowed to dip into the floor before the effect gets lifted.
        public const float GroundSink = 0.3f;

        // Everything built here, so it can be destroyed and rebuilt when a new play session starts.
        private static readonly List<Object> s_owned = new List<Object>();

        private static VfxDefinition s_hitSpark;
        private static VfxDefinition s_deathPuff;
        private static VfxDefinition s_runDust;
        private static VfxDefinition s_rollDust;
        private static VfxDefinition s_critSpark;
        private static VfxDefinition s_bloodMist;
        private static VfxDefinition s_stompWave;

        // With domain reload turned off for Play Mode, statics survive between sessions, so cached
        // sprites could be stale or destroyed. Start each session from scratch.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            foreach (Object owned in s_owned)
            {
                if (owned != null)
                    Object.DestroyImmediate(owned);
            }
            s_owned.Clear();
            s_hitSpark = null;
            s_deathPuff = null;
            s_runDust = null;
            s_rollDust = null;
            s_critSpark = null;
            s_bloodMist = null;
            s_stompWave = null;
        }

        private static bool IsUsable(VfxDefinition definition) =>
            definition != null && definition.Frames != null && definition.Frames.Length > 0 && definition.Frames[0] != null;

        private static VfxDefinition Register(VfxDefinition definition)
        {
            s_owned.Add(definition);
            return definition;
        }

        public static VfxDefinition HitSpark
        {
            get
            {
                if (!IsUsable(s_hitSpark))
                    s_hitSpark = Register(VfxDefinition.Create("Vfx_HitSpark",
                        BuildHitSpark(HitSparkSize, 1f, Cream, Ember, false), Fps, 1f, 10, 4));
                return s_hitSpark;
            }
        }

        // Bigger, gold-hot version of the hit spark for critical hits.
        public static VfxDefinition CritSpark
        {
            get
            {
                if (!IsUsable(s_critSpark))
                    s_critSpark = Register(VfxDefinition.Create("Vfx_CritSpark",
                        BuildHitSpark(CritSparkSize, 1.35f, Gold, Ember, true), Fps, 1f, 11, 2));
                return s_critSpark;
            }
        }

        // Crit kill: blood mist bursting out of the wound. Drawn going right; flip it for the left.
        public static VfxDefinition BloodMist
        {
            get
            {
                if (!IsUsable(s_bloodMist))
                    s_bloodMist = Register(VfxDefinition.Create("Vfx_BloodMist", BuildBloodMist(), Fps, 1f, 7, 1));
                return s_bloodMist;
            }
        }

        // Ground-slam shockwave: dust walls racing out both ways with rubble kicked up. Pivot at the feet.
        public static VfxDefinition StompWave
        {
            get
            {
                if (!IsUsable(s_stompWave))
                    s_stompWave = Register(VfxDefinition.Create("Vfx_StompWave", BuildStompWave(), Fps, 1f, 2, 1));
                return s_stompWave;
            }
        }

        public static VfxDefinition DeathPuff
        {
            get
            {
                if (!IsUsable(s_deathPuff))
                    s_deathPuff = Register(VfxDefinition.Create("Vfx_DeathPuff", BuildDeathPuff(), Fps, 1f, 9, 3));
                return s_deathPuff;
            }
        }

        // Puffs trail to the left of the feet; flip the effect when running left.
        public static VfxDefinition RunDust
        {
            get
            {
                if (!IsUsable(s_runDust))
                    s_runDust = Register(VfxDefinition.Create("Vfx_RunDust", BuildDust(), Fps, 1f, 1, 4));
                return s_runDust;
            }
        }

        public static VfxDefinition RollDust
        {
            get
            {
                if (!IsUsable(s_rollDust))
                    s_rollDust = Register(VfxDefinition.Create("Vfx_RollDust", BuildDust(), Fps, 1.5f, 1, 2));
                return s_rollDust;
            }
        }

        // Directional impact flare, drawn for a blow pushing to the right (flip it for the left):
        // a sharp lens of light along the blow, with a short cross glint (and a
        // diagonal one for crits), a flash at the contact point, and sparks sprayed forward in a cone.
        // The lens stretches thin and breaks up while the sparks fly on, all in a few frames.
        private static Sprite[] BuildHitSpark(int size, float reach, Color32 warm, Color32 tip, bool cross)
        {
            const int frameCount = 7;
            const int sparkCount = 9;
            // No baked tilt: EnemyVfx rotates each hit randomly around the line of the blow.
            const float tilt = 0f;
            var random = new Random(7);
            float[] sparkAngle = new float[sparkCount];
            float[] sparkSpeed = new float[sparkCount];
            for (int i = 0; i < sparkCount; i++)
            {
                sparkAngle[i] = Mathf.Lerp(-40f, 40f, (float)random.NextDouble()) * Mathf.Deg2Rad + tilt;
                sparkSpeed[i] = (26f + (float)random.NextDouble() * 16f) * reach;
            }

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                var pixels = new Pixels(size, size);
                float c = (size - 1) * 0.5f;

                for (int i = 0; i < sparkCount; i++)
                {
                    float travel = 1f - (1f - t) * (1f - t);
                    float head = 4f * reach + sparkSpeed[i] * travel;
                    float tail = head - Mathf.Lerp(10f, 2f, t) * reach;
                    float drop = t * t * 6f;
                    Color32 color = f < 2 ? White : f < 4 ? warm : tip;
                    pixels.Line(c + Mathf.Cos(sparkAngle[i]) * tail, c + Mathf.Sin(sparkAngle[i]) * tail - drop,
                        c + Mathf.Cos(sparkAngle[i]) * head, c + Mathf.Sin(sparkAngle[i]) * head - drop,
                        color, f < 3 ? 0.6f : 0f);
                }

                if (f < 4)
                {
                    float length = (34f + f * 10f) * reach;
                    float width = Mathf.Max(0.6f, (5f - f * 1.4f) * reach);
                    Color32 body = f == 0 ? White : warm;
                    pixels.Lens(c, c, tilt, length, width, body);
                    if (f < 3)
                        pixels.Lens(c, c, tilt, length * 0.8f, Mathf.Max(0.5f, width * 0.35f), White);
                    if (f < 2)
                    {
                        pixels.Lens(c, c, tilt + Mathf.PI * 0.5f, length * 0.4f, width * 0.6f, White);
                        if (cross)
                        {
                            pixels.Lens(c, c, tilt + Mathf.PI * 0.25f, length * 0.55f, width * 0.5f, White);
                            pixels.Lens(c, c, tilt - Mathf.PI * 0.25f, length * 0.55f, width * 0.5f, White);
                        }
                    }
                }

                if (f < 2)
                    pixels.Disc(c, c, (6f - f * 2.5f) * reach, White);

                pixels.Dissolve(Mathf.Clamp01((t - 0.55f) / 0.45f));
                frames[f] = pixels.ToSprite(new Vector2(0.5f, 0.5f), LowerSquash);
            }
            return frames;
        }

        // Crit kill: a cloud of blood mist bursting out of the wound, drawn going right (flip it for the
        // left). The drops themselves are real particles in BloodDrops.
        private static Sprite[] BuildBloodMist()
        {
            const int width = 112;
            const int height = 96;
            const float originX = 16f;
            const float originY = 48f;
            const int frameCount = 14;
            const int puffCount = 18;
            var random = new Random(89);
            float[] angle = new float[puffCount];
            float[] reach = new float[puffCount];
            float[] bulk = new float[puffCount];
            for (int i = 0; i < puffCount; i++)
            {
                angle[i] = Mathf.Lerp(-20f, 50f, (float)random.NextDouble()) * Mathf.Deg2Rad;
                reach[i] = 4f + (float)random.NextDouble() * 30f;
                bulk[i] = 3f + (float)random.NextDouble() * 4f;
            }

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                float travel = 1f - (1f - t) * (1f - t);
                var pixels = new Pixels(width, height);
                for (int i = 0; i < puffCount; i++)
                {
                    float x = originX + Mathf.Cos(angle[i]) * reach[i] * travel;
                    float y = originY + Mathf.Sin(angle[i]) * reach[i] * travel - t * 4f;
                    float radius = bulk[i] * (0.6f + 0.6f * travel) * (1f - 0.5f * t);
                    pixels.Disc(x, y, radius, BloodDark);
                    pixels.Disc(x - radius * 0.2f, y + radius * 0.25f, radius * 0.55f, Blood);
                }
                pixels.Dissolve(Mathf.Clamp01((t - 0.45f) / 0.55f));
                frames[f] = pixels.ToSprite(new Vector2(originX / width, originY / height));
            }
            return frames;
        }

        // Ground pound: a fan of light bursts up from the impact while the floor flashes, a shockwave arc
        // races out along the ground, dust walls roll out both ways, rubble tumbles up and falls, embers
        // fly, and it all breaks up in dithered pixels. Pivot at the feet; the canvas floor is the ground.
        private static Sprite[] BuildStompWave()
        {
            const int width = 176;
            const int height = 64;
            const int frameCount = 14;
            const int burstCount = 9;
            const int puffsPerSide = 7;
            const int rubbleCount = 16;
            const int emberCount = 8;
            var random = new Random(63);

            float[] burstAngle = new float[burstCount];
            float[] burstLength = new float[burstCount];
            for (int i = 0; i < burstCount; i++)
            {
                burstAngle[i] = Mathf.Lerp(35f, 145f, i / (float)(burstCount - 1)) * Mathf.Deg2Rad;
                burstLength[i] = 18f + (float)random.NextDouble() * 16f;
            }
            float[,] puffBack = new float[2, puffsPerSide];
            float[,] puffBulk = new float[2, puffsPerSide];
            float[,] puffLift = new float[2, puffsPerSide];
            for (int s = 0; s < 2; s++)
            {
                for (int k = 0; k < puffsPerSide; k++)
                {
                    puffBack[s, k] = k * 6f + (float)random.NextDouble() * 5f;
                    puffBulk[s, k] = 7f - k * 0.7f + (float)random.NextDouble() * 1.5f;
                    puffLift[s, k] = (float)random.NextDouble() * 5f;
                }
            }
            float[] rubbleVx = new float[rubbleCount];
            float[] rubbleVy = new float[rubbleCount];
            for (int i = 0; i < rubbleCount; i++)
            {
                rubbleVx[i] = ((float)random.NextDouble() * 2f - 1f) * 70f;
                rubbleVy[i] = 55f + (float)random.NextDouble() * 55f;
            }
            float[] emberVx = new float[emberCount];
            float[] emberVy = new float[emberCount];
            for (int i = 0; i < emberCount; i++)
            {
                emberVx[i] = ((float)random.NextDouble() * 2f - 1f) * 45f;
                emberVy[i] = 45f + (float)random.NextDouble() * 35f;
            }

            var frames = new Sprite[frameCount];
            float c = (width - 1) * 0.5f;
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                float eased = 1f - (1f - t) * (1f - t);
                float late = Mathf.Clamp01((t - 0.6f) / 0.4f);
                var pixels = new Pixels(width, height);

                // Dust walls, drawn first so the light sits on top of them.
                for (int s = 0; s < 2; s++)
                {
                    int side = s == 0 ? -1 : 1;
                    float front = 10f + eased * 70f;
                    for (int k = 0; k < puffsPerSide; k++)
                    {
                        float x = c + side * (front - puffBack[s, k] * (0.3f + eased));
                        float radius = puffBulk[s, k] * (0.55f + 0.6f * eased) * (1f - 0.45f * late);
                        float y = radius * 0.45f + puffLift[s, k] * eased + t * 3f;
                        pixels.Disc(x, y, radius, DustShade);
                        pixels.Disc(x - side * radius * 0.12f, y + radius * 0.15f, radius * 0.8f, Dust);
                        pixels.Disc(x - side * radius * 0.3f, y + radius * 0.32f, radius * 0.36f, SmokeLight);
                    }
                }

                // Shockwave: the top half of a flat ellipse sweeping out along the floor.
                if (t < 0.7f)
                {
                    float rx = 12f + eased * 74f;
                    float ry = rx * 0.32f;
                    float thickness = Mathf.Lerp(2.2f, 0.9f, t / 0.7f);
                    Color32 ring = f < 3 ? White : Cream;
                    int steps = Mathf.CeilToInt(rx * 4f);
                    for (int i = 0; i <= steps; i++)
                    {
                        float a = i / (float)steps * Mathf.PI;
                        pixels.Disc(c + Mathf.Cos(a) * rx, 1f + Mathf.Sin(a) * ry, thickness * 0.5f, ring);
                    }
                }

                // Impact: a fan of light bursting up and a flash on the floor.
                if (f < 4)
                {
                    float shrink = 1f - f * 0.28f;
                    Color32 burst = f < 2 ? White : Cream;
                    for (int i = 0; i < burstCount; i++)
                    {
                        float inner = 4f + f * 5f;
                        float outer = inner + burstLength[i] * shrink;
                        pixels.Line(c + Mathf.Cos(burstAngle[i]) * inner, 1f + Mathf.Sin(burstAngle[i]) * inner,
                            c + Mathf.Cos(burstAngle[i]) * outer, 1f + Mathf.Sin(burstAngle[i]) * outer,
                            burst, f < 2 ? 1.1f : 0.6f);
                    }
                    pixels.Disc(c, 0f, 14f - f * 3.5f, burst);
                    float crack = 26f + f * 18f;
                    pixels.Line(c - crack, 0.5f, c + crack, 0.5f, f < 2 ? White : Ember, 0.8f);
                }

                // Rubble chunks thrown up on arcs; they vanish once they fall back below the floor.
                for (int i = 0; i < rubbleCount; i++)
                {
                    float x = c + rubbleVx[i] * t;
                    float y = 2f + rubbleVy[i] * t - 100f * t * t;
                    if (y < 0f)
                        continue;
                    pixels.Disc(x, y, i % 3 == 0 ? 1.6f : 1f, Rubble);
                }

                for (int i = 0; i < emberCount; i++)
                {
                    float x = c + emberVx[i] * t;
                    float y = 2f + emberVy[i] * t - 70f * t * t;
                    if (y < 0f)
                        continue;
                    pixels.Disc(x, y, 0.8f, i % 2 == 0 ? Ember : Cream);
                }

                pixels.Dissolve(Mathf.Clamp01((t - 0.5f) / 0.5f));
                frames[f] = pixels.ToSprite(new Vector2(0.5f, 0f));
            }
            return frames;
        }

        // A white-hot flash that blows out into a dense, shaded smoke cloud, then dissolves away in
        // dithered pixels while embers and blood fly off. Irregular on purpose: no rings, no outline.
        private static Sprite[] BuildDeathPuff()
        {
            const int size = 96;
            // 18 frames at 30 fps is 0.6 s.
            const int frameCount = 18;
            const int puffCount = 24;
            const int emberCount = 10;
            var random = new Random(21);

            float[] angle = new float[puffCount];
            float[] reach = new float[puffCount];
            float[] bulk = new float[puffCount];
            float[] delay = new float[puffCount];
            for (int i = 0; i < puffCount; i++)
            {
                angle[i] = (float)random.NextDouble() * Mathf.PI * 2f;
                // Most puffs stay near the middle so the cloud is dense there and ragged at the edge.
                reach[i] = 4f + Mathf.Pow((float)random.NextDouble(), 0.7f) * 28f;
                bulk[i] = 5f + (float)random.NextDouble() * 5f;
                delay[i] = (float)random.NextDouble() * 0.25f;
            }
            float[] emberAngle = new float[emberCount];
            float[] emberSpeed = new float[emberCount];
            for (int i = 0; i < emberCount; i++)
            {
                emberAngle[i] = (float)random.NextDouble() * Mathf.PI * 2f;
                emberSpeed[i] = 0.6f + (float)random.NextDouble() * 0.8f;
            }

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                var pixels = new Pixels(size, size);
                float c = (size - 1) * 0.5f;

                for (int i = 0; i < puffCount; i++)
                {
                    float p = Mathf.Clamp01((t - delay[i]) / (1f - delay[i]));
                    float eased = 1f - (1f - p) * (1f - p);
                    float x = c + Mathf.Cos(angle[i]) * reach[i] * eased;
                    // Squashed vertically and drifting up, the way smoke rises.
                    float y = c + Mathf.Sin(angle[i]) * reach[i] * 0.7f * eased + t * 9f;
                    float radius = bulk[i] * (0.45f + 0.75f * eased) * (1f - 0.45f * Mathf.Clamp01((t - 0.6f) / 0.4f));
                    pixels.Disc(x, y, radius, SmokeShade);
                    pixels.Disc(x + radius * 0.12f, y + radius * 0.14f, radius * 0.82f, Smoke);
                    pixels.Disc(x - radius * 0.28f, y + radius * 0.3f, radius * 0.4f, SmokeLight);
                }

                if (f < 4)
                {
                    float flash = 16f - 4.5f * f;
                    pixels.Disc(c, c, flash, f < 2 ? White : Cream);
                }

                for (int i = 0; i < emberCount; i++)
                {
                    float dist = 6f + (1f - (1f - t) * (1f - t)) * 40f * emberSpeed[i];
                    float x = c + Mathf.Cos(emberAngle[i]) * dist;
                    float y = c + Mathf.Sin(emberAngle[i]) * dist - t * t * 14f;
                    Color32 color = i % 3 == 0 ? Blood : i % 3 == 1 ? Cream : Ember;
                    pixels.Disc(x, y, Mathf.Lerp(1.6f, 0.6f, t), color);
                }

                pixels.Dissolve(Mathf.Clamp01((t - 0.4f) / 0.6f));
                frames[f] = pixels.ToSprite(new Vector2(0.5f, 0.5f), LowerSquash);
            }
            return frames;
        }

        // A low, ragged cloud of dust kicked up behind the feet: overlapping puffs of different sizes
        // that billow, drift back and rise a little, then break up in dithered pixels. The canvas floor
        // is the ground, so puffs sit flat on it instead of floating as separate balls.
        private static Sprite[] BuildDust()
        {
            const int width = 56;
            const int height = 28;
            const int frameCount = 9;
            const int puffCount = 11;
            const float feetX = 46f;
            var random = new Random(5);
            float[] back = new float[puffCount];
            float[] lift = new float[puffCount];
            float[] bulk = new float[puffCount];
            float[] delay = new float[puffCount];
            float[] drift = new float[puffCount];
            for (int i = 0; i < puffCount; i++)
            {
                // Bunched near the feet, thinning out further back.
                back[i] = Mathf.Pow((float)random.NextDouble(), 0.8f) * 26f;
                lift[i] = (float)random.NextDouble() * 4f;
                bulk[i] = 2.2f + (float)random.NextDouble() * 2.8f;
                delay[i] = (float)random.NextDouble() * 0.2f;
                drift[i] = 3f + (float)random.NextDouble() * 7f;
            }

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                var pixels = new Pixels(width, height);
                for (int i = 0; i < puffCount; i++)
                {
                    float p = Mathf.Clamp01((t - delay[i]) / (1f - delay[i]));
                    float eased = 1f - (1f - p) * (1f - p);
                    float x = feetX - back[i] * (0.4f + 0.6f * eased) - drift[i] * eased;
                    float y = bulk[i] * 0.3f + (bulk[i] * 0.5f + lift[i]) * eased + t * 3f;
                    float radius = bulk[i] * (0.5f + 0.7f * eased) * (1f - 0.4f * Mathf.Clamp01((t - 0.6f) / 0.4f));
                    pixels.Disc(x, y, radius, DustShade);
                    pixels.Disc(x + radius * 0.1f, y + radius * 0.15f, radius * 0.8f, Dust);
                    pixels.Disc(x - radius * 0.25f, y + radius * 0.3f, radius * 0.38f, SmokeLight);
                }
                pixels.Dissolve(Mathf.Clamp01((t - 0.35f) / 0.65f));
                frames[f] = pixels.ToSprite(new Vector2(feetX / width, 0f));
            }
            return frames;
        }

        private static Color32 WithAlpha(Color32 color, float alpha) =>
            new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha)));

        private sealed class Pixels
        {
            private static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

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

            // Removes pixels in an ordered-dither pattern: 0 keeps everything, 1 clears everything.
            // Keeps the look crisp, unlike a soft alpha fade.
            public void Dissolve(float amount)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        int index = y * _width + x;
                        if (_data[index].a == 0)
                            continue;
                        float threshold = (Bayer[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;
                        if (threshold < amount)
                            _data[index] = default;
                    }
                }
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

            // A pointed lens (thick in the middle, tapering to sharp tips) centred on (cx, cy), lying
            // along angle. halfWidth is the half thickness at the middle.
            public void Lens(float cx, float cy, float angle, float length, float halfWidth, Color32 color)
            {
                float dx = Mathf.Cos(angle);
                float dy = Mathf.Sin(angle);
                float half = length * 0.5f;
                for (float s = -half; s <= half; s += 0.5f)
                {
                    float k = s / half;
                    float w = halfWidth * (1f - k * k);
                    for (float u = -w; u <= w; u += 0.5f)
                        Plot(Mathf.RoundToInt(cx + dx * s - dy * u), Mathf.RoundToInt(cy + dy * s + dx * u), color);
                }
            }

            // thickness is the brush radius; below 0.5 it draws single pixels.
            public void Line(float x0, float y0, float x1, float y1, Color32 color, float thickness)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 2f));
                for (int i = 0; i <= steps; i++)
                {
                    float k = i / (float)steps;
                    float x = Mathf.Lerp(x0, x1, k);
                    float y = Mathf.Lerp(y0, y1, k);
                    if (thickness < 0.5f)
                        Plot(Mathf.RoundToInt(x), Mathf.RoundToInt(y), color);
                    else
                        Disc(x, y, thickness, color);
                }
            }

            // lowerScale below 1 squashes everything under the centre row toward it, so a centred effect
            // reaches less far down.
            public Sprite ToSprite(Vector2 pivot, float lowerScale = 1f)
            {
                Color32[] data = lowerScale < 1f ? SquashLower(lowerScale) : _data;
                var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave,
                };
                texture.SetPixels32(data);
                texture.Apply();
                var sprite = Sprite.Create(texture, new Rect(0, 0, _width, _height), pivot, PixelsPerUnit);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                s_owned.Add(texture);
                s_owned.Add(sprite);
                return sprite;
            }

            private Color32[] SquashLower(float scale)
            {
                var result = new Color32[_data.Length];
                float center = (_height - 1) * 0.5f;
                for (int y = 0; y < _height; y++)
                {
                    float source = y < center ? center - (center - y) / scale : y;
                    int sourceRow = Mathf.RoundToInt(source);
                    if (sourceRow < 0 || sourceRow >= _height)
                        continue;
                    System.Array.Copy(_data, sourceRow * _width, result, y * _width, _width);
                }
                return result;
            }
        }
    }
}
