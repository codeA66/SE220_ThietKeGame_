using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class LoreTabletTests
    {
        private readonly List<GameObject> _created = new();

        private sealed class DummyPlayState : IPlayerState
        {
            public void Enter() { }
            public void Exit() { }
            public void Tick() { }
            public void FixedTick() { }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
            Time.timeScale = 1f;
        }

        [Test]
        public void U19_LoreTablet_TogglesLoreBannerInsideTrigger_AndAutoClosesOnWalkAwayOrRunEnd()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(72f, 24f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var tabletGo = Track(new GameObject("I16_LoreTablet"));
            tabletGo.transform.position = new Vector3(72f, 24f, 0f);
            var trigger = tabletGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 1.0f);
            trigger.size = new Vector2(3.0f, 2.0f);

            var promptGo = Track(new GameObject("PromptIndicator"));
            var bannerGo = Track(new GameObject("LoreBanner"));
            var bannerText = bannerGo.AddComponent<Text>();

            var tablet = tabletGo.AddComponent<LoreTablet>();
            tablet.Configure(
                "I16",
                "Biên Niên Sử Ngục Tối",
                "Mầm bệnh Malaise phát tán từ tầng cống ngầm...",
                run,
                null,
                trigger,
                null,
                promptGo,
                bannerGo,
                bannerText,
                health,
                playerCol);

            // 1. Inside trigger, not reading -> prompt visible, banner hidden.
            tablet.RefreshVisualState();
            Assert.IsTrue(tablet.PromptVisible);
            Assert.IsFalse(tablet.IsReading);
            Assert.IsFalse(bannerGo.activeSelf);

            // 2. First press -> opens lore banner, increments ReadCount to 1, hides prompt while reading.
            Assert.IsTrue(tablet.TryInteract(), "[U19/Open]");
            Assert.IsTrue(tablet.IsReading);
            Assert.AreEqual(1, tablet.ReadCount);
            Assert.IsTrue(bannerGo.activeSelf);
            Assert.IsFalse(tablet.PromptVisible);
            StringAssert.Contains("Malaise", bannerText.text);

            // 3. Second press inside trigger -> closes lore banner, restores prompt.
            Assert.IsTrue(tablet.TryInteract(), "[U19/CloseToggle]");
            Assert.IsFalse(tablet.IsReading);
            Assert.IsFalse(bannerGo.activeSelf);
            Assert.IsTrue(tablet.PromptVisible);

            // 4. Open again, then walk out of trigger -> auto-closes banner!
            Assert.IsTrue(tablet.TryInteract());
            Assert.IsTrue(tablet.IsReading);
            playerGo.transform.position = new Vector3(60f, 24f, 0f);
            Physics2D.SyncTransforms();
            tablet.RefreshVisualState();
            Assert.IsFalse(tablet.IsReading, "[U19/AutoCloseOnWalkAway]");
            Assert.IsFalse(bannerGo.activeSelf);
            Assert.IsFalse(tablet.PromptVisible);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
