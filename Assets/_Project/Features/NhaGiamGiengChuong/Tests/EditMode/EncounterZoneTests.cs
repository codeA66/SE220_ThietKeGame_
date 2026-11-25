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
    public sealed class EncounterZoneTests
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
        public void U01_InclusiveZoneBounds_WakeExactAssignedEnemies_AndRejectOutsideByOneCentimeter()
        {
            var specs = new[]
            {
                (id: "B1", min: new Vector2(20f, 0f), max: new Vector2(42f, 6f), prefabs: new[] { ZombiePrefabPath }),
                (id: "G1", min: new Vector2(58f, 0f), max: new Vector2(76f, 7.75f), prefabs: new[] { RatPrefabPath }),
                (id: "J1", min: new Vector2(-16f, 16f), max: new Vector2(6f, 22.5f), prefabs: new[] { ShieldPrefabPath }),
                (id: "E1", min: new Vector2(-16f, 24f), max: new Vector2(22f, 31f), prefabs: new[] { ZombiePrefabPath, ArcherPrefabPath })
            };

            foreach (var spec in specs)
            {
                Vector2 center = (spec.min + spec.max) * 0.5f;
                var insidePoints = new[]
                {
                    (label: "center", pt: center),
                    (label: "minX,minY", pt: new Vector2(spec.min.x, spec.min.y)),
                    (label: "maxX,minY", pt: new Vector2(spec.max.x, spec.min.y)),
                    (label: "minX,maxY", pt: new Vector2(spec.min.x, spec.max.y)),
                    (label: "maxX,maxY", pt: new Vector2(spec.max.x, spec.max.y))
                };

                foreach (var p in insidePoints)
                {
                    int wakes = ProbeZonePoint(spec.id, spec.min, spec.max, spec.prefabs, p.pt);
                    Assert.AreEqual(spec.prefabs.Length, wakes,
                        $"[U01/{spec.id}/{p.label}] feet={p.pt} expected={spec.prefabs.Length} measured={wakes}");
                }

                var outsidePoints = new[]
                {
                    (label: "west-0.01", pt: new Vector2(spec.min.x - 0.01f, center.y)),
                    (label: "east+0.01", pt: new Vector2(spec.max.x + 0.01f, center.y)),
                    (label: "south-0.01", pt: new Vector2(center.x, spec.min.y - 0.01f)),
                    (label: "north+0.01", pt: new Vector2(center.x, spec.max.y + 0.01f))
                };

                foreach (var p in outsidePoints)
                {
                    int wakes = ProbeZonePoint(spec.id, spec.min, spec.max, spec.prefabs, p.pt);
                    Assert.AreEqual(0, wakes,
                        $"[U01/{spec.id}/{p.label}] feet={p.pt} expected=0 measured={wakes}");
                }
            }

            // Explicit G1 upper-floor separation test: (68, 7.75) wakes, (68, 8.0) does not wake.
            int g1Inside = ProbeZonePoint("G1", new Vector2(58f, 0f), new Vector2(76f, 7.75f), new[] { RatPrefabPath }, new Vector2(68f, 7.75f));
            int g1FloorAbove = ProbeZonePoint("G1", new Vector2(58f, 0f), new Vector2(76f, 7.75f), new[] { RatPrefabPath }, new Vector2(68f, 8f));
            Assert.AreEqual(1, g1Inside, "[U01/G1/68,7.75] expected=1 measured=" + g1Inside);
            Assert.AreEqual(0, g1FloorAbove, "[U01/G1/68,8.0] expected=0 measured=" + g1FloorAbove);
        }

        [Test]
        public void U02_ZoneRejectsInvalidActorsSecondaryCollidersDeadPlayerAndNonPlayingPhases()
        {
            var min = new Vector2(20f, 0f);
            var max = new Vector2(23f, 6f);

            // Create Player mock with non-zero collider offset (0, 0.8), size (0.8, 1.6) => feet at transform.position.
            var playerGo = Track(new GameObject("MockPlayer"));
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var playerHealth = playerGo.AddComponent<Health>();
            playerHealth.Initialize(100, 0f);

            // Secondary child collider (head/sword/ghost) inside zone while main feet are outside.
            var swordGo = Track(new GameObject("SwordCollider"));
            swordGo.transform.SetParent(playerGo.transform, false);
            swordGo.transform.localPosition = new Vector3(2.5f, 0.8f, 0f);
            var swordCol = swordGo.AddComponent<BoxCollider2D>();

            var otherActorGo = Track(new GameObject("OtherActor"));
            otherActorGo.transform.position = new Vector3(21.5f, 0f, 0f);
            var otherCol = otherActorGo.AddComponent<BoxCollider2D>();
            var otherHealth = otherActorGo.AddComponent<Health>();
            otherHealth.Initialize(100, 0f);

            var runGo = Track(new GameObject("Run"));
            var run = runGo.AddComponent<LevelRunController>();
            run.Configure(null, playerHealth);

            var zombie = SpawnEnemy(ZombiePrefabPath, new Vector2(29f, 0f));
            var zoneGo = Track(new GameObject("Zone_B1"));
            var zone = zoneGo.AddComponent<EncounterZone>();
            zone.Configure("B1", min, max, run, null, playerCol, new[] { zombie }, playerHealth);
            zone.PutEnemiesToSleep();

            int wakeCount = 0;
            zone.EnemyWoken += _ => wakeCount++;

            // 1. Intro phase: living Player feet inside B1 -> 0 wakes.
            playerGo.transform.position = new Vector3(21.5f, 0f, 0f);
            Assert.IsFalse(zone.Evaluate(), "[U02/Intro] expected=false");
            Assert.AreEqual(0, wakeCount, "[U02/Intro] wakes expected=0");

            // Transition to Playing.
            Assert.IsTrue(run.TickIntro(new DummyPlayState(), 1f));

            // 2. Other actor inside B1 -> 0 wakes.
            Assert.IsFalse(zone.TryActivateFor(otherCol, otherHealth), "[U02/OtherActor] expected=false");
            Assert.AreEqual(0, wakeCount);

            // 3. Secondary collider inside B1 while feet outside -> 0 wakes.
            playerGo.transform.position = new Vector3(19.5f, 0f, 0f);
            Assert.IsFalse(zone.TryActivateFor(swordCol, playerHealth), "[U02/SecondaryCollider] expected=false");
            Assert.IsFalse(zone.Evaluate(), "[U02/FeetOutsideHeadInside] expected=false");
            Assert.AreEqual(0, wakeCount);

            // 4. Head/upper body overlapping vertical top (feet at y=6.2, outside maxY=6.0) -> 0 wakes.
            playerGo.transform.position = new Vector3(21.5f, 6.2f, 0f);
            Assert.IsFalse(zone.Evaluate(), "[U02/FeetAboveMaxY] expected=false");
            Assert.AreEqual(0, wakeCount);

            // 5. Dead Player inside B1 -> 0 wakes.
            playerGo.transform.position = new Vector3(21.5f, 0f, 0f);
            playerHealth.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, null));
            Assert.IsFalse(playerHealth.IsAlive);
            Assert.IsFalse(zone.Evaluate(), "[U02/DeadPlayer] expected=false");
            Assert.AreEqual(0, wakeCount);

            // 6. Ended phase with living Player inside B1 -> 0 wakes.
            var endedRunGo = Track(new GameObject("EndedRun"));
            var endedRun = endedRunGo.AddComponent<LevelRunController>();
            var aliveHealth = Track(new GameObject("AliveHealth")).AddComponent<Health>();
            aliveHealth.Initialize(100, 0f);
            endedRun.Configure(null, aliveHealth);
            endedRun.TickIntro(new DummyPlayState(), 1f);
            endedRun.RequestWin(10);
            endedRun.ResolvePendingOutcome(11);
            Assert.AreEqual(LevelPhase.Ended, endedRun.Phase);

            var endedZoneGo = Track(new GameObject("Zone_B1_Ended"));
            var endedZone = endedZoneGo.AddComponent<EncounterZone>();
            var endedZombie = SpawnEnemy(ZombiePrefabPath, new Vector2(29f, 0f));
            endedZone.Configure("B1", min, max, endedRun, null, playerCol, new[] { endedZombie }, aliveHealth);
            endedZone.PutEnemiesToSleep();
            int endedWakes = 0;
            endedZone.EnemyWoken += _ => endedWakes++;
            Assert.IsFalse(endedZone.Evaluate(), "[U02/EndedPhase] expected=false");
            Assert.AreEqual(0, endedWakes);

            // 7. Positive control: living assigned Player, feet on floor in B1, Playing phase -> wakes Zombie B once.
            playerHealth.RestoreFull();
            playerGo.transform.position = new Vector3(21.5f, 0f, 0f);
            Assert.IsTrue(zone.Evaluate(), "[U02/Control] expected=true");
            Assert.AreEqual(1, wakeCount, "[U02/Control] wakes expected=1");
            Assert.IsTrue(zombie.enabled, "[U02/Control] Zombie B must be enabled.");
        }

        [Test]
        public void U03_ZoneWakesEachEnemyOncePerRun_RetainsProgressOnRetreat_AndResetsOnNewRun()
        {
            var min = new Vector2(-15f, 24f);
            var max = new Vector2(-12f, 31f);

            // Run 1
            var (run1, player1, col1, health1, zone1, enemies1, counts1) = CreateRoomERun(min, max);
            Assert.IsFalse(enemies1[0].enabled);
            Assert.IsFalse(enemies1[1].enabled);

            // Enter E1 and stay multiple ticks.
            player1.transform.position = new Vector3(-13.5f, 24f, 0f);
            Assert.IsTrue(zone1.Evaluate(), "[U03/Run1/Enter] first enter must wake.");
            Assert.IsFalse(zone1.Evaluate(), "[U03/Run1/Stay1] stay must not re-wake.");
            Assert.IsFalse(zone1.Evaluate(), "[U03/Run1/Stay2] stay must not re-wake.");

            // Retreat to K.
            player1.transform.position = new Vector3(-20f, 24f, 0f);
            Assert.IsFalse(zone1.Evaluate());

            // Kill first enemy (Zombie E).
            var zombieHealth = enemies1[0].GetComponent<Health>();
            Assert.IsTrue(zombieHealth.TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, null)),
                "[U03/Run1/KillFirst] woken enemy must take lethal damage.");
            Assert.IsFalse(zombieHealth.IsAlive);
            Assert.IsTrue(enemies1[1].GetComponent<Health>().IsAlive);

            // Re-enter E1 -> no additional wakes, dead enemy stays dead, living enemy stays awake.
            player1.transform.position = new Vector3(-13.5f, 24f, 0f);
            Assert.IsFalse(zone1.Evaluate(), "[U03/Run1/ReEnter] must not re-wake.");
            Assert.AreEqual(1, counts1[enemies1[0]], "[U03/Run1/ZombieWakes] expected=1");
            Assert.AreEqual(1, counts1[enemies1[1]], "[U03/Run1/ArcherWakes] expected=1");
            Assert.IsFalse(zombieHealth.IsAlive);
            Assert.IsTrue(enemies1[1].enabled);

            // Run 2 (fresh run initialization) -> both enemies start sleeping, then wake once each on entry.
            var (run2, player2, col2, health2, zone2, enemies2, counts2) = CreateRoomERun(min, max);
            Assert.IsFalse(zone2.IsActivated, "[U03/Run2/Initial] zone starts dormant.");
            Assert.IsFalse(enemies2[0].enabled, "[U03/Run2/Initial] Zombie E starts asleep.");
            Assert.IsFalse(enemies2[1].enabled, "[U03/Run2/Initial] Archer E starts asleep.");
            Assert.IsTrue(enemies2[0].GetComponent<Health>().IsAlive);
            Assert.IsTrue(enemies2[1].GetComponent<Health>().IsAlive);

            player2.transform.position = new Vector3(-13.5f, 24f, 0f);
            Assert.IsTrue(zone2.Evaluate(), "[U03/Run2/Enter] fresh run must wake both.");
            Assert.AreEqual(1, counts2[enemies2[0]], "[U03/Run2/ZombieWakes] expected=1");
            Assert.AreEqual(1, counts2[enemies2[1]], "[U03/Run2/ArcherWakes] expected=1");
        }

        private int ProbeZonePoint(string id, Vector2 min, Vector2 max, string[] enemyPrefabs, Vector2 feetPos)
        {
            var playerGo = Track(new GameObject($"Player_{id}"));
            var col = playerGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);
            playerGo.transform.position = new Vector3(feetPos.x, feetPos.y, 0f);

            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var runGo = Track(new GameObject($"Run_{id}"));
            var run = runGo.AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var enemies = new EnemyController[enemyPrefabs.Length];
            for (int i = 0; i < enemyPrefabs.Length; i++)
                enemies[i] = SpawnEnemy(enemyPrefabs[i], new Vector2(100f + i, 0f));

            var zoneGo = Track(new GameObject($"Zone_{id}"));
            var zone = zoneGo.AddComponent<EncounterZone>();
            zone.Configure(id, min, max, run, null, col, enemies, health);
            zone.PutEnemiesToSleep();

            int wakes = 0;
            zone.EnemyWoken += _ => wakes++;

            zone.Evaluate();
            return wakes;
        }

        [Test]
        public void PlayerPrefab_HasRoundedBottomEdgeRadius_PreservingOuterBoundsAndExactFeet()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Player/Prefabs/Player.prefab");
            Assert.IsNotNull(prefab, "Missing Player.prefab.");
            var col = prefab.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(col, "Player.prefab missing BoxCollider2D.");
            Assert.Greater(col.edgeRadius, 0f,
                "Player BoxCollider2D must use a small edgeRadius to prevent Box2D ghost-vertex snagging on floor seams.");
            Assert.AreEqual(0.8f, col.size.x + col.edgeRadius * 2f, 0.001f, "Total outer width must stay 0.8u.");
            Assert.AreEqual(1.6f, col.size.y + col.edgeRadius * 2f, 0.001f, "Total outer height must stay 1.6u.");

            var inst = Track(Object.Instantiate(prefab, Vector3.zero, Quaternion.identity));
            Vector2 feet = EncounterZone.GetColliderFeet(inst.GetComponent<BoxCollider2D>());
            Assert.AreEqual(0f, feet.x, 0.001f);
            Assert.AreEqual(0f, feet.y, 0.001f, "GetColliderFeet must stay flush at y=0 with edgeRadius.");
        }

        private (LevelRunController run, GameObject player, BoxCollider2D col, Health health, EncounterZone zone, EnemyController[] enemies, Dictionary<EnemyController, int> wakeCounts)
            CreateRoomERun(Vector2 min, Vector2 max)
        {
            var playerGo = Track(new GameObject("Player_RoomE"));
            var col = playerGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);
            playerGo.transform.position = new Vector3(-20f, 24f, 0f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var runGo = Track(new GameObject("Run_RoomE"));
            var run = runGo.AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var enemies = new[]
            {
                SpawnEnemy(ZombiePrefabPath, new Vector2(-9f, 24f)),
                SpawnEnemy(ArcherPrefabPath, new Vector2(10f, 24f))
            };

            var counts = new Dictionary<EnemyController, int>
            {
                [enemies[0]] = 0,
                [enemies[1]] = 0
            };

            var zoneGo = Track(new GameObject("Zone_E1"));
            var zone = zoneGo.AddComponent<EncounterZone>();
            zone.Configure("E1", min, max, run, null, col, enemies, health);
            zone.PutEnemiesToSleep();
            zone.EnemyWoken += e => counts[e]++;

            return (run, playerGo, col, health, zone, enemies, counts);
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

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
