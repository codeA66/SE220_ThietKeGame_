using System;
using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class PauseMenuTests
    {
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
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
        public void PauseMenu_RejectsToggleDuringIntroEndedAndScrollChoosing_AndPausesDuringPlaying()
        {
            var run = CreateFixture(out var player, out var stats, out var playerInput, out var panelRoot, out var menu, out var scrollPickup);

            // 1. Intro phase rejects pause toggle.
            Assert.AreEqual(LevelPhase.Intro, run.Phase);
            Assert.IsFalse(menu.TogglePause(), "[P4.2/Intro] Must reject pause during Intro.");
            Assert.IsFalse(menu.IsPaused);
            Assert.IsFalse(panelRoot.activeSelf);

            // 2. Playing phase + ScrollUpgradeMenu open (IsChoosing == true) rejects pause toggle.
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 0f));
            bool choiceOpened = (bool)typeof(PlayerRunStats)
                .GetMethod("TryBeginChoice", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stats, new object[] { scrollPickup });
            Assert.IsTrue(choiceOpened);
            Assert.IsTrue(stats.IsChoosing);
            Assert.IsFalse(menu.TogglePause(), "[P4.2/ScrollChoosing] Must reject pause while picking scroll stat.");
            Assert.IsFalse(menu.IsPaused);
            Assert.IsFalse(panelRoot.activeSelf);

            // Cancel scroll choice so normal Playing pause becomes available.
            stats.CancelChoice();
            Assert.IsFalse(stats.IsChoosing);

            // 3. First TogglePause during Playing pauses cleanly.
            Assert.IsTrue(menu.TogglePause(), "[P4.2/PauseOpen] Must pause during Playing.");
            Assert.IsTrue(menu.IsPaused);
            Assert.IsTrue(panelRoot.activeSelf);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            Assert.IsFalse(playerInput.enabled, "[P4.2/PauseInputOff]");

            // 4. Second TogglePause resumes cleanly.
            Assert.IsTrue(menu.TogglePause(), "[P4.2/PauseClose] Must resume on second toggle.");
            Assert.IsFalse(menu.IsPaused);
            Assert.IsFalse(panelRoot.activeSelf);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Assert.IsTrue(playerInput.enabled, "[P4.2/PauseInputOn]");

            // 5. Re-open pause then settle run -> auto-closes cleanly and rejects Ended toggles.
            Assert.IsTrue(menu.OpenPause());
            Assert.IsTrue(menu.IsPaused);
            Assert.IsTrue(run.RequestWin(10));
            Assert.IsTrue(run.ResolvePendingOutcome(11));
            Assert.AreEqual(LevelPhase.Ended, run.Phase);
            Assert.IsFalse(menu.IsPaused, "[P4.2/AutoCloseOnEnded]");
            Assert.IsFalse(panelRoot.activeSelf);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Assert.IsFalse(menu.TogglePause(), "[P4.2/EndedReject]");
        }

        [Test]
        public void PauseMenu_RestartFromPause_UnpausesAndReloadsDuringPlaying_AndQuitInvokesHandler()
        {
            var run = CreateFixture(out _, out _, out _, out var panelRoot, out var menu, out _);
            int loadCalls = 0;
            int replayEvents = 0;
            run.ReplayLoader = () =>
            {
                loadCalls++;
                return true;
            };
            run.ReplayRequested += () => replayEvents++;

            // Rejected during Intro.
            Assert.IsFalse(run.TryRestartFromPause(), "[P4.2/RestartIntroReject]");
            Assert.IsFalse(menu.RestartRun(), "[P4.2/MenuRestartIntroReject]");
            Assert.AreEqual(0, loadCalls);

            // Enter Playing and pause.
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 0f));
            Assert.IsTrue(menu.TogglePause(), "[P4.2/PauseBeforeRestart]");
            Assert.IsTrue(menu.IsPaused);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            // RestartRun unpauses, restores Time.timeScale, and enters Reloading via TryRestartFromPause.
            Assert.IsTrue(menu.RestartRun(), "[P4.2/RestartRun]");
            Assert.IsFalse(menu.IsPaused);
            Assert.IsFalse(panelRoot.activeSelf);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            Assert.AreEqual(LevelPhase.Reloading, run.Phase);
            Assert.AreEqual(1, loadCalls);
            Assert.AreEqual(1, replayEvents);

            // Repeated restart rejected while Reloading.
            Assert.IsFalse(run.TryRestartFromPause(), "[P4.2/ReloadingReject]");
            Assert.AreEqual(1, loadCalls);

            // QuitGame invokes injected QuitHandler.
            bool quitInvoked = false;
            menu.QuitHandler = () => quitInvoked = true;
            menu.QuitGame();
            Assert.IsTrue(quitInvoked, "[P4.2/QuitHandler]");
        }

        private LevelRunController CreateFixture(
            out PlayerController player,
            out PlayerRunStats stats,
            out PlayerInputReader playerInput,
            out GameObject panelRoot,
            out PauseMenuController menu,
            out PowerScrollPickup scrollPickup)
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.IsNotNull(actions, "Missing InputActionAsset at " + InputActionsPath);

            var playerGo = Track(new GameObject("Player"));
            playerGo.SetActive(false);
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            playerGo.AddComponent<BoxCollider2D>();
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);
            playerInput = playerGo.AddComponent<PlayerInputReader>();
            SetObjectProperty(playerInput, "_actions", actions);
            player = playerGo.AddComponent<PlayerController>();
            playerGo.SetActive(true);

            stats = new PlayerRunStats(health);
            stats.ChoiceOpened += _ => { };
            typeof(PlayerController)
                .GetProperty("RunStats", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(player, stats);

            var hitStop = Track(new GameObject("HitStop")).AddComponent<HitStop>();

            var runGo = Track(new GameObject("LevelRunController"));
            var run = runGo.AddComponent<LevelRunController>();
            run.Configure(player, health, playerInput, null, hitStop);

            var scrollGo = Track(new GameObject("ScrollPickup"));
            var iconGo = Track(new GameObject("Icon"));
            iconGo.transform.SetParent(scrollGo.transform, false);
            var promptGo = Track(new GameObject("Prompt"));
            promptGo.transform.SetParent(scrollGo.transform, false);
            scrollPickup = scrollGo.AddComponent<PowerScrollPickup>();
            SetObjectProperty(scrollPickup, "_icon", iconGo.transform);
            SetObjectProperty(scrollPickup, "_prompt", promptGo);

            var menuRoot = Track(new GameObject("PauseMenu"));
            panelRoot = Track(new GameObject("PanelRoot"));
            panelRoot.transform.SetParent(menuRoot.transform, false);
            panelRoot.SetActive(false);

            var resumeBtn = CreateButton(panelRoot.transform, "ResumeButton");
            var restartBtn = CreateButton(panelRoot.transform, "RestartButton");
            var quitBtn = CreateButton(panelRoot.transform, "QuitButton");

            menu = menuRoot.AddComponent<PauseMenuController>();
            menu.Configure(run, player, playerInput, hitStop, panelRoot, resumeBtn, restartBtn, quitBtn);
            return run;
        }

        private Button CreateButton(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Button>();
        }

        private static void SetObjectProperty(Object target, string propName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
