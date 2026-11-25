using System.Reflection;
using DeadCells.Core.Audio;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public sealed class CombatAudioHubTests
    {
        private GameObject _hubGo;

        [TearDown]
        public void TearDown()
        {
            if (_hubGo != null)
            {
                var hub = _hubGo.GetComponent<CombatAudioHub>();
                if (hub != null)
                {
                    typeof(CombatAudioHub)
                        .GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.Invoke(hub, null);
                }
                Object.DestroyImmediate(_hubGo);
            }
            _hubGo = null;
        }

        private CombatAudioHub CreateAwakeHub(string name)
        {
            _hubGo = new GameObject(name);
            var hub = _hubGo.AddComponent<CombatAudioHub>();
            typeof(CombatAudioHub)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(hub, null);
            return hub;
        }

        private static AudioSource GetAmbientSource(CombatAudioHub hub) =>
            (AudioSource)typeof(CombatAudioHub)
                .GetField("_ambientSource", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(hub);

        [Test]
        public void SetAmbientMultiplier_ClampsRangeAndUpdatesAmbientSourceVolume()
        {
            var hub = CreateAwakeHub("CombatAudioHub_Test");
            var ambientSource = GetAmbientSource(hub);

            Assert.IsNotNull(ambientSource);
            Assert.AreEqual(1f, CombatAudioHub.AmbientMultiplier, 0.0001f);

            CombatAudioHub.SetAmbientMultiplier(0f);
            Assert.AreEqual(0f, CombatAudioHub.AmbientMultiplier, 0.0001f);
            Assert.AreEqual(0f, ambientSource.volume, 0.0001f);

            CombatAudioHub.SetAmbientMultiplier(2.5f);
            Assert.AreEqual(1f, CombatAudioHub.AmbientMultiplier, 0.0001f);
            Assert.AreEqual(0.18f, ambientSource.volume, 0.0001f);

            CombatAudioHub.SetAmbientMultiplier(-0.5f);
            Assert.AreEqual(0f, CombatAudioHub.AmbientMultiplier, 0.0001f);
            Assert.AreEqual(0f, ambientSource.volume, 0.0001f);
        }

        [Test]
        public void FadeAmbient_DucksAndRestoresOverUnscaledSteps_AndIsNullSafeWithoutInstance()
        {
            Assert.DoesNotThrow(() => CombatAudioHub.SetAmbientMultiplier(0.5f));
            Assert.DoesNotThrow(() => CombatAudioHub.FadeAmbient(0f, 0.5f));
            Assert.AreEqual(1f, CombatAudioHub.AmbientMultiplier, 0.0001f);

            var hub = CreateAwakeHub("CombatAudioHub_FadeTest");
            var ambientSource = GetAmbientSource(hub);

            CombatAudioHub.FadeAmbient(0f, 0.5f);
            hub.TickAmbientFade(0.25f);
            Assert.AreEqual(0.5f, CombatAudioHub.AmbientMultiplier, 0.02f);
            Assert.AreEqual(0.09f, ambientSource.volume, 0.005f);

            hub.TickAmbientFade(0.30f);
            Assert.AreEqual(0f, CombatAudioHub.AmbientMultiplier, 0.0001f);
            Assert.AreEqual(0f, ambientSource.volume, 0.0001f);

            CombatAudioHub.FadeAmbient(1f, 0.6f);
            hub.TickAmbientFade(0.60f);
            Assert.AreEqual(1f, CombatAudioHub.AmbientMultiplier, 0.0001f);
            Assert.AreEqual(0.18f, ambientSource.volume, 0.0001f);

            // OnDestroy after ducking to 0 must reset static AmbientMultiplier back to 1f
            CombatAudioHub.SetAmbientMultiplier(0f);
            Assert.AreEqual(0f, CombatAudioHub.AmbientMultiplier, 0.0001f);
            typeof(CombatAudioHub)
                .GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(hub, null);
            Assert.AreEqual(1f, CombatAudioHub.AmbientMultiplier, 0.0001f);
        }
    }
}
