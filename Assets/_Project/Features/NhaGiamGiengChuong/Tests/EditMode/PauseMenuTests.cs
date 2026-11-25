using System;
using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.HUD;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class PauseMenuTests
    {
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        private readonly List<GameObject> _created = new();
        private readonly InputTestFixture _inputFixture = new();
        private bool _inputFixtureActive;
        private string _previousScene;

        private sealed class DummyPlayState : IPlayerState
        {
            public void Enter() { }
            public void Exit() { }
            public void Tick() { }
            public void FixedTick() { }
        }

        [SetUp]
        public void SetUp()
        {
            _previousScene = Assets.GeometryAuditHelper.CaptureCurrentScenePath();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ResetMinimapStaticState();
            _inputFixture.Setup();
            _inputFixtureActive = true;
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
            ResetMinimapStaticState();
            Time.timeScale = 1f;
            if (_inputFixtureActive)
            {
                _inputFixture.TearDown();
                _inputFixtureActive = false;
            }
            Assets.GeometryAuditHelper.RestoreScene(_previousScene);
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

        [Test]
        public void HitStop_MenuPause_IsReferenceCounted_AndPreservesRemainingFreezeAndSlow()
        {
            var hitStop = Track(new GameObject("HitStop")).AddComponent<HitStop>();
            Assert.IsFalse(hitStop.IsMenuPaused);

            // Two menu callers acquire pause -> releasing one keeps Time.timeScale == 0f.
            hitStop.SetMenuPaused(true);
            hitStop.SetMenuPaused(true);
            Assert.IsTrue(hitStop.IsMenuPaused);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            hitStop.SetMenuPaused(false);
            Assert.IsTrue(hitStop.IsMenuPaused);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            hitStop.SetMenuPaused(false);
            Assert.IsFalse(hitStop.IsMenuPaused);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);

            // Extra release clamps at zero without error.
            hitStop.SetMenuPaused(false);
            Assert.IsFalse(hitStop.IsMenuPaused);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);

            // Active Trigger freeze survives menu pause + release, and Slow starts after _resumeAt.
            hitStop.Trigger(0.5f);
            float resumeAt = GetPrivateField<float>(hitStop, "_resumeAt");
            hitStop.Slow(0.25f, 0.35f);
            float slowUntil = GetPrivateField<float>(hitStop, "_slowUntil");
            Assert.GreaterOrEqual(slowUntil, resumeAt + 0.25f - 0.001f,
                "Slow window must start after active Trigger freeze ends.");

            hitStop.SetMenuPaused(true);
            Assert.IsTrue(hitStop.IsMenuPaused);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            hitStop.SetMenuPaused(false);
            Assert.IsFalse(hitStop.IsMenuPaused);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f, "Remaining Trigger freeze must remain active after menu resume.");

            // Once Trigger expires, Slow takes effect at 0.35f; once Slow expires, Time.timeScale returns to 1f.
            SetPrivateField(hitStop, "_resumeAt", Time.unscaledTime - 0.01f);
            InvokeMethod(hitStop, "Update");
            Assert.AreEqual(0.35f, Time.timeScale, 0.0001f);

            SetPrivateField(hitStop, "_slowUntil", Time.unscaledTime - 0.01f);
            InvokeMethod(hitStop, "Update");
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        }

        [Test]
        public void MinimapAndPause_EscapeClosesFullMapOnly_AndRestoresPlayerInputWithoutOpeningPause()
        {
            var run = CreateFixture(out var player, out _, out var playerInput, out var panelRoot, out var menu, out _);
            var hitStop = Object.FindAnyObjectByType<HitStop>();
            Assert.IsNotNull(hitStop);
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 0f));

            var minimapGo = Track(new GameObject("MinimapHud"));
            var minimap = minimapGo.AddComponent<MinimapHud>();
            InvokeMethod(minimap, "OnEnable");
            InvokeMethod(minimap, "Start");
            Assert.AreSame(minimap, MinimapHud.Active);

            // Full Map can open during an active combat HitStop freeze and acquires its own pause + input lock.
            hitStop.Trigger(10f);
            Assert.AreEqual(0f, Time.timeScale, 0.0001f);

            minimap.ToggleFullMap();
            Assert.IsTrue(minimap.IsFullMapOpen);
            Assert.IsTrue(MinimapHud.IsAnyFullMapOpen);
            Assert.IsTrue(hitStop.IsMenuPaused);
            Assert.IsFalse(playerInput.enabled);

            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                // Order A: MinimapHud processes Esc first -> closes Full Map, stamps LastEscapeCloseFrame, blocks Pause.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                InvokeMethod(minimap, "Update");

                Assert.IsFalse(minimap.IsFullMapOpen);
                Assert.IsFalse(MinimapHud.IsAnyFullMapOpen);
                Assert.AreEqual(Time.frameCount, MinimapHud.LastEscapeCloseFrame);
                Assert.IsFalse(hitStop.IsMenuPaused);
                Assert.IsTrue(playerInput.enabled, "Closing Full Map must restore PlayerInputReader even during combat HitStop.");
                Assert.AreEqual(0f, Time.timeScale, 0.0001f, "Remaining HitStop freeze must be preserved.");

                Assert.IsFalse(menu.HandlePauseInput(true));
                Assert.IsFalse(menu.IsPaused);
                Assert.IsFalse(panelRoot.activeSelf);

                // Clear HitStop freeze and test Order B: PauseMenuController processes Esc before MinimapHud.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                SetPrivateField(hitStop, "_resumeAt", -1f);
                InvokeMethod(hitStop, "Update");
                Assert.AreEqual(1f, Time.timeScale, 0.0001f);

                minimap.ToggleFullMap();
                Assert.IsTrue(minimap.IsFullMapOpen);
                Assert.IsFalse(menu.HandlePauseInput(true));
                Assert.IsFalse(minimap.IsFullMapOpen);
                Assert.IsFalse(menu.IsPaused);
                Assert.IsTrue(playerInput.enabled);
                Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }

            // Full Map cannot open while PauseMenuController is paused.
            Assert.IsTrue(menu.OpenPause());
            minimap.ToggleFullMap();
            Assert.IsFalse(minimap.IsFullMapOpen);
            Assert.IsTrue(menu.Resume());

            // Full Map cannot open while player is disabled, and auto-closes without re-enabling input.
            minimap.ToggleFullMap();
            Assert.IsTrue(minimap.IsFullMapOpen);
            Assert.IsFalse(playerInput.enabled);
            player.enabled = false;
            InvokeMethod(minimap, "Update");
            Assert.IsFalse(minimap.IsFullMapOpen);
            Assert.IsFalse(playerInput.enabled);
            minimap.ToggleFullMap();
            Assert.IsFalse(minimap.IsFullMapOpen);
        }

        [Test]
        public void PlayerController_SkipsInputPollingWhenInputReaderDisabled()
        {
            var floor = Track(new GameObject("Floor_PauseMenu_Test"));
            floor.layer = Assets.GeometryAuditHelper.LayerGround;
            var floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.offset = new Vector2(0f, -0.5f);
            floorCol.size = new Vector2(20f, 1f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Player/Prefabs/Player.prefab");
            Assert.IsNotNull(prefab, "Missing Player.prefab");
            var playerGo = Track((GameObject)PrefabUtility.InstantiatePrefab(prefab));
            playerGo.transform.position = Vector3.zero;
            Physics2D.SyncTransforms();

            var player = playerGo.GetComponent<PlayerController>();
            var health = playerGo.GetComponent<Health>();
            var input = playerGo.GetComponent<PlayerInputReader>();
            var animBridge = playerGo.GetComponentInChildren<PlayerAnimatorBridge>(true);

            InvokeMethod(health, "Awake");
            if (animBridge != null)
                InvokeMethod(animBridge, "Awake");
            InvokeMethod(input, "Awake");
            InvokeMethod(input, "OnEnable");
            InvokeMethod(player, "Awake");
            InvokeMethod(player, "Start");

            var machine = GetPrivateField<PlayerStateMachine>(player, "_machine");
            machine.ForceChangeState(machine.Idle);
            SetPrivateField(machine, "_lastTransitionFrame", -1);

            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C, Key.D));
                InputSystem.Update();
                InvokeMethod(player, "Update");

                Assert.AreSame(machine.Crouch, player.CurrentState, "Holding crouch on ground must enter Crouch state.");
                Assert.IsTrue(input.CrouchHeld);
                Assert.Greater(input.MoveX, 0.5f);

                // Disabling PlayerInputReader clears latched values and skips Update polling so player stays crouched.
                input.enabled = false;
                InvokeMethod(input, "OnDisable");
                Assert.AreEqual(0f, input.MoveX, 0.0001f);
                Assert.IsFalse(input.CrouchHeld);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                SetPrivateField(machine, "_lastTransitionFrame", -1);
                InvokeMethod(player, "Update");
                Assert.AreSame(machine.Crouch, player.CurrentState,
                    "Disabling PlayerInputReader must keep player crouched instead of polling zeroes.");

                // Re-enabling PlayerInputReader with keys released lets PlayerController resume polling and stand up.
                input.enabled = true;
                InvokeMethod(input, "OnEnable");
                SetPrivateField(machine, "_lastTransitionFrame", -1);
                InvokeMethod(player, "Update");
                Assert.AreSame(machine.Idle, player.CurrentState,
                    "Re-enabling PlayerInputReader without held crouch keys must restore Idle.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void ScrollUpgradeMenu_HandlesNullPlayer_AndSynchronizesInputLock()
        {
            var run = CreateFixture(out var player, out var stats, out var playerInput, out _, out _, out var scrollPickup);
            var health = player.GetComponent<Health>();
            SetObjectProperty(player, "_health", health);
            var hitStop = Object.FindAnyObjectByType<HitStop>();
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 0f));

            // Null _player on OnEnable/Start must not throw.
            var uninitializedMenuGo = Track(new GameObject("ScrollMenu_NullPlayer"));
            var uninitializedMenu = uninitializedMenuGo.AddComponent<ScrollUpgradeMenu>();
            Assert.DoesNotThrow(() =>
            {
                InvokeMethod(uninitializedMenu, "OnEnable");
                InvokeMethod(uninitializedMenu, "Start");
            });

            var scrollMenuGo = Track(new GameObject("ScrollMenu"));
            var panel = Track(new GameObject("ScrollPanel"));
            panel.transform.SetParent(scrollMenuGo.transform, false);
            panel.SetActive(false);

            var optGo = Track(new GameObject("Option_Brutality", typeof(RectTransform), typeof(Image), typeof(Button)));
            optGo.transform.SetParent(panel.transform, false);
            var previewText = Track(new GameObject("Preview", typeof(RectTransform), typeof(Text))).GetComponent<Text>();
            previewText.transform.SetParent(optGo.transform, false);
            var levelText = Track(new GameObject("Level", typeof(RectTransform), typeof(Text))).GetComponent<Text>();
            levelText.transform.SetParent(optGo.transform, false);
            var option = optGo.AddComponent<StatUpgradeOption>();
            SetPrivateField(option, "_stat", CombatStat.Brutality);
            SetObjectProperty(option, "_button", optGo.GetComponent<Button>());
            SetObjectProperty(option, "_preview", previewText);
            SetObjectProperty(option, "_hudLevel", levelText);

            var scrollMenu = scrollMenuGo.AddComponent<ScrollUpgradeMenu>();
            SetObjectProperty(scrollMenu, "_player", player);
            SetObjectProperty(scrollMenu, "_gameplayTime", hitStop);
            SetObjectProperty(scrollMenu, "_panel", panel);
            SetPrivateField(scrollMenu, "_options", new[] { option });
            InvokeMethod(scrollMenu, "Start");

            bool opened = (bool)typeof(PlayerRunStats)
                .GetMethod("TryBeginChoice", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stats, new object[] { scrollPickup });
            Assert.IsTrue(opened);
            Assert.IsTrue(hitStop.IsMenuPaused);
            Assert.IsFalse(playerInput.enabled, "ScrollUpgradeMenu.Open must disable PlayerInputReader.");

            // Normal choice release restores PlayerInputReader when no other pause ref is held.
            scrollMenu.ChooseBrutality();
            Assert.IsFalse(hitStop.IsMenuPaused);
            Assert.IsTrue(playerInput.enabled, "ScrollUpgradeMenu.Close must restore PlayerInputReader when unblocked.");

            // Re-open with a second scroll while another pause ref is held -> ChooseBrutality must NOT re-enable input early.
            var scrollGo2 = Track(new GameObject("ScrollPickup2"));
            var iconGo2 = Track(new GameObject("Icon2"));
            iconGo2.transform.SetParent(scrollGo2.transform, false);
            var promptGo2 = Track(new GameObject("Prompt2"));
            promptGo2.transform.SetParent(scrollGo2.transform, false);
            var scrollPickup2 = scrollGo2.AddComponent<PowerScrollPickup>();
            SetObjectProperty(scrollPickup2, "_icon", iconGo2.transform);
            SetObjectProperty(scrollPickup2, "_prompt", promptGo2);

            bool opened2 = (bool)typeof(PlayerRunStats)
                .GetMethod("TryBeginChoice", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stats, new object[] { scrollPickup2 });
            Assert.IsTrue(opened2);
            Assert.IsFalse(playerInput.enabled);
            hitStop.SetMenuPaused(true);
            scrollMenu.ChooseBrutality();
            Assert.IsTrue(hitStop.IsMenuPaused);
            Assert.IsFalse(playerInput.enabled, "PlayerInputReader must remain disabled while another pause ref is held.");
            hitStop.SetMenuPaused(false);
            Assert.IsFalse(hitStop.IsMenuPaused);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }

        private static void InvokeMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(method, $"Missing method {target.GetType().Name}.{methodName}");
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void ResetMinimapStaticState()
        {
            typeof(MinimapHud)
                .GetMethod("ResetStaticState", BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, null);
        }
    }
}
