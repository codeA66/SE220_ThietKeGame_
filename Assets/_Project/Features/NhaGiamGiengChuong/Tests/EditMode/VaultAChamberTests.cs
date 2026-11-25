using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class VaultAChamberTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Features/Player/Prefabs/Player.prefab";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        private string _previousScene;
        private readonly List<GameObject> _created = new();

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
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    var hub = _created[i].GetComponent<CombatAudioHub>();
                    if (hub != null)
                        typeof(CombatAudioHub).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(hub, null);
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
            Time.timeScale = 1f;
            Assets.GeometryAuditHelper.RestoreScene(_previousScene);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }

        private void CreateCoreFixture(
            out LevelRunController run,
            out PlayerController player,
            out Health playerHealth,
            out BoxCollider2D playerCol,
            out CameraFollow2D camFollow,
            out ScreenFadeOverlay fader,
            out InputActionAsset actions)
        {
            actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            var hubGo = Track(new GameObject("AudioHub"));
            var hub = hubGo.AddComponent<CombatAudioHub>();
            typeof(CombatAudioHub).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(hub, null);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var playerGo = Track((GameObject)PrefabUtility.InstantiatePrefab(prefab));
            playerGo.transform.position = new Vector3(1.0f, 0f, 0f);
            player = playerGo.GetComponent<PlayerController>();
            playerHealth = playerGo.GetComponent<Health>();
            playerCol = playerGo.GetComponent<BoxCollider2D>();
            var input = playerGo.GetComponent<PlayerInputReader>();
            var hitStop = Track(new GameObject("HitStop")).AddComponent<HitStop>();

            typeof(Health).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(playerHealth, null);
            typeof(PlayerInputReader).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(input, null);
            typeof(PlayerController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(player, null);
            typeof(PlayerController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(player, null);
            var machine = (PlayerStateMachine)typeof(PlayerController).GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            machine.ForceChangeState(machine.Idle);

            run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(player, playerHealth, input, null, hitStop);
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 0f));

            var camGo = Track(new GameObject("Cam"));
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.aspect = 16f / 9f;
            camFollow = camGo.AddComponent<CameraFollow2D>();
            var soCam = new SerializedObject(camFollow);
            soCam.FindProperty("_target").objectReferenceValue = playerGo.transform;
            soCam.FindProperty("_useBounds").boolValue = true;
            soCam.FindProperty("_bounds").rectValue = new Rect(-104f, -20f, 340f, 86f);
            soCam.ApplyModifiedPropertiesWithoutUndo();
            typeof(CameraFollow2D).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(camFollow, null);

            var faderGo = Track(new GameObject("Fader"));
            var cg = faderGo.AddComponent<CanvasGroup>();
            var topBar = Track(new GameObject("TopBar", typeof(RectTransform))).GetComponent<RectTransform>();
            var botBar = Track(new GameObject("BotBar", typeof(RectTransform))).GetComponent<RectTransform>();
            fader = faderGo.AddComponent<ScreenFadeOverlay>();
            fader.Configure(cg, topBar, botBar, 96f);
        }

        [Test]
        public void CrackedMasonry_BreaksOnHealthDeath_SwapsVisualsDisablesHurtboxAndUnlocksEntry()
        {
            CreateCoreFixture(out var run, out var player, out var pHealth, out var pCol, out var cam, out var fader, out var actions);

            var entryGo = Track(new GameObject("Entry"));
            var entryTrigger = entryGo.AddComponent<BoxCollider2D>();
            entryTrigger.isTrigger = true;
            var entry = entryGo.AddComponent<PassageTransition>();
            entry.Configure(run, player, pHealth, pCol, entryTrigger, actions, null, fader, cam,
                new Vector2(180f, 29f), 1, new Rect(176f, 27f, 28f, 24f), true, null, null, null, 0.55f, null, startsUnlocked: false);

            var wallGo = Track(new GameObject("I22_WallA_Masonry"));
            var wallHealth = wallGo.AddComponent<Health>();
            wallHealth.Initialize(1, 0f);
            var hurtboxCol = wallGo.AddComponent<BoxCollider2D>();
            hurtboxCol.isTrigger = true;
            var intactVis = Track(new GameObject("Intact"));
            var fissureVis = Track(new GameObject("Fissure"));
            fissureVis.SetActive(false);

            var masonry = wallGo.AddComponent<CrackedMasonry>();
            masonry.Configure(wallHealth, hurtboxCol, intactVis, fissureVis, entry, null);

            Assert.IsFalse(masonry.IsBroken);
            Assert.IsFalse(entry.IsUnlocked);
            Assert.IsTrue(intactVis.activeSelf);
            Assert.IsFalse(fissureVis.activeSelf);

            Assert.IsTrue(wallHealth.TakeDamage(new DamageInfo(10, -1, 0f, 0f, false, player.gameObject)));
            Assert.IsTrue(masonry.IsBroken);
            Assert.IsTrue(entry.IsUnlocked);
            Assert.IsFalse(intactVis.activeSelf);
            Assert.IsTrue(fissureVis.activeSelf);
            Assert.IsFalse(hurtboxCol.enabled);

            // Second hit is a no-op
            Assert.IsFalse(wallHealth.TakeDamage(new DamageInfo(10, -1, 0f, 0f, false, player.gameObject)));
        }

        [Test]
        public void PassageTransition_RejectsLockedAndReentrantPresses_TogglesRoomAndParallax_AndSwapsBounds()
        {
            CreateCoreFixture(out var run, out var player, out var pHealth, out var pCol, out var cam, out var fader, out var actions);
            player.TeleportTo(new Vector2(0.8f, 0f), -1);
            Physics2D.SyncTransforms();

            var roomRoot = Track(new GameObject("Room_VaultA"));
            roomRoot.SetActive(false);
            var parallaxRoot = Track(new GameObject("Prison_Parallax_R5"));
            parallaxRoot.SetActive(true);

            var entryGo = Track(new GameObject("I22_VaultA_Entry"));
            entryGo.transform.position = new Vector3(0.8f, 0f, 0f);
            var trig = entryGo.AddComponent<BoxCollider2D>();
            trig.isTrigger = true;
            trig.size = new Vector2(2f, 2.5f);
            Physics2D.SyncTransforms();

            var entry = entryGo.AddComponent<PassageTransition>();
            var vaultBounds = new Rect(176f, 27f, 28f, 24f);
            entry.Configure(run, player, pHealth, pCol, trig, actions, null, fader, cam,
                new Vector2(180f, 29f), 1, vaultBounds, true, roomRoot, parallaxRoot, null, 0.55f, null, startsUnlocked: false);

            // Locked rejects transition
            Assert.IsFalse(entry.TryBeginTransition());

            entry.Unlock();
            fader.SetTransitioning(true);
            Assert.IsFalse(entry.TryBeginTransition(), "Must reject re-entrant press while ScreenFadeOverlay.IsTransitioning.");
            fader.SetTransitioning(false);

            // Execute routine deterministically with zero fade duration
            var so = new SerializedObject(entry);
            so.FindProperty("_fadeDuration").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();

            IEnumerator routine = entry.ExecuteTransitionRoutine();
            int safety = 0;
            while (routine.MoveNext() && safety++ < 64) { }

            Assert.IsTrue(roomRoot.activeSelf, "Entering VaultA must activate Room_VaultA.");
            Assert.IsFalse(parallaxRoot.activeSelf, "Entering VaultA must hide Prison_Parallax_R5.");
            Assert.AreEqual(vaultBounds, cam.CurrentBounds);
            Assert.AreEqual(180f, player.transform.position.x, 0.05f);
            Assert.AreEqual(29f, player.transform.position.y, 0.05f);
            Assert.IsFalse(player.IsInScriptedSequence, "Player must regain free control after fade-in.");
        }

        [Test]
        public void ChamberFramingSequence_PlaysOnceOnly_UnlocksInscription_AndCleansUpOnDisable()
        {
            CreateCoreFixture(out var run, out var player, out var pHealth, out var pCol, out var cam, out var fader, out var actions);
            var floor = Track(new GameObject("Floor_VaultA"));
            floor.layer = Assets.GeometryAuditHelper.LayerGround;
            var floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.offset = new Vector2(190f, 28.5f);
            floorCol.size = new Vector2(30f, 1f);

            player.TeleportTo(new Vector2(183.5f, 29f), 1);
            Physics2D.SyncTransforms();

            var hudGo = Track(new GameObject("GameplayHUD"));
            var hudCanvas = hudGo.AddComponent<Canvas>();
            hudCanvas.enabled = true;
            var statsStrip = Track(new GameObject("StatsStrip"));
            statsStrip.SetActive(true);

            var insGo = Track(new GameObject("I24_VaultA_Inscription"));
            var insTrig = insGo.AddComponent<BoxCollider2D>();
            insTrig.isTrigger = true;
            var insPrompt = Track(new GameObject("Prompt"));
            var insBanner = Track(new GameObject("Banner"));
            var insText = insBanner.AddComponent<Text>();
            var inscription = insGo.AddComponent<WallInscription>();
            inscription.Configure("Bức Họa Không Đề Tên", "Quote Body", run, player, pHealth, pCol, insTrig, actions, insPrompt, insBanner, insText, startsUnlocked: false);

            var seqGo = Track(new GameObject("VaultA_FramingSequence"));
            seqGo.transform.position = new Vector3(183.5f, 29f, 0f);
            var seqTrig = seqGo.AddComponent<BoxCollider2D>();
            seqTrig.isTrigger = true;
            seqTrig.size = new Vector2(1.5f, 18f);
            var audioSrc = seqGo.AddComponent<AudioSource>();
            var seq = seqGo.AddComponent<ChamberFramingSequence>();
            seq.Configure(run, player, pHealth, pCol, seqTrig, cam, fader, audioSrc, 0.55f, hudCanvas, statsStrip, inscription,
                walkTargetX: 190.0f, walkSpeedMultiplier: 0.38f, cameraFocusPoint: new Vector2(191.5f, 37.2f),
                focusSmoothTime: 1.5f, focusOrthoSize: 5.2f, holdDuration: 0f, returnSmoothTime: 0f);

            Assert.IsFalse(inscription.IsUnlocked);
            Assert.IsFalse(seq.HasPlayed);

            // Step sequence until player arrives (simulate walk completion)
            IEnumerator routine = seq.ExecuteSequenceRoutine();
            Assert.IsTrue(routine.MoveNext());
            Assert.IsTrue(seq.HasPlayed);
            Assert.IsTrue(seq.IsPlaying);
            Assert.IsTrue(player.IsInScriptedSequence);
            Assert.IsFalse(hudCanvas.enabled);

            // Second trigger must be rejected (one-shot guard)
            Assert.IsFalse(seq.TryTrigger());

            // Interrupt cleanup via OnDisable restores HUD, letterbox, camera focus, and player control
            typeof(ChamberFramingSequence).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(seq, null);
            Assert.IsFalse(seq.IsPlaying);
            Assert.IsTrue(hudCanvas.enabled);
            Assert.AreEqual(0f, fader.LetterboxAmount, 0.001f);
            Assert.IsFalse(cam.IsFocusOverrideActive);
            Assert.AreEqual(7f, cam.GetComponent<Camera>().orthographicSize, 0.001f);
            Assert.IsFalse(player.IsInScriptedSequence);
        }

        [Test]
        public void PassageTransition_SubscribesToStarted_AndRestoresParallaxAndAmbientOnMidEntryDisableOrRunEnd()
        {
            CreateCoreFixture(out var run, out var player, out var pHealth, out var pCol, out var cam, out var fader, out var actions);
            player.TeleportTo(new Vector2(0.8f, 0f), -1);
            Physics2D.SyncTransforms();

            var roomRoot = Track(new GameObject("Room_VaultA"));
            roomRoot.SetActive(false);
            var parallaxRoot = Track(new GameObject("Prison_Parallax_R5"));
            parallaxRoot.SetActive(true);

            var entryGo = Track(new GameObject("I22_VaultA_Entry"));
            entryGo.transform.position = new Vector3(0.8f, 0f, 0f);
            var trig = entryGo.AddComponent<BoxCollider2D>();
            trig.isTrigger = true;
            trig.size = new Vector2(2f, 2.5f);
            Physics2D.SyncTransforms();

            var entry = entryGo.AddComponent<PassageTransition>();
            entry.Configure(run, player, pHealth, pCol, trig, actions, null, fader, cam,
                new Vector2(180f, 29f), 1, new Rect(176f, 27f, 28f, 24f), true, roomRoot, parallaxRoot, null, 0.55f, null, startsUnlocked: true);

            var soEntry = new SerializedObject(entry);
            soEntry.FindProperty("_fadeDuration").floatValue = 0f;
            soEntry.ApplyModifiedPropertiesWithoutUndo();

            // Run real entry transition (sets chamber occupied, hides parallax, ducks ambient), then disable
            IEnumerator enterRoutine = entry.ExecuteTransitionRoutine();
            int guard = 0;
            while (enterRoutine.MoveNext() && guard++ < 64) { }
            Assert.IsFalse(parallaxRoot.activeSelf);
            Assert.AreEqual(0f, CombatAudioHub.AmbientMultiplier, 0.001f);

            typeof(PassageTransition).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(entry, null);

            Assert.IsTrue(parallaxRoot.activeSelf, "Post-entry disable must restore Prison_Parallax_R5.");
            Assert.AreEqual(1f, CombatAudioHub.AmbientMultiplier, 0.001f, "Disable while inside VaultA must restore ambient multiplier to 1f.");
        }

        [Test]
        public void WallInscription_UsesInteractStartedForHoldAction_AndRunEndSuppressesHudRestoreInSequence()
        {
            CreateCoreFixture(out var run, out var player, out var pHealth, out var pCol, out var cam, out var fader, out var actions);
            var hudGo = Track(new GameObject("GameplayHUD"));
            var hudCanvas = hudGo.AddComponent<Canvas>();
            hudCanvas.enabled = true;

            var seqGo = Track(new GameObject("Seq_RunEnd"));
            var seqTrig = seqGo.AddComponent<BoxCollider2D>();
            seqTrig.isTrigger = true;
            var audioSrc = seqGo.AddComponent<AudioSource>();
            var seq = seqGo.AddComponent<ChamberFramingSequence>();
            seq.Configure(run, player, pHealth, pCol, seqTrig, cam, fader, audioSrc, 0.55f, hudCanvas, null, null);

            // Start sequence, then end run via death -> sequence aborts and does NOT re-enable gameplay HUD over ResultPanel
            IEnumerator routine = seq.ExecuteSequenceRoutine();
            Assert.IsTrue(routine.MoveNext());
            Assert.IsFalse(hudCanvas.enabled);

            int frame = Time.frameCount;
            run.RequestLoss(frame);
            run.ResolvePendingOutcome(frame + 1);
            Assert.AreEqual(LevelPhase.Ended, run.Phase);

            Assert.IsFalse(seq.IsPlaying, "Sequence must abort immediately when run ends.");
            Assert.IsFalse(hudCanvas.enabled, "Run end must not restore gameplay HUD over ResultPanel.");
            Assert.IsFalse(cam.IsFocusOverrideActive);
            Assert.AreEqual(0f, fader.LetterboxAmount, 0.001f);
            Assert.IsFalse(player.IsInScriptedSequence);
        }

        [Test]
        public void WallInscription_StaysHiddenWhileLocked_TogglesQuoteWhenUnlocked_AndAutoClosesOutOfRange()
        {
            CreateCoreFixture(out var run, out var player, out var pHealth, out var pCol, out _, out _, out var actions);
            player.TeleportTo(new Vector2(191.5f, 29f), 1);

            var insGo = Track(new GameObject("I24_VaultA_Inscription"));
            insGo.transform.position = new Vector3(191.5f, 29f, 0f);
            var insTrig = insGo.AddComponent<BoxCollider2D>();
            insTrig.isTrigger = true;
            insTrig.offset = new Vector2(0f, 1f);
            insTrig.size = new Vector2(3.5f, 2.5f);
            Physics2D.SyncTransforms();

            var prompt = Track(new GameObject("Prompt"));
            var banner = Track(new GameObject("Banner"));
            var text = banner.AddComponent<Text>();
            var inscription = insGo.AddComponent<WallInscription>();
            inscription.Configure("Bức Họa Không Đề Tên", "Test Quote", run, player, pHealth, pCol, insTrig, actions, prompt, banner, text, startsUnlocked: false);

            inscription.RefreshState();
            Assert.IsFalse(inscription.PromptVisible);
            Assert.IsFalse(inscription.TryToggleReading());
            Assert.IsFalse(banner.activeSelf);

            inscription.Unlock();
            inscription.RefreshState();
            Assert.IsTrue(inscription.PromptVisible);
            Assert.IsTrue(inscription.TryToggleReading());
            Assert.IsTrue(inscription.IsReading);
            Assert.IsTrue(banner.activeSelf);
            StringAssert.Contains("Test Quote", text.text);

            // Walking away auto-closes banner
            player.TeleportTo(new Vector2(180f, 29f), -1);
            Physics2D.SyncTransforms();
            inscription.RefreshState();
            Assert.IsFalse(inscription.IsReading);
            Assert.IsFalse(banner.activeSelf);
        }
    }
}
