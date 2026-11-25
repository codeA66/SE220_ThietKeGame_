using System;
using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Core.StateMachine;
using DeadCells.Enemies;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class HealingChestTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Features/Player/Prefabs/Player.prefab";
        private readonly List<GameObject> _created = new();
        private string _previousScene;

        private sealed class DummyPlayState : IPlayerState
        {
            public void Enter() { }
            public void Exit() { }
            public void Tick() { }
            public void FixedTick() { }
        }

        private sealed class CallbackCoreState : IState
        {
            private readonly Action _onTick;
            public CallbackCoreState(Action onTick) => _onTick = onTick;
            public void Enter() { }
            public void Exit() { }
            public void Tick() => _onTick?.Invoke();
            public void FixedTick() { }
        }

        private sealed class CallbackPlayerState : IPlayerState
        {
            private readonly Action _onTick;
            public CallbackPlayerState(Action onTick) => _onTick = onTick;
            public void Enter() { }
            public void Exit() { }
            public void Tick() => _onTick?.Invoke();
            public void FixedTick() { }
        }

        [SetUp]
        public void SetUp()
        {
            _previousScene = Assets.GeometryAuditHelper.CaptureCurrentScenePath();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
            DamagePopup.ClearPoolForTests();
            foreach (var popup in Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include))
            {
                if (popup != null)
                    Object.DestroyImmediate(popup.gameObject);
            }
            Time.timeScale = 1f;
            Assets.GeometryAuditHelper.RestoreScene(_previousScene);
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

        [Test]
        public void PlayerController_TeleportTo_SyncsColliderBoundsImmediately()
        {
#pragma warning disable CS0618
            bool previousAutoSync = Physics2D.autoSyncTransforms;
            Physics2D.autoSyncTransforms = false;
#pragma warning restore CS0618
            try
            {
                var player = CreatePlayerFromPrefab(Vector2.zero);
                Assert.IsTrue(player.gameObject.CompareTag("Player"), "Player.prefab root must carry the Player tag.");

                var col = player.GetComponent<BoxCollider2D>();
                Physics2D.SyncTransforms();

                var destination = new Vector2(25f, 12f);
                player.TeleportTo(destination, -1);

                Vector2 expectedCenter = destination + col.offset;
                Assert.AreEqual(expectedCenter.x, col.bounds.center.x, 1e-3f,
                    "TeleportTo must sync BoxCollider2D.bounds.center.x even when Physics2D.autoSyncTransforms is false.");
                Assert.AreEqual(expectedCenter.y, col.bounds.center.y, 1e-3f,
                    "TeleportTo must sync BoxCollider2D.bounds.center.y even when Physics2D.autoSyncTransforms is false.");
                Assert.AreEqual(-1, player.Context.FacingDirection);
            }
            finally
            {
#pragma warning disable CS0618
                Physics2D.autoSyncTransforms = previousAutoSync;
#pragma warning restore CS0618
            }
        }

        [Test]
        public void StateMachine_ForceChangeBeforeTick_DoesNotBlockTransitionInsideTick()
        {
            // 1. Core StateMachine
            var core = new StateMachine();
            IState coreB = null;
            IState coreC = null;
            bool coreFirst = false;
            bool coreSecond = true;
            bool coreNextTick = false;

            var coreForced = new CallbackCoreState(() =>
            {
                coreFirst = core.ChangeState(coreB);
                coreSecond = core.ChangeState(coreC);
            });
            coreB = new CallbackCoreState(() => coreNextTick = core.ChangeState(coreC));
            coreC = new CallbackCoreState(null);

            core.ForceChangeState(coreForced);
            core.Tick();
            Assert.IsTrue(coreFirst, "ForceChangeState before Tick must not block ChangeState inside Tick.");
            Assert.IsFalse(coreSecond, "A second ChangeState inside the same Tick must be rejected.");
            Assert.AreSame(coreB, core.Current);

            core.Tick();
            Assert.IsTrue(coreNextTick, "A subsequent Tick on the same frame must allow another transition.");
            Assert.AreSame(coreC, core.Current);

            // 2. PlayerStateMachine
            var player = CreatePlayerFromPrefab(Vector2.zero);
            var playerMachine = (PlayerStateMachine)typeof(PlayerController)
                .GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(player);
            IPlayerState playerB = null;
            IPlayerState playerC = null;
            bool playerFirst = false;
            bool playerSecond = true;
            bool playerNextTick = false;

            var playerForced = new CallbackPlayerState(() =>
            {
                playerFirst = playerMachine.ChangeState(playerB);
                playerSecond = playerMachine.ChangeState(playerC);
            });
            playerB = new CallbackPlayerState(() => playerNextTick = playerMachine.ChangeState(playerC));
            playerC = new CallbackPlayerState(null);

            playerMachine.ForceChangeState(playerForced);
            playerMachine.Tick();
            Assert.IsTrue(playerFirst, "PlayerStateMachine ForceChangeState before Tick must not block ChangeState inside Tick.");
            Assert.IsFalse(playerSecond, "PlayerStateMachine must reject a second ChangeState inside the same Tick.");
            Assert.AreSame(playerB, playerMachine.Current);

            playerMachine.Tick();
            Assert.IsTrue(playerNextTick, "PlayerStateMachine must allow a transition on the next Tick.");
            Assert.AreSame(playerC, playerMachine.Current);
        }

        [Test]
        public void Projectile_DeactivateViaSetActiveFalse_ReleasesBackToPool()
        {
            var projGo = Track(new GameObject("PooledArrow"));
            var body = projGo.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var col = projGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var proj = projGo.AddComponent<Projectile>();

            // Leave _renderer null so Setup does not spawn an unparented TrailRibbon view in EditMode.
            typeof(Projectile).GetField("_mode", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(proj, Projectile.Mode.Straight);

            int releasedCount = 0;
            proj.Setup(_ => releasedCount++, null, 0, 0);
            proj.LaunchStraight(Vector2.zero, new Vector2(8f, 0f), 10, 1f, null);
            Assert.IsTrue(proj.IsLive);
            Assert.IsTrue(proj.gameObject.activeSelf);

            proj.gameObject.SetActive(false);
            // EditMode SetActive(false) does not trigger OnDisable on non-[ExecuteAlways] components.
            InvokeNonPublic(proj, "OnDisable");
            Assert.IsFalse(proj.IsLive, "Disabling a live Projectile via SetActive(false) must mark it non-live.");
            Assert.AreEqual(1, releasedCount, "Disabling a live Projectile via SetActive(false) must release it back to the pool once.");

            // Repeated OnDisable must be idempotent (no double-release).
            InvokeNonPublic(proj, "OnDisable");
            Assert.AreEqual(1, releasedCount, "Repeated OnDisable must not double-release a non-live Projectile.");

            // Pooled reuse still works cleanly without re-entry issues.
            proj.LaunchStraight(Vector2.one, new Vector2(-6f, 0f), 10, 1f, null);
            Assert.IsTrue(proj.IsLive);
            proj.gameObject.SetActive(false);
            InvokeNonPublic(proj, "OnDisable");
            Assert.IsFalse(proj.IsLive);
            Assert.AreEqual(2, releasedCount);
        }

        [Test]
        public void DamagePopup_Prewarm_RefillsPoolAfterSceneObjectsDestroyed()
        {
            DamagePopup.ClearPoolForTests();
            try
            {
                DamagePopup.Prewarm();
                var initial = Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include);
                Assert.AreEqual(8, initial.Length, "Initial Prewarm must populate PrewarmCount (8) instances.");

                // Simulate scene unload destroying pooled GameObjects while s_font remains initialized.
                foreach (var popup in initial)
                {
                    if (popup != null)
                        Object.DestroyImmediate(popup.gameObject);
                }
                Assert.AreEqual(0, Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include).Length);

                DamagePopup.Prewarm();
                var refilled = Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include);
                Assert.AreEqual(8, refilled.Length, "Prewarm after scene object destruction must refill live pooled instances.");

                DamagePopup.Spawn(Vector3.zero, 42, 1, false);
                int activeCount = 0;
                foreach (var popup in Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include))
                {
                    if (popup.gameObject.activeSelf)
                        activeCount++;
                }
                Assert.AreEqual(1, activeCount, "Spawn after refill must activate a pooled DamagePopup.");
            }
            finally
            {
                DamagePopup.ClearPoolForTests();
            }
        }

        [Test]
        public void HealingChest_BlocksPromptAndInteraction_DuringScriptedOrUpgradeLock()
        {
            var player = CreatePlayerFromPrefab(new Vector2(72f, 0f));
            var health = player.Health;
            var playerCol = player.GetComponent<BoxCollider2D>();

            var runGo = Track(new GameObject("Run"));
            var run = runGo.AddComponent<LevelRunController>();
            run.Configure(player, health);
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 0f));

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
            chest.Configure(run, player, trigger, null, promptGo, glowGo, health, playerCol);

            Assert.IsTrue(health.TakeDamage(new DamageInfo(35, 1, 0f, 0f, false, null)));
            Assert.AreEqual(65, health.Current);

            chest.RefreshVisualState();
            Assert.IsTrue(chest.PromptVisible, "Precondition: prompt visible while damaged and unlocked.");

            // 1. Scripted sequence lock hides prompt and blocks interaction.
            player.EnterTransitionLock();
            Assert.IsTrue(player.IsInScriptedSequence);
            chest.RefreshVisualState();
            Assert.IsFalse(chest.PromptVisible, "Prompt must hide during scripted sequence.");
            Assert.IsFalse(promptGo.activeSelf);
            Assert.IsFalse(chest.TryInteract(), "TryInteract must fail during scripted sequence.");
            Assert.AreEqual(65, health.Current);
            Assert.IsTrue(chest.HasReward);

            player.EndScriptedSequence();
            Assert.IsFalse(player.IsInScriptedSequence);
            chest.RefreshVisualState();
            Assert.IsTrue(chest.PromptVisible, "Prompt must return once scripted sequence ends.");

            // 2. Scroll upgrade choice lock hides prompt and blocks interaction.
            var scrollGo = Track(new GameObject("DummyScroll"));
            var scroll = scrollGo.AddComponent<PowerScrollPickup>();
            player.RunStats.ChoiceOpened += _ => { };
            var tryBeginChoice = typeof(PlayerRunStats).GetMethod("TryBeginChoice", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(tryBeginChoice);
            Assert.IsTrue((bool)tryBeginChoice.Invoke(player.RunStats, new object[] { scroll }));
            Assert.IsTrue(player.RunStats.BlocksGameplayInput);

            chest.RefreshVisualState();
            Assert.IsFalse(chest.PromptVisible, "Prompt must hide while scroll upgrade menu is open.");
            Assert.IsFalse(promptGo.activeSelf);
            Assert.IsFalse(chest.TryInteract(), "TryInteract must fail while scroll upgrade menu is open.");
            Assert.AreEqual(65, health.Current);
            Assert.IsTrue(chest.HasReward);

            player.RunStats.CancelChoice();
            Assert.IsTrue(player.RunStats.BlocksGameplayInput, "Same-frame close guard keeps gameplay input blocked.");
            chest.RefreshVisualState();
            Assert.IsFalse(chest.PromptVisible);
            Assert.IsFalse(chest.TryInteract());

            var closedFrameField = typeof(PlayerRunStats).GetField("_closedFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(closedFrameField);
            closedFrameField.SetValue(player.RunStats, -1);
            Assert.IsFalse(player.RunStats.BlocksGameplayInput);

            chest.RefreshVisualState();
            Assert.IsTrue(chest.PromptVisible, "Prompt must return once upgrade input lock clears.");
            Assert.IsTrue(chest.TryInteract(), "Chest must heal once all input locks clear.");
            Assert.AreEqual(health.Max, health.Current);
            Assert.IsTrue(chest.IsConsumed);
        }

        private PlayerController CreatePlayerFromPrefab(Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.IsNotNull(prefab, $"Missing {PlayerPrefabPath}");
            var playerGo = Track((GameObject)PrefabUtility.InstantiatePrefab(prefab));
            playerGo.transform.position = new Vector3(position.x, position.y, 0f);

            var player = playerGo.GetComponent<PlayerController>();
            var health = playerGo.GetComponent<Health>();
            var input = playerGo.GetComponent<PlayerInputReader>();

            InvokeNonPublic(health, "Awake");
            InvokeNonPublic(input, "Awake");
            InvokeNonPublic(player, "Awake");
            return player;
        }

        private static void InvokeNonPublic(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing {target.GetType().Name}.{methodName}");
            method.Invoke(target, null);
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
