using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class EncounterGateTests
    {
        private const string ZombiePrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Zombie.prefab";
        private const string RatPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Rat.prefab";
        private const string ShieldPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Shield.prefab";
        private const string ArcherPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Archer.prefab";

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

            LootPickup.ClearPoolForTests();
            DamagePopup.ClearPoolForTests();
            foreach (var loot in Object.FindObjectsByType<LootPickup>(FindObjectsInactive.Include))
            {
                if (loot != null)
                    Object.DestroyImmediate(loot.gameObject);
            }
            foreach (var popup in Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include))
            {
                if (popup != null)
                    Object.DestroyImmediate(popup.gameObject);
            }

            Time.timeScale = 1f;
        }

        [Test]
        public void EditModeEnemyDeath_DoesNotLeakRootLootOrPopupsAfterTearDown()
        {
            var zombie = SpawnEnemy(ZombiePrefabPath, new Vector2(-9f, 24f));
            KillEnemy(zombie);

            Assert.Greater(Object.FindObjectsByType<LootPickup>(FindObjectsInactive.Include).Length, 0,
                "Killing an enemy should spawn at least one LootPickup before teardown.");
            Assert.Greater(Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include).Length, 0,
                "Killing an enemy should prewarm/spawn DamagePopup instances before teardown.");

            TearDown();

            Assert.AreEqual(0, Object.FindObjectsByType<LootPickup>(FindObjectsInactive.Include).Length,
                "TearDown must purge all active and pooled LootPickup instances.");
            Assert.AreEqual(0, Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include).Length,
                "TearDown must purge all active and pooled DamagePopup instances.");
        }

        [Test]
        public void U08_EncounterGate_OpensOnlyOnSecondRoomEEnemyDeath_InEitherOrder_AndHidesBarrierAtomically()
        {
            // Order 1: Zombie E -> Archer E
            VerifyOrder("ZombieThenArcher", killFirstThenSecond: true);

            // Order 2: Archer E -> Zombie E
            VerifyOrder("ArcherThenZombie", killFirstThenSecond: false);
        }

        [Test]
        public void U09_EncounterGate_IgnoresNonRoomEEnemies_AndDuplicateDeathCallbacks()
        {
            var run = CreatePlayingRun();
            var zombieB = SpawnEnemy(ZombiePrefabPath, new Vector2(29f, 0f));
            var ratG = SpawnEnemy(RatPrefabPath, new Vector2(62f, 0f));
            var shieldJ = SpawnEnemy(ShieldPrefabPath, new Vector2(-4f, 16f));
            var zombieE = SpawnEnemy(ZombiePrefabPath, new Vector2(-9f, 24f));
            var archerE = SpawnEnemy(ArcherPrefabPath, new Vector2(10f, 24f));

            var (gate, barrier) = CreateGate(run, zombieE, archerE);
            int openCount = 0;
            gate.Opened += () => openCount++;

            // Kill enemies outside Room E -> count remains 0, gate stays closed.
            KillEnemy(zombieB);
            KillEnemy(ratG);
            KillEnemy(shieldJ);
            Assert.AreEqual(0, gate.DefeatedCount, "[U09/NonRoomE] count expected=0");
            Assert.IsFalse(gate.IsOpen);
            Assert.IsTrue(barrier.activeSelf);

            // Zombie E dies once + duplicate callback -> count is 1, gate stays closed.
            KillEnemy(zombieE);
            gate.NotifyEnemyDied(zombieE);
            Assert.AreEqual(1, gate.DefeatedCount, "[U09/ZombieEDup] count expected=1");
            Assert.IsFalse(gate.IsOpen);
            Assert.IsTrue(barrier.activeSelf);

            // Repeat non-E notifications -> count still 1.
            gate.NotifyEnemyDied(zombieB);
            gate.NotifyEnemyDied(ratG);
            gate.NotifyEnemyDied(shieldJ);
            Assert.AreEqual(1, gate.DefeatedCount);
            Assert.IsFalse(gate.IsOpen);

            // Archer E dies + duplicate callback -> gate opens exactly once.
            KillEnemy(archerE);
            gate.NotifyEnemyDied(archerE);
            Assert.AreEqual(2, gate.DefeatedCount, "[U09/ArcherE] count expected=2");
            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(barrier.activeSelf);
            Assert.AreEqual(1, openCount, "[U09/OpenCount] expected=1");
        }

        [Test]
        public void U10_EncounterGate_PersistsProgressAcrossRetreat_AndFreezesStateAfterRunEnded()
        {
            // Run 1: Retreat after 1/2, return and kill 2/2 -> opens and stays open across floor revisit.
            {
                var playerGo = Track(new GameObject("Player_U10"));
                playerGo.transform.position = new Vector3(-13.5f, 24f, 0f);
                var playerCol = playerGo.AddComponent<BoxCollider2D>();
                playerCol.size = new Vector2(0.8f, 1.6f);
                playerCol.offset = new Vector2(0f, 0.8f);
                var playerHealth = playerGo.AddComponent<Health>();
                playerHealth.Initialize(100, 0f);

                var run = Track(new GameObject("Run_U10")).AddComponent<LevelRunController>();
                run.Configure(null, playerHealth);
                run.TickIntro(new DummyPlayState(), 0f);

                var zombieE = SpawnEnemy(ZombiePrefabPath, new Vector2(-9f, 24f));
                var archerE = SpawnEnemy(ArcherPrefabPath, new Vector2(10f, 24f));
                var zoneGo = Track(new GameObject("Zone_E1"));
                var zone = zoneGo.AddComponent<EncounterZone>();
                zone.Configure("E1", new Vector2(-15f, 24f), new Vector2(-12f, 31f), run, null, playerCol,
                    new[] { zombieE, archerE }, playerHealth);
                zone.PutEnemiesToSleep();

                var (gate, barrier) = CreateGate(run, zombieE, archerE);
                int openCount = 0;
                gate.Opened += () => openCount++;

                Assert.IsTrue(zone.Evaluate(), "[U10/EnterE1] expected E1 zone to wake enemies.");
                KillEnemy(zombieE);
                Assert.AreEqual(1, gate.DefeatedCount, "[U10/Retreat] progress=1/2");
                Assert.IsFalse(gate.IsOpen);
                Assert.IsTrue(barrier.activeSelf);

                // Player retreats to K and re-enters E1: gate re-subscribes and retains 1/2.
                playerGo.transform.position = new Vector3(-20f, 24f, 0f);
                Assert.IsFalse(zone.Evaluate(), "[U10/RetreatOutside] zone must not re-activate.");
                gate.enabled = false;
                gate.enabled = true;

                playerGo.transform.position = new Vector3(-13.5f, 24f, 0f);
                Assert.IsFalse(zone.Evaluate(), "[U10/ReEnterE1] zone remains activated.");
                Assert.AreEqual(1, gate.DefeatedCount);
                Assert.IsFalse(gate.IsOpen);
                Assert.IsTrue(barrier.activeSelf);
                Assert.AreEqual(0, openCount);

                // Kill second enemy after re-enable -> gate opens once and stays open across floor revisit.
                KillEnemy(archerE);
                Assert.IsTrue(gate.IsOpen);
                Assert.IsFalse(barrier.activeSelf);
                Assert.AreEqual(2, gate.DefeatedCount);
                Assert.AreEqual(1, openCount);

                gate.enabled = false;
                gate.enabled = true;
                Assert.IsTrue(gate.IsOpen, "[U10/Revisit] opened gate remains open after re-enable.");
                Assert.IsFalse(barrier.activeSelf);
                Assert.AreEqual(2, gate.DefeatedCount);
                Assert.AreEqual(1, openCount);
            }

            // Run 2: Lock while closed (1/2) -> further deaths after Ended do NOT open the gate.
            {
                var run = CreatePlayingRun();
                var zombieE = SpawnEnemy(ZombiePrefabPath, new Vector2(-9f, 24f));
                var archerE = SpawnEnemy(ArcherPrefabPath, new Vector2(10f, 24f));
                var (gate, barrier) = CreateGate(run, zombieE, archerE);
                int openCount = 0;
                gate.Opened += () => openCount++;

                KillEnemy(zombieE);
                Assert.AreEqual(1, gate.DefeatedCount);

                run.RequestLoss(20);
                run.ResolvePendingOutcome(21);
                Assert.AreEqual(LevelPhase.Ended, run.Phase);

                gate.NotifyEnemyDied(archerE);
                Assert.IsFalse(gate.IsOpen, "[U10/LockedClosed] gate must remain closed after run ends.");
                Assert.IsTrue(barrier.activeSelf, "[U10/LockedClosed] barrier must remain active.");
                Assert.AreEqual(0, openCount);
            }

            // Run 3: Lock while already open (2/2) -> remains open, openCount stays 1.
            {
                var run = CreatePlayingRun();
                var zombieE = SpawnEnemy(ZombiePrefabPath, new Vector2(-9f, 24f));
                var archerE = SpawnEnemy(ArcherPrefabPath, new Vector2(10f, 24f));
                var (gate, barrier) = CreateGate(run, zombieE, archerE);
                int openCount = 0;
                gate.Opened += () => openCount++;

                KillEnemy(zombieE);
                KillEnemy(archerE);
                Assert.IsTrue(gate.IsOpen);
                Assert.IsFalse(barrier.activeSelf);
                Assert.AreEqual(1, openCount);

                run.RequestWin(30);
                run.ResolvePendingOutcome(31);
                gate.NotifyEnemyDied(zombieE);
                gate.NotifyEnemyDied(archerE);

                Assert.IsTrue(gate.IsOpen, "[U10/LockedOpen] gate stays open.");
                Assert.IsFalse(barrier.activeSelf, "[U10/LockedOpen] barrier stays disabled.");
                Assert.AreEqual(1, openCount);
            }
        }

        private void VerifyOrder(string label, bool killFirstThenSecond)
        {
            var run = CreatePlayingRun();
            var zombieE = SpawnEnemy(ZombiePrefabPath, new Vector2(-9f, 24f));
            var archerE = SpawnEnemy(ArcherPrefabPath, new Vector2(10f, 24f));
            var (gate, barrier) = CreateGate(run, zombieE, archerE);
            var blocker = barrier.GetComponent<BoxCollider2D>();
            var sprite = barrier.GetComponent<SpriteRenderer>();

            int openCount = 0;
            bool blockerOffAtOpen = false;
            bool visualOffAtOpen = false;
            gate.Opened += () =>
            {
                openCount++;
                blockerOffAtOpen = !blocker.gameObject.activeInHierarchy;
                visualOffAtOpen = !sprite.gameObject.activeInHierarchy;
            };

            // 0/2
            Assert.IsFalse(gate.IsOpen, $"[U08/{label}/0of2]");
            Assert.IsTrue(barrier.activeSelf);

            // 1/2
            KillEnemy(killFirstThenSecond ? zombieE : archerE);
            Assert.AreEqual(1, gate.DefeatedCount, $"[U08/{label}/1of2]");
            Assert.IsFalse(gate.IsOpen);
            Assert.IsTrue(barrier.activeSelf);
            Assert.AreEqual(0, openCount);

            // 2/2
            KillEnemy(killFirstThenSecond ? archerE : zombieE);
            Assert.AreEqual(2, gate.DefeatedCount, $"[U08/{label}/2of2]");
            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(barrier.activeSelf);
            Assert.AreEqual(1, openCount);
            Assert.IsTrue(blockerOffAtOpen && visualOffAtOpen, $"[U08/{label}/AtomicHide] blocker and visual must vanish together.");
        }

        private LevelRunController CreatePlayingRun()
        {
            var health = Track(new GameObject("PlayerHealth")).AddComponent<Health>();
            health.Initialize(100, 0f);
            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);
            return run;
        }

        private (EncounterGate gate, GameObject barrier) CreateGate(LevelRunController run, EnemyController first, EnemyController second)
        {
            var gateGo = Track(new GameObject("I4_EncounterGate"));
            var barrier = Track(new GameObject("GateBarrier"));
            barrier.transform.SetParent(gateGo.transform, false);
            barrier.AddComponent<SpriteRenderer>();
            barrier.AddComponent<BoxCollider2D>();

            var gate = gateGo.AddComponent<EncounterGate>();
            gate.Configure(run, first, second, barrier);
            return (gate, barrier);
        }

        private EnemyController SpawnEnemy(string prefabPath, Vector2 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, "Missing prefab: " + prefabPath);
            var inst = Track(Object.Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity));
            InitializeEnemyInEditMode(inst);
            return inst.GetComponent<EnemyController>();
        }

        private static void InitializeEnemyInEditMode(GameObject inst)
        {
            foreach (var mb in inst.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                var awake = mb.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                awake?.Invoke(mb, null);
            }
        }

        private static void KillEnemy(EnemyController enemy)
        {
            var h = enemy.GetComponent<Health>();
            Assert.IsTrue(h.TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, null)));
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
