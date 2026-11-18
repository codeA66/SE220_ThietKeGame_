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
        // Blood red, matching the blood drops.
        private static readonly Color32 Blood = new Color32(138, 6, 12, 255);
        // Dust clouds use the scheme sampled from the imported Dead Cells landing cloud (fx_land): mostly
        // pure white with bright sky-blue accents and ice-blue tints in between.
        private static readonly Color32 DustLight = new Color32(255, 255, 255, 255);
        // The colour of the empty pixels around dust, and how strongly its fade removes dark pixels first.
        private static readonly Color32 DustBleed = new Color32(235, 248, 255, 255);
        private const float DustLightBias = 3f;
        private static readonly Color32 Dust = new Color32(220, 243, 255, 255);
        private static readonly Color32 DustShade = new Color32(108, 211, 255, 255);
        private static readonly Color32 Smoke = new Color32(214, 210, 205, 255);
        private static readonly Color32 SmokeShade = new Color32(150, 144, 150, 255);
        private static readonly Color32 SmokeLight = new Color32(244, 242, 238, 255);
        private static readonly Color32 Gold = new Color32(255, 214, 90, 255);
        private static readonly Color32 BloodDark = new Color32(80, 3, 8, 255);
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
        // Dust is drawn several times with different seeds; one is picked at random each time it plays.
        private const int DustVariants = 4;
        private static readonly VfxDefinition[] s_runDust = new VfxDefinition[DustVariants];
        private static readonly VfxDefinition[] s_rollDust = new VfxDefinition[DustVariants];
        private static readonly VfxDefinition[] s_ledgeDust = new VfxDefinition[DustVariants];
        private static VfxDefinition s_critSpark;
        private static VfxDefinition s_bloodMist;
        private static VfxDefinition s_ascendBeam;
        private static VfxDefinition s_shieldClang;
        private static VfxDefinition s_muzzleFlash;
        private static VfxDefinition s_muzzleFlashHeavy;
        private static VfxDefinition s_arrowImpact;
        private static VfxDefinition s_arrowImpactHeavy;

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
            System.Array.Clear(s_runDust, 0, DustVariants);
            System.Array.Clear(s_rollDust, 0, DustVariants);
            System.Array.Clear(s_ledgeDust, 0, DustVariants);
            s_critSpark = null;
            s_bloodMist = null;
            s_ascendBeam = null;
            s_shieldClang = null;
            s_muzzleFlash = null;
            s_muzzleFlashHeavy = null;
            s_arrowImpact = null;
            s_arrowImpactHeavy = null;
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

        // The ascension beam played when the player finishes standing up. It draws behind the character.
        public static VfxDefinition AscendBeam
        {
            get
            {
                if (!IsUsable(s_ascendBeam))
                    s_ascendBeam = Register(VfxDefinition.Create("Vfx_AscendBeam", BuildAscendBeam(), Fps, 1f, -1, 1).AsUnlit());
                return s_ascendBeam;
            }
        }

        // Bow release flash, drawn firing right. The heavy one is for the combo finishing shot.
        public static VfxDefinition MuzzleFlash
        {
            get
            {
                if (!IsUsable(s_muzzleFlash))
                    s_muzzleFlash = Register(VfxDefinition.Create("Vfx_MuzzleFlash", BuildMuzzleFlash(1f, Cream), Fps, 1f, 6, 2));
                return s_muzzleFlash;
            }
        }

        public static VfxDefinition MuzzleFlashHeavy
        {
            get
            {
                if (!IsUsable(s_muzzleFlashHeavy))
                    s_muzzleFlashHeavy = Register(VfxDefinition.Create("Vfx_MuzzleFlashHeavy", BuildMuzzleFlash(1.5f, Gold), Fps, 1f, 6, 1));
                return s_muzzleFlashHeavy;
            }
        }

        // Arrow hitting a wall or floor: debris bursts away from the surface. Drawn bursting right, so
        // flip it when the arrow was flying right.
        public static VfxDefinition ArrowImpact
        {
            get
            {
                if (!IsUsable(s_arrowImpact))
                    s_arrowImpact = Register(VfxDefinition.Create("Vfx_ArrowImpact", BuildArrowImpact(1f, 13), Fps, 1f, 6, 3));
                return s_arrowImpact;
            }
        }

        public static VfxDefinition ArrowImpactHeavy
        {
            get
            {
                if (!IsUsable(s_arrowImpactHeavy))
                    s_arrowImpactHeavy = Register(VfxDefinition.Create("Vfx_ArrowImpactHeavy", BuildArrowImpact(1.6f, 29), Fps, 1f, 6, 1));
                return s_arrowImpactHeavy;
            }
        }

        // A weapon or arrow ringing off a shield. Drawn for an attacker on the right; flip it for the left.
        public static VfxDefinition ShieldClang
        {
            get
            {
                if (!IsUsable(s_shieldClang))
                    s_shieldClang = Register(VfxDefinition.Create("Vfx_ShieldClang", BuildShieldClang(), Fps, 1f, 11, 2));
                return s_shieldClang;
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

        // The dust getters return a randomly chosen variant on every call, so no two puffs are drawn alike.
        // Puffs trail to the left of the feet; flip the effect when facing left.
        public static VfxDefinition RunDust =>
            Variant(s_runDust, i => VfxDefinition.Create($"Vfx_RunDust{i}", BuildDust(5 + i * 17), Fps, 1f, 1, 2));

        // A small puff for getting up onto a ledge: the run dust at about half size. Trails to the left.
        public static VfxDefinition LedgeDust =>
            Variant(s_ledgeDust, i => VfxDefinition.Create($"Vfx_LedgeDust{i}", BuildDust(41 + i * 11), Fps, 0.55f, 1, 1));

        public static VfxDefinition RollDust =>
            Variant(s_rollDust, i => VfxDefinition.Create($"Vfx_RollDust{i}", BuildRollDust(77 + i * 13), Fps, 1f, 1, 1));

        private static VfxDefinition Variant(VfxDefinition[] pool, System.Func<int, VfxDefinition> create)
        {
            int index = UnityEngine.Random.Range(0, pool.Length);
            if (!IsUsable(pool[index]))
                pool[index] = Register(create(index));
            return pool[index];
        }

        // Shield clang, drawn for an attacker on the right (flip it for the left): a sharp lens of light
        // standing along the shield face with a short cross glint and diagonal glints, a hot white core, and
        // thin rays fanning out toward the attacker. Pale white and cream, then it breaks up in dithered
        // pixels. The physical sparks and their glow play on top of it.
        private static Sprite[] BuildShieldClang()
        {
            const int size = 72;
            const int frameCount = 7;
            const int rayCount = 7;
            var random = new Random(97);
            float[] rayAngle = new float[rayCount];
            float[] rayLength = new float[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                float spread = Mathf.Lerp(-60f, 60f, i / (float)(rayCount - 1));
                rayAngle[i] = (spread + ((float)random.NextDouble() - 0.5f) * 8f) * Mathf.Deg2Rad;
                // The middle rays, straight at the attacker, run longest.
                rayLength[i] = (11f + 7f * (1f - Mathf.Abs(spread) / 60f) + (float)random.NextDouble() * 4f);
            }

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                var pixels = new Pixels(size, size);
                float c = (size - 1) * 0.5f;

                if (f < 5)
                {
                    Color32 rayColor = f < 2 ? White : Cream;
                    for (int i = 0; i < rayCount; i++)
                    {
                        float inner = 6f + t * 26f;
                        float outer = inner + rayLength[i] * (1f - t * 0.6f);
                        pixels.Line(c + Mathf.Cos(rayAngle[i]) * inner, c + Mathf.Sin(rayAngle[i]) * inner,
                            c + Mathf.Cos(rayAngle[i]) * outer, c + Mathf.Sin(rayAngle[i]) * outer, rayColor, 0f);
                    }
                }

                if (f < 4)
                {
                    float length = 46f - f * 8f;
                    float halfWidth = Mathf.Max(0.6f, 3.2f - f * 0.8f);
                    pixels.Lens(c, c, Mathf.PI * 0.5f, length, halfWidth, f == 0 ? White : Cream);
                }
                if (f < 3)
                    pixels.Lens(c, c, 0f, 26f - f * 6f, 2f - f * 0.5f, White);
                if (f < 2)
                {
                    pixels.Lens(c, c, Mathf.PI * 0.25f, 22f, 1.3f, White);
                    pixels.Lens(c, c, -Mathf.PI * 0.25f, 22f, 1.3f, White);
                }
                if (f < 3)
                    pixels.Disc(c, c, 6.5f - f * 2.2f, White);

                pixels.Dissolve(Mathf.Clamp01((t - 0.5f) / 0.5f));
                frames[f] = pixels.ToSprite(new Vector2(0.5f, 0.5f));
            }
            return frames;
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

        // Bow release, drawn firing right (flip it for the left): a lens of light shooting out of the bow
        // with a cross glint, rays of light fanning out ahead in a wide cone, and a puff of dust kicked
        // back. scale grows the whole thing for the combo finisher, which also burns gold, not cream.
        private static Sprite[] BuildMuzzleFlash(float scale, Color32 warm)
        {
            const int frameCount = 6;
            const int rayCount = 5;
            const float coneHalfAngle = 60f;
            int width = Mathf.CeilToInt(8 + 46 * scale);
            int height = Mathf.CeilToInt(88 * scale);
            float originX = 8f;
            float originY = (height - 1) * 0.5f;
            var random = new Random(53);
            float[] rayAngle = new float[rayCount];
            float[] rayLength = new float[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                // Spread evenly across the cone with a little jitter; the middle rays run longest.
                float spread = Mathf.Lerp(-coneHalfAngle, coneHalfAngle, i / (float)(rayCount - 1));
                rayAngle[i] = (spread + ((float)random.NextDouble() - 0.5f) * 8f) * Mathf.Deg2Rad;
                rayLength[i] = (7f + 6f * (1f - Mathf.Abs(spread) / coneHalfAngle) + (float)random.NextDouble() * 3f) * scale;
            }

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                float travel = 1f - (1f - t) * (1f - t);
                var pixels = new Pixels(width, height);

                for (int k = 0; k < 3; k++)
                {
                    float x = originX - t * 8f * scale - k * 2f;
                    float y = originY + (k - 1) * 3f * scale + t * 3f;
                    float radius = (1.5f + t * 1.8f) * scale;
                    DustWisp(pixels, x, y, radius, -(0.1f + t * 0.25f));
                }

                if (f < 5)
                {
                    Color32 rayColor = f < 2 ? White : warm;
                    for (int i = 0; i < rayCount; i++)
                    {
                        float inner = (5f + travel * 24f) * scale;
                        float outer = inner + rayLength[i] * (1f - t * 0.7f);
                        pixels.Line(originX + Mathf.Cos(rayAngle[i]) * inner, originY + Mathf.Sin(rayAngle[i]) * inner,
                            originX + Mathf.Cos(rayAngle[i]) * outer, originY + Mathf.Sin(rayAngle[i]) * outer,
                            rayColor, f < 1 ? 0.6f : 0f);
                    }
                }

                if (f < 3)
                {
                    float length = (16f + f * 8f) * scale;
                    float halfWidth = Mathf.Max(0.6f, (3f - f) * scale);
                    pixels.Lens(originX + length * 0.5f, originY, 0f, length, halfWidth, f == 0 ? White : warm);
                }
                if (f < 2)
                {
                    pixels.Lens(originX, originY, Mathf.PI * 0.5f, 10f * scale, 1.5f * scale, White);
                    pixels.Disc(originX, originY, (3.5f - f * 1.5f) * scale, White);
                }

                pixels.Dissolve(Mathf.Clamp01((t - 0.5f) / 0.5f));
                frames[f] = pixels.ToSprite(new Vector2(originX / width, originY / height));
            }
            return frames;
        }

        // An arrow striking stone, drawn with the surface on the left and debris bursting right (flip it
        // when the arrow was flying right): a flash, sparks cooling from white to ember, stone chips
        // falling under gravity and a little dust. scale grows it for the combo finisher.
        private static Sprite[] BuildArrowImpact(float scale, int seed)
        {
            int width = Mathf.CeilToInt(48 * scale);
            int height = Mathf.CeilToInt(40 * scale);
            float originX = 6f;
            float originY = (height - 1) * 0.5f;
            const int frameCount = 7;
            int sparkCount = Mathf.RoundToInt(7 * scale);
            int chipCount = Mathf.RoundToInt(4 * scale);
            var random = new Random(seed);
            float[] sparkAngle = new float[sparkCount];
            float[] sparkSpeed = new float[sparkCount];
            for (int i = 0; i < sparkCount; i++)
            {
                sparkAngle[i] = Mathf.Lerp(-70f, 70f, (float)random.NextDouble()) * Mathf.Deg2Rad;
                sparkSpeed[i] = (10f + (float)random.NextDouble() * 12f) * scale;
            }
            float[] chipAngle = new float[chipCount];
            float[] chipSpeed = new float[chipCount];
            for (int i = 0; i < chipCount; i++)
            {
                chipAngle[i] = Mathf.Lerp(-30f, 60f, (float)random.NextDouble()) * Mathf.Deg2Rad;
                chipSpeed[i] = (8f + (float)random.NextDouble() * 10f) * scale;
            }

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                float travel = 1f - (1f - t) * (1f - t);
                var pixels = new Pixels(width, height);

                for (int k = 0; k < 2; k++)
                {
                    float radius = (2f + t * 2.5f) * scale;
                    float x = originX + 2f + k * 3f * scale + t * 3f;
                    float y = originY + (k == 0 ? -2f : 2f) * scale + t * 3f;
                    DustWisp(pixels, x, y, radius, 0.15f + t * 0.3f);
                }

                for (int i = 0; i < chipCount; i++)
                {
                    float x = originX + Mathf.Cos(chipAngle[i]) * chipSpeed[i] * travel;
                    float y = originY + Mathf.Sin(chipAngle[i]) * chipSpeed[i] * travel - t * t * 14f;
                    pixels.Disc(x, y, i % 2 == 0 ? 1.3f : 0.8f, Rubble);
                }

                Color32 sparkColor = f < 2 ? White : f < 4 ? Cream : Ember;
                for (int i = 0; i < sparkCount; i++)
                {
                    float head = 2f + travel * sparkSpeed[i];
                    float tail = Mathf.Max(0f, head - Mathf.Lerp(7f, 1.5f, t) * scale);
                    float drop = t * t * 6f;
                    pixels.Line(originX + Mathf.Cos(sparkAngle[i]) * tail, originY + Mathf.Sin(sparkAngle[i]) * tail - drop,
                        originX + Mathf.Cos(sparkAngle[i]) * head, originY + Mathf.Sin(sparkAngle[i]) * head - drop,
                        sparkColor, f < 2 ? 0.6f : 0f);
                }

                if (f < 3)
                    pixels.Disc(originX, originY, (5f - f * 2f) * scale, White);

                pixels.Dissolve(Mathf.Clamp01((t - 0.5f) / 0.5f));
                frames[f] = pixels.ToSprite(new Vector2(originX / width, originY / height));
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

        // The ascension beam when the player finishes standing up: a solid column of white straight down from the sky
        // to the feet. It slams in thin, swells to full width with a ragged flickering edge, holds, then narrows away,
        // with a flat flash along the floor where it lands. Plain white only. Drawn behind the character. Pivot at the feet.
        private static Sprite[] BuildAscendBeam()
        {
            const int width = 112;
            const int height = 300;
            const int frameCount = 9;
            const int baseY = 6;
            // Half width of the column per frame (px): slam in, full, hold, narrow.
            float[] half = { 4f, 16f, 16f, 16f, 16f, 16f, 16f, 10f, 3f };
            int cx = width / 2;
            var white = DustLight;

            var frames = new Sprite[frameCount];
            var random = new System.Random(77);
            for (int f = 0; f < frameCount; f++)
            {
                var pixels = new Pixels(width, height);
                for (int y = baseY; y < height; y++)
                {
                    // Ragged edge: each row wobbles a pixel or two so the column shimmers from frame to frame.
                    int wobble = 0;
                    int h = Mathf.Max(1, Mathf.RoundToInt(half[f]) + wobble);
                    // Slight bloom: one thin translucent white halo hugging the solid rectangular core.
                    // Slightly brighter near the floor where the light is most intense.
                    float intensity = 0.75f + 0.25f * (1f - (y - baseY) / (float)(height - baseY));
                    int[] reach = { 5 };
                    byte[] alpha = { 90 };
                    for (int layer = 0; layer < reach.Length; layer++)
                    {
                        int hh = h + reach[layer];
                        var halo = new Color32(255, 255, 255, (byte)(alpha[layer] * intensity));
                        for (int x = cx - hh; x < cx + hh; x++)
                            pixels.Plot(x, y, halo);
                    }
                    for (int x = cx - h; x < cx + h; x++)
                        pixels.Plot(x, y, white);
                }
                // The flash along the floor at the foot of the column.
                if (f <= 4)
                {
                    float reach = (f == 0 ? 12f : 24f) - f * 2f;
                    pixels.Lens(cx, baseY, 0f, reach * 2f, Mathf.Max(1f, 3.2f - f * 0.5f), white);
                }
                frames[f] = pixels.ToSprite(new Vector2(0.5f, baseY / (float)height));
            }
            return frames;
        }

        // Roll dust, drawn for a roll going right so the cloud trails to the left (flip it for the left): a few
        // big blades sweep up and back out of the feet, and a row of smaller ones peels off along the floor
        // behind, each one starting a little later than the one before it. Flat white with a blue underside.
        private static Sprite[] BuildRollDust(int seed)
        {
            const int width = 112;
            const int height = 60;
            const float feetX = 92f;
            const int frameCount = 12;
            const int trailCount = 6;
            const int burstCount = 3;
            var random = new Random(seed);

            var trail = new float[trailCount, 8];
            for (int i = 0; i < trailCount; i++)
            {
                float along = i / (float)(trailCount - 1);
                trail[i, 0] = feetX - (6f + along * 52f);                        // base x
                trail[i, 1] = 1.5f;                                               // base y
                trail[i, 2] = 0.05f + (float)random.NextDouble() * 0.22f;         // elevation
                trail[i, 3] = (16f + (float)random.NextDouble() * 14f);           // length
                trail[i, 4] = (3f + (float)random.NextDouble() * 1.6f) * Mathf.Lerp(1f, 0.65f, along); // thickness
                trail[i, 5] = 0.35f + (float)random.NextDouble() * 0.35f;          // curl
                trail[i, 6] = along * 0.35f;                                      // delay
                trail[i, 7] = 6f + (float)random.NextDouble() * 8f;               // reach
            }
            var burst = new float[burstCount, 8];
            for (int i = 0; i < burstCount; i++)
            {
                burst[i, 0] = feetX - (float)random.NextDouble() * 6f;
                burst[i, 1] = 2f + (float)random.NextDouble() * 3f;
                burst[i, 2] = 0.06f + i * 0.1f + (float)random.NextDouble() * 0.05f;
                burst[i, 3] = 24f + (float)random.NextDouble() * 10f;
                burst[i, 4] = 4f + (float)random.NextDouble() * 2.5f;
                burst[i, 5] = 0.5f + (float)random.NextDouble() * 0.25f;
                burst[i, 6] = (float)random.NextDouble() * 0.06f;
                burst[i, 7] = 10f + (float)random.NextDouble() * 8f;
            }

            var frames = new Sprite[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                var pixels = new Pixels(width, height);
                for (int i = 0; i < trailCount; i++)
                    AgedBlade(pixels, t, trail[i, 6], trail[i, 0], trail[i, 1], -1, trail[i, 2], trail[i, 3], trail[i, 4], trail[i, 5], trail[i, 7], 2f, seed * 31 + i, 0.45f);
                for (int i = 0; i < burstCount; i++)
                    AgedBlade(pixels, t, burst[i, 6], burst[i, 0], burst[i, 1], -1, burst[i, 2], burst[i, 3], burst[i, 4], burst[i, 5], burst[i, 7], 3f, seed * 17 + 100 + i, 0.45f);
                DustSpecks(pixels, t, feetX - 4f, 4f, -1, 8, seed);
                frames[f] = pixels.ToSprite(new Vector2(feetX / width, 0f));
            }
            return frames;
        }

        // A small puff for the run and the ledge: three or four little blades trailing left, same style.
        private static Sprite[] BuildDust(int seed)
        {
            const int width = 72;
            const int height = 38;
            const float feetX = 62f;
            const int frameCount = 9;
            const int bladeCount = 4;
            var random = new Random(seed);
            var frames = new Sprite[frameCount];

            var blades = new float[bladeCount, 8];
            for (int i = 0; i < bladeCount; i++)
            {
                blades[i, 0] = feetX - (float)random.NextDouble() * 10f;
                blades[i, 1] = 1.5f + (float)random.NextDouble() * 2.5f;
                blades[i, 2] = 0.1f + (float)random.NextDouble() * 0.45f;
                blades[i, 3] = 12f + (float)random.NextDouble() * 14f;
                blades[i, 4] = 2.2f + (float)random.NextDouble() * 1.8f;
                blades[i, 5] = 0.6f + (float)random.NextDouble() * 0.5f;
                blades[i, 6] = (float)random.NextDouble() * 0.18f;
                blades[i, 7] = 5f + (float)random.NextDouble() * 8f;
            }

            for (int f = 0; f < frameCount; f++)
            {
                float t = f / (float)(frameCount - 1);
                var pixels = new Pixels(width, height);
                for (int i = 0; i < bladeCount; i++)
                    AgedBlade(pixels, t, blades[i, 6], blades[i, 0], blades[i, 1], -1, blades[i, 2], blades[i, 3], blades[i, 4], blades[i, 5], blades[i, 7], 4f, seed * 13 + i);
                DustSpecks(pixels, t, feetX - 3f, 3f, -1, 4, seed);
                frames[f] = pixels.ToSprite(new Vector2(feetX / width, 0f));
            }
            return frames;
        }

        // Dead Cells-style dust is drawn flat: a few big solid shapes, no gradients, no dithering. A blade is a
        // swooping crescent of pure white with one thin flat sky-blue sliver along its underside, tapering to a
        // point at both ends and curling upward toward its tip (this is what the landing cloud is made of).
        // facing is the way it points (-1 left, 1 right); elevation is how far above horizontal it starts
        // (radians); curl is how far it bends upward over its length (radians). pieces splits it into that many
        // separate fragments, which is how a blade breaks up as it fades. The fragments are uneven on purpose:
        // each gets its own share of the length and its own thickness, the gaps differ, and now and then a
        // fragment drops out completely. seed keeps that the same on every frame of one blade. scatterUp scales
        // how far the fragments of a breaking blade drift upward and tiltScale how far they tilt (1 = the default).
        private static void DrawBlade(Pixels pixels, float x, float y, int facing, float elevation, float length,
            float thickness, float curl, int pieces, float gap, int seed, float scatterUp = 1f, float tiltScale = 1f)
        {
            if (thickness < 0.6f || length < 2f)
                return;
            const float step = 0.5f;
            var random = new Random(seed);
            var share = new float[pieces];
            var fatness = new float[pieces];
            var dropped = new bool[pieces];
            var gapShare = new float[pieces];
            var scatter = new Vector2[pieces];
            var tilt = new float[pieces];
            float shareTotal = 0f;
            float gapTotal = 0f;
            for (int i = 0; i < pieces; i++)
            {
                share[i] = 0.5f + (float)random.NextDouble() * 1.2f;
                fatness[i] = pieces == 1 ? 1f : 0.4f + (float)random.NextDouble() * 0.7f;
                dropped[i] = pieces >= 3 && i > 0 && random.NextDouble() < 0.28;
                gapShare[i] = 0.5f + (float)random.NextDouble() * 1.1f;
                // Fragments drift off the original curve: mostly up, some sideways, each tilted its own way.
                // The first one stays put as the anchor, so the break-up reads as pieces flying off it.
                scatter[i] = i == 0 ? Vector2.zero : new Vector2(((float)random.NextDouble() - 0.5f) * 1.2f, (0.2f + (float)random.NextDouble() * 0.9f) * scatterUp);
                tilt[i] = i == 0 ? 0f : ((float)random.NextDouble() - 0.5f) * 1.6f * tiltScale;
                shareTotal += share[i];
                if (i < pieces - 1)
                    gapTotal += gapShare[i];
            }

            float gapUnit = gap * length / Mathf.Max(1f, gapTotal + pieces);
            float usable = length - gapUnit * gapTotal;
            float travelled = 0f;
            var position = new Vector2(x, y);
            for (int piece = 0; piece < pieces; piece++)
            {
                float pieceLength = usable * share[piece] / shareTotal;
                Vector2 start = position;
                Vector2 drift = scatter[piece] * (gap * length * 0.45f);
                float cosTilt = Mathf.Cos(tilt[piece] * gap);
                float sinTilt = Mathf.Sin(tilt[piece] * gap);
                for (float s = 0f; s <= pieceLength; s += step)
                {
                    float u = s / pieceLength;
                    float phi = elevation + curl * ((travelled + s) / length);
                    var direction = new Vector2(facing * Mathf.Cos(phi), Mathf.Sin(phi));
                    // Always the upper side, whichever way the blade points.
                    var normal = new Vector2(-direction.y, direction.x) * facing;
                    float width = thickness * fatness[piece] * Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.75f));
                    if (width >= 0.3f && !dropped[piece])
                    {
                        for (float v = -width; v <= width; v += 0.5f)
                        {
                            bool underside = v < -width * 0.55f && u > 0.15f && u < 0.8f;
                            Vector2 point = position + normal * v;
                            Vector2 local = point - start;
                            point = start + new Vector2(local.x * cosTilt - local.y * sinTilt, local.x * sinTilt + local.y * cosTilt) + drift;
                            pixels.Plot(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), underside ? DustShade : DustLight);
                        }
                    }
                    position += direction * step;
                }

                float gapLength = piece < pieces - 1 ? gapUnit * gapShare[piece] : 0f;
                float gapPhi = elevation + curl * ((travelled + pieceLength + gapLength * 0.5f) / length);
                position += new Vector2(facing * Mathf.Cos(gapPhi), Mathf.Sin(gapPhi)) * gapLength;
                travelled += pieceLength + gapLength;
            }
        }

        // The fade schedule of one blade, different for every seed so no two fade alike: some vanish early and
        // some linger, they start thinning at different times and at different speeds, and they break into
        // fragments at different moments (up to three) that scatter at different rates. progress is the
        // blade's own 0-1 time. Returns false once the blade is gone.
        private static bool BladeFade(float progress, int seed, out float thin, out int pieces, out float gap)
        {
            var random = new Random(seed * 7 + 3);
            float span = 0.65f + (float)random.NextDouble() * 0.35f;
            float thinFrom = 0.1f + (float)random.NextDouble() * 0.35f;
            float thinPower = 0.7f + (float)random.NextDouble() * 1.3f;
            float breakOne = 0.25f + (float)random.NextDouble() * 0.3f;
            float breakTwo = breakOne + 0.08f + (float)random.NextDouble() * 0.2f;
            float gapRate = 0.3f + (float)random.NextDouble() * 0.7f;

            thin = 0f;
            pieces = 1;
            gap = 0f;
            float life = progress / span;
            if (life >= 1f)
                return false;
            thin = 1f - Mathf.Pow(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((life - thinFrom) / (1f - thinFrom))), thinPower);
            pieces = life < breakOne ? 1 : life < breakTwo ? 2 : 3;
            gap = life < breakOne ? 0f : 0.2f + (life - breakOne) * gapRate;
            return true;
        }

        // A blade over its life: it sweeps out and up as it grows, then fades by its own schedule. t is the
        // cloud's time (0-1); delay holds the blade back until its turn.
        private static void AgedBlade(Pixels pixels, float t, float delay, float baseX, float baseY, int facing,
            float elevation, float length, float thickness, float curl, float reach, float rise, int seed, float scatterUp = 1f)
        {
            if (t < delay)
                return;
            float life = Mathf.Clamp01((t - delay) / (1f - delay));
            if (!BladeFade(life, seed, out float thin, out int pieces, out float gap))
                return;
            float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01(life / 0.4f), 2f);
            float travel = 1f - Mathf.Pow(1f - life, 2f);
            DrawBlade(pixels, baseX + facing * reach * travel, baseY + rise * travel, facing, elevation,
                length * (0.2f + 0.8f * grow), thickness * thin, curl * (0.6f + 0.8f * life), pieces, gap, seed, scatterUp);
        }

        // A few single-pixel flecks thrown out of a cloud, white with the odd blue one, like the reference.
        // Each one lives for its own time, so they wink out one by one instead of all together.
        private static void DustSpecks(Pixels pixels, float t, float originX, float originY, int facing, int count, int seed)
        {
            var random = new Random(seed);
            float ease = 1f - (1f - t) * (1f - t);
            for (int i = 0; i < count; i++)
            {
                float angle = 0.1f + (float)random.NextDouble() * 1.1f;
                float speed = 12f + (float)random.NextDouble() * 22f;
                float lifetime = 0.3f + (float)random.NextDouble() * 0.65f;
                float x = originX + facing * Mathf.Cos(angle) * speed * ease;
                float y = originY + Mathf.Sin(angle) * speed * ease - t * t * 8f;
                if (y < 0f || t > lifetime)
                    continue;
                pixels.Plot(Mathf.RoundToInt(x), Mathf.RoundToInt(y), i % 3 == 0 ? DustShade : DustLight);
            }
        }

        // One wisp of dust: a long, flat, tapered streak with pointed tips instead of a round blob, shaded
        // like the landing cloud (sky-blue underside, ice-blue body, white highlight along the top).
        // radius sets the size. angle tilts it in radians, counter-clockwise from horizontal: a positive angle
        // rises toward the right, so clouds that trail to the left (behind the player) use a negative one.
        private static void DustWisp(Pixels pixels, float x, float y, float radius, float angle)
        {
            float length = radius * 3.4f;
            float half = Mathf.Max(0.7f, radius * 0.4f);
            pixels.Lens(x, y - half * 0.3f, angle, length, half, DustShade);
            pixels.Lens(x + length * 0.04f, y + half * 0.05f, angle, length * 0.9f, half * 0.72f, Dust);
            pixels.Lens(x - length * 0.08f, y + half * 0.4f, angle, length * 0.55f, half * 0.3f, DustLight);
        }


        private static Color32 WithAlpha(Color32 color, float alpha) =>
            new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha)));

        private sealed class Pixels
        {
            private static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            private readonly int _width;
            private readonly int _height;
            private readonly Color32[] _data;

            // background is the colour of the empty (fully transparent) pixels. Transparent pixels still carry
            // a colour, and anything that blends them with their neighbours (scaling, filtering) would darken
            // the edges if that colour were black, so light effects pass a light one.
            public Pixels(int width, int height, Color32 background = default)
            {
                _width = width;
                _height = height;
                _data = new Color32[width * height];
                if (background.r != 0 || background.g != 0 || background.b != 0)
                {
                    var clear = new Color32(background.r, background.g, background.b, 0);
                    for (int i = 0; i < _data.Length; i++)
                        _data[i] = clear;
                }
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
            // Keeps the look crisp, unlike a soft alpha fade. A removed pixel keeps its colour and only loses
            // its alpha. lightBias above 0 removes darker pixels sooner, so what is left of a fading cloud
            // stays light instead of drifting toward its shadow colour.
            public void Dissolve(float amount, float lightBias = 0f)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        int index = y * _width + x;
                        Color32 pixel = _data[index];
                        if (pixel.a == 0)
                            continue;
                        float darkness = 1f - (0.3f * pixel.r + 0.59f * pixel.g + 0.11f * pixel.b) / 255f;
                        float threshold = (Bayer[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;
                        if (threshold < amount * (1f + lightBias * darkness))
                            _data[index] = new Color32(pixel.r, pixel.g, pixel.b, 0);
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
