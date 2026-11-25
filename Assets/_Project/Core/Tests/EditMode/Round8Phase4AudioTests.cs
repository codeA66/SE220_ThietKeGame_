using System;
using System.Collections.Generic;
using System.Reflection;
using DeadCells.Core.Audio;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public sealed class Round8Phase4AudioTests
    {
        [Test]
        public void ProceduralSfxLibrary_GeneratesValidNonSilentMonoClips_ForAll21SfxIdsAndAmbientLoop()
        {
            var ids = (SfxId[])Enum.GetValues(typeof(SfxId));
            Assert.AreEqual(21, ids.Length, "SfxId enum must contain exactly 21 sound IDs.");

            foreach (var id in ids)
            {
                AudioClip clip = ProceduralSfxLibrary.GetClip(id);
                Assert.IsNotNull(clip, $"Missing procedural clip for SfxId.{id}.");
                Assert.AreEqual(1, clip.channels, $"Clip for {id} must be mono.");
                Assert.AreEqual(22050, clip.frequency, $"Clip for {id} must use 22050 Hz.");
                Assert.Greater(clip.samples, 0, $"Clip for {id} must have > 0 samples.");
                Assert.AreSame(clip, ProceduralSfxLibrary.GetClip(id), $"Clip for {id} must be cached.");

                float[] samples = new float[clip.samples];
                Assert.IsTrue(clip.GetData(samples, 0));
                float maxAbs = 0f;
                for (int i = 0; i < samples.Length; i++)
                {
                    float s = samples[i];
                    Assert.IsFalse(float.IsNaN(s) || float.IsInfinity(s), $"Invalid sample in {id}.");
                    float abs = Mathf.Abs(s);
                    if (abs > maxAbs)
                        maxAbs = abs;
                }
                Assert.Greater(maxAbs, 0.01f, $"Clip for {id} must not be silent.");
            }

            AudioClip ambient = ProceduralSfxLibrary.GetAmbientLoopClip();
            Assert.IsNotNull(ambient, "Ambient loop clip must not be null.");
            Assert.AreEqual(1, ambient.channels, "Ambient clip must be mono.");
            Assert.AreEqual(22050, ambient.frequency, "Ambient clip must use 22050 Hz.");
            Assert.AreEqual(22050 * 4, ambient.samples, "Ambient clip must be 4.0 seconds long.");
            Assert.AreSame(ambient, ProceduralSfxLibrary.GetAmbientLoopClip(), "Ambient clip must be cached.");

            float[] ambSamples = new float[ambient.samples];
            Assert.IsTrue(ambient.GetData(ambSamples, 0));
            float ambMaxAbs = 0f;
            for (int i = 0; i < ambSamples.Length; i++)
            {
                float s = ambSamples[i];
                Assert.IsFalse(float.IsNaN(s) || float.IsInfinity(s));
                float abs = Mathf.Abs(s);
                if (abs > ambMaxAbs)
                    ambMaxAbs = abs;
            }
            Assert.Greater(ambMaxAbs, 0.005f, "Ambient loop clip must not be silent.");
            Assert.Less(Mathf.Abs(ambSamples[0] - ambSamples[ambSamples.Length - 1]), 0.05f,
                "Ambient loop clip must loop seamlessly across end/start boundary.");
        }

        [Test]
        public void CombatAudioHub_Play_IsNullSafeWithoutInstance_AndCyclesPooledSourcesWithInstance()
        {
            Assert.DoesNotThrow(() => CombatAudioHub.Play(SfxId.SwordSlash1),
                "CombatAudioHub.Play must be null-safe when no hub instance exists.");

            var root = new GameObject("__Round8Phase4AudioHubTest");
            try
            {
                var hub = root.AddComponent<CombatAudioHub>();
                var awake = typeof(CombatAudioHub).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(awake, "CombatAudioHub must implement Awake() to create its pooled AudioSources.");
                awake.Invoke(hub, null);

                var sources = hub.GetComponentsInChildren<AudioSource>(true);
                Assert.GreaterOrEqual(sources.Length, 9, "CombatAudioHub must create 8 pooled SFX AudioSources + 1 ambient AudioSource.");

                AudioSource ambient = null;
                var sfxSources = new List<AudioSource>();
                foreach (var src in sources)
                {
                    Assert.AreEqual(0f, src.spatialBlend, 0.001f);
                    if (src.loop)
                        ambient = src;
                    else
                        sfxSources.Add(src);
                }

                Assert.IsNotNull(ambient, "CombatAudioHub must configure 1 looping ambient AudioSource.");
                Assert.AreEqual(0.18f, ambient.volume, 0.01f);
                Assert.IsNotNull(ambient.clip, "Ambient AudioSource must be bound to the ambient loop clip.");
                Assert.AreEqual(8, sfxSources.Count, "CombatAudioHub must maintain 8 pooled SFX AudioSources.");

                CombatAudioHub.Play(SfxId.SwordSlash1);
                CombatAudioHub.Play(SfxId.BowRelease, 0.75f);
                CombatAudioHub.Play(SfxId.Jump);

                int assignedCount = 0;
                foreach (var src in sfxSources)
                {
                    if (src.clip != null)
                    {
                        assignedCount++;
                        Assert.GreaterOrEqual(src.pitch, 0.93f - 0.001f);
                        Assert.LessOrEqual(src.pitch, 1.07f + 0.001f);
                    }
                }
                Assert.GreaterOrEqual(assignedCount, 3, "Play calls must cycle across distinct pooled AudioSources.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
