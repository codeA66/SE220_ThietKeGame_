using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.HUD;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class SwitchDoorTests
    {
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
        }

        [Test]
        public void U17_SwitchDoor_RejectsOutsideDeadOrEnded_AndOpensBarrierAtomicallyOnceWhenAliveInsideTrigger()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(144f, 0f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);

            var doorGo = Track(new GameObject("SD_1_EastVault"));
            doorGo.transform.position = new Vector3(144f, 0f, 0f);
            var trigger = doorGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 0.75f);
            trigger.size = new Vector2(2.5f, 1.5f);

            var barrierGo = Track(new GameObject("DoorBarrier"));
            barrierGo.transform.SetParent(doorGo.transform, false);
            var barrierCol = barrierGo.AddComponent<BoxCollider2D>();
            var barrierSr = barrierGo.AddComponent<SpriteRenderer>();

            var switchVisualGo = Track(new GameObject("SwitchVisual"));
            switchVisualGo.transform.SetParent(doorGo.transform, false);
            var switchSr = switchVisualGo.AddComponent<SpriteRenderer>();
            var offSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);
            var onSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);

            var promptGo = Track(new GameObject("PromptIndicator"));
            promptGo.transform.SetParent(doorGo.transform, false);

            var sd = doorGo.AddComponent<SwitchDoor>();
            sd.Configure("SD_1", run, null, trigger, barrierGo, switchSr, offSprite, onSprite, null, promptGo, health, playerCol);

            int openEvents = 0;
            bool barrierOffAtEvent = false;
            sd.Opened += _ =>
            {
                openEvents++;
                barrierOffAtEvent = !barrierCol.gameObject.activeInHierarchy && !barrierSr.gameObject.activeInHierarchy;
            };

            // 1. Intro phase -> prompt hidden, interact rejected.
            sd.RefreshVisualState();
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(sd.TryInteract(), "[U17/Intro] Must reject before Playing.");
            Assert.IsFalse(sd.IsOpen);
            Assert.IsTrue(barrierGo.activeSelf);
            Assert.AreSame(offSprite, switchSr.sprite);

            // Enter Playing phase.
            run.TickIntro(new DummyPlayState(), 0f);
            sd.RefreshVisualState();
            Assert.IsTrue(sd.PromptVisible, "[U17/InTrigger] Prompt must show when alive and inside trigger.");

            // 2. Outside trigger -> rejected.
            playerGo.transform.position = new Vector3(130f, 0f, 0f);
            Physics2D.SyncTransforms();
            sd.RefreshVisualState();
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(sd.TryInteract(), "[U17/Outside] Must reject outside lever trigger.");
            Assert.IsFalse(sd.IsOpen);

            // 3. Return inside trigger and interact -> opens atomically once, swaps sprite, hides prompt.
            playerGo.transform.position = new Vector3(144f, 0f, 0f);
            Physics2D.SyncTransforms();
            Assert.IsTrue(sd.TryInteract(), "[U17/ValidOpen] Expected true.");
            Assert.IsTrue(sd.IsOpen);
            Assert.AreEqual(1, openEvents);
            Assert.IsTrue(barrierOffAtEvent, "[U17/Atomic] Barrier collider and renderer must disable atomically before event.");
            Assert.IsFalse(barrierGo.activeSelf);
            Assert.AreSame(onSprite, switchSr.sprite);
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(promptGo.activeSelf);

            // 4. Repeat interact -> rejected, remains open.
            Assert.IsFalse(sd.TryInteract(), "[U17/Repeat] Repeat press must return false.");
            Assert.AreEqual(1, openEvents);
            Assert.IsTrue(sd.IsOpen);

            // 5. Re-configuring restores barrier active state, re-enables barrier Collider2D, and resets sprite.
            sd.Configure("SD_1", run, null, trigger, barrierGo, switchSr, offSprite, onSprite, null, promptGo, health, playerCol);
            Assert.IsFalse(sd.IsOpen, "[U17/ReconfigureResetOpen]");
            Assert.IsTrue(barrierGo.activeSelf, "[U17/ReconfigureBarrierActive]");
            Assert.IsTrue(barrierCol.enabled, "[U17/ReconfigureColliderReenabled]");
            Assert.AreSame(offSprite, switchSr.sprite, "[U17/ReconfigureOffSprite]");
        }

        [Test]
        public void Minimap_EncounterGateAndSwitchDoorOpen_InvalidatesCachedSolidBarrierCells()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            Assert.GreaterOrEqual(groundLayer, 0, "Ground layer must exist in TagManager.");

            var (player, playerHealth, playerCol, run) = CreatePlayerAndRunFixture(Vector3.zero);

            var gateGo = Track(new GameObject("I4_EncounterGate"));
            var gateBarrier = Track(new GameObject("GateBarrier"));
            gateBarrier.layer = groundLayer;
            gateBarrier.transform.SetParent(gateGo.transform, false);
            gateBarrier.transform.position = new Vector3(2.25f, 0.75f, 0f);
            var gateCol = gateBarrier.AddComponent<BoxCollider2D>();
            gateCol.size = new Vector2(0.5f, 2f);

            var enemyA = Track(new GameObject("EnemyA")).AddComponent<EnemyController>();
            var enemyB = Track(new GameObject("EnemyB")).AddComponent<EnemyController>();
            var gate = gateGo.AddComponent<EncounterGate>();
            gate.Configure(run, enemyA, enemyB, gateBarrier);

            var doorGo = Track(new GameObject("SwitchDoor"));
            doorGo.transform.position = Vector3.zero;
            var doorTrigger = doorGo.AddComponent<BoxCollider2D>();
            doorTrigger.isTrigger = true;
            doorTrigger.size = new Vector2(2.5f, 2.5f);

            var doorBarrier = Track(new GameObject("DoorBarrier"));
            doorBarrier.layer = groundLayer;
            doorBarrier.transform.SetParent(doorGo.transform, false);
            doorBarrier.transform.position = new Vector3(-2.25f, 0.75f, 0f);
            var doorCol = doorBarrier.AddComponent<BoxCollider2D>();
            doorCol.size = new Vector2(0.5f, 2f);

            var switchDoor = doorGo.AddComponent<SwitchDoor>();
            switchDoor.Configure("SD_Minimap", run, player, doorTrigger, doorBarrier,
                playerHealth: playerHealth, playerCollider: playerCol);

            Physics2D.SyncTransforms();

            var minimapGo = Track(new GameObject("MinimapHud"));
            var minimap = minimapGo.AddComponent<MinimapHud>();
            InvokePrivateMethod(minimap, "OnEnable");
            InvokePrivateMethod(minimap, "Start");
            Assert.AreSame(minimap, MinimapHud.Active);

            Vector2Int gateCell = minimap.MapData.WorldToCell(new Vector2(2.25f, 0.75f));
            Vector2Int doorCell = minimap.MapData.WorldToCell(new Vector2(-2.25f, 0.75f));

            Assert.IsTrue(minimap.MapData.IsSampled(gateCell.x, gateCell.y));
            Assert.AreEqual(MinimapCellKind.Solid, minimap.MapData.GetCell(gateCell.x, gateCell.y),
                "Closed EncounterGate barrier must initially sample as Solid.");
            Assert.IsTrue(minimap.MapData.IsSampled(doorCell.x, doorCell.y));
            Assert.AreEqual(MinimapCellKind.Solid, minimap.MapData.GetCell(doorCell.x, doorCell.y),
                "Closed SwitchDoor barrier must initially sample as Solid.");

            // Open EncounterGate -> barrier cells immediately invalidated, then re-sampled as Empty on next refresh.
            gate.NotifyEnemyDied(enemyA);
            Assert.IsTrue(gate.NotifyEnemyDied(enemyB));
            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(minimap.MapData.IsSampled(gateCell.x, gateCell.y),
                "EncounterGate.OpenGate must invalidate cached minimap samples over the gate barrier.");

            InvokePrivateMethod(minimap, "Update");
            Assert.IsTrue(minimap.MapData.IsSampled(gateCell.x, gateCell.y));
            Assert.AreEqual(MinimapCellKind.Empty, minimap.MapData.GetCell(gateCell.x, gateCell.y),
                "Opened EncounterGate barrier cell must re-sample as Empty.");

            // Open SwitchDoor -> barrier cells immediately invalidated, then re-sampled as Empty on next refresh.
            Assert.IsTrue(switchDoor.TryInteract());
            Assert.IsTrue(switchDoor.IsOpen);
            Assert.IsFalse(minimap.MapData.IsSampled(doorCell.x, doorCell.y),
                "SwitchDoor.TryInteract must invalidate cached minimap samples over the door barrier.");

            InvokePrivateMethod(minimap, "Update");
            Assert.IsTrue(minimap.MapData.IsSampled(doorCell.x, doorCell.y));
            Assert.AreEqual(MinimapCellKind.Empty, minimap.MapData.GetCell(doorCell.x, doorCell.y),
                "Opened SwitchDoor barrier cell must re-sample as Empty.");
        }

        [Test]
        public void MinimapWorldScanner_ExcludesCrackedWallFromEnemies_IncludesSecondaryExits_AndHidesConsumedChest()
        {
            var (player, playerHealth, playerCol, run) = CreatePlayerAndRunFixture(new Vector3(3f, 0f, 0f));

            // 1. Breakable secret wall (Health without EnemyController) must never appear as an Enemy dot.
            var crackedWallGo = Track(new GameObject("I22_WallA_Masonry"));
            crackedWallGo.transform.position = new Vector3(1f, 0f, 0f);
            var wallHealth = crackedWallGo.AddComponent<Health>();
            wallHealth.Initialize(1, 0f);
            var wallTrigger = crackedWallGo.AddComponent<BoxCollider2D>();
            wallTrigger.isTrigger = true;
            var crackedWall = crackedWallGo.AddComponent<CrackedMasonry>();
            crackedWall.Configure(wallHealth, wallTrigger, Track(new GameObject("Intact")), Track(new GameObject("Fissure")), null, null);

            // 2. Real enemy (Health + EnemyController) must still appear as an Enemy dot.
            var realEnemyGo = Track(new GameObject("RealEnemy"));
            realEnemyGo.transform.position = new Vector3(6f, 0f, 0f);
            var enemyHealth = realEnemyGo.AddComponent<Health>();
            enemyHealth.Initialize(50, 0f);
            realEnemyGo.AddComponent<EnemyController>();

            // 3. Secondary biome exit must be classified as Exit.
            var secondaryExitGo = Track(new GameObject("SecondaryExit_ToxicSewers"));
            secondaryExitGo.transform.position = new Vector3(10f, 0f, 0f);
            secondaryExitGo.AddComponent<SecondaryBiomeExitTrigger>();

            // 4. HealingChest appears while unconsumed, then disappears once consumed.
            var chestGo = Track(new GameObject("HealingChest_I3"));
            chestGo.transform.position = new Vector3(3f, 0f, 0f);
            var chestTrigger = chestGo.AddComponent<BoxCollider2D>();
            chestTrigger.isTrigger = true;
            chestTrigger.size = new Vector2(2f, 2f);
            var chest = chestGo.AddComponent<HealingChest>();
            chest.Configure(run, player, chestTrigger, playerHealth: playerHealth, playerCollider: playerCol);

            Assert.IsTrue(playerHealth.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null)));
            Physics2D.SyncTransforms();

            var scanner = new MinimapWorldScanner(0.5f);
            scanner.ScanStaticLandmarks();
            var markers = new List<MinimapMarker>();
            scanner.CollectMarkers(player, markers, 0f);

            int enemyCount = 0;
            int exitCount = 0;
            int chestCount = 0;
            for (int i = 0; i < markers.Count; i++)
            {
                switch (markers[i].Kind)
                {
                    case MinimapMarkerKind.Enemy:
                        enemyCount++;
                        Assert.Greater(Vector2.Distance(markers[i].WorldPosition, new Vector2(1f, 0.6f)), 0.5f,
                            "CrackedMasonry secret wall must not emit an Enemy minimap marker.");
                        break;
                    case MinimapMarkerKind.Exit:
                        exitCount++;
                        Assert.Less(Vector2.Distance(markers[i].WorldPosition, new Vector2(10f, 0f)), 0.1f);
                        break;
                    case MinimapMarkerKind.Chest:
                        chestCount++;
                        break;
                }
            }

            Assert.AreEqual(1, enemyCount, "Only the real EnemyController entity should emit an Enemy marker.");
            Assert.AreEqual(1, exitCount, "SecondaryBiomeExitTrigger must emit an Exit marker.");
            Assert.AreEqual(1, chestCount, "Unconsumed HealingChest must emit a Chest marker.");

            // Consume the chest and verify it hides both immediately and after dynamic rescan + static rescan.
            Assert.IsTrue(chest.TryInteract());
            Assert.IsTrue(chest.IsConsumed);

            scanner.CollectMarkers(player, markers, 0.1f);
            Assert.AreEqual(0, CountMarkers(markers, MinimapMarkerKind.Chest),
                "Consumed HealingChest must hide immediately before the next scan interval.");

            scanner.ScanStaticLandmarks();
            scanner.CollectMarkers(player, markers, 1.0f);
            Assert.AreEqual(0, CountMarkers(markers, MinimapMarkerKind.Chest),
                "Consumed HealingChest must remain hidden across static and dynamic rescans.");
        }

        private (PlayerController player, Health health, BoxCollider2D col, LevelRunController run) CreatePlayerAndRunFixture(Vector3 playerPos)
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.SetActive(false);
            playerGo.transform.position = playerPos;
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = playerGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);
            var player = playerGo.AddComponent<PlayerController>();
            playerGo.SetActive(true);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(player, health);
            run.TickIntro(new DummyPlayState(), 0f);

            return (player, health, col, run);
        }

        private static int CountMarkers(List<MinimapMarker> markers, MinimapMarkerKind kind)
        {
            int count = 0;
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].Kind == kind)
                    count++;
            }
            return count;
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing method {target.GetType().Name}.{methodName}");
            method.Invoke(target, args);
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
