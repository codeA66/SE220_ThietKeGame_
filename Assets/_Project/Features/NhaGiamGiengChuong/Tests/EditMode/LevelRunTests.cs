using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Player.StateMachine;
using DeadCells.Player.StateMachine.States;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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
        public void U16_InputActions_GamepadRollAndCrouchBindings_DoNotCollide()
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
