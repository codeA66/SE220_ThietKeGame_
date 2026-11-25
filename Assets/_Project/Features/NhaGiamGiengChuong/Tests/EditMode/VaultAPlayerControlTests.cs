using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class VaultAPlayerControlTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Features/Player/Prefabs/Player.prefab";
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
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
            Time.timeScale = 1f;
            Assets.GeometryAuditHelper.RestoreScene(_previousScene);
        }

        [Test]
        public void ScriptedSequence_BlocksPauseAndDamage_WaitsForGround_WalksAtMultiplier_AndRestoresIdle()
        {
            var floor = new GameObject("Floor_VaultA_Test");
            floor.layer = Assets.GeometryAuditHelper.LayerGround;
            var floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.offset = new Vector2(190f, 28.5f);
            floorCol.size = new Vector2(30f, 1f);
            _created.Add(floor);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.IsNotNull(prefab, "Missing Player.prefab");
            var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _created.Add(playerGo);
            playerGo.transform.position = new Vector3(183.5f, 29f, 0f);

            var player = playerGo.GetComponent<PlayerController>();
            var body = playerGo.GetComponent<Rigidbody2D>();
            var health = playerGo.GetComponent<Health>();
            var input = playerGo.GetComponent<PlayerInputReader>();
            var hitStop = new GameObject("HitStop").AddComponent<HitStop>();
            _created.Add(hitStop.gameObject);

            typeof(Health).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(health, null);
            typeof(PlayerInputReader).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(input, null);
            typeof(PlayerController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(player, null);
            typeof(PlayerController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(player, null);

            var run = new GameObject("Run").AddComponent<LevelRunController>();
            _created.Add(run.gameObject);
            run.Configure(player, health, input, null, hitStop);
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 0f));

            var panelRoot = new GameObject("PausePanel");
            _created.Add(panelRoot);
            var pauseMenu = new GameObject("PauseMenu").AddComponent<PauseMenuController>();
            _created.Add(pauseMenu.gameObject);
            pauseMenu.Configure(run, player, input, hitStop, panelRoot, null, null, null);

            var machine = (PlayerStateMachine)typeof(PlayerController)
                .GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(player);
            machine.ForceChangeState(machine.Idle);

            Assert.IsTrue(pauseMenu.CanPause, "Pause should be allowed before scripted sequence.");

            // 1. Transition lock blocks Pause and damage
            player.EnterTransitionLock();
            Assert.IsTrue(player.IsInScriptedSequence);
            Assert.IsFalse(pauseMenu.CanPause, "Pause must be blocked during transition lock.");
            Assert.IsFalse(health.TakeDamage(new DamageInfo(25, 1, 0f, 0f, false, null)),
                "Player must ignore damage during scripted sequence.");

            // 2. Airborne guard: Teleport above floor -> BeginScriptedWalk holds vx = 0 until grounded
            player.TeleportTo(new Vector2(183.5f, 32f), 1);
            Physics2D.SyncTransforms();
            player.BeginScriptedWalk(190.0f, 0.38f);
            player.Context.RefreshFrame();
            Assert.IsFalse(player.Context.IsGrounded);
            machine.FixedTick();
            Assert.AreEqual(0f, body.linearVelocity.x, 0.001f, "Must not walk horizontally while airborne.");

            // 3. Grounded slow walk to x = 190.0f
            player.TeleportTo(new Vector2(183.5f, 29f), 1);
            Physics2D.SyncTransforms();
            player.BeginScriptedWalk(190.0f, 0.38f);

            float expectedSpeed = player.Context.Config.RunSpeed * 0.38f;
            bool observedWalkSpeed = false;
            const float dt = 1f / 60f;
            for (int step = 0; step < 360 && !player.HasArrivedAtScriptedTarget; step++)
            {
                player.Context.RefreshFrame();
                machine.FixedTick();
                machine.Tick();
                if (!player.HasArrivedAtScriptedTarget && body.linearVelocity.x > 0.1f)
                {
                    Assert.AreEqual(expectedSpeed, body.linearVelocity.x, 0.15f);
                    observedWalkSpeed = true;
                }
                body.position += new Vector2(body.linearVelocity.x * dt, 0f);
                Physics2D.SyncTransforms();
            }

            Assert.IsTrue(observedWalkSpeed, "Player should walk horizontally at 0.38x RunSpeed.");
            Assert.IsTrue(player.HasArrivedAtScriptedTarget, "Player should arrive at targetX = 190.0.");
            Assert.AreEqual(190.0f, body.position.x, 0.10f);
            Assert.AreEqual(0f, body.linearVelocity.x, 0.001f);
            Assert.AreEqual(1, player.Context.FacingDirection);

            // 4. EndScriptedSequence restores Idle & Pause
            player.EndScriptedSequence();
            Assert.IsFalse(player.IsInScriptedSequence);
            Assert.AreEqual(machine.Idle, machine.Current);
            Assert.IsTrue(pauseMenu.CanPause);
        }
    }
}
