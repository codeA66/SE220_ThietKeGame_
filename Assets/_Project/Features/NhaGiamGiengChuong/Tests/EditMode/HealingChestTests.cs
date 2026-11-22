using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class HealingChestTests
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
        public void U04_HealingChest_RestoresFullOnce_DisablesPromptAndGlow_AndIgnoresRepeatOrSecondLoss()
        {
            var f = CreateFixture();
            Assert.IsTrue(f.Health.TakeDamage(new DamageInfo(35, 1, 0f, 0f, false, null)));
            Assert.AreEqual(65, f.Health.Current);

            int healEvents = 0;
            f.Chest.Healed += () => healEvents++;
            f.Chest.RefreshVisualState();
            Assert.IsTrue(f.Chest.PromptVisible, "[U04/Before] prompt should be visible when missing HP.");
            Assert.IsTrue(f.Chest.GlowVisible, "[U04/Before] glow should be visible.");

            // First press heals to Max, consumes reward, turns off prompt and glow.
            Assert.IsTrue(f.Chest.TryInteract(), "[U04/FirstPress] expected=true");
            Assert.AreEqual(f.Health.Max, f.Health.Current, "[U04/FirstPress] HP expected=Max");
            Assert.AreEqual(1, healEvents, "[U04/FirstPress] healEvents expected=1");
            Assert.IsTrue(f.Chest.IsConsumed);
            Assert.IsFalse(f.Chest.PromptVisible);
            Assert.IsFalse(f.Chest.GlowVisible);
            Assert.IsFalse(f.PromptGo.activeSelf);
            Assert.IsFalse(f.GlowGo.activeSelf);

            // Repeat interact ignored.
            Assert.IsFalse(f.Chest.TryInteract(), "[U04/RepeatPress] expected=false");
            Assert.AreEqual(1, healEvents);

            // Second damage taken later in same run: chest stays consumed, HP remains damaged.
            Assert.IsTrue(f.Health.TakeDamage(new DamageInfo(40, 1, 0f, 0f, false, null)));
            Assert.AreEqual(60, f.Health.Current);
            Assert.IsFalse(f.Chest.TryInteract(), "[U04/AfterSecondDamage] expected=false");
            Assert.AreEqual(60, f.Health.Current, "[U04/AfterSecondDamage] HP expected=60");
            Assert.AreEqual(1, healEvents);
        }

        [Test]
        public void U05_HealingChest_AtFullHealth_ShowsFullMessage_KeepsReward_AndHealsAfterDamage()
        {
            var f = CreateFixture();
            Assert.AreEqual(f.Health.Max, f.Health.Current);

            int healEvents = 0;
            string shownFeedback = null;
            f.Chest.Healed += () => healEvents++;
            f.Chest.FeedbackShown += msg => shownFeedback = msg;

            // Interact at full HP -> shows "Đầy máu", keeps reward and glow, does not heal.
            Assert.IsFalse(f.Chest.TryInteract(), "[U05/FullHP] expected=false");
            Assert.AreEqual(HealingChest.FullHealthMessage, shownFeedback, "[U05/FullHP] feedback expected='Đầy máu'");
            Assert.AreEqual(HealingChest.FullHealthMessage, f.Chest.LastFeedbackMessage);
            Assert.IsTrue(f.Chest.HasReward);
            Assert.IsTrue(f.Chest.GlowVisible);
            Assert.AreEqual(0, healEvents);

            // Take damage -> prompt appears, next interact heals to Max once.
            Assert.IsTrue(f.Health.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null)));
            Assert.AreEqual(70, f.Health.Current);
            f.Chest.RefreshVisualState();
            Assert.IsTrue(f.Chest.PromptVisible, "[U05/Damaged] prompt must appear.");

            Assert.IsTrue(f.Chest.TryInteract(), "[U05/AfterDamage] expected=true");
            Assert.AreEqual(f.Health.Max, f.Health.Current);
            Assert.AreEqual(1, healEvents);
            Assert.IsTrue(f.Chest.IsConsumed);
        }

        [Test]
        public void U06_HealingChest_DeferredGrantRevalidatesConditions_AndAllowsRetryOnReturn()
        {
            // Variant 1: Other actor attempts deferred grant.
            {
                var f = CreateFixture();
                f.Health.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null));
                var otherGo = Track(new GameObject("OtherActor"));
                otherGo.transform.position = new Vector3(72f, 0f, 0f);
                var otherCol = otherGo.AddComponent<BoxCollider2D>();
                var otherHealth = otherGo.AddComponent<Health>();
                otherHealth.Initialize(100, 0f);
                otherHealth.TakeDamage(new DamageInfo(25, 1, 0f, 0f, false, null));

                var deferred = f.Chest.CreateDeferredGrant(otherGo, otherHealth, otherCol);
                Assert.IsFalse(deferred(), "[U06/OtherActor] expected=false");
                Assert.AreEqual(75, otherHealth.Current);
                Assert.AreEqual(70, f.Health.Current);
                Assert.IsTrue(f.Chest.HasReward);
            }

            // Variant 2: Player leaves trigger before deferred grant executes, then returns and presses again.
            {
                var f = CreateFixture();
                f.Health.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null));
                var deferred = f.Chest.CreateDeferredGrant(f.PlayerGo, f.Health, f.PlayerCol);

                // Move outside trigger (trigger is [70.75, 73.25]).
                f.PlayerGo.transform.position = new Vector3(65f, 0f, 0f);
                Assert.IsFalse(deferred(), "[U06/LeftTrigger] expected=false");
                Assert.AreEqual(70, f.Health.Current);
                Assert.IsTrue(f.Chest.HasReward);

                // Return into trigger -> fresh interact succeeds once.
                f.PlayerGo.transform.position = new Vector3(72f, 0f, 0f);
                Assert.IsTrue(f.Chest.TryInteract(), "[U06/Returned] expected=true");
                Assert.AreEqual(100, f.Health.Current);
                Assert.IsTrue(f.Chest.IsConsumed);
            }

            // Variant 3: Player dies before deferred grant executes.
            {
                var f = CreateFixture();
                f.Health.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null));
                var deferred = f.Chest.CreateDeferredGrant(f.PlayerGo, f.Health, f.PlayerCol);

                f.Health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, null));
                Assert.AreEqual(0, f.Health.Current);
                Assert.IsFalse(deferred(), "[U06/PlayerDead] expected=false");
                Assert.AreEqual(0, f.Health.Current);
                Assert.IsTrue(f.Chest.HasReward);
            }

            // Variant 4: Run ends/locks before deferred grant executes.
            {
                var f = CreateFixture();
                f.Health.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null));
                var deferred = f.Chest.CreateDeferredGrant(f.PlayerGo, f.Health, f.PlayerCol);

                f.Run.RequestWin(5);
                f.Run.ResolvePendingOutcome(6);
                Assert.AreEqual(LevelPhase.Ended, f.Run.Phase);
                Assert.IsFalse(deferred(), "[U06/RunEnded] expected=false");
                Assert.AreEqual(70, f.Health.Current);
                Assert.IsTrue(f.Chest.HasReward);
            }

            // Variant 5: Player healed to Max from another source before deferred grant executes.
            {
                var f = CreateFixture();
                f.Health.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null));
                var deferred = f.Chest.CreateDeferredGrant(f.PlayerGo, f.Health, f.PlayerCol);

                f.Health.RestoreFull();
                Assert.IsFalse(deferred(), "[U06/HealedExternally] expected=false");
                Assert.AreEqual(100, f.Health.Current);
                Assert.IsTrue(f.Chest.HasReward);
            }

            // Variant 6: Initial press from outside trigger.
            {
                var f = CreateFixture();
                f.Health.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null));
                f.PlayerGo.transform.position = new Vector3(60f, 0f, 0f);
                Assert.IsFalse(f.Chest.TryInteract(), "[U06/PressOutside] expected=false");
                Assert.AreEqual(70, f.Health.Current);
                Assert.IsTrue(f.Chest.HasReward);
            }
        }

        [Test]
        public void U07_HealingChest_GuardsReentrancyInsideHealthChangedCallback()
        {
            var f = CreateFixture();
            Assert.IsTrue(f.Health.TakeDamage(new DamageInfo(40, 1, 0f, 0f, false, null)));
            Assert.AreEqual(60, f.Health.Current);

            int maxTransitions = 0;
            bool reenteredWhileDamaged = false;
            bool reenteredResult = true;
            bool guardFlag = false;

            f.Health.Changed += (current, max) =>
            {
                if (current == max)
                {
                    maxTransitions++;
                    if (!guardFlag)
                    {
                        guardFlag = true;
                        bool hitApplied = f.Health.TakeDamage(new DamageInfo(20, 1, 0f, 0f, false, null));
                        Assert.IsTrue(hitApplied, "[U07/CallbackHit] damage inside Changed must apply.");
                        Assert.AreEqual(80, f.Health.Current);
                        reenteredWhileDamaged = f.Health.Current < f.Health.Max;
                        reenteredResult = f.Chest.TryInteract();
                    }
                }
            };

            Assert.IsTrue(f.Chest.TryInteract(), "[U07/OuterInteract] expected=true");
            Assert.IsTrue(reenteredWhileDamaged, "[U07/ReentryConfirmed] nested call must happen while missing HP.");
            Assert.IsFalse(reenteredResult, "[U07/NestedResult] nested interact must be rejected.");
            Assert.AreEqual(1, maxTransitions, "[U07/MaxTransitions] RestoreFull to Max must occur exactly once.");
            Assert.AreEqual(80, f.Health.Current, "[U07/FinalHP] HP must stay at 80 after nested hit.");
            Assert.IsTrue(f.Chest.IsConsumed, "[U07/Consumed] chest ends consumed.");
            Assert.IsFalse(f.Chest.PromptVisible, "[U07/PromptOff]");
            Assert.IsFalse(f.Chest.GlowVisible, "[U07/GlowOff]");
        }

        [Test]
        public void TryInteract_WhenHealed_SwapsVisualSpriteAndTint()
        {
            var f = CreateFixture();
            var visualGo = Track(new GameObject("Visual"));
            visualGo.transform.SetParent(f.Chest.transform, false);
            var visualSr = visualGo.AddComponent<SpriteRenderer>();
            var closedSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 24f);
            var openedSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 24f);
            visualSr.sprite = closedSprite;
            visualSr.color = Color.white;

            var so = new SerializedObject(f.Chest);
            var visualProp = so.FindProperty("_visualRenderer");
            var openedProp = so.FindProperty("_openedSprite");
            if (visualProp != null) visualProp.objectReferenceValue = visualSr;
            if (openedProp != null) openedProp.objectReferenceValue = openedSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(f.Health.TakeDamage(new DamageInfo(35, 1, 0f, 0f, false, null)));
            Assert.IsTrue(f.Chest.TryInteract(), "[T1.5/ChestHeal]");

            Assert.AreSame(openedSprite, visualSr.sprite, "[T1.5/ChestSprite] HealingChest must swap to _openedSprite on heal.");
            var expectedTint = new Color(0.55f, 0.58f, 0.62f, 1f);
            Assert.AreEqual(expectedTint.r, visualSr.color.r, 1e-3f, "[T1.5/ChestTintR]");
            Assert.AreEqual(expectedTint.g, visualSr.color.g, 1e-3f, "[T1.5/ChestTintG]");
            Assert.AreEqual(expectedTint.b, visualSr.color.b, 1e-3f, "[T1.5/ChestTintB]");
            Assert.AreEqual(expectedTint.a, visualSr.color.a, 1e-3f, "[T1.5/ChestTintA]");
        }

        private (LevelRunController Run, GameObject PlayerGo, BoxCollider2D PlayerCol, Health Health, HealingChest Chest, GameObject PromptGo, GameObject GlowGo) CreateFixture()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(72f, 0f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var runGo = Track(new GameObject("Run"));
            var run = runGo.AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var chestGo = Track(new GameObject("I3_HealingChest"));
            chestGo.transform.position = new Vector3(72f, 0f, 0f);
            var trigger = chestGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 0.75f);
            trigger.size = new Vector2(2.5f, 1.5f);

            var promptGo = Track(new GameObject("Prompt"));
            promptGo.transform.SetParent(chestGo.transform, false);
            var glowGo = Track(new GameObject("Glow"));
            glowGo.transform.SetParent(chestGo.transform, false);

            var chest = chestGo.AddComponent<HealingChest>();
            chest.Configure(run, null, trigger, null, promptGo, glowGo, health, playerCol);

            return (run, playerGo, playerCol, health, chest, promptGo, glowGo);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
