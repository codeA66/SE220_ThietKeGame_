using System.Collections.Generic;
using System.Reflection;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core;
using DeadCells.Currency;
using DeadCells.HUD;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using DeadCells.Player.StateMachine.States;
using DeadCells.VFX;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class LevelRunTests
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
            Wallet.Reset();
            KillCounter.Reset();
            Time.timeScale = 1f;
        }

        [Test]
        public void U11_LevelRunController_StaysInIntroDuringNullOrSpawnState_AndStartsClockOnceOnControlHandover()
        {
            var run = CreateRun(out _);
            int startedCount = 0;
            run.RunStarted += () => startedCount++;

            Assert.AreEqual(LevelPhase.Intro, run.Phase);
            Assert.IsFalse(run.ClockStarted);

            // Null state -> remains Intro.
            Assert.IsFalse(run.TickIntro(null, 0.5f), "[U11/NullState]");
            Assert.AreEqual(LevelPhase.Intro, run.Phase);
            Assert.IsFalse(run.ClockStarted);
            Assert.AreEqual(0, startedCount);

            // PlayerSpawnState across multiple updates -> remains Intro.
            var spawnState = new PlayerSpawnState(null, null);
            Assert.IsFalse(run.TickIntro(spawnState, 1.0f), "[U11/Spawn1]");
            Assert.IsFalse(run.TickIntro(spawnState, 1.5f), "[U11/Spawn2]");
            Assert.AreEqual(LevelPhase.Intro, run.Phase);
            Assert.IsFalse(run.ClockStarted);
            Assert.AreEqual(0, startedCount);

            // Real gameplay state handover -> transitions Playing once and records single start timestamp.
            var playState = new DummyPlayState();
            Assert.IsTrue(run.TickIntro(playState, 2.25f), "[U11/Handover]");
            Assert.AreEqual(LevelPhase.Playing, run.Phase);
            Assert.IsTrue(run.ClockStarted);
            Assert.AreEqual(2.25f, run.RunStartTime, 0.0001f);
            Assert.AreEqual(1, startedCount);

            // Repeated ticks in play state do not re-trigger start or overwrite timestamp.
            Assert.IsFalse(run.TickIntro(playState, 5.0f), "[U11/Repeat]");
            Assert.AreEqual(2.25f, run.RunStartTime, 0.0001f);
            Assert.AreEqual(1, startedCount);
        }

        [Test]
        public void U12_LevelRunController_SettlesSingleWinOrLossCandidateOnceAtNextFrame()
        {
            // Fixture 1: Win only at frame N.
            {
                var run = CreatePlayingRun(out _);
                var outcomes = new List<LevelOutcome>();
                run.Finished += outcomes.Add;

                Assert.IsTrue(run.RequestWin(100));
                Assert.IsFalse(run.ResolvePendingOutcome(100), "[U12/Win/SameFrame] must not settle on frame N.");
                Assert.AreEqual(0, outcomes.Count);

                Assert.IsTrue(run.ResolvePendingOutcome(101), "[U12/Win/NextFrame] settles at N+1.");
                Assert.IsFalse(run.ResolvePendingOutcome(102), "[U12/Win/Repeat]");
                Assert.AreEqual(1, outcomes.Count);
                Assert.AreEqual(LevelOutcome.Win, outcomes[0]);
            }

            // Fixture 2: Loss only via real Health.Died at frame N.
            {
                var run = CreatePlayingRun(out var health);
                int currentFrame = 200;
                run.FrameProvider = () => currentFrame;
                var outcomes = new List<LevelOutcome>();
                run.Finished += outcomes.Add;

                Assert.IsTrue(health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, null)));
                Assert.IsFalse(run.ResolvePendingOutcome(200), "[U12/Loss/SameFrame] must not settle on frame N.");
                Assert.AreEqual(0, outcomes.Count);

                currentFrame = 201;
                Assert.IsTrue(run.ResolvePendingOutcome(201), "[U12/Loss/NextFrame] settles at N+1.");
                Assert.IsFalse(run.ResolvePendingOutcome(202), "[U12/Loss/Repeat]");
                Assert.AreEqual(1, outcomes.Count);
                Assert.AreEqual(LevelOutcome.Loss, outcomes[0]);
            }
        }

        [Test]
        public void U13_LevelRunController_SameFrameWinAndLoss_AlwaysSettlesLossInBothOrders()
        {
            // Order 1: Win(N) then Health.Died(N) -> Loss only.
            {
                var run = CreatePlayingRun(out var health);
                int frame = 300;
                run.FrameProvider = () => frame;
                var outcomes = new List<LevelOutcome>();
                run.Finished += outcomes.Add;

                Assert.IsTrue(run.RequestWin(frame));
                Assert.IsTrue(health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, null)));

                Assert.IsTrue(run.ResolvePendingOutcome(frame + 1));
                Assert.AreEqual(1, outcomes.Count, "[U13/WinThenLoss] count expected=1");
                Assert.AreEqual(LevelOutcome.Loss, outcomes[0], "[U13/WinThenLoss] outcome expected=Loss");
            }

            // Order 2: Health.Died(N) then Win(N) -> Loss only.
            {
                var run = CreatePlayingRun(out var health);
                int frame = 400;
                run.FrameProvider = () => frame;
                var outcomes = new List<LevelOutcome>();
                run.Finished += outcomes.Add;

                Assert.IsTrue(health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, null)));
                Assert.IsTrue(run.RequestWin(frame));

                Assert.IsTrue(run.ResolvePendingOutcome(frame + 1));
                Assert.AreEqual(1, outcomes.Count, "[U13/LossThenWin] count expected=1");
                Assert.AreEqual(LevelOutcome.Loss, outcomes[0], "[U13/LossThenWin] outcome expected=Loss");
            }
        }

        [Test]
        public void U14_LevelRunController_NextFrameSignalNeverOverridesPendingFrameOutcome()
        {
            // Variant A: Win at frame N, then Loss at N+1 arrives BEFORE settlement of N -> Win must stand.
            {
                var run = CreatePlayingRun(out var health);
                int frame = 500;
                run.FrameProvider = () => frame;
                var outcomes = new List<LevelOutcome>();
                run.Finished += outcomes.Add;

                Assert.IsTrue(run.RequestWin(500));
                frame = 501;
                Assert.IsTrue(health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, null)));

                Assert.IsTrue(run.ResolvePendingOutcome(501));
                Assert.IsFalse(run.ResolvePendingOutcome(502));
                Assert.AreEqual(1, outcomes.Count, "[U14/A] count expected=1");
                Assert.AreEqual(LevelOutcome.Win, outcomes[0], "[U14/A] Win at N must not be overwritten by Loss at N+1.");
            }

            // Variant B: Loss at frame N, then Win at N+1 arrives BEFORE settlement of N -> Loss must stand.
            {
                var run = CreatePlayingRun(out var health);
                int frame = 600;
                run.FrameProvider = () => frame;
                var outcomes = new List<LevelOutcome>();
                run.Finished += outcomes.Add;

                Assert.IsTrue(health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, null)));
                Assert.IsFalse(run.RequestWin(601), "[U14/B] later frame signal must be ignored.");

                Assert.IsTrue(run.ResolvePendingOutcome(601));
                Assert.IsFalse(run.ResolvePendingOutcome(602));
                Assert.AreEqual(1, outcomes.Count, "[U14/B] count expected=1");
                Assert.AreEqual(LevelOutcome.Loss, outcomes[0], "[U14/B] Loss at N must not be overwritten by Win at N+1.");
            }
        }

        [Test]
        public void U15_LevelRunController_ReplayAndRestartKeyRejectedBeforeEnd_AcceptedOnceAfterEnd_AndRestoresTimeScale()
        {
            foreach (var outcome in new[] { LevelOutcome.Win, LevelOutcome.Loss })
            {
                var run = CreateRun(out _);
                int loadCalls = 0;
                int replayEvents = 0;
                run.ReplayLoader = () => { loadCalls++; return true; };
                run.ReplayRequested += () => replayEvents++;

                // Intro: TryReplay & R key rejected.
                Assert.IsFalse(run.TryReplay(), $"[U15/{outcome}/Intro] TryReplay rejected.");
                Assert.IsFalse(run.HandleRestartKeyPressed(), $"[U15/{outcome}/Intro] R rejected.");
                Assert.AreEqual(0, loadCalls);

                // Playing: TryReplay & R key rejected.
                run.TickIntro(new DummyPlayState(), 0f);
                Assert.IsFalse(run.TryReplay(), $"[U15/{outcome}/Playing] TryReplay rejected.");
                Assert.IsFalse(run.HandleRestartKeyPressed(), $"[U15/{outcome}/Playing] R rejected.");
                Assert.AreEqual(0, loadCalls);

                // Settle outcome with timeScale = 0 to verify replay & OnDestroy restore Time.timeScale = 1f.
                Time.timeScale = 0f;
                if (outcome == LevelOutcome.Win)
                    run.RequestWin(10);
                else
                    run.RequestLoss(10);
                Assert.IsTrue(run.ResolvePendingOutcome(11));

                Assert.AreEqual(LevelPhase.Ended, run.Phase);
                Assert.IsFalse(run.IsPlaying);
                Assert.IsTrue(run.GameplayLocked);

                // First replay accepted once; restores Time.timeScale = 1f.
                Assert.IsTrue(run.TryReplay(), $"[U15/{outcome}/FirstReplay] expected=true");
                Assert.AreEqual(LevelPhase.Reloading, run.Phase);
                Assert.AreEqual(1f, Time.timeScale, 0.0001f);
                Assert.AreEqual(1, loadCalls);
                Assert.AreEqual(1, replayEvents);

                // Second replay rejected while reloading.
                Assert.IsFalse(run.TryReplay(), $"[U15/{outcome}/SecondReplay] expected=false");
                Assert.IsFalse(run.HandleRestartKeyPressed(), $"[U15/{outcome}/SecondR] expected=false");
                Assert.AreEqual(1, loadCalls);
                Assert.AreEqual(1, replayEvents);
            }

            // Verify Awake and OnDestroy restore Time.timeScale = 1f.
            var cleanupRun = CreateRun(out _);
            Time.timeScale = 0f;
            typeof(LevelRunController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(cleanupRun, null);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "[U15/Awake] Time.timeScale must be restored to 1.");

            Time.timeScale = 0f;
            typeof(LevelRunController).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(cleanupRun, null);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "[U15/OnDestroy] Time.timeScale must be restored to 1.");
            Object.DestroyImmediate(cleanupRun.gameObject);
        }

        [Test]
        public void InputActions_GamepadRollAndCrouchBindings_DoNotCollide()
        {
            const string inputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputActionsPath);
            Assert.IsNotNull(actions, $"[U16/Asset] Missing InputActionAsset at '{inputActionsPath}'.");

            var playerMap = actions.FindActionMap("Player", throwIfNotFound: true);
            var rollAction = playerMap.FindAction("Roll", throwIfNotFound: true);
            var crouchAction = playerMap.FindAction("Crouch", throwIfNotFound: true);

            var rollGamepadPaths = CollectGamepadBindingPaths(rollAction);
            var crouchGamepadPaths = CollectGamepadBindingPaths(crouchAction);

            CollectionAssert.Contains(rollGamepadPaths, "<Gamepad>/buttonEast",
                "[U16/RollGamepad] Roll must keep <Gamepad>/buttonEast.");
            CollectionAssert.Contains(crouchGamepadPaths, "<Gamepad>/rightStickPress",
                "[U16/CrouchGamepad] Crouch must use <Gamepad>/rightStickPress.");

            foreach (string path in rollGamepadPaths)
            {
                Assert.IsFalse(crouchGamepadPaths.Contains(path),
                    $"[U16/Collision] Gamepad Roll and Crouch must not share binding '{path}'.");
            }
        }

        private static HashSet<string> CollectGamepadBindingPaths(InputAction action)
        {
            var paths = new HashSet<string>();
            foreach (var binding in action.bindings)
            {
                if (!binding.isComposite && !binding.isPartOfComposite &&
                    !string.IsNullOrEmpty(binding.path) && binding.path.StartsWith("<Gamepad>/"))
                {
                    paths.Add(binding.path);
                }
            }
            return paths;
        }

        [Test]
        public void RunEconomyReset_ZeroesWalletKillCounterAndGoldHud_WithoutTriggeringHitStopOrPickupFeedback()
        {
            Wallet.Reset();
            KillCounter.Reset();
            Time.timeScale = 1f;

            var movementConfig = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>("Assets/_Project/Features/Player/Data/PlayerMovementConfig.asset");
            var swordData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset");
            var bowData = AssetDatabase.LoadAssetAtPath<BowWeaponData>("Assets/_Project/Features/Combat/Data/Bow_StartBow.asset");
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<RecoverableArrow>("Assets/_Project/Features/Player/Prefabs/Projectile_PlayerArrow.prefab");
            Assert.IsNotNull(movementConfig, "Missing PlayerMovementConfig asset.");
            Assert.IsNotNull(swordData, "Missing Weapon_StartSword asset.");
            Assert.IsNotNull(bowData, "Missing Bow_StartBow asset.");
            Assert.IsNotNull(arrowPrefab, "Missing Projectile_PlayerArrow prefab.");

            var glintDef = VfxDefinition.Create("TestGoldGlint", System.Array.Empty<Sprite>(), 30f, 1f, 1, 1);
            var coinClip = AudioClip.Create("TestCoinClip", 256, 1, 22050, false);

            var walletGo = Track(new GameObject("GoldWallet"));
            var goldWallet = walletGo.AddComponent<GoldWallet>();

            var hudGo = Track(new GameObject("GoldHud"));
            var amountText = Track(new GameObject("GoldAmount", typeof(RectTransform))).AddComponent<Text>();
            var goldHud = hudGo.AddComponent<GoldHud>();
            SetPrivateField(goldHud, "_wallet", goldWallet);
            SetPrivateField(goldHud, "_amount", amountText);

            var vfxGo = Track(new GameObject("VfxSpawner"));
            var vfxSpawner = vfxGo.AddComponent<VfxSpawner>();
            SetPrivateField(vfxSpawner, "_prewarm", System.Array.Empty<VfxDefinition>());

            var audioGo = Track(new GameObject("PickupAudio"));
            var audioSource = audioGo.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var playerGo = Track(new GameObject("Player"));
            var sprite = playerGo.AddComponent<SpriteRenderer>();
            var animBridge = playerGo.AddComponent<PlayerAnimatorBridge>();
            var inputReader = playerGo.AddComponent<PlayerInputReader>();
            var playerHealth = playerGo.AddComponent<Health>();
            playerHealth.Initialize(100, 0f);

            var bow = playerGo.AddComponent<PlayerBow>();
            SetPrivateField(bow, "_data", bowData);
            SetPrivateField(bow, "_arrowPrefab", arrowPrefab);

            var controller = playerGo.AddComponent<PlayerController>();
            SetPrivateField(controller, "_config", movementConfig);
            SetPrivateField(controller, "_input", inputReader);
            SetPrivateField(controller, "_animator", animBridge);
            SetPrivateField(controller, "_sprite", sprite);
            SetPrivateField(controller, "_health", playerHealth);
            SetPrivateField(controller, "_weapon", swordData);
            SetPrivateField(controller, "_bow", bow);
            SetPrivateField(controller, "_autoRespawn", false);
            InvokePrivateMethod(controller, "Awake");

            var hitStop = Track(new GameObject("HitStop")).AddComponent<HitStop>();
            var cameraGo = Track(new GameObject("MainCamera"));
            cameraGo.AddComponent<Camera>();
            var cameraFollow = cameraGo.AddComponent<CameraFollow2D>();
            InvokePrivateMethod(cameraFollow, "Awake");

            var playerFeedback = playerGo.AddComponent<PlayerFeedback>();
            SetPrivateField(playerFeedback, "_controller", controller);
            SetPrivateField(playerFeedback, "_sprite", sprite);
            SetPrivateField(playerFeedback, "_hitStop", hitStop);
            SetPrivateField(playerFeedback, "_camera", cameraFollow);

            var pickupGo = Track(new GameObject("ItemPickupFeedback"));
            var pickupFeedback = pickupGo.AddComponent<ItemPickupFeedback>();
            SetPrivateField(pickupFeedback, "_player", playerGo.transform);
            SetPrivateField(pickupFeedback, "_wallet", goldWallet);
            SetPrivateField(pickupFeedback, "_effects", vfxSpawner);
            SetPrivateField(pickupFeedback, "_glint", glintDef);
            SetPrivateField(pickupFeedback, "_audio", audioSource);
            SetPrivateField(pickupFeedback, "_goldSound", coinClip);

            try
            {
                InvokePrivateMethod(goldWallet, "OnEnable");
                InvokePrivateMethod(goldHud, "OnEnable");
                InvokePrivateMethod(pickupFeedback, "OnEnable");
                InvokePrivateMethod(playerFeedback, "Start");

                // 1. Populate economy & kills while HUD and feedback listeners are active.
                Wallet.AddGold(35);
                Wallet.AddGems(4);
                KillCounter.AddKill();

                Assert.AreEqual(35, Wallet.Gold);
                Assert.AreEqual(4, Wallet.Gems);
                Assert.AreEqual(1, KillCounter.Kills);
                Assert.AreEqual("35", goldHud.Amount.text);
                Assert.AreEqual(35, GetPrivateField<int>(pickupFeedback, "_lastBalance"));
                Assert.AreEqual(0f, Time.timeScale, 0.0001f, "Positive kill should trigger HitStop.");

                var pooledGlint = vfxGo.GetComponentInChildren<PooledVfx>(true);
                Assert.IsNotNull(pooledGlint, "Positive gold gain must instantiate pooled glint VFX.");
                Assert.IsTrue(pooledGlint.gameObject.activeSelf);

                // Reset feedback probes before triggering economy reset.
                SetPrivateField(hitStop, "_resumeAt", -1f);
                SetPrivateField(hitStop, "_slowUntil", -1f);
                SetPrivateField(hitStop, "_isActive", false);
                Time.timeScale = 1f;
                SetPrivateField(cameraFollow, "_shakeUntil", -1f);
                pooledGlint.gameObject.SetActive(false);
                InvokePrivateMethod(vfxSpawner, "Release", pooledGlint);
                audioSource.Stop();

                // 2. Direct Reset() zeroes Wallet, KillCounter, and GoldHud without HitStop/shake/pickup FX.
                Wallet.Reset();
                KillCounter.Reset();

                Assert.AreEqual(0, Wallet.Gold);
                Assert.AreEqual(0, Wallet.Gems);
                Assert.AreEqual(0, KillCounter.Kills);
                Assert.AreEqual("0", goldHud.Amount.text);
                Assert.AreEqual(0, GetPrivateField<int>(pickupFeedback, "_lastBalance"));
                Assert.AreEqual(1f, Time.timeScale, 0.0001f, "KillCounter.Reset() must not trigger HitStop freeze.");
                Assert.Less(GetPrivateField<float>(hitStop, "_resumeAt"), 0f, "KillCounter.Reset() must not arm HitStop.");
                Assert.Less(GetPrivateField<float>(cameraFollow, "_shakeUntil"), 0f, "KillCounter.Reset() must not trigger camera shake.");
                Assert.IsFalse(pooledGlint.gameObject.activeSelf, "Wallet.Reset() must not trigger pickup glint VFX.");
                Assert.IsFalse(audioSource.isPlaying, "Wallet.Reset() must not trigger pickup SFX.");

                // 3. Subsequent positive gain updates GoldHud and _lastBalance and plays VFX normally.
                Wallet.AddGold(5);
                Assert.AreEqual("5", goldHud.Amount.text);
                Assert.AreEqual(5, GetPrivateField<int>(pickupFeedback, "_lastBalance"));
                Assert.IsTrue(pooledGlint.gameObject.activeSelf, "Subsequent positive gold gain must trigger glint VFX.");

                // 4. LevelRunController.Awake() also resets leftover economy & kills silently.
                Wallet.AddGold(12);
                Wallet.AddGems(2);
                KillCounter.AddKill();
                SetPrivateField(hitStop, "_resumeAt", -1f);
                SetPrivateField(hitStop, "_slowUntil", -1f);
                SetPrivateField(hitStop, "_isActive", false);
                Time.timeScale = 1f;
                SetPrivateField(cameraFollow, "_shakeUntil", -1f);
                pooledGlint.gameObject.SetActive(false);
                InvokePrivateMethod(vfxSpawner, "Release", pooledGlint);
                audioSource.Stop();

                var resetRun = Track(new GameObject("LevelRunController_Reset")).AddComponent<LevelRunController>();
                InvokePrivateMethod(resetRun, "Awake");

                Assert.AreEqual(0, Wallet.Gold);
                Assert.AreEqual(0, Wallet.Gems);
                Assert.AreEqual(0, KillCounter.Kills);
                Assert.AreEqual("0", goldHud.Amount.text);
                Assert.AreEqual(0, GetPrivateField<int>(pickupFeedback, "_lastBalance"));
                Assert.AreEqual(1f, Time.timeScale, 0.0001f, "LevelRunController.Awake() reset must not freeze Time.timeScale.");
                Assert.Less(GetPrivateField<float>(hitStop, "_resumeAt"), 0f);
                Assert.Less(GetPrivateField<float>(cameraFollow, "_shakeUntil"), 0f);
                Assert.IsFalse(pooledGlint.gameObject.activeSelf, "LevelRunController.Awake() reset must not trigger pickup glint VFX.");
                Assert.IsFalse(audioSource.isPlaying, "LevelRunController.Awake() reset must not trigger pickup SFX.");
            }
            finally
            {
                InvokePrivateMethod(pickupFeedback, "OnDisable");
                InvokePrivateMethod(goldHud, "OnDisable");
                InvokePrivateMethod(goldWallet, "OnDisable");
                InvokePrivateMethod(playerFeedback, "OnDestroy");
                InvokePrivateMethod(controller, "OnDestroy");
                InvokePrivateMethod(hitStop, "OnDisable");
                Object.DestroyImmediate(glintDef);
                Object.DestroyImmediate(coinClip);
            }
        }

        [Test]
        public void GoldHud_WhenWalletOrAmountIsNull_DoesNotThrowOnEnableOrRender()
        {
            var unlinkedHudGo = Track(new GameObject("GoldHud_NullWallet"));
            var unlinkedHud = unlinkedHudGo.AddComponent<GoldHud>();
            Assert.DoesNotThrow(() =>
            {
                InvokePrivateMethod(unlinkedHud, "OnEnable");
                InvokePrivateMethod(unlinkedHud, "OnDisable");
            }, "GoldHud with null _wallet must not throw on OnEnable/OnDisable.");

            var walletGo = Track(new GameObject("GoldWallet_NullText"));
            var goldWallet = walletGo.AddComponent<GoldWallet>();
            var noTextHudGo = Track(new GameObject("GoldHud_NullAmount"));
            var noTextHud = noTextHudGo.AddComponent<GoldHud>();
            SetPrivateField(noTextHud, "_wallet", goldWallet);

            Assert.DoesNotThrow(() =>
            {
                InvokePrivateMethod(goldWallet, "OnEnable");
                InvokePrivateMethod(noTextHud, "OnEnable");
                Wallet.AddGold(10);
                InvokePrivateMethod(noTextHud, "OnDisable");
                InvokePrivateMethod(goldWallet, "OnDisable");
            }, "GoldHud with null _amount must not throw on OnEnable/Render.");
        }

        [Test]
        public void WalletAndKillCounter_ResetOnPlay_ZeroesValuesAndClearsStaleSubscribers()
        {
            int walletCallbacks = 0;
            int killCallbacks = 0;
            void OnWallet(int _, int __) => walletCallbacks++;
            void OnKill(int _) => killCallbacks++;

            Wallet.Changed += OnWallet;
            KillCounter.Changed += OnKill;
            try
            {
                Wallet.AddGold(20);
                Wallet.AddGems(3);
                KillCounter.AddKill();
                Assert.AreEqual(2, walletCallbacks);
                Assert.AreEqual(1, killCallbacks);

                InvokeStaticPrivateMethod(typeof(Wallet), "ResetOnPlay");
                InvokeStaticPrivateMethod(typeof(KillCounter), "ResetOnPlay");

                Assert.AreEqual(0, Wallet.Gold);
                Assert.AreEqual(0, Wallet.Gems);
                Assert.AreEqual(0, KillCounter.Kills);

                Wallet.AddGold(1);
                KillCounter.AddKill();
                Assert.AreEqual(2, walletCallbacks, "Wallet.ResetOnPlay must clear stale Changed subscribers.");
                Assert.AreEqual(1, killCallbacks, "KillCounter.ResetOnPlay must clear stale Changed subscribers.");
            }
            finally
            {
                Wallet.Changed -= OnWallet;
                KillCounter.Changed -= OnKill;
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method {target.GetType().Name}.{methodName}");
            method.Invoke(target, args);
        }

        private static void InvokeStaticPrivateMethod(System.Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing static private method {type.Name}.{methodName}");
            method.Invoke(null, null);
        }

        private LevelRunController CreateRun(out Health health)
        {
            var hGo = Track(new GameObject("PlayerHealth"));
            health = hGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var runGo = Track(new GameObject("LevelRunController"));
            var run = runGo.AddComponent<LevelRunController>();
            run.Configure(null, health);
            return run;
        }

        private LevelRunController CreatePlayingRun(out Health health)
        {
            var run = CreateRun(out health);
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 0f));
            return run;
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
