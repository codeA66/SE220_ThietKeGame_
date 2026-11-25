using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Core.Audio
{
    /// <summary>
    /// Deterministic mono 22050 Hz PCM synthesizer and cache for all 21 gameplay SFX and the dungeon ambient loop.
    /// </summary>
    public static class ProceduralSfxLibrary
    {
        public const int SampleRate = 22050;
        private const float AmbientDurationSeconds = 4.0f;

        private static readonly Dictionary<SfxId, AudioClip> Clips = new();
        private static AudioClip _ambientClip;

        public static AudioClip GetClip(SfxId id)
        {
            if (Clips.TryGetValue(id, out var cached) && cached != null)
                return cached;

            var clip = SynthesizeSfx(id);
            Clips[id] = clip;
            return clip;
        }

        public static AudioClip GetAmbientLoopClip()
        {
            if (_ambientClip != null)
                return _ambientClip;

            int samplesCount = Mathf.RoundToInt(SampleRate * AmbientDurationSeconds);
            float[] data = new float[samplesCount];
            uint rng = 0x51A7BEEFu;
            float lpNoise = 0f;

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / SampleRate;
                // Integer cycles over 4.0s (k * 0.25 Hz) guarantee a seamless loop boundary.
                float lfo = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                float sub = Mathf.Sin(2f * Mathf.PI * 55.0f * t) * 0.35f;
                float fifth = Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.20f * lfo;
                float octave = Mathf.Sin(2f * Mathf.PI * 110.0f * t) * 0.12f * (0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * 0.25f * t));

                float white = NextSigned(ref rng);
                lpNoise += 0.015f * (white - lpNoise);
                float breath = lpNoise * 0.18f * lfo;

                // Fade noise component cleanly at loop seam while keeping harmonic drones continuous.
                float seamFade = Mathf.Sin(Mathf.PI * ((float)i / samplesCount));
                data[i] = Mathf.Clamp(sub + fifth + octave + breath * seamFade, -0.8f, 0.8f);
            }

            _ambientClip = CreateMonoClip("Ambient_DungeonDrone", data);
            return _ambientClip;
        }

        private static AudioClip SynthesizeSfx(SfxId id)
        {
            return id switch
            {
                SfxId.SwordSlash1 => BuildSweepNoise($"SFX_{id}", 0.14f, 520f, 160f, 0.55f, 0.45f, 0x101u),
                SfxId.SwordSlash2 => BuildSweepNoise($"SFX_{id}", 0.14f, 640f, 200f, 0.55f, 0.45f, 0x202u),
                SfxId.SwordSlash3 => BuildSweepNoise($"SFX_{id}", 0.22f, 440f, 105f, 0.65f, 0.50f, 0x303u, metalRingHz: 980f),
                SfxId.BowDraw => BuildSweepNoise($"SFX_{id}", 0.16f, 240f, 420f, 0.30f, 0.25f, 0x404u),
                SfxId.BowRelease => BuildSweepNoise($"SFX_{id}", 0.12f, 780f, 210f, 0.50f, 0.40f, 0x505u),
                SfxId.ArrowHit => BuildImpact($"SFX_{id}", 0.10f, 340f, 130f, 0.65f, 0x606u),
                SfxId.Jump => BuildSweepNoise($"SFX_{id}", 0.11f, 230f, 540f, 0.55f, 0.12f, 0x707u),
                SfxId.AirJump => BuildSweepNoise($"SFX_{id}", 0.13f, 360f, 780f, 0.55f, 0.18f, 0x808u),
                SfxId.Land => BuildImpact($"SFX_{id}", 0.11f, 150f, 58f, 0.65f, 0x909u),
                SfxId.Roll => BuildSweepNoise($"SFX_{id}", 0.17f, 280f, 140f, 0.25f, 0.55f, 0xA0Au),
                SfxId.StompImpact => BuildImpact($"SFX_{id}", 0.24f, 165f, 42f, 0.85f, 0xB0Bu),
                SfxId.WallJump => BuildSweepNoise($"SFX_{id}", 0.11f, 300f, 620f, 0.50f, 0.20f, 0xC0Cu),
                SfxId.PlayerHurt => BuildImpact($"SFX_{id}", 0.18f, 260f, 95f, 0.75f, 0xD0Du),
                SfxId.PlayerDeath => BuildImpact($"SFX_{id}", 0.38f, 220f, 48f, 0.80f, 0xE0Eu),
                SfxId.ShieldClang => BuildMetalClang($"SFX_{id}", 0.24f, 820f, 1290f, 0xF0Fu),
                SfxId.PerfectDodge => BuildMetalClang($"SFX_{id}", 0.20f, 1040f, 1560f, 0x1111u),
                SfxId.ChestHeal => BuildChime($"SFX_{id}", 0.34f, 523.25f, 659.25f, 783.99f),
                SfxId.SwitchDoorOpen => BuildImpact($"SFX_{id}", 0.30f, 180f, 72f, 0.70f, 0x1212u),
                SfxId.TeleportWarp => BuildSweepNoise($"SFX_{id}", 0.32f, 280f, 1180f, 0.60f, 0.25f, 0x1313u, metalRingHz: 620f),
                SfxId.GrenadeBounce => BuildImpact($"SFX_{id}", 0.08f, 380f, 190f, 0.55f, 0x1414u),
                SfxId.GrenadeExplode => BuildExplosion($"SFX_{id}", 0.36f, 0x1515u),
                _ => BuildImpact($"SFX_{id}", 0.10f, 220f, 110f, 0.50f, 0x1616u)
            };
        }

        private static AudioClip BuildSweepNoise(string name, float duration, float startHz, float endHz,
            float toneAmp, float noiseAmp, uint seed, float metalRingHz = 0f)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[count];
            float phase = 0f;
            float ringPhase = 0f;
            uint rng = seed;

            for (int i = 0; i < count; i++)
            {
                float u = (float)i / count;
                float env = Mathf.Pow(1f - u, 1.6f) * Mathf.Min(1f, i / (SampleRate * 0.004f + 1f));
                float hz = Mathf.Lerp(startHz, endHz, u);
                phase += 2f * Mathf.PI * hz / SampleRate;
                float tone = Mathf.Sin(phase) * toneAmp;
                if (metalRingHz > 0f)
                {
                    ringPhase += 2f * Mathf.PI * metalRingHz / SampleRate;
                    tone += Mathf.Sin(ringPhase) * 0.22f * Mathf.Pow(1f - u, 2.2f);
                }
                float noise = NextSigned(ref rng) * noiseAmp * Mathf.Pow(1f - u, 1.3f);
                data[i] = Mathf.Clamp((tone + noise) * env, -0.95f, 0.95f);
            }

            return CreateMonoClip(name, data);
        }

        private static AudioClip BuildImpact(string name, float duration, float startHz, float endHz, float gain, uint seed)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[count];
            float phase = 0f;
            uint rng = seed;

            for (int i = 0; i < count; i++)
            {
                float u = (float)i / count;
                float env = Mathf.Pow(1f - u, 2.1f);
                float hz = Mathf.Lerp(startHz, endHz, Mathf.Sqrt(u));
                phase += 2f * Mathf.PI * hz / SampleRate;
                float thud = Mathf.Sin(phase) * 0.72f;
                float transient = NextSigned(ref rng) * 0.55f * Mathf.Pow(1f - u, 5.0f);
                data[i] = Mathf.Clamp((thud + transient) * env * gain, -0.95f, 0.95f);
            }

            return CreateMonoClip(name, data);
        }

        private static AudioClip BuildMetalClang(string name, float duration, float hzA, float hzB, uint seed)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[count];
            uint rng = seed;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float env = Mathf.Pow(1f - u, 2.0f);
                float clang = Mathf.Sin(2f * Mathf.PI * hzA * t) * 0.45f
                            + Mathf.Sin(2f * Mathf.PI * hzB * t) * 0.35f
                            + Mathf.Sin(2f * Mathf.PI * (hzB * 1.41f) * t) * 0.20f;
                float pingNoise = NextSigned(ref rng) * 0.35f * Mathf.Pow(1f - u, 6.0f);
                data[i] = Mathf.Clamp((clang + pingNoise) * env, -0.95f, 0.95f);
            }

            return CreateMonoClip(name, data);
        }

        private static AudioClip BuildChime(string name, float duration, float hz1, float hz2, float hz3)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float u = (float)i / count;
                float env1 = u < 0.02f ? u / 0.02f : Mathf.Pow(1f - u, 1.5f);
                float s1 = Mathf.Sin(2f * Mathf.PI * hz1 * t) * 0.35f * env1;
                float s2 = u >= 0.20f ? Mathf.Sin(2f * Mathf.PI * hz2 * t) * 0.35f * Mathf.Pow(1f - (u - 0.20f) / 0.80f, 1.5f) : 0f;
                float s3 = u >= 0.40f ? Mathf.Sin(2f * Mathf.PI * hz3 * t) * 0.40f * Mathf.Pow(1f - (u - 0.40f) / 0.60f, 1.4f) : 0f;
                data[i] = Mathf.Clamp(s1 + s2 + s3, -0.95f, 0.95f);
            }

            return CreateMonoClip(name, data);
        }

        private static AudioClip BuildExplosion(string name, float duration, uint seed)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[count];
            float phase = 0f;
            float lp = 0f;
            uint rng = seed;

            for (int i = 0; i < count; i++)
            {
                float u = (float)i / count;
                float env = Mathf.Pow(1f - u, 1.7f);
                float hz = Mathf.Lerp(130f, 34f, Mathf.Sqrt(u));
                phase += 2f * Mathf.PI * hz / SampleRate;
                float boom = Mathf.Sin(phase) * 0.55f;
                float white = NextSigned(ref rng);
                lp += 0.14f * (white - lp);
                float crack = lp * 0.75f;
                data[i] = Mathf.Clamp((boom + crack) * env, -0.95f, 0.95f);
            }

            return CreateMonoClip(name, data);
        }

        private static float NextSigned(ref uint state)
        {
            state = state * 1664525u + 1013904223u;
            return ((state >> 8) / 16777215f) * 2f - 1f;
        }

        private static AudioClip CreateMonoClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
