using System;
using System.Collections.Generic;
using System.Linq;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
using DeadCells.Enemies;
using DeadCells.HUD;
using DeadCells.Player;
using DeadCells.VFX;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class MapWiringTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A07_AssembledScene_ZeroMissingScriptsAndValidatesAllSerializedWiringAndBuildSettings()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. Zero missing scripts anywhere in scene
            var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGameObjects)
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                Assert.AreEqual(0, missing, $"[A07/MissingScript] GameObject '{go.name}' has {missing} missing scripts.");
            }

            // 2. Single Player at (3, 0) facing right + overrides wired
            var players = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
            Assert.AreEqual(1, players.Length, "[A07/PlayerCount]");
            var player = players[0];
            Assert.AreEqual(3f, player.transform.position.x, GeometryAuditHelper.Eps, "[A07/PlayerSpawnX]");
            Assert.AreEqual(0f, player.transform.position.y, GeometryAuditHelper.Eps, "[A07/PlayerSpawnY]");
            Assert.Greater(player.transform.localScale.x, 0f, "[A07/PlayerFacingRight]");

            var vfx = Object.FindAnyObjectByType<VfxSpawner>();
            var hitStop = Object.FindAnyObjectByType<HitStop>();
            var camFollow = Object.FindAnyObjectByType<CameraFollow2D>();
            var projSpawner = Object.FindAnyObjectByType<ProjectileSpawner>();
            var run = Object.FindAnyObjectByType<LevelRunController>();

            Assert.IsNotNull(vfx, "[A07/VfxSpawner]");
            Assert.IsNotNull(hitStop, "[A07/HitStop]");
            Assert.IsNotNull(camFollow, "[A07/CameraFollow2D]");
            Assert.IsNotNull(projSpawner, "[A07/ProjectileSpawner]");
            Assert.IsNotNull(run, "[A07/LevelRunController]");

            // Player overrides
            var pVfx = player.GetComponentInChildren<PlayerVfx>(true);
            Assert.AreEqual(vfx, new SerializedObject(pVfx).FindProperty("_spawner").objectReferenceValue, "[A07/PlayerVfx]");
            var pFb = player.GetComponentInChildren<PlayerFeedback>(true);
            Assert.AreEqual(camFollow, new SerializedObject(pFb).FindProperty("_camera").objectReferenceValue, "[A07/PlayerFeedback.Camera]");
            Assert.AreEqual(hitStop, new SerializedObject(pFb).FindProperty("_hitStop").objectReferenceValue, "[A07/PlayerFeedback.HitStop]");
            var pHead = player.GetComponentInChildren<PlayerSpawnHead>(true);
            Assert.AreEqual(vfx, new SerializedObject(pHead).FindProperty("_vfx").objectReferenceValue, "[A07/SpawnHead.Vfx]");
            Assert.AreEqual(camFollow, new SerializedObject(pHead).FindProperty("_camera").objectReferenceValue, "[A07/SpawnHead.Camera]");

            // 3. Camera size 7, target Player, bounds Rect(-104, -20, 340, 86)
            var cam = camFollow.GetComponent<Camera>();
            Assert.IsNotNull(cam);
            Assert.AreEqual(7f, cam.orthographicSize, GeometryAuditHelper.Eps, "[A07/CamSize]");
            var soCam = new SerializedObject(camFollow);
            Assert.AreEqual(player.transform, soCam.FindProperty("_target").objectReferenceValue, "[A07/CamTarget]");
            Assert.IsTrue(soCam.FindProperty("_useBounds").boolValue, "[A07/CamUseBounds]");
            var boundsRect = soCam.FindProperty("_bounds").rectValue;
            Assert.AreEqual(new Rect(-104f, -20f, 340f, 86f), boundsRect, "[A07/CamBounds]");

            // 4. ProjectileSpawner wiring (VFX, Projectile_Arrow + Projectile_Grenade, targets=128, ground=576)
            var soProj = new SerializedObject(projSpawner);
            Assert.AreEqual(vfx, soProj.FindProperty("_vfx").objectReferenceValue, "[A07/Proj.Vfx]");
            Assert.AreEqual(128, soProj.FindProperty("_targets").intValue, "[A07/Proj.Targets]");
            Assert.AreEqual(576, soProj.FindProperty("_ground").intValue, "[A07/Proj.Ground]");
            var prewarmProp = soProj.FindProperty("_prewarm");
            bool hasArrow = false;
            bool hasGrenade = false;
            for (int i = 0; i < prewarmProp.arraySize; i++)
            {
                var obj = prewarmProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (obj != null && obj.name == "Projectile_Arrow")
                    hasArrow = true;
                if (obj != null && obj.name == "Projectile_Grenade")
                    hasGrenade = true;
            }
            Assert.IsTrue(hasArrow, "[A07/Proj.ArrowPrefab]");
            Assert.IsTrue(hasGrenade, "[A07/Proj.GrenadePrefab]");

            // 5. Thirty-four enemies wired + exact spawn positions + dormant before tick 0
            var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include);
            Assert.AreEqual(34, enemies.Length, "[A07/EnemyCount] Expected 34 enemies.");

            var expectedSpawns = new[]
            {
                // Core Spine (9)
                ("Enemy_Zombie_B", new Vector2(29f, 0f)),
                ("Enemy_Rat_B2", new Vector2(35f, 0f)),
                ("Enemy_Rat_G", new Vector2(62f, 0f)),
                ("Enemy_Shield_J", new Vector2(-4f, 16f)),
                ("Enemy_Rat_J2", new Vector2(-10f, 16f)),
                ("Enemy_Zombie_E", new Vector2(-9f, 24f)),
                ("Enemy_Archer_E", new Vector2(10f, 24f)),
                ("Enemy_Rat_E2", new Vector2(0f, 24f)),
                ("Enemy_Zombie_D1", new Vector2(31f, 8f)),
                // Room M (4)
                ("Enemy_Zombie_M1", new Vector2(86f, 0f)),
                ("Enemy_Grenadier_M1", new Vector2(92f, 3.5f)),
                ("Enemy_Shield_M1", new Vector2(132f, 0f)),
                ("Enemy_Archer_M1", new Vector2(138f, 3.5f)),
                // Room N (4)
                ("Enemy_Zombie_N1", new Vector2(90f, 8f)),
                ("Enemy_Archer_N1", new Vector2(91f, 12.5f)),
                ("Enemy_Grenadier_N1", new Vector2(122f, 12.5f)),
                ("Enemy_Shield_N1", new Vector2(142f, 8f)),
                // Room O (4)
                ("Enemy_Rat_O1", new Vector2(88f, -16f)),
                ("Enemy_Rat_O2", new Vector2(102f, -16f)),
                ("Enemy_Zombie_O1", new Vector2(134f, -16f)),
                ("Enemy_Grenadier_O1", new Vector2(144f, -11.5f)),
                // Room P (3)
                ("Enemy_Rat_P1", new Vector2(176f, -16f)),
                ("Enemy_Shield_P1", new Vector2(192f, -16f)),
                ("Enemy_Archer_P1", new Vector2(191f, -11.5f)),
                // Room Q (3)
                ("Enemy_Zombie_Q1", new Vector2(-45f, 16f)),
                ("Enemy_Rat_Q1", new Vector2(-58f, 16f)),
                ("Enemy_Archer_Q1", new Vector2(-82f, 19.5f)),
                // Room R (3)
                ("Enemy_Shield_R1", new Vector2(-48f, 24f)),
                ("Enemy_Grenadier_R1", new Vector2(-48f, 28.5f)),
                ("Enemy_Zombie_R1", new Vector2(-76f, 28.5f)),
                // Room T (4)
                ("Enemy_Zombie_T1", new Vector2(58f, 40f)),
                ("Enemy_Archer_T1", new Vector2(58f, 44.5f)),
                ("Enemy_Shield_T1", new Vector2(98f, 40f)),
                ("Enemy_Grenadier_T1", new Vector2(99f, 44.5f)),
            };
            foreach (var (name, pos) in expectedSpawns)
            {
                var e = enemies.SingleOrDefault(x => x.name == name);
                Assert.IsNotNull(e, $"[A07/Enemy/{name}] Missing instance.");
                Assert.AreEqual(pos.x, e.transform.position.x, GeometryAuditHelper.Eps, $"[A07/Enemy/{name}/X]");
                Assert.AreEqual(pos.y, e.transform.position.y, GeometryAuditHelper.Eps, $"[A07/Enemy/{name}/Y]");
                Assert.IsFalse(e.enabled, $"[A07/Enemy/{name}] Controller must start disabled before zone wake.");
                Assert.IsFalse(e.GetComponent<Rigidbody2D>().simulated, $"[A07/Enemy/{name}] Rigidbody2D must start unsimulated.");

                var soE = new SerializedObject(e);
                Assert.AreEqual(player.transform, soE.FindProperty("_player").objectReferenceValue, $"[A07/Enemy/{name}.Player]");
                Assert.AreEqual(projSpawner, soE.FindProperty("_projectiles").objectReferenceValue, $"[A07/Enemy/{name}.Projectiles]");
                Assert.AreEqual(vfx, soE.FindProperty("_vfx").objectReferenceValue, $"[A07/Enemy/{name}.Vfx]");
            }

            // 6. Twelve EncounterZones matching Spec §5.3 bounds and members
            var zones = Object.FindObjectsByType<EncounterZone>(FindObjectsInactive.Include);
            Assert.AreEqual(12, zones.Length, "[A07/ZoneCount]");
            AssertZone(zones, "B1", new Vector2(20f, 0f), new Vector2(42f, 6f), "Enemy_Zombie_B", "Enemy_Rat_B2");
            AssertZone(zones, "D1", new Vector2(23f, 8f), new Vector2(41f, 13.5f), "Enemy_Zombie_D1");
            AssertZone(zones, "G1", new Vector2(58f, 0f), new Vector2(76f, 7.75f), "Enemy_Rat_G");
            AssertZone(zones, "J1", new Vector2(-16f, 16f), new Vector2(6f, 22.5f), "Enemy_Shield_J", "Enemy_Rat_J2");
            AssertZone(zones, "E1", new Vector2(-16f, 24f), new Vector2(22f, 31f), "Enemy_Zombie_E", "Enemy_Archer_E", "Enemy_Rat_E2");
            AssertZone(zones, "M1", new Vector2(78f, 0f), new Vector2(148f, 6.8f), "Enemy_Zombie_M1", "Enemy_Grenadier_M1", "Enemy_Shield_M1", "Enemy_Archer_M1");
            AssertZone(zones, "N1", new Vector2(78f, 8f), new Vector2(160f, 22f), "Enemy_Zombie_N1", "Enemy_Archer_N1", "Enemy_Grenadier_N1", "Enemy_Shield_N1");
            AssertZone(zones, "O1", new Vector2(78f, -16f), new Vector2(162f, -2f), "Enemy_Rat_O1", "Enemy_Rat_O2", "Enemy_Zombie_O1", "Enemy_Grenadier_O1");
            AssertZone(zones, "P1", new Vector2(166f, -16f), new Vector2(210f, -2f), "Enemy_Rat_P1", "Enemy_Shield_P1", "Enemy_Archer_P1");
            AssertZone(zones, "Q1", new Vector2(-100f, 16f), new Vector2(-32f, 23f), "Enemy_Zombie_Q1", "Enemy_Rat_Q1", "Enemy_Archer_Q1");
            AssertZone(zones, "R1", new Vector2(-86f, 24f), new Vector2(-32f, 38f), "Enemy_Shield_R1", "Enemy_Grenadier_R1", "Enemy_Zombie_R1");
            AssertZone(zones, "T1", new Vector2(40f, 40f), new Vector2(138f, 62f), "Enemy_Zombie_T1", "Enemy_Archer_T1", "Enemy_Shield_T1", "Enemy_Grenadier_T1");

            foreach (var z in zones)
            {
                foreach (var member in z.Enemies)
                {
                    Assert.IsNotNull(member, $"[A07/Zone/{z.ZoneId}/NullMember]");
                    Assert.IsTrue(z.ContainsPoint(member.transform.position),
                        $"[A07/Zone/{z.ZoneId}/EnemyInZone] '{member.name}' at {member.transform.position} must lie inside zone [{z.MinBounds}..{z.MaxBounds}].");
                }
            }

            var i11Scroll = GameObject.Find("I11_Scroll_EastCourtyard");
            var tp1EastVault = GameObject.Find("TP_1_EastVault");
            Assert.IsNotNull(i11Scroll, "[A07/I11_Scroll_EastCourtyard]");
            Assert.IsNotNull(tp1EastVault, "[A07/TP_1_EastVault]");
            float i11ToTp1Dist = Vector2.Distance(i11Scroll.transform.position, tp1EastVault.transform.position);
            Assert.GreaterOrEqual(i11ToTp1Dist, 4.0f,
                $"[A07/I11_TP1_Clearance] I11_Scroll_EastCourtyard ({i11Scroll.transform.position}) and TP_1_EastVault ({tp1EastVault.transform.position}) distance={i11ToTp1Dist:F2} must be >= 4.0f.");

            // 7. I3 HealingChest (unique singleton) at (72, 0.75) size (2.5, 1.5), InputActions wired
            var chests = Object.FindObjectsByType<HealingChest>(FindObjectsInactive.Include);
            Assert.AreEqual(1, chests.Length, "[A07/I3.Unique] I3_HealingChest must remain unique singleton.");
            var chest = chests[0];
            Assert.IsNotNull(chest.TriggerZone, "[A07/I3.Trigger]");
            var cb = GeometryAuditHelper.GetWorldBounds(chest.TriggerZone);
            Assert.AreEqual(72f, cb.center.x, GeometryAuditHelper.Eps, "[A07/I3.CenterX]");
            Assert.AreEqual(0.75f, cb.center.y, GeometryAuditHelper.Eps, "[A07/I3.CenterY]");
            Assert.AreEqual(2.5f, cb.size.x, GeometryAuditHelper.Eps, "[A07/I3.SizeX]");
            Assert.AreEqual(1.5f, cb.size.y, GeometryAuditHelper.Eps, "[A07/I3.SizeY]");
            Assert.IsNotNull(chest.InputActions, "[A07/I3.InputActions]");
            Assert.IsNotNull(chest.InputActions.FindActionMap("Player")?.FindAction("Interact"), "[A07/I3.InteractAction]");
            Assert.IsNotNull(chest.PromptIndicator, "[A07/I3.Prompt]");
            Assert.IsNotNull(chest.GlowHighlight, "[A07/I3.Glow]");

            // 8. I4 EncounterGate (unique singleton) wired to distinct Zombie E + Archer E and barrier at x=22, y=24..28
            var gates = Object.FindObjectsByType<EncounterGate>(FindObjectsInactive.Include);
            Assert.AreEqual(1, gates.Length, "[A07/I4.Unique] I4_EncounterGate must remain unique singleton.");
            var gate = gates[0];
            Assert.IsNotNull(gate.FirstEnemy);
            Assert.IsNotNull(gate.SecondEnemy);
            Assert.AreNotEqual(gate.FirstEnemy, gate.SecondEnemy, "[A07/I4.DistinctEnemies]");
            CollectionAssert.AreEquivalent(
                new[] { "Enemy_Zombie_E", "Enemy_Archer_E" },
                new[] { gate.FirstEnemy.name, gate.SecondEnemy.name },
                "[A07/I4.RoomEEnemies]");
            Assert.IsNotNull(gate.GateBarrierRoot);
            Assert.IsNotNull(gate.GateBarrierRoot.GetComponent<SpriteRenderer>());
            Assert.IsNotNull(gate.GateBarrierRoot.GetComponent<BoxCollider2D>());

            // 9. I5 ExitTrigger (unique singleton) at (5, 33) size (2, 2) wired to run, gate, player
            var exits = Object.FindObjectsByType<ExitTrigger>(FindObjectsInactive.Include);
            Assert.AreEqual(1, exits.Length, "[A07/I5.Unique] I5_ExitTrigger must remain unique singleton.");
            var exit = exits[0];
            var eb = GeometryAuditHelper.GetWorldBounds(exit.GetComponent<BoxCollider2D>());
            Assert.AreEqual(5f, eb.center.x, GeometryAuditHelper.Eps, "[A07/I5.CenterX]");
            Assert.AreEqual(33f, eb.center.y, GeometryAuditHelper.Eps, "[A07/I5.CenterY]");
            Assert.AreEqual(2f, eb.size.x, GeometryAuditHelper.Eps, "[A07/I5.SizeX]");
            Assert.AreEqual(2f, eb.size.y, GeometryAuditHelper.Eps, "[A07/I5.SizeY]");
            Assert.AreEqual(run, exit.Run, "[A07/I5.Run]");
            Assert.AreEqual(gate, exit.Gate, "[A07/I5.Gate]");
            Assert.AreEqual(player, exit.Player, "[A07/I5.Player]");

            // 10. ResultPanel & EventSystem
            var panel = Object.FindAnyObjectByType<ResultPanel>();
            Assert.IsNotNull(panel, "[A07/ResultPanel]");
            Assert.IsTrue(panel.gameObject.activeSelf, "[A07/ResultPanel.RootActive]");
            Assert.IsNotNull(panel.PanelRoot, "[A07/ResultPanel.PanelRoot]");
            Assert.IsFalse(panel.PanelRoot.activeSelf, "[A07/ResultPanel.HiddenInitially]");
            Assert.AreEqual(run, panel.Run, "[A07/ResultPanel.Run]");
            Assert.IsNotNull(panel.TitleText, "[A07/ResultPanel.Title]");
            Assert.IsNotNull(panel.ReplayButton, "[A07/ResultPanel.ReplayButton]");

            var es = Object.FindAnyObjectByType<EventSystem>();
            Assert.IsNotNull(es, "[A07/EventSystem]");
            var uiMod = es.GetComponent<InputSystemUIInputModule>();
            Assert.IsNotNull(uiMod, "[A07/InputSystemUIInputModule]");
            Assert.IsNotNull(uiMod.actionsAsset, "[A07/UIInputActions]");

            // 11. Scene registered in EditorBuildSettings (and Main_Level remains index 0)
            var buildScenes = EditorBuildSettings.scenes;
            Assert.IsTrue(buildScenes.Any(s => s.path == GeometryAuditHelper.ScenePath && s.enabled),
                "[A07/BuildSettings] NhaGiamGiengChuong.unity must be enabled in EditorBuildSettings.");
            Assert.AreEqual("Assets/_Project/Scenes/Main_Level.unity", buildScenes[0].path,
                "[A07/BuildSettings] Main_Level.unity must remain entry scene at index 0.");
        }

        [Test]
        public void A08_VisualCoverageGroundedPropsAndCorrectChestSprite()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. Swapped chest/barrel fix & cohesive Prison Room C/G palette: I3_HealingChest uses golden chest StorageAndVessel_003; Art_G_Barrel uses floorjunk_0_00
            var chest = Object.FindAnyObjectByType<HealingChest>();
            Assert.IsNotNull(chest);
            var chestSr = chest.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(chestSr?.sprite, "[A08/ChestSprite] Missing I3_HealingChest/Visual sprite.");
            Assert.AreEqual("StorageAndVessel_003", chestSr.sprite.name,
                "[A08/ChestSprite] I3_HealingChest must display golden chest StorageAndVessel_003, not barrel StorageAndVessel_014.");
            Assert.LessOrEqual(Mathf.Abs(chestSr.bounds.min.y - 0f), 0.08f,
                "[A08/ChestGrounded] Chest visual base must sit flush on floor y=0.");

            var barrelGo = GameObject.Find("Art_G_Barrel");
            Assert.IsNotNull(barrelGo, "[A08/Barrel] Missing Art_G_Barrel.");
            var barrelSr = barrelGo.GetComponent<SpriteRenderer>();
            Assert.AreEqual("floorjunk_0_00", barrelSr.sprite.name,
                "[A08/BarrelSprite] Art_G_Barrel must display cohesive 24-PPU Prison floorjunk_0_00, not GothicDungeon StorageAndVessel_014.");

            string[] bannedRoomCGSprites =
            {
                "StorageAndVessel_014", "StorageAndVessel_010", "MachineryAndBell_001", "MachineryAndBell_012",
            };
            foreach (string roomPrefabName in new[] { "CD", "G" })
            {
                var roomGo = GameObject.Find($"Rooms/Room_{roomPrefabName}");
                Assert.IsNotNull(roomGo, $"[A08/RoomMissing/{roomPrefabName}]");
                foreach (var sr in roomGo.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr == null || sr.sprite == null)
                        continue;
                    CollectionAssert.DoesNotContain(bannedRoomCGSprites, sr.sprite.name,
                        $"[A08/RoomCGCohesion] '{sr.gameObject.name}' in Room {roomPrefabName} must not use legacy GothicDungeon sprite '{sr.sprite.name}'.");
                }
            }

            // 2. Every room prefab must contain visual-only Tilemaps (0 TilemapCollider2D): 9 rooms * 4 >= 36
            var tilemaps = Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>();
            Assert.GreaterOrEqual(tilemaps.Length, 36, "[A08/Tilemaps] All 9 rooms must render visual-only tilemaps.");
            foreach (var tm in tilemaps)
            {
                Assert.IsNull(tm.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>(),
                    $"[A08/TilemapNoCol] Visual Tilemap '{tm.name}' must not carry TilemapCollider2D.");
            }

            // 3. Previously invisible solid floors, outer walls, ceilings, stair core, interior doorway headers, AND wing floors must have painted Tilemap cells
            var solidTilemaps = tilemaps.Where(t => t.name == "TM_SolidVisual").ToArray();
            Assert.AreEqual(9, solidTilemaps.Length, "[A08/SolidTilemaps] Expected 1 TM_SolidVisual per room prefab (9 total).");

            Vector2[] requiredSolidSamples =
            {
                new Vector2(60.5f, 7.5f),   // Floor_G_UpperWest [56,65]x[7,8]
                new Vector2(74.0f, 7.5f),   // Floor_G_UpperEast [72,76]x[7,8]
                new Vector2(20.0f, 7.5f),   // Stairs_H landing [18,22]x[7,8]
                new Vector2(10.5f, 11.5f),  // Stairs_H solid stone core
                new Vector2(-0.25f, 2.5f),  // Wall_A_West outer wall
                new Vector2(76.25f, 5.0f),  // Wall_GM_Mid_76 outer wall
                new Vector2(10.0f, 5.75f),  // Ceiling_A
                new Vector2(11.0f, 31.5f),  // Floor_F_Ceiling_E
                new Vector2(18.10f, 5.0f),  // Interior doorway header Wall_AB_Header_18 (half-offset wall)
                new Vector2(-15.90f, 22.0f),// Interior doorway header Wall_JK_Mid_Neg16 (negative X half-offset wall)
                new Vector2(90.0f, -0.5f),  // Floor_M_West (W1_EastCourtyard)
                new Vector2(120.0f, -16.5f),// Floor_O (W2_SewerCistern)
                new Vector2(-60.0f, 15.5f), // Floor_Q (W3_WestBastion)
                new Vector2(60.0f, 39.5f),  // Floor_T_West (U1_UpperPromenade)
            };
            foreach (var pt in requiredSolidSamples)
            {
                bool covered = solidTilemaps.Any(tm => tm.GetTile(tm.WorldToCell(pt)) != null);
                Assert.IsTrue(covered, $"[A08/SolidCoverage] World point ({pt.x:F2}, {pt.y:F2}) has no painted TM_SolidVisual tile.");
            }

            // 4. Key architectural props must be grounded on their room floor (delta <= 0.12u), and Art_F_ExitGlow must keep its Light2D
            (string name, float expectedFloorY)[] groundedProps =
            {
                ("Art_A_Fountain", 0f),
                ("Art_B_Arch1", 0f),
                ("Art_B_Pillar", 0f),
                ("Art_B_Arch2", 0f),
                ("Art_G_Winch", 8f),
                ("Art_J_Arch", 16f),
                ("Art_E_Pillar1", 24f),
                ("Art_E_Pillar2", 24f),
                ("Art_F_ExitArch", 32f),
                ("Art_F_ExitGlow", 32f),
                ("Art_M_ArmoryDome1", 0f),
                ("Art_O_MonsterDoor", -16f),
                ("Art_P_SewerExitDoor", -16f),
                ("Art_Q_JudasSeat", 16f),
                ("Art_R_ArmoryDome", 24f),
                ("Art_S_AlchemyBibli1", 24f),
                ("Art_T_CourtyardArch", 40f),
            };
            foreach (var (propName, floorY) in groundedProps)
            {
                var go = GameObject.Find(propName);
                Assert.IsNotNull(go, $"[A08/Grounded/{propName}] Missing prop.");
                var sr = go.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite);
                Assert.AreEqual(floorY, sr.bounds.min.y, 0.12f,
                    $"[A08/Grounded/{propName}] Visual bottom ({sr.bounds.min.y:F2}) must rest on floor y={floorY:F2}, not float.");
            }
            var exitGlow = GameObject.Find("Art_F_ExitGlow");
            Assert.IsNotNull(exitGlow.GetComponent<UnityEngine.Rendering.Universal.Light2D>(),
                "[A08/ExitGlowLight] Art_F_ExitGlow must retain its warm Light2D beacon.");
        }

        [Test]
        public void A09_SceneHudAndChestWorldUiAreWiredAndVisible()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. PlayerHUD instantiated and wired to Player Health + Equipment + TrailFill
            var player = Object.FindAnyObjectByType<PlayerController>();
            var playerHealth = player.GetComponent<Health>();
            var playerBow = player.GetComponentInChildren<PlayerBow>(true);

            var healthHud = Object.FindAnyObjectByType<DeadCells.HUD.PlayerHealthHud>();
            var equipHud = Object.FindAnyObjectByType<DeadCells.HUD.PlayerEquipmentHud>();
            var barView = Object.FindAnyObjectByType<DeadCells.HUD.HealthBarView>();
            Assert.IsNotNull(healthHud, "[A09/HUD] NhaGiamGiengChuong.unity must contain PlayerHealthHud.");
            Assert.IsNotNull(equipHud, "[A09/HUD] NhaGiamGiengChuong.unity must contain PlayerEquipmentHud.");
            Assert.IsNotNull(barView, "[A09/HUD] NhaGiamGiengChuong.unity must contain HealthBarView.");
            Assert.AreEqual(playerHealth, new SerializedObject(healthHud).FindProperty("_health").objectReferenceValue, "[A09/HUD.Health]");
            Assert.AreEqual(player, new SerializedObject(equipHud).FindProperty("_player").objectReferenceValue, "[A09/HUD.Player]");
            Assert.AreEqual(playerBow, new SerializedObject(equipHud).FindProperty("_bow").objectReferenceValue, "[A09/HUD.Bow]");
            Assert.IsNotNull(new SerializedObject(barView).FindProperty("_trailFill").objectReferenceValue, "[A09/HUD.TrailFill]");

            // 2. I3_HealingChest PromptIndicator and FullHealthFeedback must contain real visible UI Text with valid font
            var chest = Object.FindAnyObjectByType<HealingChest>();
            Assert.IsNotNull(chest);
            var fullFeedbackGo = new SerializedObject(chest).FindProperty("_fullHealthFeedback").objectReferenceValue as GameObject;
            Assert.IsNotNull(fullFeedbackGo);
            var promptText = chest.PromptIndicator.GetComponentInChildren<UnityEngine.UI.Text>(true);
            var fullText = fullFeedbackGo.GetComponentInChildren<UnityEngine.UI.Text>(true);
            Assert.IsNotNull(promptText, "[A09/ChestPrompt] PromptIndicator must render real UI Text.");
            Assert.IsNotNull(promptText.font, "[A09/ChestPromptFont] World-Space UI Text must assign LegacyRuntime.ttf.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(promptText.text));
            Assert.IsNotNull(fullText, "[A09/ChestFull] FullHealthFeedback must render real UI Text.");
            Assert.IsNotNull(fullText.font, "[A09/ChestFullFont] World-Space UI Text must assign LegacyRuntime.ttf.");
            Assert.AreEqual(HealingChest.FullHealthMessage, fullText.text);

            // 3. Global Light 2D intensity calibrated for Dead Cells contrast ([0.35f, 0.62f])
            var globalLight = Object.FindObjectsByType<Light2D>()
                .FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
            Assert.IsNotNull(globalLight);
            Assert.GreaterOrEqual(globalLight.intensity, 0.35f,
                "[A09/Lighting] Global Light 2D intensity must be >= 0.35f for readable dungeon silhouettes.");
            Assert.LessOrEqual(globalLight.intensity, 0.62f,
                "[A09/Lighting] Global Light 2D intensity must be <= 0.62f so warm beacons and silhouettes pop.");
        }

        [Test]
        public void A10_DeadCellsPrisonVisualStandard()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. Native 1.0u Prison Grid & Zero Mixel Explosion
            var grids = Object.FindObjectsByType<Grid>()
                .Where(g => g.gameObject.name == "VisualGrid")
                .ToArray();
            Assert.AreEqual(9, grids.Length, "[A10/GridCount] Expected 9 VisualGrid instances (one per room prefab).");
            foreach (var grid in grids)
            {
                Assert.AreEqual(new Vector3(1f, 1f, 0f), grid.cellSize,
                    $"[A10/GridCellSize] VisualGrid under '{grid.transform.root.name}' must use native 1.0u cellSize.");
            }

            var tilemaps = Object.FindObjectsByType<Tilemap>();
            Assert.GreaterOrEqual(tilemaps.Length, 36,
                "[A10/TilemapCount] Expected >= 36 visual-only Tilemaps (9 rooms * 4: TM_Background, TM_WallDecals, TM_SolidVisual, TM_SolidRim).");
            foreach (var tm in tilemaps)
            {
                Assert.IsNull(tm.GetComponent<TilemapCollider2D>(),
                    $"[A10/TilemapNoCol] Visual Tilemap '{tm.name}' must not carry TilemapCollider2D.");
            }

            var bgTilemaps = tilemaps.Where(t => t.name == "TM_Background").ToArray();
            var solidTilemaps = tilemaps.Where(t => t.name == "TM_SolidVisual").ToArray();
            Assert.AreEqual(9, bgTilemaps.Length, "[A10/BgTilemaps] Expected 9 TM_Background tilemaps.");
            Assert.AreEqual(9, solidTilemaps.Length, "[A10/SolidTilemaps] Expected 9 TM_SolidVisual tilemaps.");

            int checkedBgTiles = 0;
            foreach (var bgTm in bgTilemaps)
            {
                var bounds = bgTm.cellBounds;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        var cell = new Vector3Int(x, y, 0);
                        var sp = bgTm.GetSprite(cell);
                        if (sp == null)
                            continue;
                        checkedBgTiles++;
                        StringAssert.StartsWith("backWall_", sp.name,
                            $"[A10/BgTile] TM_Background cell {cell} must use backWall_* tile, got '{sp.name}'.");
                        var m = bgTm.GetTransformMatrix(cell);
                        float sx = new Vector3(m.m00, m.m10, m.m20).magnitude;
                        float sy = new Vector3(m.m01, m.m11, m.m21).magnitude;
                        Assert.AreEqual(1.0f, sx, 0.05f, $"[A10/BgScaleX] TM_Background cell {cell} scale.x must be 1.0.");
                        Assert.AreEqual(1.0f, sy, 0.05f, $"[A10/BgScaleY] TM_Background cell {cell} scale.y must be 1.0.");
                    }
                }
            }
            Assert.Greater(checkedBgTiles, 100, "[A10/BgTileCount] TM_Background must have painted backWall_* cells.");

            Vector3Int[] walkableFloorCells =
            {
                new Vector3Int(10, -1, 0),
                new Vector3Int(60, 7, 0),
                new Vector3Int(74, 7, 0),
                new Vector3Int(10, 31, 0),
            };
            foreach (var cell in walkableFloorCells)
            {
                var sp = solidTilemaps.Select(tm => tm.GetSprite(cell)).FirstOrDefault(s => s != null);
                Assert.IsNotNull(sp, $"[A10/FloorStamp] Missing TM_SolidVisual tile at cell {cell}.");
                StringAssert.StartsWith("floorStamp_", sp.name,
                    $"[A10/FloorStamp] Walkable floor cell {cell} must use floorStamp_*, got '{sp.name}'.");
            }

            var stairsCoreSprite = solidTilemaps.Select(tm => tm.GetSprite(new Vector3Int(10, 11, 0))).FirstOrDefault(s => s != null);
            Assert.IsNotNull(stairsCoreSprite, "[A10/StairsCore] Missing TM_SolidVisual tile at Stairs_H core (10, 11).");
            Assert.AreEqual("dirt_00", stairsCoreSprite.name,
                $"[A10/StairsCore] Stairs_H interior (10, 11) must use opaque dirt_00, got '{stairsCoreSprite.name}'.");

            // 2. Prop Proportion Discipline (Player = 1.66u)
            var chest = Object.FindAnyObjectByType<HealingChest>();
            Assert.IsNotNull(chest, "[A10/Chest]");
            var chestSr = chest.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
            var barrelSr = GameObject.Find("Art_G_Barrel")?.GetComponent<SpriteRenderer>();
            var crateSr = GameObject.Find("Art_G_Crate")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(chestSr?.sprite, "[A10/ChestVisual]");
            Assert.IsNotNull(barrelSr?.sprite, "[A10/BarrelVisual]");
            Assert.IsNotNull(crateSr?.sprite, "[A10/CrateVisual]");
            Assert.LessOrEqual(chestSr.bounds.size.y, 1.35f, $"[A10/ChestHeight] Expected <= 1.35u, got {chestSr.bounds.size.y:F2}u.");
            Assert.LessOrEqual(barrelSr.bounds.size.y, 1.35f, $"[A10/BarrelHeight] Expected <= 1.35u, got {barrelSr.bounds.size.y:F2}u.");
            Assert.LessOrEqual(crateSr.bounds.size.y, 1.35f, $"[A10/CrateHeight] Expected <= 1.35u, got {crateSr.bounds.size.y:F2}u.");

            var gate = Object.FindAnyObjectByType<EncounterGate>();
            Assert.IsNotNull(gate?.GateBarrierRoot, "[A10/Gate]");
            var gateSr = gate.GateBarrierRoot.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(gateSr?.sprite, "[A10/GateVisual]");
            Assert.LessOrEqual(gateSr.bounds.size.x, 1.25f, $"[A10/GateWidth] Expected <= 1.25u, got {gateSr.bounds.size.x:F2}u.");

            var cageSr = GameObject.Find("Art_A_Cage")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(cageSr?.sprite, "[A10/Cage]");
            Assert.GreaterOrEqual(cageSr.bounds.min.y, 3.0f, $"[A10/CageClearance] Art_A_Cage bottom ({cageSr.bounds.min.y:F2}) must be >= 3.0f.");

            float promptBottomWorldY = chest.PromptIndicator.transform.position.y - 0.36f;
            float promptGap = promptBottomWorldY - chestSr.bounds.max.y;
            Assert.GreaterOrEqual(promptGap, 0.20f,
                $"[A10/PromptGap] PromptIndicator bottom ({promptBottomWorldY:F2}) must sit >= +0.20u above chest lid ({chestSr.bounds.max.y:F2}), gap={promptGap:F2}.");

            // 3. No Floating Dining Tables on One-Way Platforms
            var allSprites = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            var bannedWood = allSprites
                .Where(sr => sr.sprite != null && (sr.sprite.name == "WoodPlatform_007" || sr.sprite.name == "WoodPlatform_008"))
                .Select(sr => sr.gameObject.name)
                .ToArray();
            Assert.IsEmpty(bannedWood, $"[A10/BannedWood] Found banned WoodPlatform_007/008 sprites on: {string.Join(", ", bannedWood)}");

            string[] plankRowNames =
            {
                // Core Spine (8) — P0-04 Part A pruned Art_A_PracticePlanks + Art_C_RestPlanks
                "Art_C_TopPlanks",
                "Art_G_UpperPlanks",
                "Art_K_TopPlanks",
                "Art_E_CombatPlanks",
                "Art_L1_Planks",
                "Art_L2_Planks",
                "Art_L3_Planks",
                "Art_L4_Planks",
                // W1_EastCourtyard (6) — P0-04 Part A pruned Art_N_HighPlanks
                "Art_M_WellBridgePlanks",
                "Art_M_Perch1Planks",
                "Art_M_Perch2Planks",
                "Art_N_WellCapPlanks",
                "Art_N_Mid1Planks",
                "Art_N_Mid2Planks",
                // W2_SewerCistern (4) — P0-04 Part A pruned Art_O_WestPlanks
                "Art_O_ShaftStep1Planks",
                "Art_O_ShaftStep2Planks",
                "Art_O_EastPlanks",
                "Art_P_MidPlanks",
                // W3_WestBastion (4)
                "Art_Q_MidPlanks",
                "Art_R_WellCapPlanks",
                "Art_R_Mid1Planks",
                "Art_R_Mid2Planks",
                // U1_UpperPromenade (3) — P0-04 Part A pruned Art_S_GalleryPlanks + Art_T_HighPlanks
                "Art_T_ShaftCapPlanks",
                "Art_T_Mid1Planks",
                "Art_T_Mid2Planks",
            };
            Assert.AreEqual(25, plankRowNames.Length, "[A10/PlankRowCount]");
            foreach (var rowName in plankRowNames)
            {
                var rowGo = GameObject.Find(rowName);
                Assert.IsNotNull(rowGo, $"[A10/Planks/{rowName}] Missing plank row.");
                var pieces = rowGo.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.IsNotEmpty(pieces, $"[A10/Planks/{rowName}] Plank row has no SpriteRenderers.");
                foreach (var piece in pieces)
                {
                    Assert.IsNotNull(piece.sprite, $"[A10/Planks/{rowName}/{piece.name}] Missing sprite.");
                    StringAssert.StartsWith("woodenPlatform_", piece.sprite.name,
                        $"[A10/Planks/{rowName}/{piece.name}] Expected woodenPlatform_*, got '{piece.sprite.name}'.");
                }
            }

            // P0-04 Part A: the six pruned nonsense ledges and their dressing must be gone, not just uncounted
            string[] prunedPlankRows =
            {
                "Art_A_PracticePlanks", "Art_C_RestPlanks", "Art_N_HighPlanks",
                "Art_O_WestPlanks", "Art_S_GalleryPlanks", "Art_T_HighPlanks",
            };
            foreach (string prunedRow in prunedPlankRows)
            {
                Assert.IsNull(GameObject.Find(prunedRow), $"[A10/Pruned/{prunedRow}] Pruned plank row must be removed from the scene.");
            }
            foreach (string prunedBrace in new[] { "Art_S_GalleryBraceL", "Art_S_GalleryBraceR", "Art_T_HighBraceL", "Art_T_HighBraceR" })
            {
                Assert.IsNull(GameObject.Find(prunedBrace), $"[A10/Pruned/{prunedBrace}] Brace of a pruned plank row must be removed from the scene.");
            }

            // 4. Full 19/19 Room Point-Light Coverage (>= 75% total, >= 50% in every single room)
            var pointLights = Object.FindObjectsByType<Light2D>()
                .Where(l => l.enabled && l.lightType == Light2D.LightType.Point && l.intensity > 0.01f && l.pointLightOuterRadius > 0.1f)
                .ToArray();
            Assert.IsNotEmpty(pointLights, "[A10/PointLights] No active Point Light2D found.");

            (string roomId, Rect rect)[] rooms =
            {
                ("A", new Rect(0f, 0f, 18f, 5.5f)),
                ("B", new Rect(18f, 0f, 24f, 6f)),
                ("C", new Rect(42f, 0f, 14f, 18.5f)),
                ("D", new Rect(22f, 8f, 20f, 5.5f)),
                ("G", new Rect(56f, 0f, 20f, 14.5f)),
                ("H", new Rect(6f, 8f, 16f, 14.5f)),
                ("J", new Rect(-16f, 16f, 22f, 6.5f)),
                ("K", new Rect(-30f, 16f, 14f, 14.5f)),
                ("E", new Rect(-16f, 24f, 38f, 7f)),
                ("L", new Rect(22f, 24f, 16f, 14.5f)),
                ("F", new Rect(0f, 32f, 22f, 6f)),
                ("M", new Rect(76f, 0f, 88f, 7f)),
                ("N", new Rect(76f, 8f, 88f, 14.5f)),
                ("O", new Rect(76f, -16f, 88f, 15f)),
                ("P", new Rect(164f, -16f, 70f, 14.5f)),
                ("Q", new Rect(-102f, 16f, 72f, 7f)),
                ("R", new Rect(-102f, 24f, 72f, 14.5f)),
                ("S", new Rect(38f, 24f, 54f, 15f)),
                ("T", new Rect(38f, 40f, 102f, 22.5f)),
            };

            int totalSamples = 0;
            int totalLit = 0;
            foreach (var (roomId, rect) in rooms)
            {
                int roomSamples = 0;
                int roomLit = 0;
                for (float x = rect.xMin + 0.5f; x < rect.xMax; x += 1f)
                {
                    for (float y = rect.yMin + 0.5f; y < rect.yMax; y += 1f)
                    {
                        var pt = new Vector2(x, y);
                        roomSamples++;
                        bool lit = pointLights.Any(l => Vector2.Distance(pt, l.transform.position) <= l.pointLightOuterRadius);
                        if (lit)
                            roomLit++;
                    }
                }
                float ratio = roomSamples > 0 ? (float)roomLit / roomSamples : 0f;
                Assert.GreaterOrEqual(ratio, 0.50f,
                    $"[A10/LightCoverage/{roomId}] Room {roomId} point-light coverage is {ratio:P1} ({roomLit}/{roomSamples}), expected >= 50%.");
                totalSamples += roomSamples;
                totalLit += roomLit;
            }
            float totalRatio = totalSamples > 0 ? (float)totalLit / totalSamples : 0f;
            Assert.GreaterOrEqual(totalRatio, 0.75f,
                $"[A10/LightCoverage/Total] Map point-light coverage is {totalRatio:P1} ({totalLit}/{totalSamples}), expected >= 75%.");

            // 5. URP 2D Post-Processing & Ambient VFX
            var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            Assert.IsNotNull(cam, "[A10/Camera]");
            var uacd = cam.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsNotNull(uacd, "[A10/UACD]");
            Assert.IsTrue(uacd.renderPostProcessing, "[A10/PostProcess] Main Camera must enable renderPostProcessing = true.");

            var globalVol = Object.FindObjectsByType<Volume>().FirstOrDefault(v => v.enabled && v.isGlobal);
            Assert.IsNotNull(globalVol, "[A10/Volume] Missing enabled Global Volume.");
            Assert.IsNotNull(globalVol.sharedProfile, "[A10/VolumeProfile] Global Volume must reference DeadCells_PostProcessProfile.asset.");
            Assert.AreEqual("Assets/_Project/Settings/DeadCells_PostProcessProfile.asset",
                AssetDatabase.GetAssetPath(globalVol.sharedProfile),
                "[A10/VolumeProfilePath] Global Volume must reference shared DeadCells_PostProcessProfile.asset.");
            Assert.IsTrue(globalVol.sharedProfile.TryGet<Bloom>(out var bloom) && bloom.intensity.overrideState && bloom.intensity.value > 0f,
                "[A10/Bloom] Bloom override missing.");
            Assert.IsTrue(globalVol.sharedProfile.TryGet<Vignette>(out var vignette) && vignette.intensity.overrideState && vignette.intensity.value > 0f,
                "[A10/Vignette] Vignette override missing.");
            Assert.IsTrue(globalVol.sharedProfile.TryGet<Tonemapping>(out var tone) && tone.mode.overrideState && tone.mode.value == TonemappingMode.ACES,
                "[A10/Tonemapping] ACES Tonemapping override missing.");

            var particleSystems = Object.FindObjectsByType<ParticleSystem>();
            Assert.GreaterOrEqual(particleSystems.Length, 2,
                $"[A10/AmbientVfx] Expected >= 2 ambient ParticleSystems, found {particleSystems.Length}.");
        }

        [Test]
        public void A11_Round5_ParallaxShadersNormalMapsZeroMixelPropsAndOneWayArc()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. [PM-1] All 25 PlatformEffector2D components across the 9 room prefabs use surfaceArc == 160f
            var effectors = Object.FindObjectsByType<PlatformEffector2D>(FindObjectsInactive.Include);
            Assert.AreEqual(25, effectors.Length, "[A11/OneWayCount] Expected 25 PlatformEffector2D components in NhaGiamGiengChuong.");
            foreach (var eff in effectors)
            {
                Assert.IsTrue(eff.useOneWay, $"[A11/OneWay/{eff.name}] useOneWay must be true.");
                Assert.AreEqual(160f, eff.surfaceArc, 1e-3f,
                    $"[A11/OneWayArc/{eff.name}] surfaceArc must be 160f so side contacts never block walk/roll.");
            }

            // 2. [P1-R1] & [PM-3] Zero legacy LightAndBanner_001/005; >= 80 24-PPU LanternFixture_R5 (Background/-9), LanternFlameCore_R5 (Background/-8), TorchEmbers (Background/-7)
            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            foreach (var sr in allRenderers)
            {
                if (sr.sprite == null)
                    continue;
                Assert.AreNotEqual("LightAndBanner_001", sr.sprite.name, $"[A11/NoMixelTorch/{sr.name}] Must not use legacy 32-PPU LightAndBanner_001.");
                Assert.AreNotEqual("LightAndBanner_005", sr.sprite.name, $"[A11/NoMixelLantern/{sr.name}] Must not use legacy 32-PPU LightAndBanner_005.");
            }

            var fixtures = allRenderers.Where(sr => sr.gameObject.name == "LanternFixture_R5").ToArray();
            Assert.GreaterOrEqual(fixtures.Length, 80, $"[A11/LanternFixtures] Expected >= 80 LanternFixture_R5 renderers, found {fixtures.Length}.");
            foreach (var fix in fixtures)
            {
                Assert.IsNotNull(fix.sprite);
                Assert.AreEqual("prisonWallLantern_00", fix.sprite.name);
                Assert.AreEqual(24f, fix.sprite.pixelsPerUnit, 1e-4f);
                Assert.AreEqual(Vector3.one, fix.transform.localScale);
                Assert.AreEqual("Background", fix.sortingLayerName);
                Assert.AreEqual(-9, fix.sortingOrder);

                var parent = fix.transform.parent;
                Assert.IsNotNull(parent);
                var core = parent.Find("LanternFlameCore_R5")?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(core, $"[A11/FlameCore/{parent.name}] Missing LanternFlameCore_R5.");
                Assert.AreEqual("Background", core.sortingLayerName, "[A11/FlameCoreLayer] Must sort on Background behind Actors.");
                Assert.AreEqual(-8, core.sortingOrder);
                Assert.IsNotNull(core.sharedMaterial);
                Assert.AreEqual("DeadCells/EmissiveHdr2D", core.sharedMaterial.shader.name);

                var embers = parent.Find("TorchEmbers")?.GetComponent<ParticleSystemRenderer>();
                Assert.IsNotNull(embers, $"[A11/TorchEmbers/{parent.name}] Missing TorchEmbers.");
                Assert.AreEqual("Background", embers.sortingLayerName, "[A11/TorchEmbersLayer] Must sort on Background behind Actors.");
                Assert.AreEqual(-7, embers.sortingOrder);
            }

            // 3. [P1-R2] 8/8 GlowingMushroom_0..7 (24 PPU @ scale 1,1,1), 8/8 MushroomHalo_0..7 (_DstBlend=1, scale.x<=0.20, above floor), 8/8 MushroomLight_0..7
            float[] expectedMushroomFloors = { 0f, 0f, 16f, 24f, 0f, -16f, -16f, 16f };
            for (int i = 0; i < 8; i++)
            {
                var mGo = GameObject.Find($"GlowingMushroom_{i}");
                Assert.IsNotNull(mGo, $"[A11/Mushroom/{i}] Missing GlowingMushroom_{i}.");
                var mSr = mGo.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(mSr?.sprite);
                Assert.AreEqual("glowingMushroom_3_00", mSr.sprite.name);
                Assert.AreEqual(Vector3.one, mGo.transform.localScale);
                Assert.AreEqual("DeadCells/EmissiveHdr2D", mSr.sharedMaterial.shader.name);

                var hGo = GameObject.Find($"MushroomHalo_{i}");
                Assert.IsNotNull(hGo, $"[A11/MushroomHalo/{i}] Missing MushroomHalo_{i}.");
                var hSr = hGo.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(hSr?.sharedMaterial);
                Assert.AreEqual(1f, hSr.sharedMaterial.GetFloat("_DstBlend"), 1e-4f);
                Assert.LessOrEqual(hGo.transform.localScale.x, 0.20f + 1e-4f);
                Assert.Greater(hSr.bounds.min.y, expectedMushroomFloors[i],
                    $"[A11/MushroomHaloClearFloor/{i}] Halo bottom ({hSr.bounds.min.y:F2}) must stay above floorTop ({expectedMushroomFloors[i]:F2}).");

                var lGo = GameObject.Find($"MushroomLight_{i}");
                Assert.IsNotNull(lGo?.GetComponent<Light2D>(), $"[A11/MushroomLight/{i}] Missing MushroomLight_{i}.");
            }

            // 4. [P1-R3] & [PM-5] 9 carved arch bays with >= 45 ArchSill_* and >= 70 ArchJamb_* at 24 PPU, scale (1,1,1), Background/-17
            var archSills = allRenderers.Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal)).ToArray();
            Assert.GreaterOrEqual(archSills.Length, 45, $"[A11/ArchSills] Expected >= 45 ArchSill_* renderers, found {archSills.Length}.");
            foreach (var sill in archSills)
            {
                Assert.IsTrue(
                    sill.sprite.name == "prisonArchSill_00" || sill.sprite.name == "prisonArchSill_01" || sill.sprite.name == "prisonArchSill_02",
                    $"[A11/ArchSillVariant/{sill.name}] Expected prisonArchSill_00..02, got '{sill.sprite.name}'.");
                Assert.AreEqual(24f, sill.sprite.pixelsPerUnit, 1e-4f);
                Assert.AreEqual(Vector3.one, sill.transform.localScale);
                Assert.AreEqual("Background", sill.sortingLayerName);
                Assert.AreEqual(-17, sill.sortingOrder);
            }

            var archJambs = allRenderers.Where(sr => sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal)).ToArray();
            Assert.GreaterOrEqual(archJambs.Length, 70, $"[A11/ArchJambs] Expected >= 70 ArchJamb_* renderers, found {archJambs.Length}.");
            foreach (var jamb in archJambs)
            {
                Assert.IsTrue(
                    jamb.sprite.name == "prisonArchJamb_00" || jamb.sprite.name == "prisonArchJamb_01" || jamb.sprite.name == "prisonArchJamb_02",
                    $"[A11/ArchJambVariant/{jamb.name}] Expected prisonArchJamb_00..02, got '{jamb.sprite.name}'.");
                Assert.AreEqual(24f, jamb.sprite.pixelsPerUnit, 1e-4f);
                Assert.AreEqual(Vector3.one, jamb.transform.localScale);
                Assert.AreEqual("Background", jamb.sortingLayerName);
                Assert.AreEqual(-17, jamb.sortingOrder);
            }

            // 5. [P2-2] 8 multi-elevation 3-piece vertical room banners carry VertexSwayPin2D with exact _PinTopWorldY via MaterialPropertyBlock
            (string name, float topY)[] expectedBanners =
            {
                ("Art_C_Banner", 16.5f),
                ("Art_J_Banner", 21.8f),
                ("Art_E_Banner", 30.2f),
                ("Art_F_Banner", 37.2f),
                ("Art_N_Banner", 21.8f),
                ("Art_P_Banner", -2.2f),
                ("Art_R_Banner", 37.8f),
                ("Art_T_Banner", 61.8f),
            };
            var block = new MaterialPropertyBlock();
            foreach (var (bannerName, topY) in expectedBanners)
            {
                var bannerGo = GameObject.Find(bannerName);
                Assert.IsNotNull(bannerGo, $"[A11/Banner/{bannerName}] Missing multi-piece banner container.");
                var pin = bannerGo.GetComponent<VertexSwayPin2D>();
                Assert.IsNotNull(pin, $"[A11/BannerPin/{bannerName}] Missing VertexSwayPin2D.");
                pin.ApplyPropertyBlock();

                var pieces = bannerGo.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.AreEqual(3, pieces.Length, $"[A11/BannerPieces/{bannerName}] Expected 3 pieces (Top, Mid, Bot).");
                foreach (var piece in pieces)
                {
                    Assert.AreEqual(Vector3.one, piece.transform.localScale, $"[A11/BannerScale/{bannerName}/{piece.name}] Must use 1:1 24-PPU scale.");
                    Assert.IsNotNull(piece.sharedMaterial);
                    Assert.AreEqual("DeadCells/VertexSway2D", piece.sharedMaterial.shader.name);
                    piece.GetPropertyBlock(block);
                    Assert.AreEqual(1.0f, block.GetFloat("_UseWorldYPin"), 1e-4f);
                    Assert.AreEqual(topY, block.GetFloat("_PinTopWorldY"), 1e-3f,
                        $"[A11/BannerPinY/{bannerName}/{piece.name}] _PinTopWorldY must match room topY={topY:F1}.");
                }
            }

            // 6. 4-Tier Hybrid Parallax, Lighting Isolation, GroundMist_R5 & ShadowCasters_R5
            var parallaxRoot = GameObject.Find("Prison_Parallax_R5");
            Assert.IsNotNull(parallaxRoot, "[A11/ParallaxRoot] Missing Prison_Parallax_R5.");
            var farBackdrop = parallaxRoot.transform.Find("ParallaxFar_Backdrop");
            var midAlcoves = parallaxRoot.transform.Find("ParallaxMid_Alcoves");
            var fgSilhouettes = parallaxRoot.transform.Find("Foreground_Silhouettes");
            Assert.IsNotNull(farBackdrop?.GetComponent<ParallaxLayer2D>(), "[A11/ParallaxFar]");
            Assert.IsNotNull(midAlcoves?.GetComponent<ParallaxLayer2D>(), "[A11/ParallaxMid]");
            Assert.IsNotNull(fgSilhouettes?.GetComponent<ParallaxLayer2D>(), "[A11/Foreground]");

            var farRenderers = farBackdrop.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.GreaterOrEqual(farRenderers.Length, 20, "[A11/ParallaxFarPanels] Expected >= 20 (5x4 grid) T_Prison_ParallaxFar_Grayscale panels.");
            foreach (var sr in farRenderers)
            {
                Assert.AreEqual("T_Prison_ParallaxFar_Grayscale", sr.sprite.name);
                Assert.AreEqual(Vector3.one, sr.transform.localScale);
                Assert.AreEqual("DeadCells/ParallaxGradientMap2D", sr.sharedMaterial.shader.name);
            }

            var midRenderers = midAlcoves.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.IsNotEmpty(midRenderers);
            Assert.IsFalse(midRenderers.Any(sr => sr.sprite != null && sr.sprite.name.StartsWith("alccoveParalaxeB_00", StringComparison.Ordinal)),
                "[A11/NoBlackAlcoveB] ParallaxMid_Alcoves must contain 0 alccoveParalaxeB_00 sprites.");

            var soFg = new SerializedObject(fgSilhouettes.GetComponent<ParallaxLayer2D>());
            Assert.IsTrue(soFg.FindProperty("_lockYToCameraStartOffset").boolValue,
                "[A11/FgVerticalLock] Foreground_Silhouettes must enable _lockYToCameraStartOffset.");

            int farLayerId = SortingLayer.NameToID("ParallaxFar");
            int midLayerId = SortingLayer.NameToID("ParallaxMid");
            int fgLayerId = SortingLayer.NameToID("Foreground");
            var pointLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
                .Where(l => l.lightType == Light2D.LightType.Point)
                .ToArray();
            int volumetricFlickerCount = 0;
            foreach (var light in pointLights)
            {
                var soLight = new SerializedObject(light);
                var layersProp = soLight.FindProperty("m_ApplyToSortingLayers");
                Assert.IsNotNull(layersProp);
                var applied = Enumerable.Range(0, layersProp.arraySize).Select(idx => layersProp.GetArrayElementAtIndex(idx).intValue).ToArray();
                CollectionAssert.DoesNotContain(applied, farLayerId, $"[A11/LightIsolation/{light.name}] Must exclude ParallaxFar.");
                CollectionAssert.DoesNotContain(applied, midLayerId, $"[A11/LightIsolation/{light.name}] Must exclude ParallaxMid.");
                CollectionAssert.DoesNotContain(applied, fgLayerId, $"[A11/LightIsolation/{light.name}] Must exclude Foreground.");

                if (light.enabled && light.volumetricEnabled && light.volumeIntensity > 0f && light.GetComponent<TorchFlicker2D>() != null)
                    volumetricFlickerCount++;
            }
            Assert.GreaterOrEqual(volumetricFlickerCount, 20,
                $"[A11/VolumetricFlicker] Expected >= 20 Point Light2Ds with volumetricEnabled + TorchFlicker2D, found {volumetricFlickerCount}.");

            var mistRoot = GameObject.Find("GroundMist_R5");
            Assert.IsNotNull(mistRoot, "[A11/GroundMistRoot]");
            var mistRenderers = mistRoot.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(9, mistRenderers.Length, "[A11/GroundMistCount] Expected 9 GroundMist_R5 strips.");
            string[] expectedMistNames =
            {
                "Mist_RoomAB", "Mist_RoomC", "Mist_RoomG", "Mist_RoomK", "Mist_RoomE",
                "Mist_RoomO", "Mist_RoomP", "Mist_RoomQ", "Mist_RoomT",
            };
            CollectionAssert.AreEquivalent(expectedMistNames, mistRenderers.Select(mr => mr.gameObject.name).ToArray(),
                "[A11/GroundMistNames] GroundMist_R5 strip names mismatch.");
            foreach (var mr in mistRenderers)
            {
                Assert.AreEqual("DeadCells/GroundMist2D", mr.sharedMaterial.shader.name);
                Assert.GreaterOrEqual(mr.color.a, 0.80f - 1e-4f,
                    $"[A11/GroundMistAlpha/{mr.name}] Ground mist strip vertex alpha ({mr.color.a:F2}) must be >= 0.80f.");
            }

            var castersRoot = GameObject.Find("ShadowCasters_R5");
            Assert.IsNotNull(castersRoot, "[A11/ShadowCastersRoot]");
            Assert.AreEqual(12, castersRoot.GetComponentsInChildren<ShadowCaster2D>(true).Length, "[A11/ShadowCastersCount] Expected 12 ShadowCasters_R5 casters.");
        }

        [Test]
        public void A12_Round5FinalReview_SubFloorDirtSkirtsBayClearanceAndCalibratedLighting()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. [Critical] Visual-only opaque dirt_00 sub-floor skirts seal parallax bleed across full 340u x 86u bounds without intruding into playable interiors
            var solidTilemaps = Object.FindObjectsByType<Tilemap>()
                .Where(t => t.name == "TM_SolidVisual")
                .ToArray();
            Vector3Int[] requiredSubFloorDirtCells =
            {
                new Vector3Int(10, -2, 0),
                new Vector3Int(10, -4, 0),
                new Vector3Int(49, -4, 0),
                new Vector3Int(66, -4, 0),
                new Vector3Int(66, -18, 0),
                new Vector3Int(120, -20, 0),
                new Vector3Int(200, -20, 0),
                new Vector3Int(-8, 13, 0),
                new Vector3Int(-24, 13, 0),
                new Vector3Int(-60, 10, 0),
                new Vector3Int(40, 18, 0),
                new Vector3Int(50, 20, 0),
                new Vector3Int(65, 18, 0),
                new Vector3Int(110, 35, 0),
            };
            foreach (var cell in requiredSubFloorDirtCells)
            {
                var sp = solidTilemaps.Select(tm => tm.GetSprite(cell)).FirstOrDefault(s => s != null);
                Assert.IsNotNull(sp, $"[A12/SubFloorSkirt] Missing visual-only dirt_00 tile at {cell} (exposes parallax beneath floor).");
                Assert.AreEqual("dirt_00", sp.name, $"[A12/SubFloorSkirt] Cell {cell} must use opaque dirt_00, got '{sp.name}'.");
            }

            Vector3Int[] forbiddenSolidInteriorCells =
            {
                new Vector3Int(80, -3, 0),
                new Vector3Int(104, -3, 0),
                new Vector3Int(49, 17, 0),
            };
            foreach (var cell in forbiddenSolidInteriorCells)
            {
                var tile = solidTilemaps.Select(tm => tm.GetTile(cell)).FirstOrDefault(t => t != null);
                Assert.IsNull(tile, $"[A12/NoInteriorDirt] Playable interior cell {cell} must have zero TM_SolidVisual tiles, got '{tile?.name}'.");
            }

            // 2. [Important] Carved arch bays (ArchSill/ArchJamb) must not overlap grounded architectural landmarks or vertical banners across all 9 bays
            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            var bayFrames = allRenderers
                .Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal) ||
                             sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal))
                .ToArray();
            Assert.IsNotEmpty(bayFrames);

            string[] landmarkNames =
            {
                "Art_B_Arch1", "Art_B_Pillar", "Art_B_Arch2",
                "Art_G_Alcove", "Art_J_Arch",
                "Art_E_Pillar1", "Art_E_Pillar2", "Art_F_ExitArch",
                "Art_A_Fountain",
                "Art_M_Dummy", "Art_M_ArmoryDome1", "Art_M_ArmoryCorps", "Art_M_WeaponDeco",
                "Art_N_Alcove1", "Art_N_Alcove2",
                "Art_O_MonsterDoor", "Art_O_CellDoor1", "Art_O_CellDoor2",
                "Art_P_CorruptedDoor", "Art_P_SewerExitDoor",
                "Art_Q_JudasSeat", "Art_Q_Billot", "Art_Q_SkeletonSpawn", "Art_Q_SnakeLandmark",
                "Art_R_ArmoryDome", "Art_R_BigSword",
                "Art_S_AlchemyBibli1", "Art_S_AlchemyBibli2", "Art_S_AspectsDesk",
                "Art_T_BigAlcove", "Art_T_Pillar1", "Art_T_Pillar2", "Art_T_CourtyardArch",
                "Art_C_Banner", "Art_J_Banner", "Art_E_Banner", "Art_F_Banner",
                "Art_N_Banner", "Art_P_Banner", "Art_R_Banner", "Art_T_Banner",
            };
            foreach (var lmName in landmarkNames)
            {
                var lmGo = GameObject.Find(lmName);
                Assert.IsNotNull(lmGo, $"[A12/LandmarkMissing/{lmName}]");
                var lmRenderers = lmGo.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var lmSr in lmRenderers)
                {
                    Bounds lmBounds = lmSr.bounds;
                    foreach (var frame in bayFrames)
                    {
                        bool overlapX = frame.bounds.max.x > lmBounds.min.x + 0.05f && frame.bounds.min.x < lmBounds.max.x - 0.05f;
                        bool overlapY = frame.bounds.max.y > lmBounds.min.y + 0.05f && frame.bounds.min.y < lmBounds.max.y - 0.05f;
                        Assert.IsFalse(overlapX && overlapY,
                            $"[A12/BayClash] '{frame.name}' ({frame.bounds.min}..{frame.bounds.max}) overlaps landmark '{lmName}/{lmSr.name}' ({lmBounds.min}..{lmBounds.max}).");
                    }
                }
            }

            // 3. [Important] Lighting calibration parity with Task 4 Main_Level (cool slate global tint + restrained volumetric/beacon intensity)
            var globalLight = Object.FindObjectsByType<Light2D>().Single(l => l.lightType == Light2D.LightType.Global);
            Assert.LessOrEqual(globalLight.color.r, 0.76f, "[A12/GlobalTint] Global Light 2D must use calibrated cool slate tint matching Main_Level.");

            var volumetricLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
                .Where(l => l.lightType == Light2D.LightType.Point && l.volumetricEnabled)
                .ToArray();
            Assert.GreaterOrEqual(volumetricLights.Length, 20);
            foreach (var light in volumetricLights)
            {
                Assert.LessOrEqual(light.volumeIntensity, 0.10f + 1e-4f,
                    $"[A12/Volumetric/{light.name}] volumeIntensity ({light.volumeIntensity:F2}) must be <= 0.10f to prevent bloom haze.");
                Assert.LessOrEqual(light.intensity, 1.50f + 1e-4f,
                    $"[A12/Intensity/{light.name}] intensity ({light.intensity:F2}) must be <= 1.50f to prevent highlight clipping.");
            }

            // 4. [P2-R5-2] Open doorway bottom corners must have null tile on TM_SolidRim; solid outer corner (0, 1, 0) keeps webSmallBL_00
            var rimTilemaps = Object.FindObjectsByType<Tilemap>()
                .Where(t => t.name == "TM_SolidRim")
                .ToArray();
            Assert.IsNotEmpty(rimTilemaps, "[A12/SolidRim] Missing TM_SolidRim tilemaps.");

            Vector3Int[] openDoorwayBottomCells =
            {
                new Vector3Int(17, 1, 0),
                new Vector3Int(18, 1, 0),
            };
            foreach (var cell in openDoorwayBottomCells)
            {
                var tile = rimTilemaps.Select(tm => tm.GetTile(cell)).FirstOrDefault(t => t != null);
                Assert.IsNull(tile, $"[A12/NoFloatingDoorwayWeb] Open A-B doorway cell {cell} on TM_SolidRim must be null, got '{tile?.name}'.");
            }

            var solidCornerCell = new Vector3Int(0, 1, 0);
            var solidCornerSprite = rimTilemaps.Select(tm => tm.GetSprite(solidCornerCell)).FirstOrDefault(s => s != null);
            Assert.IsNotNull(solidCornerSprite, $"[A12/SolidCornerWeb] Solid outer corner {solidCornerCell} on TM_SolidRim must keep cobweb tile.");
            Assert.AreEqual("webSmallBL_00", solidCornerSprite.name,
                $"[A12/SolidCornerWeb] Expected webSmallBL_00 at {solidCornerCell}, got '{solidCornerSprite.name}'.");
        }

        [Test]
        public void A13_FullScalePrisonersQuarters_ScrollsGoldTeleportersSwitchDoorsLoreAndDualExits()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            var player = Object.FindAnyObjectByType<PlayerController>();
            var hitStop = Object.FindAnyObjectByType<HitStop>();
            var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include);
            Assert.AreEqual(34, enemies.Length);
            Assert.AreEqual(5, enemies.Count(e => e.name.Contains("Grenadier")), "[A13/GrenadierCount] Expected 5 Enemy_Grenadier instances.");

            // 1. 3 Scrolls of Power + ScrollAura_R5_* under _icon + _glow pointing to Scroll glow + PowerScrollInteraction + ScrollUpgradeMenu + ScrollPickupPresentation
            var scrolls = Object.FindObjectsByType<PowerScrollPickup>(FindObjectsInactive.Include);
            Assert.AreEqual(3, scrolls.Length, "[A13/ScrollCount] Expected 3 PowerScrollPickup instances.");
            foreach (var scroll in scrolls)
            {
                var soScroll = new SerializedObject(scroll);
                var iconTr = soScroll.FindProperty("_icon").objectReferenceValue as Transform;
                var glowGo = soScroll.FindProperty("_glow").objectReferenceValue as GameObject;
                var promptGo = soScroll.FindProperty("_prompt").objectReferenceValue as GameObject;
                Assert.IsNotNull(iconTr, $"[A13/ScrollIcon/{scroll.name}]");
                Assert.IsNotNull(glowGo, $"[A13/ScrollGlow/{scroll.name}]");
                Assert.IsNotNull(glowGo.GetComponent<Light2D>(), $"[A13/ScrollGlowLight/{scroll.name}] _glow must remain wired to Scroll glow Light2D.");
                Assert.IsTrue(iconTr.GetComponentsInChildren<SpriteRenderer>(true).Any(sr => sr.name.StartsWith("ScrollAura_R5_", StringComparison.Ordinal)),
                    $"[A13/ScrollAura/{scroll.name}] ScrollAura_R5_* must be parented under _icon so Reveal(true) hides it.");
                Assert.IsNotNull(promptGo, $"[A13/ScrollPrompt/{scroll.name}]");
                var promptRenderers = promptGo.GetComponentsInChildren<Renderer>(true);
                Assert.IsNotEmpty(promptRenderers, $"[A13/ScrollPromptRenderer/{scroll.name}]");
                foreach (var pr in promptRenderers)
                    Assert.AreEqual("VFX", pr.sortingLayerName, $"[A13/ScrollPromptSorting/{scroll.name}/{pr.name}] Scroll prompt must sort on VFX above TM_Background.");
            }
            var scrollInteraction = Object.FindAnyObjectByType<PowerScrollInteraction>();
            Assert.IsNotNull(scrollInteraction, "[A13/PowerScrollInteraction]");
            Assert.AreEqual(scrollInteraction, new SerializedObject(player).FindProperty("_powerScrolls").objectReferenceValue, "[A13/Player.PowerScrolls]");
            var scrollMenu = Object.FindAnyObjectByType<DeadCells.HUD.ScrollUpgradeMenu>();
            var scrollPres = Object.FindAnyObjectByType<DeadCells.HUD.ScrollPickupPresentation>();
            Assert.IsNotNull(scrollMenu, "[A13/ScrollUpgradeMenu]");
            Assert.IsNotNull(scrollPres, "[A13/ScrollPickupPresentation]");
            var soMenu = new SerializedObject(scrollMenu);
            Assert.AreEqual(player, soMenu.FindProperty("_player").objectReferenceValue, "[A13/ScrollMenu.Player]");
            Assert.AreEqual(hitStop, soMenu.FindProperty("_gameplayTime").objectReferenceValue, "[A13/ScrollMenu.GameplayTime]");
            Assert.AreEqual(scrollPres, soMenu.FindProperty("_presentation").objectReferenceValue, "[A13/ScrollMenu.Presentation]");
            var soPres = new SerializedObject(scrollPres);
            Assert.IsNotNull(soPres.FindProperty("_playerSprite").objectReferenceValue, "[A13/ScrollPres.PlayerSprite]");
            Assert.IsNotNull(soPres.FindProperty("_gameplayHud").objectReferenceValue, "[A13/ScrollPres.GameplayHud]");
            Assert.IsNotNull(soPres.FindProperty("_audio").objectReferenceValue, "[A13/ScrollPres.Audio] Must point to non-null AudioSource on Scroll cinematic.");
            Assert.IsNotNull(soPres.FindProperty("_pose").objectReferenceValue, "[A13/ScrollPres.Pose]");

            // 1b. P0-04 Part B: 3rd High Bastion scroll lives in Room T at (114, 40) and is registered on PowerScrollInteraction
            var highBastionScroll = scrolls.Single(s => s.name == "I17_Scroll_HighBastion");
            Assert.AreEqual(114f, highBastionScroll.transform.position.x, GeometryAuditHelper.Eps, "[A13/I17ScrollX]");
            Assert.AreEqual(40f, highBastionScroll.transform.position.y, GeometryAuditHelper.Eps, "[A13/I17ScrollY]");
            var soScrollInteraction = new SerializedObject(scrollInteraction);
            var registeredScrolls = soScrollInteraction.FindProperty("_scrolls");
            Assert.AreEqual(3, registeredScrolls.arraySize, "[A13/ScrollRegistered] PowerScrollInteraction._scrolls must register all 3 scrolls.");
            Assert.AreEqual(highBastionScroll, registeredScrolls.GetArrayElementAtIndex(2).objectReferenceValue, "[A13/ScrollRegisteredI17]");

            // 2. GoldWallet + EnemyGoldDrops (all 34 enemies registered) + GoldHud + GoldPickup Vfx sorting
            var wallet = Object.FindAnyObjectByType<DeadCells.Currency.GoldWallet>();
            var goldDrops = Object.FindAnyObjectByType<DeadCells.Currency.EnemyGoldDrops>();
            var goldHud = Object.FindAnyObjectByType<DeadCells.HUD.GoldHud>();
            Assert.IsNotNull(wallet, "[A13/GoldWallet]");
            Assert.IsNotNull(goldDrops, "[A13/EnemyGoldDrops]");
            Assert.IsNotNull(goldHud, "[A13/GoldHud]");
            var soDrops = new SerializedObject(goldDrops);
            Assert.AreEqual(wallet, soDrops.FindProperty("_wallet").objectReferenceValue);
            Assert.AreEqual(34, soDrops.FindProperty("_enemies").arraySize, "[A13/GoldDropsEnemies] All 34 enemies must be registered.");
            var pickupPrefab = soDrops.FindProperty("_pickupPrefab").objectReferenceValue as DeadCells.Currency.GoldPickup;
            Assert.IsNotNull(pickupPrefab, "[A13/GoldPickupPrefab]");
            Assert.AreEqual("VFX", pickupPrefab.GetComponentInChildren<SpriteRenderer>(true).sortingLayerName, "[A13/GoldPickupSorting]");

            // 2b. P0-04 Part B: 4 statically placed Room N treasure coins (100 gold) resting on the North Gallery floor
            var goldCacheCoins = Object.FindObjectsByType<DeadCells.Currency.GoldPickup>(FindObjectsInactive.Include)
                .Where(c => c.name.StartsWith("GoldCache_N_", StringComparison.Ordinal))
                .ToArray();
            Assert.AreEqual(4, goldCacheCoins.Length, "[A13/GoldCacheCount] Expected 4 static Room N GoldPickup coins.");
            float[] expectedCacheXs = { 158f, 159f, 160f, 161f };
            for (int i = 0; i < expectedCacheXs.Length; i++)
            {
                var coin = goldCacheCoins.Single(c => c.name == $"GoldCache_N_{i}");
                var soCoin = new SerializedObject(coin);
                Assert.AreEqual(wallet, soCoin.FindProperty("_wallet").objectReferenceValue, $"[A13/GoldCacheWallet/{coin.name}]");
                Assert.AreEqual(player.GetComponent<Health>(), soCoin.FindProperty("_player").objectReferenceValue, $"[A13/GoldCachePlayer/{coin.name}]");
                Assert.AreEqual(25, soCoin.FindProperty("_value").intValue, $"[A13/GoldCacheValue/{coin.name}] 4 coins x 25 must total 100 gold.");
                Assert.IsTrue(soCoin.FindProperty("_staticPlacement").boolValue, $"[A13/GoldCacheStatic/{coin.name}] Static scene coins must keep _staticPlacement = true.");
                Assert.AreEqual("VFX", coin.GetComponentInChildren<SpriteRenderer>(true).sortingLayerName, $"[A13/GoldCacheSorting/{coin.name}]");
                Assert.AreEqual(expectedCacheXs[i], coin.transform.position.x, GeometryAuditHelper.Eps, $"[A13/GoldCacheX/{coin.name}]");
                Assert.AreEqual(8.45f, coin.transform.position.y, GeometryAuditHelper.Eps, $"[A13/GoldCacheY/{coin.name}]");
            }

            // 3. 3 SwitchDoors (SD_1_EastVault, SD_2_SewerExit, SD_3_WestArmory)
            var switchDoors = Object.FindObjectsByType<SwitchDoor>(FindObjectsInactive.Include);
            Assert.AreEqual(3, switchDoors.Length, "[A13/SwitchDoorCount]");
            foreach (var sd in switchDoors)
            {
                Assert.IsNotNull(sd.DoorBarrierRoot?.GetComponent<BoxCollider2D>(), $"[A13/SwitchDoorBarrier/{sd.name}]");
                Assert.IsNotNull(sd.SwitchTrigger, $"[A13/SwitchDoorTrigger/{sd.name}]");
                Assert.IsNotNull(sd.PromptIndicator, $"[A13/SwitchDoorPrompt/{sd.name}]");
            }

            // 4. 6 TeleportGates forming 3 bi-directional pairs with discovery gating
            var tpGates = Object.FindObjectsByType<TeleportGate>(FindObjectsInactive.Include);
            Assert.AreEqual(6, tpGates.Length, "[A13/TeleportGateCount]");
            foreach (var tp in tpGates)
            {
                Assert.IsNotNull(tp.PairedGate, $"[A13/TeleportPair/{tp.name}]");
                Assert.AreEqual(tp, tp.PairedGate.PairedGate, $"[A13/TeleportReciprocal/{tp.name}]");
            }
            var tp1 = tpGates.Single(t => t.name == "TP_1_EastVault");
            var tp3 = tpGates.Single(t => t.name == "TP_3_SewerExit");
            Assert.IsFalse(new SerializedObject(tp1).FindProperty("_startsDiscovered").boolValue, "[A13/TP1StartsUndiscovered]");
            Assert.IsFalse(new SerializedObject(tp3).FindProperty("_startsDiscovered").boolValue, "[A13/TP3StartsUndiscovered]");

            // 5. 2 LoreTablets (I16_LoreTablet in Room S + I19_LoreTablet_NorthGallery in Room N) & 2 SecondaryBiomeExitTriggers (I15 Sewers + I18 Promenade)
            var loreTablets = Object.FindObjectsByType<LoreTablet>(FindObjectsInactive.Include);
            Assert.AreEqual(2, loreTablets.Length, "[A13/LoreTabletCount] Expected 2 LoreTablet instances.");
            var lore = loreTablets.Single(t => t.name == "I16_LoreTablet");
            var northLore = loreTablets.Single(t => t.name == "I19_LoreTablet_NorthGallery");
            foreach (var tablet in loreTablets)
            {
                Assert.IsNotNull(tablet.PromptIndicator, $"[A13/LoreTabletPrompt/{tablet.name}]");
                Assert.GreaterOrEqual(tablet.PromptIndicator.GetComponent<RectTransform>().sizeDelta.x, 380f,
                    $"[A13/LoreTabletPromptWidth/{tablet.name}] Prompt badge must be wide enough for the label without vertical clipping.");
            }
            Assert.AreEqual(152.5f, northLore.transform.position.x, GeometryAuditHelper.Eps, "[A13/NorthLoreX]");
            Assert.AreEqual(8f, northLore.transform.position.y, GeometryAuditHelper.Eps, "[A13/NorthLoreY]");
            Assert.AreEqual("Nhật Ký Cai Ngục", northLore.LoreTitle, "[A13/NorthLoreTitle]");

            var biomeExits = Object.FindObjectsByType<SecondaryBiomeExitTrigger>(FindObjectsInactive.Include);
            Assert.AreEqual(2, biomeExits.Length, "[A13/BiomeExitCount] Expected 2 SecondaryBiomeExitTrigger instances.");
            var sewerExit = biomeExits.Single(e => e.name == "I15_SewerExitTrigger");
            Assert.AreEqual("Toxic Sewers", sewerExit.BiomeName);
            Assert.IsNotNull(sewerExit.SwitchDoor);
            Assert.IsNotNull(sewerExit.FirstGuardian);
            Assert.IsNotNull(sewerExit.SecondGuardian);

            var promenadeExit = biomeExits.Single(e => e.name == "I18_PromenadeExitTrigger");
            Assert.AreEqual("Promenade of the Condemned", promenadeExit.BiomeName, "[A13/PromenadeBiomeName]");
            Assert.IsNull(promenadeExit.SwitchDoor, "[A13/PromenadeNoSwitchDoor] Promenade exit unlocks on guardians alone.");
            Assert.IsNotNull(promenadeExit.FirstGuardian, "[A13/PromenadeFirstGuardian]");
            Assert.IsNotNull(promenadeExit.SecondGuardian, "[A13/PromenadeSecondGuardian]");
            CollectionAssert.AreEquivalent(
                new[] { "Enemy_Shield_T1", "Enemy_Grenadier_T1" },
                new[] { promenadeExit.FirstGuardian.name, promenadeExit.SecondGuardian.name },
                "[A13/PromenadeGuardians]");
            var promenadeBounds = GeometryAuditHelper.GetWorldBounds(promenadeExit.GetComponent<BoxCollider2D>());
            Assert.AreEqual(126f, promenadeBounds.center.x, GeometryAuditHelper.Eps, "[A13/PromenadeCenterX]");
            Assert.AreEqual(41.2f, promenadeBounds.center.y, GeometryAuditHelper.Eps, "[A13/PromenadeCenterY]");
            Assert.AreEqual(2.2f, promenadeBounds.size.x, GeometryAuditHelper.Eps, "[A13/PromenadeSizeX]");
            Assert.AreEqual(2.4f, promenadeBounds.size.y, GeometryAuditHelper.Eps, "[A13/PromenadeSizeY]");

            // 6. P0-04 Part B: new payoffs keep >= 5.0u breathing room from TP_5_HighBastion and relocated floor junk
            var tp5 = tpGates.Single(t => t.name == "TP_5_HighBastion");
            Assert.GreaterOrEqual(Vector2.Distance(highBastionScroll.transform.position, tp5.transform.position), 5.0f,
                "[A13/I17Clearance] I17_Scroll_HighBastion must sit >= 5.0u clear of TP_5_HighBastion.");
            Assert.GreaterOrEqual(Vector2.Distance(promenadeExit.transform.position, tp5.transform.position), 5.0f,
                "[A13/I18Clearance] I18_PromenadeExitTrigger must sit >= 5.0u clear of TP_5_HighBastion.");
            Assert.GreaterOrEqual(Vector2.Distance(promenadeExit.transform.position, highBastionScroll.transform.position), 5.0f,
                "[A13/I17I18Clearance] The Promenade payoff pair must not stack on one another.");
            Assert.AreEqual(102f, GameObject.Find("Art_T_Junk3_A").transform.position.x, 0.01f, "[A13/TJunk3Shift]");
            Assert.AreEqual(136.5f, GameObject.Find("Art_T_JunkEast3_A").transform.position.x, 0.01f, "[A13/TJunkEast3Shift]");
            Assert.AreEqual(145f, GameObject.Find("Art_N_JunkEast2_A").transform.position.x, 0.01f, "[A13/NJunkEast2Shift]");
            Assert.AreEqual(139f, GameObject.Find("Art_N_JunkEast3_A").transform.position.x, 0.01f, "[A13/NJunkEast3Shift]");

            // 7. P0-04 Part B: the Room N payoff column (alcove / lore tablet / torch / coins) never self-overlaps
            var northPayoffProps = new List<(string name, Bounds bounds)>
            {
                ("Art_N_Alcove2", GetVisualSpriteBounds(GameObject.Find("Art_N_Alcove2"))),
                ("I19_LoreTablet_NorthGallery", GeometryAuditHelper.GetWorldBounds(northLore.TriggerZone)),
                ("Art_N_Torch_4", GetVisualSpriteBounds(GameObject.Find("Art_N_Torch_4"))),
                ("Art_N_JunkEast2", GetVisualSpriteBounds(GameObject.Find("Art_N_JunkEast2_A"))),
                ("Art_N_JunkEast3", GetVisualSpriteBounds(GameObject.Find("Art_N_JunkEast3_A"))),
            };
            foreach (var coin in goldCacheCoins)
                northPayoffProps.Add((coin.name, GeometryAuditHelper.GetWorldBounds(coin.GetComponent<Collider2D>())));

            for (int a = 0; a < northPayoffProps.Count; a++)
            {
                for (int b = a + 1; b < northPayoffProps.Count; b++)
                {
                    Bounds ba = northPayoffProps[a].bounds;
                    Bounds bb = northPayoffProps[b].bounds;
                    bool overlapX = ba.min.x < bb.max.x - 0.05f && ba.max.x > bb.min.x + 0.05f;
                    bool overlapY = ba.min.y < bb.max.y - 0.05f && ba.max.y > bb.min.y + 0.05f;
                    Assert.IsFalse(overlapX && overlapY,
                        $"P0-04b: Room N payoff '{northPayoffProps[a].name}' ({ba.min.x:F2}..{ba.max.x:F2}, {ba.min.y:F2}..{ba.max.y:F2}) overlaps '{northPayoffProps[b].name}' ({bb.min.x:F2}..{bb.max.x:F2}, {bb.min.y:F2}..{bb.max.y:F2}).");
                }
            }
        }

        [Test]
        public void A14_Round7Phase1_SpawnBedrockSkirtChromakeyFreeSpritesAndZeroPropPlatformTorchClashes()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. P0-01: Outer bedrock skirt around Spawn / Room AB so orthoSize=7 camera at (3, 1.5) sees zero void
            var solidTilemaps = Object.FindObjectsByType<Tilemap>()
                .Where(t => t.name == "TM_SolidVisual")
                .ToArray();
            Assert.AreEqual(9, solidTilemaps.Length, "[A14/SolidTilemaps] Expected 9 TM_SolidVisual tilemaps.");

            for (int x = -15; x <= -2; x++)
            {
                for (int y = -6; y <= 12; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    Assert.IsTrue(solidTilemaps.Any(tm => tm.GetTile(cell) != null),
                        $"P0-01: Outer west bedrock cell ({x},{y}) must be filled on TM_SolidVisual.");
                }
            }

            for (int x = 0; x <= 5; x++)
            {
                for (int y = 6; y <= 12; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    Assert.IsTrue(solidTilemaps.Any(tm => tm.GetTile(cell) != null),
                        $"P0-01: Outer upper bedrock cell ({x},{y}) above Room A must be filled on TM_SolidVisual.");
                }
            }

            for (int x = 6; x <= 17; x++)
            {
                for (int y = 6; y <= 7; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    Assert.IsTrue(solidTilemaps.Any(tm => tm.GetTile(cell) != null),
                        $"P0-01: Bedrock transition cell ({x},{y}) between Ceiling_A and Stairs_H must be filled on TM_SolidVisual.");
                }
            }

            Vector3Int[] roomHStairAirCells =
            {
                new Vector3Int(15, 11, 0),
                new Vector3Int(12, 14, 0),
            };
            foreach (var cell in roomHStairAirCells)
            {
                foreach (var tm in solidTilemaps)
                {
                    Assert.IsNull(tm.GetTile(cell),
                        $"P0-01: Room H walkable stair air cell {cell} must remain null on '{tm.transform.root.name}/TM_SolidVisual'.");
                }
            }

            // 2. P0-02: Art_P_ExitGlow renders in front of Art_P_SewerExitDoor
            var pDoor = GameObject.Find("Art_P_SewerExitDoor");
            var pGlow = GameObject.Find("Art_P_ExitGlow");
            Assert.IsNotNull(pDoor, "Missing Art_P_SewerExitDoor.");
            Assert.IsNotNull(pGlow, "Missing Art_P_ExitGlow.");
            var pDoorSr = pDoor.GetComponent<SpriteRenderer>();
            var pGlowSr = pGlow.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(pDoorSr, "Missing SpriteRenderer on Art_P_SewerExitDoor.");
            Assert.IsNotNull(pGlowSr, "Missing SpriteRenderer on Art_P_ExitGlow.");
            Assert.AreEqual(pDoorSr.sortingLayerName, pGlowSr.sortingLayerName,
                "P0-02: Art_P_ExitGlow and Art_P_SewerExitDoor must share sorting layer 'Ground'.");
            Assert.Greater(pGlowSr.sortingOrder, pDoorSr.sortingOrder,
                "P0-02: Art_P_ExitGlow sortingOrder must be strictly greater than Art_P_SewerExitDoor so glow is not occluded.");

            // 3. P0-03: Zero shrunk toy-scale (scale < 0.70) armory_dome_00 or armory_corps_00 in the scene
            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.sprite == null)
                    continue;
                string sName = sr.sprite.name;
                if (sName.StartsWith("armory_dome_00", StringComparison.Ordinal) ||
                    sName.StartsWith("armory_corps_00", StringComparison.Ordinal))
                {
                    Assert.GreaterOrEqual(sr.transform.localScale.x, 0.70f,
                        $"P0-03: '{sr.gameObject.name}' uses '{sName}' at toy scale {sr.transform.localScale.x:F2} (< 0.70).");
                }
            }

            // 4. P0-04a: Zero AABB overlaps between any of the 25 OneWay platforms and architectural landmarks/doors/shelves/banners
            var oneWayBoxes = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include)
                .Where(box => box.gameObject.layer == GeometryAuditHelper.LayerOneWay &&
                              box.gameObject.name.StartsWith("OneWay_", StringComparison.Ordinal))
                .ToList();
            Assert.AreEqual(25, oneWayBoxes.Count, "Expected exactly 25 OneWay platforms.");

            string[] platformForbiddenProps =
            {
                "Art_G_Alcove",
                "Art_M_Dummy", "Art_M_ArmoryDome1", "Art_M_ArmoryCorps", "Art_M_WeaponDeco",
                "Art_N_Banner",
                "Art_O_MonsterDoor", "Art_O_CellDoor1", "Art_O_CellDoor2",
                "Art_P_CorruptedDoor", "Art_P_SewerExitDoor", "Art_P_Banner",
                "Art_Q_JudasSeat", "Art_Q_Billot", "Art_Q_SkeletonSpawn", "Art_Q_SnakeLandmark",
                "Art_R_ArmoryDome", "Art_R_BigSword", "Art_R_Banner",
                "Art_S_AlchemyBibli1", "Art_S_AlchemyBibli2", "Art_S_AspectsDesk", "Art_S_TableauNoir",
                "Art_T_BigAlcove", "Art_T_Pillar1", "Art_T_Pillar2", "Art_T_CourtyardArch", "Art_T_Banner",
            };

            foreach (string propName in platformForbiddenProps)
            {
                var propGo = GameObject.Find(propName);
                Assert.IsNotNull(propGo, $"Missing expected landmark '{propName}'.");
                var propRenderers = propGo.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.IsNotEmpty(propRenderers, $"Missing SpriteRenderer(s) on '{propName}'.");

                foreach (var propSr in propRenderers)
                {
                    Bounds pb = propSr.bounds;
                    foreach (var ow in oneWayBoxes)
                    {
                        Bounds ob = GeometryAuditHelper.GetWorldBounds(ow);
                        bool overlapX = pb.min.x < ob.max.x - 0.05f && pb.max.x > ob.min.x + 0.05f;
                        bool overlapY = pb.min.y < ob.max.y + 0.10f && pb.max.y > ob.min.y - 0.35f;
                        Assert.IsFalse(overlapX && overlapY,
                            $"P0-04a: Prop '{propName}/{propSr.name}' bounds ({pb.min.x:F2}..{pb.max.x:F2}, {pb.min.y:F2}..{pb.max.y:F2}) collide with OneWay platform '{ow.name}' ({ob.min.x:F2}..{ob.max.x:F2}, {ob.min.y:F2}..{ob.max.y:F2}).");
                    }
                }
            }

            // 5. P0-04b: Zero AABB overlaps between any LanternFixture_R5 and windows, climbing chains, exit doors, pillars, or landmarks
            var lanterns = new List<SpriteRenderer>();
            var lanternBlockers = new List<SpriteRenderer>();
            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.sprite == null)
                    continue;
                string n = sr.gameObject.name;
                string parentName = sr.transform.parent != null ? sr.transform.parent.name : string.Empty;
                if (n == "LanternFixture_R5")
                {
                    lanterns.Add(sr);
                }
                else if (n.Contains("Window") ||
                         (parentName.StartsWith("Art_I", StringComparison.Ordinal) && parentName.EndsWith("_Chain", StringComparison.Ordinal)) ||
                         n == "Art_F_ExitArch" || n == "Art_P_SewerExitDoor" || n == "Art_T_CourtyardArch" ||
                         n == "Art_E_Pillar1" || n == "Art_E_Pillar2" ||
                         n == "Art_R_BigSword" || n == "Art_Q_SkeletonSpawn" ||
                         n == "Art_S_AlchemyBibli1" || n == "Art_S_AlchemyBibli2" ||
                         parentName == "Art_T_Banner")
                {
                    lanternBlockers.Add(sr);
                }
            }

            Assert.GreaterOrEqual(lanterns.Count, 80, "[A14/LanternCount] Expected >= 80 LanternFixture_R5 fixtures.");
            foreach (var lantern in lanterns)
            {
                Bounds lb = lantern.bounds;
                string lanternLabel = lantern.transform.parent != null ? lantern.transform.parent.name : lantern.name;
                foreach (var blocker in lanternBlockers)
                {
                    Bounds bb = blocker.bounds;
                    bool overlapX = lb.min.x < bb.max.x - 0.04f && lb.max.x > bb.min.x + 0.04f;
                    bool overlapY = lb.min.y < bb.max.y - 0.04f && lb.max.y > bb.min.y + 0.04f;
                    Assert.IsFalse(overlapX && overlapY,
                        $"P0-04b: Torch '{lanternLabel}' at ({lantern.transform.position.x:F2},{lantern.transform.position.y:F2}) overlaps '{blocker.name}' ({bb.min.x:F2}..{bb.max.x:F2}, {bb.min.y:F2}..{bb.max.y:F2}).");
                }
            }

            // 6. P0-04c: Zero overlap between PowerScroll pedestals, weapon props, mushroom_4, and disjoint Room A props
            string[][] disjointPairs =
            {
                new[] { "I11_Scroll_EastCourtyard", "Art_M_WeaponDeco" },
                new[] { "I12_Scroll_WestArmory", "Art_R_BigSword" },
                new[] { "GlowingMushroom_4", "Art_M_ArmoryDome1" },
                new[] { "Art_A_Bed", "Art_A_Fountain" },
                new[] { "Art_A_Fountain", "Art_A_Window" },
                new[] { "Art_A_Window", "TP_0_Spawn" },
            };

            foreach (var pair in disjointPairs)
            {
                var goA = GameObject.Find(pair[0]);
                var goB = GameObject.Find(pair[1]);
                Assert.IsNotNull(goA, $"Missing '{pair[0]}'.");
                Assert.IsNotNull(goB, $"Missing '{pair[1]}'.");
                Bounds bA = GetVisualSpriteBounds(goA);
                Bounds bB = GetVisualSpriteBounds(goB);
                bool overlapX = bA.min.x < bB.max.x - 0.05f && bA.max.x > bB.min.x + 0.05f;
                bool overlapY = bA.min.y < bB.max.y - 0.05f && bA.max.y > bB.min.y + 0.05f;
                Assert.IsFalse(overlapX && overlapY,
                    $"P0-04c: '{pair[0]}' ({bA.min.x:F2}..{bA.max.x:F2}, {bA.min.y:F2}..{bA.max.y:F2}) overlaps '{pair[1]}' ({bB.min.x:F2}..{bB.max.x:F2}, {bB.min.y:F2}..{bB.max.y:F2}).");
            }
        }

        [Test]
        public void A15_Round7Phase2_SixteenArchBaysWingWindowsStorytellingClustersAndChiaroscuroLighting()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

            // 1. P1-01: All 16 carved arch bays (Bay #0..#15) exist and their interior cells are carved (null) on TM_Background
            var bgTilemaps = Object.FindObjectsByType<Tilemap>()
                .Where(t => t.name == "TM_Background")
                .ToArray();
            Assert.AreEqual(9, bgTilemaps.Length, "[A15/BgTilemaps] Expected 9 TM_Background tilemaps.");

            for (int bayIndex = 0; bayIndex <= 15; bayIndex++)
            {
                var tl = GameObject.Find($"ArchSpandrel_TL_{bayIndex}");
                var tr = GameObject.Find($"ArchSpandrel_TR_{bayIndex}");
                Assert.IsNotNull(tl, $"P1-01/Task3.1: Missing curved stone arch spandrel 'ArchSpandrel_TL_{bayIndex}'.");
                Assert.IsNotNull(tr, $"P1-01/Task3.1: Missing curved stone arch spandrel 'ArchSpandrel_TR_{bayIndex}'.");
                var tlSr = tl.GetComponent<SpriteRenderer>();
                var trSr = tr.GetComponent<SpriteRenderer>();
                Assert.AreEqual("prisonArchSpandrel_00", tlSr?.sprite?.name, $"[A15/SpandrelTL/{bayIndex}]");
                Assert.AreEqual("prisonArchSpandrel_00", trSr?.sprite?.name, $"[A15/SpandrelTR/{bayIndex}]");
                Assert.IsTrue(trSr.flipX, $"[A15/SpandrelTRFlipX/{bayIndex}] ArchSpandrel_TR_{bayIndex} must flipX.");
                Assert.IsNull(GameObject.Find($"ArchBeam_TL_{bayIndex}"),
                    $"Task3.1: Legacy black ear 'ArchBeam_TL_{bayIndex}' must be removed.");
                Assert.IsNull(GameObject.Find($"ArchBeam_TR_{bayIndex}"),
                    $"Task3.1: Legacy black ear 'ArchBeam_TR_{bayIndex}' must be removed.");
            }
            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.sprite == null)
                    continue;
                if (sr.gameObject.name.StartsWith("Arch", StringComparison.Ordinal))
                {
                    Assert.AreNotEqual("littleBlackBeam_topLeft_0_00", sr.sprite.name,
                        $"[A15/NoLittleBeam/{sr.name}] Carved bay must not use LittleBeamTL.");
                    Assert.AreNotEqual("littleBlackBeam_topRight_0_00", sr.sprite.name,
                        $"[A15/NoLittleBeam/{sr.name}] Carved bay must not use LittleBeamTR.");
                }
            }

            Vector3Int[] carvedWingBayInteriorSamples =
            {
                new Vector3Int(99, 3, 0),    // Bay #9  (Room M)
                new Vector3Int(141, 19, 0),  // Bay #10 (Room N)
                new Vector3Int(144, -6, 0),  // Bay #11 (Room O)
                new Vector3Int(191, -6, 0),  // Bay #12 (Room P)
                new Vector3Int(-72, 19, 0),  // Bay #13 (Room Q)
                new Vector3Int(51, 35, 0),   // Bay #14 (Room S clerestory [47..55, 34..38])
                new Vector3Int(100, 50, 0),  // Bay #15 (Room T)
            };
            foreach (var cell in carvedWingBayInteriorSamples)
            {
                Assert.IsTrue(bgTilemaps.All(tm => tm.GetTile(cell) == null),
                    $"P1-01: Wing arch bay interior cell {cell} must be carved (null) on all TM_Background tilemaps.");
            }

            // 2. P1-02 & P1-03: Every expansion wing room (M, N, O, P, Q, R, S, T) has windows with additive GodRays, shelves, and storytelling props
            string[] requiredWingWindows =
            {
                "Art_M_Window1", "Art_M_Window2", "Art_M_Window3",
                "Art_N_Window1", "Art_N_Window2", "Art_N_Window3",
                "Art_O_Window1", "Art_O_Window2",
                "Art_P_Window1", "Art_P_Window2",
                "Art_Q_Window1", "Art_Q_Window2",
                "Art_R_Window1", "Art_R_Window2",
                "Art_S_Window1", "Art_S_Window2",
                "Art_T_Window1", "Art_T_Window2", "Art_T_Window3", "Art_T_Window4",
            };
            foreach (string winName in requiredWingWindows)
            {
                var winGo = GameObject.Find(winName);
                Assert.IsNotNull(winGo, $"P1-02: Missing wing window '{winName}'.");
                var rayTr = winGo.transform.Find("GodRay");
                Assert.IsNotNull(rayTr, $"P1-02: Missing child 'GodRay' under '{winName}'.");
                var raySr = rayTr.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(raySr?.sharedMaterial, $"P1-02: Missing GodRay material under '{winName}'.");
                Assert.AreEqual("DeadCells/AdditiveGodRay2D", raySr.sharedMaterial.shader.name,
                    $"P1-02: '{winName}/GodRay' must use DeadCells/AdditiveGodRay2D.");
            }

            string[] requiredWingStorytellingProps =
            {
                "Art_M_Bed1", "Art_M_Bed2", "Art_M_Shelf1", "Art_M_Junk1_A", "Art_M_Junk2_A",
                "Art_N_Pillar1", "Art_N_Pillar2", "Art_N_BannerBig1", "Art_N_Shelf1", "Art_N_Junk1_A", "Art_N_Junk2_A",
                "Art_O_Pillar1", "Art_O_Cage1", "Art_O_Cage2", "Art_O_Bed1", "Art_O_Shelf1", "Art_O_Junk1_A", "Art_O_Junk2_A",
                "Art_P_Arch1", "Art_P_Cage1", "Art_P_Cage2", "Art_P_Shelf1", "Art_P_Junk1_A", "Art_P_Junk2_A",
                "Art_Q_Shelf1", "Art_Q_Bed1", "Art_Q_Junk1_A", "Art_Q_Junk2_A",
                "Art_R_BannerBig1", "Art_R_Pillar1", "Art_R_Shelf1", "Art_R_Junk1_A", "Art_R_Junk2_A",
                "Art_S_BannerBig1", "Art_S_Shelf1", "Art_S_Shelf2", "Art_S_Cage1", "Art_S_Junk1_A", "Art_S_Junk2_A",
                "Art_T_BannerBig1", "Art_T_BannerBig2", "Art_T_Arch1", "Art_T_Cage1", "Art_T_Cage2", "Art_T_Cage3", "Art_T_Shelf1", "Art_T_Shelf2", "Art_T_Junk1_A", "Art_T_Junk2_A", "Art_T_Junk3_A",
                "Art_O_ShaftStep1BraceL", "Art_O_ShaftStep2BraceL", "Art_T_Mid1BraceL", "Art_T_Mid2BraceL",
            };
            foreach (string propName in requiredWingStorytellingProps)
            {
                Assert.IsNotNull(GameObject.Find(propName), $"P1-03/P1-05b: Missing wing storytelling/brace prop '{propName}'.");
            }

            // 3. P1-03b: Zero AABB overlaps between any new wing structural prop and any carved arch bay frame (ArchSill_* / ArchJamb_*) or OneWay_* platform
            var bayFrames = allRenderers
                .Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal) ||
                             sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal))
                .ToArray();
            var oneWayBoxes = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include)
                .Where(box => box.gameObject.layer == GeometryAuditHelper.LayerOneWay &&
                              box.gameObject.name.StartsWith("OneWay_", StringComparison.Ordinal))
                .ToArray();

            string[] phase2StructuralProps =
            {
                "Art_M_Bed1", "Art_M_Bed2", "Art_M_Shelf1",
                "Art_N_Pillar1", "Art_N_Pillar2", "Art_N_BannerBig1", "Art_N_Shelf1",
                "Art_O_Pillar1", "Art_O_Bed1", "Art_O_Shelf1",
                "Art_P_Arch1", "Art_P_Shelf1",
                "Art_Q_Shelf1", "Art_Q_Bed1",
                "Art_R_BannerBig1", "Art_R_Pillar1", "Art_R_Shelf1",
                "Art_S_BannerBig1", "Art_S_Shelf1", "Art_S_Shelf2",
                "Art_T_BannerBig1", "Art_T_BannerBig2", "Art_T_Arch1", "Art_T_Shelf1", "Art_T_Shelf2",
            };
            foreach (string propName in phase2StructuralProps)
            {
                var propGo = GameObject.Find(propName);
                Assert.IsNotNull(propGo, $"Missing '{propName}'.");
                Bounds pb = GetVisualSpriteBounds(propGo);

                foreach (var frame in bayFrames)
                {
                    bool overlapX = frame.bounds.max.x > pb.min.x + 0.05f && frame.bounds.min.x < pb.max.x - 0.05f;
                    bool overlapY = frame.bounds.max.y > pb.min.y + 0.05f && frame.bounds.min.y < pb.max.y - 0.05f;
                    Assert.IsFalse(overlapX && overlapY,
                        $"P1-01/P1-03: Arch frame '{frame.name}' overlaps new wing prop '{propName}' ({pb.min}..{pb.max}).");
                }

                foreach (var ow in oneWayBoxes)
                {
                    Bounds ob = GeometryAuditHelper.GetWorldBounds(ow);
                    bool overlapX = pb.min.x < ob.max.x - 0.05f && pb.max.x > ob.min.x + 0.05f;
                    bool overlapY = pb.min.y < ob.max.y + 0.10f && pb.max.y > ob.min.y - 0.35f;
                    Assert.IsFalse(overlapX && overlapY,
                        $"P1-03: Wing prop '{propName}' ({pb.min.x:F2}..{pb.max.x:F2}, {pb.min.y:F2}..{pb.max.y:F2}) overlaps OneWay '{ow.name}' ({ob.min.x:F2}..{ob.max.x:F2}, {ob.min.y:F2}..{ob.max.y:F2}).");
                }
            }

            // 4. P1-04: Chiaroscuro lighting calibration (Global Light 2D <= 0.48f cool slate, Point Light intensity modulation spread >= 0.40f, falloff >= 0.58f)
            var globalLight = Object.FindObjectsByType<Light2D>().Single(l => l.lightType == Light2D.LightType.Global);
            Assert.That(globalLight.intensity, Is.InRange(0.35f, 0.48f),
                $"P1-04: Global Light 2D intensity ({globalLight.intensity:F2}) must be in [0.35, 0.48] for Dead Cells chiaroscuro contrast.");
            Assert.LessOrEqual(globalLight.color.r, 0.62f,
                $"P1-04: Global Light 2D red channel ({globalLight.color.r:F2}) must be <= 0.62 for cool indigo-slate shadows.");

            var lanternLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
                .Where(l => l.lightType == Light2D.LightType.Point &&
                            (l.gameObject.name == "FlameLight" || l.gameObject.name == "LanternLight"))
                .ToArray();
            Assert.GreaterOrEqual(lanternLights.Length, 80, "[A15/LanternLights] Expected >= 80 wall/ceiling lantern point lights.");

            float minInt = lanternLights.Min(l => l.intensity);
            float maxInt = lanternLights.Max(l => l.intensity);
            Assert.GreaterOrEqual(maxInt - minInt, 0.40f,
                $"P1-04: Lantern point light intensities must be modulated (spread={maxInt - minInt:F2}, min={minInt:F2}, max={maxInt:F2}), not clamped to uniform 1.40.");
            foreach (var l in lanternLights)
            {
                Assert.GreaterOrEqual(l.falloffIntensity, 0.58f - 1e-4f,
                    $"P1-04: Lantern light '{l.transform.parent?.name}' falloffIntensity ({l.falloffIntensity:F2}) must be >= 0.58 for soft chiaroscuro roll-off.");
            }
        }

        [Test]
        public void A16_Round7Phase3_ChainCapMicroFixesWeatheredMasonryCeilingRhythmDepthPropsAndBedrockStrata()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

            // 1. P2-01a: All 7 climbing chains use unified dark-iron chainTile_00 CapBottom strictly above floor; zero lavender chainDown_00 anywhere
            foreach (var sr in allRenderers)
            {
                if (sr != null && sr.sprite != null)
                {
                    Assert.AreNotEqual("chainDown_00", sr.sprite.name,
                        $"P2-01a: '{sr.gameObject.name}' must not use mismatched lavender chainDown_00.");
                }
            }

            (string chainName, float floorY)[] expectedChains =
            {
                ("Art_I1_Chain", 0f),
                ("Art_I2_Chain", 0f),
                ("Art_I6_Chain", 16f),
                ("Art_I7_Chain", 0f),
                ("Art_I8_Chain", -16f),
                ("Art_I9_Chain", 16f),
                ("Art_I10_Chain", 24f),
            };
            foreach (var (chainName, floorY) in expectedChains)
            {
                var chainGo = GameObject.Find(chainName);
                Assert.IsNotNull(chainGo, $"P2-01a: Missing climbing chain '{chainName}'.");
                var capSr = chainGo.transform.Find("CapBottom")?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(capSr?.sprite, $"P2-01a: Missing CapBottom SpriteRenderer under '{chainName}'.");
                Assert.AreEqual("chainTile_00", capSr.sprite.name,
                    $"P2-01a: '{chainName}/CapBottom' must use dark-iron chainTile_00, got '{capSr.sprite.name}'.");
                Assert.Greater(capSr.bounds.min.y, floorY + 0.02f,
                    $"P2-01a: '{chainName}/CapBottom' bottom ({capSr.bounds.min.y:F2}) must remain strictly above floorY={floorY:F2}.");
            }

            // 2. P2-01b & P2-01c: Art_G_TorchUpperWest clears Art_G_Alcove; Art_J_Bed clears carved Bay #3 (all ArchSill_*/ArchJamb_*)
            var gTorchFixture = GameObject.Find("Art_G_TorchUpperWest")?.transform.Find("LanternFixture_R5")?.GetComponent<SpriteRenderer>();
            var gAlcove = GameObject.Find("Art_G_Alcove")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(gTorchFixture, "P2-01b: Missing Art_G_TorchUpperWest/LanternFixture_R5.");
            Assert.IsNotNull(gAlcove, "P2-01b: Missing Art_G_Alcove.");
            bool gOverlapX = gTorchFixture.bounds.min.x < gAlcove.bounds.max.x - 0.02f && gTorchFixture.bounds.max.x > gAlcove.bounds.min.x + 0.02f;
            bool gOverlapY = gTorchFixture.bounds.min.y < gAlcove.bounds.max.y - 0.02f && gTorchFixture.bounds.max.y > gAlcove.bounds.min.y + 0.02f;
            Assert.IsFalse(gOverlapX && gOverlapY,
                $"P2-01b: Art_G_TorchUpperWest ({gTorchFixture.bounds.min}..{gTorchFixture.bounds.max}) must not overlap Art_G_Alcove ({gAlcove.bounds.min}..{gAlcove.bounds.max}).");

            var bayFrames = allRenderers
                .Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal) ||
                             sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal))
                .ToArray();
            var jBed = GameObject.Find("Art_J_Bed")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(jBed, "P2-01c: Missing Art_J_Bed.");
            foreach (var frame in bayFrames)
            {
                bool ox = frame.bounds.max.x > jBed.bounds.min.x + 0.05f && frame.bounds.min.x < jBed.bounds.max.x - 0.05f;
                bool oy = frame.bounds.max.y > jBed.bounds.min.y + 0.05f && frame.bounds.min.y < jBed.bounds.max.y - 0.05f;
                Assert.IsFalse(ox && oy,
                    $"P2-01c: Art_J_Bed ({jBed.bounds.min}..{jBed.bounds.max}) must not overlap carved bay frame '{frame.name}'.");
            }

            // 3. P2-02: Weathered masonry wall patches (Background / -19, >= 120 across all 19 rooms, zero overlap with any carved bay)
            var wallPatches = allRenderers
                .Where(sr => sr.gameObject.name.Contains("_WallPatch_"))
                .ToArray();
            Assert.GreaterOrEqual(wallPatches.Length, 120,
                $"P2-02: Expected >= 120 weathered stone wall patches across 19 rooms, found {wallPatches.Length}.");

            string[] allRoomTags = { "A", "B", "C", "D", "G", "H", "J", "K", "E", "L", "F", "M", "N", "O", "P", "Q", "R", "S", "T" };
            foreach (string tag in allRoomTags)
            {
                Assert.IsTrue(wallPatches.Any(sr => sr.gameObject.name.StartsWith($"Art_{tag}_WallPatch_", StringComparison.Ordinal)),
                    $"P2-02: Room {tag} must contain at least 1 weathered stone wall patch.");
            }

            foreach (var wp in wallPatches)
            {
                Assert.AreEqual("Background", wp.sortingLayerName, $"P2-02: '{wp.name}' must sort on Background.");
                Assert.AreEqual(-19, wp.sortingOrder, $"P2-02: '{wp.name}' must sort at order -19 (above TM_Background -20, behind TM_WallDecals -18).");
                Assert.IsTrue(wp.sprite != null &&
                              (wp.sprite.name.StartsWith("stampWallBig_", StringComparison.Ordinal) ||
                               wp.sprite.name.StartsWith("stampWallMedium_", StringComparison.Ordinal)),
                    $"P2-02: '{wp.name}' must use stampWallBig_* or stampWallMedium_*, got '{wp.sprite?.name}'.");

                Bounds b = wp.bounds;
                foreach (var bay in CarvedBays)
                {
                    bool ox = b.min.x < bay.xMax - 0.05f && b.max.x > bay.xMin + 0.05f;
                    bool oy = b.min.y < bay.yMax - 0.05f && b.max.y > bay.yMin + 0.05f;
                    Assert.IsFalse(ox && oy,
                        $"P2-02: WallPatch '{wp.name}' ({b.min}..{b.max}) bleeds into carved Bay #{bay.bayIdx} ([{bay.xMin}..{bay.xMax}], [{bay.yMin}..{bay.yMax}]).");
                }
            }

            // 4. P2-03: All 9 long rooms (> 28u wide) have multi-span ceiling stamps & roof moss on Background / -11, never floating over open shafts, and zero ceiling accents overlap lanterns
            var lanternFixtures = allRenderers.Where(sr => sr.gameObject.name == "LanternFixture_R5").ToArray();

            (string tag, float shaftMinX, float shaftMaxX)[] longRoomShafts =
            {
                ("E", float.NaN, float.NaN),
                ("M", 101f, 107f),
                ("N", float.NaN, float.NaN),
                ("O", 115f, 121f),
                ("P", float.NaN, float.NaN),
                ("Q", -69f, -63f),
                ("R", float.NaN, float.NaN),
                ("S", 77f, 83f),
                ("T", float.NaN, float.NaN),
            };
            foreach (var (tag, sMin, sMax) in longRoomShafts)
            {
                var extraStamps = allRenderers
                    .Where(sr => sr.gameObject.name.StartsWith($"Art_{tag}_CeilStamp_", StringComparison.Ordinal))
                    .ToArray();
                var extraMoss = allRenderers
                    .Where(sr => sr.gameObject.name.StartsWith($"Art_{tag}_RoofMoss_", StringComparison.Ordinal))
                    .ToArray();
                Assert.IsNotEmpty(extraStamps, $"P2-03: Long room {tag} must have >= 1 multi-span Art_{tag}_CeilStamp_*.");
                Assert.IsNotEmpty(extraMoss, $"P2-03: Long room {tag} must have >= 1 multi-span Art_{tag}_RoofMoss_*.");

                foreach (var sr in extraStamps.Concat(extraMoss))
                {
                    Assert.IsNotNull(sr.sprite, $"P2-03: '{sr.name}' is missing its sprite.");
                    Assert.AreEqual("Background", sr.sortingLayerName, $"P2-03: '{sr.name}' must sort on Background.");
                    Assert.AreEqual(-11, sr.sortingOrder, $"P2-03: '{sr.name}' must sort at -11 (behind LanternFixture_R5 -9 & Cages Ground/-7).");
                    if (!float.IsNaN(sMin))
                    {
                        bool overShaft = sr.bounds.max.x > sMin + 0.05f && sr.bounds.min.x < sMax - 0.05f;
                        Assert.IsFalse(overShaft,
                            $"P2-03: Ceiling accent '{sr.name}' ({sr.bounds.min.x:F2}..{sr.bounds.max.x:F2}) floats over open shaft [{sMin}..{sMax}].");
                    }
                }
            }

            var allCeilingAccents = allRenderers
                .Where(sr => sr.gameObject.name.Contains("_CeilStamp") || sr.gameObject.name.Contains("_RoofMoss"))
                .ToArray();
            Assert.IsNotEmpty(allCeilingAccents, "P2-03: Expected ceiling stamp / roof moss accents across rooms.");
            foreach (var sr in allCeilingAccents)
            {
                Assert.IsNotNull(sr.sprite, $"P2-03: '{sr.name}' is missing its sprite.");
                Assert.AreEqual("Background", sr.sortingLayerName, $"P2-03: '{sr.name}' must sort on Background.");
                Assert.AreEqual(-11, sr.sortingOrder, $"P2-03: '{sr.name}' must sort at -11 (behind LanternFixture_R5 -9 & Cages Ground/-7).");

                Bounds cb = sr.bounds;
                foreach (var lf in lanternFixtures)
                {
                    bool ox = cb.min.x < lf.bounds.max.x - 0.04f && cb.max.x > lf.bounds.min.x + 0.04f;
                    bool oy = cb.min.y < lf.bounds.max.y - 0.04f && cb.max.y > lf.bounds.min.y + 0.04f;
                    Assert.IsFalse(ox && oy,
                        $"P2-03: Ceiling accent '{sr.name}' ({cb.min.x:F2}..{cb.max.x:F2}, {cb.min.y:F2}..{cb.max.y:F2}) overlaps lantern '{lf.transform.parent?.name}'.");
                }
            }

            // 5. P2-04: All 36 curated depth props exist on exact sorting layers and have zero clashes with carved bays, OneWay platforms, lanterns, or windows
            (string name, float floorY, float ceilingY)[] phase3Pilasters =
            {
                ("Art_N_Truss1", 8f, 22.5f),
                ("Art_N_Truss2", 8f, 22.5f),
                ("Art_O_Truss1", -16f, -1f),
                ("Art_P_Truss1", -16f, -1.5f),
                ("Art_R_Truss1", 24f, 38.5f),
                ("Art_T_Truss1", 40f, 62.5f),
                ("Art_T_Truss2", 40f, 62.5f),
            };
            string[] phase3Trusses = phase3Pilasters.Select(p => p.name).ToArray();
            string[] phase3Cages = { "Art_G_OrnateCage1", "Art_K_OrnateCage1", "Art_N_OrnateCage1", "Art_O_OrnateCage1", "Art_P_OrnateCage1", "Art_R_OrnateCage1", "Art_S_OrnateCage1", "Art_T_OrnateCage1", "Art_T_OrnateCage2" };
            string[] phase3Chained = { "Art_M_Chained1", "Art_N_Chained1", "Art_O_Chained1", "Art_P_Chained1", "Art_Q_Chained1", "Art_R_Chained1", "Art_T_Chained1", "Art_T_Chained2" };
            string[] phase3Foliage =
            {
                "Art_C_Foliage1", "Art_G_Foliage1", "Art_G_Foliage2", "Art_M_Foliage1",
                "Art_O_Foliage1", "Art_O_Foliage2", "Art_O_Foliage3",
                "Art_P_Foliage1", "Art_P_Foliage2", "Art_Q_Foliage1",
                "Art_S_Foliage1", "Art_S_Foliage2",
            };

            var oneWayBoxes = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include)
                .Where(box => box.gameObject.layer == GeometryAuditHelper.LayerOneWay &&
                              box.gameObject.name.StartsWith("OneWay_", StringComparison.Ordinal))
                .ToArray();
            var windowRenderers = allRenderers.Where(sr => sr.gameObject.name.Contains("Window") && sr.gameObject.name != "GodRay").ToArray();

            Assert.IsFalse(allRenderers.Any(sr => sr != null && sr.sprite != null && sr.sprite.name == "PrisonwoodenBeam_00"),
                "[A16/ZeroWoodenBeamTruss] Scene must contain ZERO legacy PrisonwoodenBeam_00 renderers.");

            foreach (var (name, floorY, ceilingY) in phase3Pilasters)
            {
                var pilasterGo = GameObject.Find(name);
                Assert.IsNotNull(pilasterGo, $"P2-04/Task3.4: Missing stone pilaster '{name}'.");
                var segRenderers = pilasterGo.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.IsNotEmpty(segRenderers, $"P2-04/Task3.4: Pilaster '{name}' must have stacked spawnerPillar_00 SpriteRenderers.");
                foreach (var sr in segRenderers)
                {
                    Assert.IsNotNull(sr.sprite, $"P2-04/Task3.4: Pilaster segment '{name}/{sr.name}' missing sprite.");
                    Assert.AreEqual("spawnerPillar_00", sr.sprite.name, $"P2-04/Task3.4: '{name}/{sr.name}' must use spawnerPillar_00.");
                    Assert.AreEqual("Background", sr.sortingLayerName, $"P2-04/Task3.4: '{name}/{sr.name}' sorting layer.");
                    Assert.AreEqual(-13, sr.sortingOrder, $"P2-04/Task3.4: '{name}/{sr.name}' sorting order.");
                }
                Bounds pb = GetVisualSpriteBounds(pilasterGo);
                Assert.LessOrEqual(pb.min.y, floorY + 0.12f,
                    $"P2-04/Task3.4: Pilaster '{name}' bottom ({pb.min.y:F2}) must be grounded on floorY={floorY:F2}.");
                Assert.GreaterOrEqual(pb.max.y, ceilingY - 0.15f,
                    $"P2-04/Task3.4: Pilaster '{name}' top ({pb.max.y:F2}) must reach ceilingY={ceilingY:F2}.");
            }
            foreach (string name in phase3Cages)
            {
                var sr = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite, $"P2-04: Missing ornate cage '{name}'.");
                StringAssert.StartsWith("chainBot_", sr.sprite.name);
                Assert.AreEqual("Ground", sr.sortingLayerName);
                Assert.AreEqual(-7, sr.sortingOrder);
            }
            foreach (string name in phase3Chained)
            {
                var sr = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite, $"P2-04: Missing chained silhouette '{name}'.");
                StringAssert.StartsWith("wallJunk_", sr.sprite.name);
                Assert.AreEqual("Background", sr.sortingLayerName);
                Assert.AreEqual(-14, sr.sortingOrder);
            }
            foreach (string name in phase3Foliage)
            {
                var sr = GameObject.Find(name)?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr?.sprite, $"P2-04: Missing floor-lip foliage '{name}'.");
                StringAssert.StartsWith("groundFoliage_", sr.sprite.name);
                Assert.AreEqual("Ground", sr.sortingLayerName);
                Assert.AreEqual(-2, sr.sortingOrder);
            }

            foreach (string propName in phase3Trusses.Concat(phase3Cages).Concat(phase3Chained).Concat(phase3Foliage))
            {
                var propGo = GameObject.Find(propName);
                Bounds pb = GetVisualSpriteBounds(propGo);
                bool isFoliage = propName.Contains("_Foliage");

                foreach (var frame in bayFrames)
                {
                    bool ox = frame.bounds.max.x > pb.min.x + 0.05f && frame.bounds.min.x < pb.max.x - 0.05f;
                    bool oy = frame.bounds.max.y > pb.min.y + 0.05f && frame.bounds.min.y < pb.max.y - 0.05f;
                    Assert.IsFalse(ox && oy, $"P2-04: '{propName}' overlaps bay frame '{frame.name}'.");
                }

                foreach (var ow in oneWayBoxes)
                {
                    Bounds ob = GeometryAuditHelper.GetWorldBounds(ow);
                    bool ox = pb.min.x < ob.max.x - 0.05f && pb.max.x > ob.min.x + 0.05f;
                    bool oy = pb.min.y < ob.max.y + 0.10f && pb.max.y > ob.min.y - 0.35f;
                    Assert.IsFalse(ox && oy, $"P2-04: '{propName}' overlaps OneWay platform '{ow.name}'.");
                }

                if (!isFoliage)
                {
                    foreach (var lf in lanternFixtures)
                    {
                        bool ox = pb.min.x < lf.bounds.max.x - 0.04f && pb.max.x > lf.bounds.min.x + 0.04f;
                        bool oy = pb.min.y < lf.bounds.max.y - 0.04f && pb.max.y > lf.bounds.min.y + 0.04f;
                        Assert.IsFalse(ox && oy, $"P2-04: '{propName}' overlaps lantern '{lf.transform.parent?.name}'.");
                    }

                    foreach (var wr in windowRenderers)
                    {
                        bool ox = pb.min.x < wr.bounds.max.x - 0.04f && pb.max.x > wr.bounds.min.x + 0.04f;
                        bool oy = pb.min.y < wr.bounds.max.y - 0.04f && pb.max.y > wr.bounds.min.y + 0.04f;
                        Assert.IsFalse(ox && oy, $"P2-04: '{propName}' overlaps window '{wr.name}'.");
                    }
                }
            }

            // 6. P2-05: Subterranean bedrock rock strata stamps (Ground / -4, >= 60, every stamp center inside opaque dirt_00 on TM_SolidVisual)
            var solidTilemaps = Object.FindObjectsByType<Tilemap>()
                .Where(t => t.name == "TM_SolidVisual")
                .ToArray();
            var bedrockStones = allRenderers
                .Where(sr => sr.gameObject.name.Contains("_BedrockStone_"))
                .ToArray();
            Assert.GreaterOrEqual(bedrockStones.Length, 60,
                $"P2-05: Expected >= 60 subterranean bedrock rock strata stamps, found {bedrockStones.Length}.");
            foreach (var bs in bedrockStones)
            {
                Assert.IsNotNull(bs.sprite, $"P2-05: '{bs.name}' is missing its stoneStamp sprite.");
                Assert.AreEqual("Ground", bs.sortingLayerName, $"P2-05: '{bs.name}' must sort on Ground.");
                Assert.AreEqual(-4, bs.sortingOrder, $"P2-05: '{bs.name}' must sort at -4 (above TM_SolidVisual -5, behind TM_SolidRim -3).");
                StringAssert.StartsWith("stoneStamp_", bs.sprite.name, $"P2-05: '{bs.name}' must use stoneStamp_*.");
                Vector3Int cell = new Vector3Int(Mathf.FloorToInt(bs.transform.position.x), Mathf.FloorToInt(bs.transform.position.y), 0);
                var sp = solidTilemaps.Select(tm => tm.GetSprite(cell)).FirstOrDefault(s => s != null);
                Assert.IsNotNull(sp, $"P2-05: Bedrock stamp '{bs.name}' at cell {cell} sits outside TM_SolidVisual.");
                Assert.AreEqual("dirt_00", sp.name, $"P2-05: Bedrock stamp '{bs.name}' at cell {cell} must sit on opaque dirt_00, got '{sp.name}'.");
            }
        }

        [Test]
        public void A17_Round8Phase2_EnemyImpactFeedbackPrewarmAndPlayerVfxWiring()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. 34 EnemyImpactFeedback instances wired to all 34 enemies with VFX-sorted health bars
            var feedbacks = Object.FindObjectsByType<DeadCells.HUD.EnemyImpactFeedback>(FindObjectsInactive.Include);
            Assert.AreEqual(34, feedbacks.Length, "[A17/FeedbackCount] Expected 34 EnemyImpactFeedback instances.");

            string[] requiredProps =
            {
                "_target", "_body", "_vfx",
                "_impactBlood", "_defeatBlood", "_impactFlash",
                "_audio", "_hitSound", "_fatalSound",
                "_numberPrefab", "_bar", "_fill",
            };
            var coveredTargets = new HashSet<Object>();
            foreach (var fb in feedbacks)
            {
                var soFb = new SerializedObject(fb);
                foreach (var propName in requiredProps)
                {
                    var prop = soFb.FindProperty(propName);
                    Assert.IsNotNull(prop?.objectReferenceValue, $"[A17/Feedback/{fb.name}] Missing '{propName}'.");
                }
                coveredTargets.Add(soFb.FindProperty("_target").objectReferenceValue);

                var barGo = soFb.FindProperty("_bar").objectReferenceValue as GameObject;
                Assert.IsNotNull(barGo, $"[A17/Feedback/{fb.name}] _bar must be a GameObject.");
                var barRenderers = barGo.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.IsNotEmpty(barRenderers, $"[A17/Feedback/{fb.name}] _bar missing SpriteRenderers.");
                foreach (var sr in barRenderers)
                {
                    Assert.AreEqual("VFX", sr.sortingLayerName,
                        $"[A17/FeedbackBarSorting/{fb.name}/{sr.name}] Enemy health bar must render on VFX sorting layer.");
                }
            }
            Assert.AreEqual(34, coveredTargets.Count, "[A17/DistinctTargets] Expected 34 distinct EnemyImpactFeedback targets.");

            // 2. VfxSpawner._prewarm contains 11 non-null VfxDefinition assets
            var vfxSpawner = Object.FindAnyObjectByType<VfxSpawner>();
            Assert.IsNotNull(vfxSpawner, "[A17/VfxSpawner]");
            var prewarmProp = new SerializedObject(vfxSpawner).FindProperty("_prewarm");
            Assert.IsNotNull(prewarmProp);
            Assert.AreEqual(11, prewarmProp.arraySize, "[A17/PrewarmCount] Expected 11 prewarmed VfxDefinitions.");
            for (int i = 0; i < prewarmProp.arraySize; i++)
            {
                Assert.IsNotNull(prewarmProp.GetArrayElementAtIndex(i).objectReferenceValue,
                    $"[A17/PrewarmNull/{i}] Null VfxDefinition in VfxSpawner._prewarm.");
            }

            // 3. PlayerSpawnHead._slimeVfx and PlayerVfx._dodgeGlintVfx are wired on Player
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "[A17/Player]");
            var spawnHead = player.GetComponentInChildren<PlayerSpawnHead>(true);
            Assert.IsNotNull(spawnHead, "[A17/PlayerSpawnHead]");
            Assert.IsNotNull(new SerializedObject(spawnHead).FindProperty("_slimeVfx")?.objectReferenceValue,
                "[A17/SpawnHeadSlimeVfx] PlayerSpawnHead._slimeVfx must be wired.");

            var playerVfx = player.GetComponentInChildren<PlayerVfx>(true);
            Assert.IsNotNull(playerVfx, "[A17/PlayerVfx]");
            Assert.IsNotNull(new SerializedObject(playerVfx).FindProperty("_dodgeGlintVfx")?.objectReferenceValue,
                "[A17/DodgeGlintVfx] PlayerVfx._dodgeGlintVfx must be wired.");
        }

        [Test]
        public void A18_Round8Phase3_StairsMistPropsLightingAndAtmosphere()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. M-01: Stairs_H is ONE clean 45-degree diagonal row of 8 flipped stair tiles (zero double-stacking)
            var hjkGo = GameObject.Find("Room_HJK");
            Assert.IsNotNull(hjkGo, "[A18/Room_HJK]");
            var hjkTms = hjkGo.GetComponentsInChildren<Tilemap>(true);
            var hjkRimTm = hjkTms.SingleOrDefault(t => t.name == "TM_SolidRim");
            var hjkSolidTm = hjkTms.SingleOrDefault(t => t.name == "TM_SolidVisual");
            Assert.IsNotNull(hjkRimTm, "[A18/HJK_Rim]");
            Assert.IsNotNull(hjkSolidTm, "[A18/HJK_Solid]");

            (int x, int y)[] diagonalStairCells =
            {
                (17, 8), (16, 9), (15, 10), (14, 11), (13, 12), (12, 13), (11, 14), (10, 15),
            };

            int flippedStairTiles = 0;
            var rimBounds = hjkRimTm.cellBounds;
            for (int x = rimBounds.xMin; x < rimBounds.xMax; x++)
            {
                for (int y = rimBounds.yMin; y < rimBounds.yMax; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    var sp = hjkRimTm.GetSprite(cell);
                    if (sp == null)
                        continue;
                    if (sp.name == "stairs_0_00" || sp.name == "stairs_1_00")
                    {
                        flippedStairTiles++;
                        var mat = hjkRimTm.GetTransformMatrix(cell);
                        Assert.Less(mat.m00, 0f, $"[A18/StairsFlip] '{sp.name}' at {cell} must have negative scale X (m00 < 0f), got {mat.m00}.");
                    }
                }
            }
            Assert.AreEqual(8, flippedStairTiles, "[A18/StairsTileCount] Expected exactly 8 single-row 45-degree Stairs_H tiles (no double-stacked diagonals).");

            foreach (var (dx, dy) in diagonalStairCells)
            {
                var cell = new Vector3Int(dx, dy, 0);
                var stairSp = hjkRimTm.GetSprite(cell);
                Assert.IsNotNull(stairSp, $"[A18/DiagonalStair/{cell}] Missing 45-degree stair tile on the diagonal run.");
                StringAssert.StartsWith("stairs_", stairSp.name, $"[A18/DiagonalStair/{cell}] Diagonal run must use stairs_* tiles.");

                var belowSp = hjkRimTm.GetSprite(new Vector3Int(dx, dy - 1, 0));
                Assert.IsFalse(belowSp != null && belowSp.name.StartsWith("stairs_", StringComparison.Ordinal),
                    $"[A18/DoubleStack/{cell}] No stair tile may sit directly below diagonal stair cell (single clean row only).");

                var coreSp = hjkSolidTm.GetSprite(new Vector3Int(dx, dy - 1, 0));
                Assert.IsNotNull(coreSp, $"[A18/StairCore/{cell}] Diagonal stair cell must rest on solid stone core at y={dy - 1}.");
                Assert.AreEqual("dirt_00", coreSp.name, $"[A18/StairCore/{cell}] Stone core below diagonal stair must be opaque dirt_00.");
            }

            Vector3Int[] landingCapCells =
            {
                new Vector3Int(18, 7, 0),
                new Vector3Int(7, 15, 0),
            };
            foreach (var cell in landingCapCells)
            {
                var solidSp = hjkSolidTm.GetSprite(cell);
                Assert.IsNotNull(solidSp, $"[A18/StairLandingVisual/{cell}] Missing solid floorStamp sprite.");
                StringAssert.StartsWith("floorStamp_", solidSp.name, $"[A18/StairLandingVisual/{cell}]");
                var rimSp = hjkRimTm.GetSprite(cell + Vector3Int.up);
                Assert.IsNotNull(rimSp, $"[A18/StairLandingRim/{cell + Vector3Int.up}] Missing landing rim sprite.");
                StringAssert.StartsWith("ground_", rimSp.name, $"[A18/StairLandingRim/{cell + Vector3Int.up}] Landing cap rim must use ground_*.");
            }

            // 2. M-02: GroundMist2D world-space shader & 9 GroundMist_R5 strips
            string mistShaderSrc = System.IO.File.ReadAllText("Assets/_Project/Art/Shaders/GroundMist2D.shader");
            StringAssert.Contains("TransformObjectToWorld", mistShaderSrc, "[A18/MistWorldSpace]");
            StringAssert.Contains("Blend SrcAlpha OneMinusSrcAlpha", mistShaderSrc, "[A18/MistBlendMode]");
            var mistRoot = GameObject.Find("GroundMist_R5");
            Assert.IsNotNull(mistRoot, "[A18/GroundMistRoot]");
            var mistRenderers = mistRoot.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(9, mistRenderers.Length, "[A18/GroundMistCount]");
            string[] expectedMistNames =
            {
                "Mist_RoomAB", "Mist_RoomC", "Mist_RoomG", "Mist_RoomK", "Mist_RoomE",
                "Mist_RoomO", "Mist_RoomP", "Mist_RoomQ", "Mist_RoomT",
            };
            CollectionAssert.AreEquivalent(expectedMistNames, mistRenderers.Select(mr => mr.gameObject.name).ToArray(),
                "[A18/GroundMistNames]");
            foreach (var mr in mistRenderers)
            {
                Assert.AreEqual("DeadCells/GroundMist2D", mr.sharedMaterial.shader.name, $"[A18/MistShader/{mr.name}]");
                Assert.GreaterOrEqual(mr.color.a, 0.80f - 1e-4f, $"[A18/MistAlpha/{mr.name}]");
            }

            // 3. M-03 & M-04: Art_Q_SkeletonPier sockets Art_Q_SkeletonSpawn left edge + grounded floor props clash-free
            var skelPierSr = GameObject.Find("Art_Q_SkeletonPier")?.GetComponent<SpriteRenderer>();
            var skelSpawnSr = GameObject.Find("Art_Q_SkeletonSpawn")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(skelPierSr, "[A18/SkeletonPier]");
            Assert.IsNotNull(skelSpawnSr, "[A18/SkeletonSpawn]");
            Assert.AreEqual("prisonBigAlcove_00", skelSpawnSr.sprite?.name, "[A18/SkeletonSpawnSprite]");
            Assert.AreEqual("Background", skelPierSr.sortingLayerName, "[A18/SkeletonPierLayer]");
            Assert.AreEqual(-12, skelPierSr.sortingOrder, "[A18/SkeletonPierOrder]");
            Assert.AreEqual(-51.50f, skelSpawnSr.bounds.min.x, 0.15f, "[A18/SkeletonSpawnMinX]");
            Assert.LessOrEqual(skelPierSr.bounds.min.x, skelSpawnSr.bounds.min.x + 0.05f, "[A18/SkeletonPierCoversLeft]");
            Assert.GreaterOrEqual(skelPierSr.bounds.max.x, skelSpawnSr.bounds.min.x - 0.05f, "[A18/SkeletonPierCoversRight]");

            string[] newJunkClusters =
            {
                "Art_A_JunkEast",
                "Art_D_JunkWest", "Art_D_JunkEast2",
                "Art_H_LandingJunk", "Art_H_StepJunkTop",
                "Art_K_JunkMid",
                "Art_E_JunkWest", "Art_E_JunkMid", "Art_E_JunkEast",
                "Art_L_JunkMid", "Art_L_JunkEast",
                "Art_F_JunkMid", "Art_F_JunkEast",
                "Art_M_JunkWest2", "Art_M_JunkMid", "Art_M_JunkMid2", "Art_M_JunkEast2", "Art_M_JunkEast3",
                "Art_N_JunkWest2", "Art_N_JunkMid1", "Art_N_JunkShaftEdge", "Art_N_JunkMid2", "Art_N_JunkMid3", "Art_N_JunkEast1", "Art_N_JunkEast2", "Art_N_JunkEast3",
                "Art_O_JunkMid1", "Art_O_JunkMid3", "Art_O_JunkMid2", "Art_O_JunkEast4", "Art_O_JunkEast2", "Art_O_JunkEast3",
                "Art_P_JunkWest2", "Art_P_JunkMid1", "Art_P_JunkMid2", "Art_P_JunkEast2", "Art_P_JunkEast3",
                "Art_Q_JunkMid1", "Art_Q_JunkMid2", "Art_Q_JunkEast2",
                "Art_R_JunkWest2", "Art_R_JunkWest3", "Art_R_JunkMid1", "Art_R_JunkMid2", "Art_R_JunkShaftEdge", "Art_R_JunkEast2",
                "Art_S_JunkMid1", "Art_S_JunkMid2", "Art_S_JunkMid3", "Art_S_JunkEast2",
                "Art_T_JunkWest2", "Art_T_JunkMid1", "Art_T_JunkShaft", "Art_T_JunkMid2", "Art_T_JunkMid3", "Art_T_JunkEast1", "Art_T_JunkEast3", "Art_T_JunkEast2",
            };
            var newFloorPropObjects = new List<GameObject>();
            Assert.IsNull(GameObject.Find("Art_H_StepJunkMid_A"),
                "[A18/FloorProp/Art_H_StepJunkMid] Mid-slope junk cluster must be deleted (it sat on the open 45-degree stair run).");
            var bedGo = GameObject.Find("Art_D_Bed1");
            Assert.IsNotNull(bedGo, "[A18/FloorProp/Art_D_Bed1]");
            newFloorPropObjects.Add(bedGo);
            foreach (string cluster in newJunkClusters)
            {
                var aGo = GameObject.Find(cluster + "_A");
                var bGo = GameObject.Find(cluster + "_B");
                Assert.IsNotNull(aGo, $"[A18/FloorProp/{cluster}_A]");
                Assert.IsNotNull(bGo, $"[A18/FloorProp/{cluster}_B]");
                newFloorPropObjects.Add(aGo);
                newFloorPropObjects.Add(bGo);
            }

            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            var bayFrames = allRenderers
                .Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal) ||
                             sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal))
                .ToArray();
            var oneWayBoxes = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include)
                .Where(box => box.gameObject.layer == GeometryAuditHelper.LayerOneWay &&
                              box.gameObject.name.StartsWith("OneWay_", StringComparison.Ordinal))
                .ToArray();

            foreach (var propGo in newFloorPropObjects)
            {
                Bounds pb = GetVisualSpriteBounds(propGo);
                foreach (var frame in bayFrames)
                {
                    bool ox = frame.bounds.max.x > pb.min.x + 0.05f && frame.bounds.min.x < pb.max.x - 0.05f;
                    bool oy = frame.bounds.max.y > pb.min.y + 0.05f && frame.bounds.min.y < pb.max.y - 0.05f;
                    Assert.IsFalse(ox && oy, $"[A18/FloorPropBayClash] '{propGo.name}' overlaps bay frame '{frame.name}'.");
                }
                foreach (var ow in oneWayBoxes)
                {
                    Bounds ob = GeometryAuditHelper.GetWorldBounds(ow);
                    bool ox = pb.min.x < ob.max.x - 0.05f && pb.max.x > ob.min.x + 0.05f;
                    bool oy = pb.min.y < ob.max.y + 0.10f && pb.max.y > ob.min.y - 0.35f;
                    Assert.IsFalse(ox && oy, $"[A18/FloorPropOneWayClash] '{propGo.name}' overlaps OneWay '{ow.name}'.");
                }
            }

            // 4. M-05 & A-17: Effective point-light coverage (<= 0.85 * outerRadius) >= 80% in all 19 rooms + 12 ShadowCasters
            var pointLights = Object.FindObjectsByType<Light2D>()
                .Where(l => l.enabled && l.lightType == Light2D.LightType.Point && l.intensity > 0.01f && l.pointLightOuterRadius > 0.1f)
                .ToArray();
            (string roomId, Rect rect)[] rooms =
            {
                ("A", new Rect(0f, 0f, 18f, 5.5f)),
                ("B", new Rect(18f, 0f, 24f, 6f)),
                ("C", new Rect(42f, 0f, 14f, 18.5f)),
                ("D", new Rect(22f, 8f, 20f, 5.5f)),
                ("G", new Rect(56f, 0f, 20f, 14.5f)),
                ("H", new Rect(6f, 8f, 16f, 14.5f)),
                ("J", new Rect(-16f, 16f, 22f, 6.5f)),
                ("K", new Rect(-30f, 16f, 14f, 14.5f)),
                ("E", new Rect(-16f, 24f, 38f, 7f)),
                ("L", new Rect(22f, 24f, 16f, 14.5f)),
                ("F", new Rect(0f, 32f, 22f, 6f)),
                ("M", new Rect(76f, 0f, 88f, 7f)),
                ("N", new Rect(76f, 8f, 88f, 14.5f)),
                ("O", new Rect(76f, -16f, 88f, 15f)),
                ("P", new Rect(164f, -16f, 70f, 14.5f)),
                ("Q", new Rect(-102f, 16f, 72f, 7f)),
                ("R", new Rect(-102f, 24f, 72f, 14.5f)),
                ("S", new Rect(38f, 24f, 54f, 15f)),
                ("T", new Rect(38f, 40f, 102f, 22.5f)),
            };
            foreach (var (roomId, rect) in rooms)
            {
                int roomSamples = 0;
                int roomLit = 0;
                for (float x = rect.xMin + 0.5f; x < rect.xMax; x += 1f)
                {
                    for (float y = rect.yMin + 0.5f; y < rect.yMax; y += 1f)
                    {
                        var pt = new Vector2(x, y);
                        roomSamples++;
                        if (pointLights.Any(l => Vector2.Distance(pt, l.transform.position) <= 0.85f * l.pointLightOuterRadius))
                            roomLit++;
                    }
                }
                float ratio = roomSamples > 0 ? (float)roomLit / roomSamples : 0f;
                Assert.GreaterOrEqual(ratio, 0.80f,
                    $"[A18/EffectiveLight/{roomId}] Room {roomId} effective point-light coverage (<=0.85*R) is {ratio:P1} ({roomLit}/{roomSamples}), expected >= 80%.");
            }

            var castersRoot = GameObject.Find("ShadowCasters_R5");
            Assert.IsNotNull(castersRoot, "[A18/ShadowCastersRoot]");
            Assert.AreEqual(12, castersRoot.GetComponentsInChildren<ShadowCaster2D>(true).Length, "[A18/ShadowCastersCount]");

            // Caster_StairsH must hug the 45-degree stone mass (zero floating shadow box in mid-air).
            // Stone top surface: y=8 for x in [18,22], y=26-x for x in [10,18], y=16 for x in [6,10].
            var stairsCaster = castersRoot.GetComponentsInChildren<ShadowCaster2D>(true)
                .SingleOrDefault(c => c.name == "Caster_StairsH");
            Assert.IsNotNull(stairsCaster, "[A18/StairsCaster] Missing fitted Caster_StairsH ShadowCaster2D.");
            var casterSo = new SerializedObject(stairsCaster);
            var casterPath = casterSo.FindProperty("m_ShapePath");
            Assert.IsNotNull(casterPath, "[A18/StairsCasterPath] Caster_StairsH must expose m_ShapePath.");
            Assert.AreEqual(6, casterPath.arraySize, "[A18/StairsCasterVertexCount] Fitted caster must be the 6-vertex stone-mass polygon.");
            var casterWorldVerts = new Vector3[casterPath.arraySize];
            for (int i = 0; i < casterPath.arraySize; i++)
            {
                var local = casterPath.GetArrayElementAtIndex(i).vector3Value;
                casterWorldVerts[i] = stairsCaster.transform.TransformPoint(local);
            }
            for (int i = 0; i < casterWorldVerts.Length; i++)
            {
                var v = casterWorldVerts[i];
                float stoneTop = Mathf.Clamp(26f - v.x, 8f, 16f);
                Assert.LessOrEqual(v.y, stoneTop + 0.05f,
                    $"[A18/StairsCasterFloating/{i}] Caster vertex ({v.x:F2},{v.y:F2}) floats above the 45-degree stone top y={stoneTop:F2}.");
                Assert.GreaterOrEqual(v.y, 7f - 0.05f,
                    $"[A18/StairsCasterBelowFloor/{i}] Caster vertex ({v.x:F2},{v.y:F2}) sinks below the Stairs_H base y=7.");
            }
            Assert.AreEqual(6f, casterWorldVerts.Min(v => v.x), 0.05f, "[A18/StairsCasterMinX]");
            Assert.AreEqual(22f, casterWorldVerts.Max(v => v.x), 0.05f, "[A18/StairsCasterMaxX]");
            Assert.AreEqual(7f, casterWorldVerts.Min(v => v.y), 0.05f, "[A18/StairsCasterMinY]");
            Assert.AreEqual(16f, casterWorldVerts.Max(v => v.y), 0.05f, "[A18/StairsCasterMaxY]");

            // 5. A-13 & A-14: Foreground_Silhouettes >= 4 peripheral FgColumn_* spanning [-36..+36] with zero center clutter (|x| >= 12f) + ambient ParticleSystems
            var fgSilhouettes = GameObject.Find("Foreground_Silhouettes");
            Assert.IsNotNull(fgSilhouettes, "[A18/ForegroundSilhouettes]");
            var fgColumns = new List<Transform>();
            foreach (Transform child in fgSilhouettes.transform)
            {
                if (child.name.StartsWith("FgColumn_", StringComparison.Ordinal))
                    fgColumns.Add(child);
            }
            Assert.GreaterOrEqual(fgColumns.Count, 4, "[A18/FgColumnCount] Expected >= 4 peripheral FgColumn_* columns.");
            foreach (var col in fgColumns)
            {
                Bounds colBounds = GetVisualSpriteBounds(col.gameObject);
                float relCenterX = colBounds.center.x - fgSilhouettes.transform.position.x;
                Assert.GreaterOrEqual(Mathf.Abs(relCenterX), 12.0f,
                    $"[A18/FgColumnClearCenter/{col.name}] FgColumn must stay outside center |x| < 12f, got {relCenterX:F2}.");
            }
            var fgRenderers = fgSilhouettes.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.IsNotEmpty(fgRenderers, "[A18/FgRenderers]");
            Bounds fgBounds = fgRenderers[0].bounds;
            for (int i = 1; i < fgRenderers.Length; i++)
                fgBounds.Encapsulate(fgRenderers[i].bounds);
            Assert.LessOrEqual(fgBounds.min.x, -36.0f, $"[A18/FgMinX] min.x={fgBounds.min.x:F2} must be <= -36.0f.");
            Assert.GreaterOrEqual(fgBounds.max.x, 36.0f, $"[A18/FgMaxX] max.x={fgBounds.max.x:F2} must be >= 36.0f.");

            foreach (string psName in new[] { "AmbientDustMotes", "AmbientDrippingWater" })
            {
                var psGo = GameObject.Find(psName);
                Assert.IsNotNull(psGo, $"[A18/{psName}]");
                Assert.IsNotNull(psGo.GetComponent<CameraFollowEmitter2D>(), $"[A18/{psName}/CameraFollowEmitter2D]");
                var ps = psGo.GetComponent<ParticleSystem>();
                Assert.IsNotNull(ps, $"[A18/{psName}/ParticleSystem]");
                Assert.LessOrEqual(ps.shape.scale.x, 32f, $"[A18/{psName}/ShapeWidth]");
            }
        }

        [Test]
        public void A19_Round8Phase4_AudioHubItemPickupAndPauseMenu()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. Task 4.1: CombatAudioHub + ItemPickupFeedback on "Combat and Pickup Feedback"
            var audioHubs = Object.FindObjectsByType<CombatAudioHub>(FindObjectsInactive.Include);
            Assert.AreEqual(1, audioHubs.Length, "[A19/CombatAudioHubCount]");

            var pickupFeedbacks = Object.FindObjectsByType<ItemPickupFeedback>(FindObjectsInactive.Include);
            Assert.AreEqual(1, pickupFeedbacks.Length, "[A19/ItemPickupFeedbackCount]");
            var pickupFeedback = pickupFeedbacks[0];
            Assert.AreEqual("Combat and Pickup Feedback", pickupFeedback.gameObject.name, "[A19/ItemPickupFeedbackName]");

            var soFeedback = new SerializedObject(pickupFeedback);
            Assert.IsNotNull(soFeedback.FindProperty("_player").objectReferenceValue, "[A19/ItemPickup.Player]");
            Assert.IsNotNull(soFeedback.FindProperty("_wallet").objectReferenceValue, "[A19/ItemPickup.Wallet]");
            Assert.IsNotNull(soFeedback.FindProperty("_effects").objectReferenceValue, "[A19/ItemPickup.Effects]");
            Assert.IsNotNull(soFeedback.FindProperty("_glint").objectReferenceValue, "[A19/ItemPickup.Glint]");
            Assert.IsNotNull(soFeedback.FindProperty("_audio").objectReferenceValue, "[A19/ItemPickup.Audio]");
            Assert.IsNotNull(soFeedback.FindProperty("_goldSound").objectReferenceValue, "[A19/ItemPickup.GoldSound]");
            Assert.IsNotNull(soFeedback.FindProperty("_weapons"), "[A19/ItemPickup.WeaponsArray]");

            // 2. Task 4.2: PauseMenu uGUI Canvas + PauseMenuController + bottom-right ControlsHintBadge under PlayerHUD
            var pauseMenus = Object.FindObjectsByType<PauseMenuController>(FindObjectsInactive.Include);
            Assert.AreEqual(1, pauseMenus.Length, "[A19/PauseMenuCount]");
            var pauseMenu = pauseMenus[0];
            Assert.IsTrue(pauseMenu.gameObject.activeSelf, "[A19/PauseMenu.RootActive]");

            var pauseCanvas = pauseMenu.GetComponent<Canvas>() ?? pauseMenu.GetComponentInParent<Canvas>(true);
            Assert.IsNotNull(pauseCanvas, "[A19/PauseMenu.Canvas]");
            Assert.AreEqual(90, pauseCanvas.sortingOrder, "[A19/PauseMenu.SortingOrder]");
            var pauseScaler = pauseCanvas.GetComponent<CanvasScaler>();
            Assert.IsNotNull(pauseScaler, "[A19/PauseMenu.CanvasScaler]");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, pauseScaler.uiScaleMode, "[A19/PauseMenu.ScaleMode]");
            Assert.AreEqual(new Vector2(1920f, 1080f), pauseScaler.referenceResolution, "[A19/PauseMenu.RefRes]");

            var soPause = new SerializedObject(pauseMenu);
            Assert.IsNotNull(soPause.FindProperty("_run").objectReferenceValue, "[A19/PauseMenu.Run]");
            Assert.IsNotNull(soPause.FindProperty("_player").objectReferenceValue, "[A19/PauseMenu.Player]");
            Assert.IsNotNull(soPause.FindProperty("_playerInput").objectReferenceValue, "[A19/PauseMenu.PlayerInput]");
            Assert.IsNotNull(soPause.FindProperty("_hitStop").objectReferenceValue, "[A19/PauseMenu.HitStop]");
            var panelRoot = soPause.FindProperty("_panelRoot").objectReferenceValue as GameObject;
            Assert.IsNotNull(panelRoot, "[A19/PauseMenu.PanelRoot]");
            Assert.IsFalse(panelRoot.activeSelf, "[A19/PauseMenu.HiddenInitially]");

            var resumeBtn = soPause.FindProperty("_resumeButton").objectReferenceValue as Button;
            var restartBtn = soPause.FindProperty("_restartButton").objectReferenceValue as Button;
            var quitBtn = soPause.FindProperty("_quitButton").objectReferenceValue as Button;
            Assert.IsNotNull(resumeBtn, "[A19/PauseMenu.ResumeButton]");
            Assert.IsNotNull(restartBtn, "[A19/PauseMenu.RestartButton]");
            Assert.IsNotNull(quitBtn, "[A19/PauseMenu.QuitButton]");

            var panelTexts = panelRoot.GetComponentsInChildren<Text>(true).Select(t => t.text).ToArray();
            Assert.IsTrue(panelTexts.Any(t => t.Contains("TẠM DỪNG")), "[A19/PauseMenu.Title]");
            Assert.IsTrue(panelTexts.Any(t => t.Contains("Tiếp tục")), "[A19/PauseMenu.ResumeLabel]");
            Assert.IsTrue(panelTexts.Any(t => t.Contains("Chơi lại")), "[A19/PauseMenu.RestartLabel]");
            Assert.IsTrue(panelTexts.Any(t => t.Contains("Thoát game")), "[A19/PauseMenu.QuitLabel]");
            string joinedLegend = string.Join(" ", panelTexts);
            StringAssert.Contains("A/D", joinedLegend, "[A19/PauseMenu.LegendMove]");
            StringAssert.Contains("Space", joinedLegend, "[A19/PauseMenu.LegendJump]");
            StringAssert.Contains("Shift", joinedLegend, "[A19/PauseMenu.LegendRoll]");
            StringAssert.Contains("E", joinedLegend, "[A19/PauseMenu.LegendInteract]");

            var playerHud = GameObject.Find("PlayerHUD");
            Assert.IsNotNull(playerHud, "[A19/PlayerHUD]");
            var hintTr = playerHud.transform.Find("ControlsHintBadge");
            Assert.IsNotNull(hintTr, "[A19/ControlsHintBadge]");
            var hintRect = hintTr.GetComponent<RectTransform>();
            Assert.IsNotNull(hintRect, "[A19/ControlsHintBadge.Rect]");
            Assert.AreEqual(new Vector2(1f, 0f), hintRect.anchorMin, "[A19/ControlsHintBadge.AnchorMin]");
            Assert.AreEqual(new Vector2(1f, 0f), hintRect.anchorMax, "[A19/ControlsHintBadge.AnchorMax]");
            var hintText = hintTr.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(hintText, "[A19/ControlsHintBadge.Text]");
            StringAssert.Contains("[Esc] Tạm dừng", hintText.text, "[A19/ControlsHintBadge.Esc]");
            StringAssert.Contains("[1/2] Vũ khí", hintText.text, "[A19/ControlsHintBadge.Weapons]");
        }

        [Test]
        public void A20_Round9_ZeroOverlapsGroundedPilastersAnchoredChainedAndMixelDiscipline()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

            // 1. Task 3.3: Zero junk self-overlaps (*_Junk*_A vs *_Junk*_B overlapX <= 0.02f) & zero cross-junk overlaps (-0.04f tolerance)
            var junkRenderers = allRenderers
                .Where(sr => sr != null && sr.sprite != null &&
                             sr.gameObject.name.Contains("_Junk", StringComparison.Ordinal) &&
                             (sr.gameObject.name.EndsWith("_A", StringComparison.Ordinal) ||
                              sr.gameObject.name.EndsWith("_B", StringComparison.Ordinal)))
                .ToArray();
            Assert.IsNotEmpty(junkRenderers, "[A20/JunkRenderers] Expected floor junk renderers.");

            var junkByName = new Dictionary<string, SpriteRenderer>(StringComparer.Ordinal);
            foreach (var sr in junkRenderers)
                junkByName[sr.gameObject.name] = sr;

            foreach (var srA in junkRenderers.Where(sr => sr.gameObject.name.EndsWith("_A", StringComparison.Ordinal)))
            {
                string nameB = srA.gameObject.name.Substring(0, srA.gameObject.name.Length - 2) + "_B";
                Assert.IsTrue(junkByName.TryGetValue(nameB, out var srB),
                    $"[A20/JunkPairMissing] Missing '{nameB}' paired with '{srA.gameObject.name}'.");
                float overlapX = Mathf.Min(srA.bounds.max.x, srB.bounds.max.x) - Mathf.Max(srA.bounds.min.x, srB.bounds.min.x);
                Assert.LessOrEqual(overlapX, 0.02f,
                    $"[A20/JunkSelfOverlap] '{srA.gameObject.name}' ({srA.bounds.min.x:F2}..{srA.bounds.max.x:F2}) overlaps '{nameB}' ({srB.bounds.min.x:F2}..{srB.bounds.max.x:F2}), overlapX={overlapX:F3}.");
            }

            for (int i = 0; i < junkRenderers.Length; i++)
            {
                string clusterI = junkRenderers[i].gameObject.name.Substring(0, junkRenderers[i].gameObject.name.Length - 2);
                for (int j = i + 1; j < junkRenderers.Length; j++)
                {
                    string clusterJ = junkRenderers[j].gameObject.name.Substring(0, junkRenderers[j].gameObject.name.Length - 2);
                    if (clusterI == clusterJ)
                        continue;
                    Assert.IsFalse(OverlapsAabb(junkRenderers[i].bounds, junkRenderers[j].bounds, -0.04f, -0.04f),
                        $"[A20/CrossJunkOverlap] '{junkRenderers[i].gameObject.name}' ({junkRenderers[i].bounds.min}..{junkRenderers[i].bounds.max}) overlaps '{junkRenderers[j].gameObject.name}' ({junkRenderers[j].bounds.min}..{junkRenderers[j].bounds.max}).");
                }
            }

            // 2. Task 3.3: Zero floor junk overlaps with landmarks, POIs, or torches
            var junkObstacles = new List<(string label, Bounds bounds)>();
            string[] landmarkTokens =
            {
                "Door", "Bibli", "Alcove", "Arch", "Fountain", "Pillar", "Dummy", "JudasSeat", "Billot",
            };
            foreach (var sr in allRenderers)
            {
                if (sr == null || sr.sprite == null)
                    continue;
                string n = sr.gameObject.name;
                string pn = sr.transform.parent != null ? sr.transform.parent.name : string.Empty;

                if (n == "LanternFixture_R5")
                {
                    junkObstacles.Add(($"{pn}/{n}", sr.bounds));
                    continue;
                }

                if (n == "GodRay" || n.Contains("Glow", StringComparison.Ordinal) || n == "LanternFlameCore_R5")
                    continue;

                bool isArtLandmark = (n.StartsWith("Art_", StringComparison.Ordinal) || pn.StartsWith("Art_", StringComparison.Ordinal)) &&
                                     !n.StartsWith("Arch", StringComparison.Ordinal) &&
                                     landmarkTokens.Any(tok => n.Contains(tok, StringComparison.Ordinal) || pn.Contains(tok, StringComparison.Ordinal));
                if (isArtLandmark)
                    junkObstacles.Add((n, sr.bounds));
            }

            foreach (var tp in Object.FindObjectsByType<TeleportGate>(FindObjectsInactive.Include))
            {
                foreach (var sr in tp.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr != null && sr.sprite != null)
                        junkObstacles.Add(($"{tp.name}/{sr.name}", sr.bounds));
                }
            }
            foreach (var scroll in Object.FindObjectsByType<PowerScrollPickup>(FindObjectsInactive.Include))
                junkObstacles.Add((scroll.name, GetVisualSpriteBounds(scroll.gameObject)));
            foreach (var chest in Object.FindObjectsByType<HealingChest>(FindObjectsInactive.Include))
                junkObstacles.Add((chest.name, GetVisualSpriteBounds(chest.gameObject)));
            foreach (var tablet in Object.FindObjectsByType<LoreTablet>(FindObjectsInactive.Include))
            {
                if (tablet.TriggerZone != null)
                    junkObstacles.Add((tablet.name, GeometryAuditHelper.GetWorldBounds(tablet.TriggerZone)));
            }
            foreach (var sd in Object.FindObjectsByType<SwitchDoor>(FindObjectsInactive.Include))
            {
                foreach (var sr in sd.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr != null && sr.sprite != null)
                        junkObstacles.Add(($"{sd.name}/{sr.name}", sr.bounds));
                }
            }

            Assert.IsNotEmpty(junkObstacles, "[A20/JunkObstacles]");
            foreach (var junk in junkRenderers)
            {
                foreach (var (obsLabel, obsBounds) in junkObstacles)
                {
                    Assert.IsFalse(OverlapsAabb(junk.bounds, obsBounds, -0.02f, -0.02f),
                        $"[A20/JunkObstacleOverlap] '{junk.gameObject.name}' ({junk.bounds.min:F2}..{junk.bounds.max:F2}) overlaps '{obsLabel}' ({obsBounds.min:F2}..{obsBounds.max:F2}).");
                }
            }

            // 3. Task 3.4: Zero wallJunk_10..13 tiles on TM_WallDecals & zero PrisonwoodenBeam_00 renderers
            var decalTilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
                .Where(t => t.name == "TM_WallDecals")
                .ToArray();
            Assert.AreEqual(9, decalTilemaps.Length, "[A20/WallDecalTilemaps]");
            foreach (var tm in decalTilemaps)
            {
                var cb = tm.cellBounds;
                for (int x = cb.xMin; x < cb.xMax; x++)
                {
                    for (int y = cb.yMin; y < cb.yMax; y++)
                    {
                        var sp = tm.GetSprite(new Vector3Int(x, y, 0));
                        if (sp == null)
                            continue;
                        Assert.IsFalse(
                            sp.name == "wallJunk_10" || sp.name == "wallJunk_11" ||
                            sp.name == "wallJunk_12" || sp.name == "wallJunk_13",
                            $"[A20/NoWallBlobs] Found banned wall blob '{sp.name}' at ({x},{y}) on '{tm.transform.root.name}/TM_WallDecals'.");
                    }
                }
            }

            Assert.IsFalse(allRenderers.Any(sr => sr != null && sr.sprite != null && sr.sprite.name == "PrisonwoodenBeam_00"),
                "[A20/ZeroWoodenBeamTruss] Scene must contain 0 PrisonwoodenBeam_00 renderers.");

            // 4. Task 3.4: All 8 ChainedPrisoners have dual left & right anchor chains {name}_AnchorL_* and {name}_AnchorR_* reaching ceilingY - 0.15f
            (string chainedName, float ceilingY)[] expectedChained =
            {
                ("Art_M_Chained1", 7f),
                ("Art_N_Chained1", 22.5f),
                ("Art_O_Chained1", -1f),
                ("Art_P_Chained1", -1.5f),
                ("Art_Q_Chained1", 23f),
                ("Art_R_Chained1", 38.5f),
                ("Art_T_Chained1", 62.5f),
                ("Art_T_Chained2", 62.5f),
            };
            foreach (var (chainedName, ceilingY) in expectedChained)
            {
                var chainedGo = GameObject.Find(chainedName);
                Assert.IsNotNull(chainedGo, $"[A20/ChainedMissing/{chainedName}]");
                foreach (string sideSuffix in new[] { "_AnchorL_", "_AnchorR_" })
                {
                    string anchorPrefix = chainedName + sideSuffix;
                    var anchors = allRenderers
                        .Where(sr => sr != null && sr.gameObject.name.StartsWith(anchorPrefix, StringComparison.Ordinal))
                        .ToArray();
                    Assert.IsNotEmpty(anchors, $"[A20/ChainedAnchorMissing/{chainedName}{sideSuffix}] Expected '{anchorPrefix}*' chain segments reaching ceiling.");
                    float maxAnchorY = anchors.Max(a => a.bounds.max.y);
                    Assert.GreaterOrEqual(maxAnchorY, ceilingY - 0.15f,
                        $"[A20/ChainedAnchorTop/{chainedName}{sideSuffix}] Top anchor max.y={maxAnchorY:F2} must reach ceilingY={ceilingY:F2} (-0.15).");
                    Assert.LessOrEqual(maxAnchorY, ceilingY + 0.15f,
                        $"[A20/ChainedAnchorOverflow/{chainedName}{sideSuffix}] Top anchor max.y={maxAnchorY:F2} must not protrude past ceilingY={ceilingY:F2}.");
                }
            }

            // 5. Task 3.4: No floating open-doorway top webs at (17,4)/(18,4)
            var rimTilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
                .Where(t => t.name == "TM_SolidRim")
                .ToArray();
            Assert.AreEqual(9, rimTilemaps.Length, "[A20/SolidRimCount]");
            foreach (var openTopCell in new[] { new Vector3Int(17, 4, 0), new Vector3Int(18, 4, 0) })
            {
                var tile = rimTilemaps.Select(tm => tm.GetTile(openTopCell)).FirstOrDefault(t => t != null);
                Assert.IsNull(tile, $"[A20/NoOpenDoorwayTopWeb] Open doorway top cell {openTopCell} on TM_SolidRim must be null, got '{tile?.name}'.");
            }

            // 5b. Reviewer Visual Guard: Zero *_CeilStamp* or *_RoofMoss* overlap with any of the 16 carved arch bays
            var ceilingAccents = allRenderers
                .Where(sr => sr != null && sr.sprite != null &&
                             (sr.gameObject.name.Contains("_CeilStamp", StringComparison.Ordinal) ||
                              sr.gameObject.name.Contains("_RoofMoss", StringComparison.Ordinal)))
                .ToArray();
            Assert.IsNotEmpty(ceilingAccents, "[A20/CeilingAccents]");
            foreach (var ca in ceilingAccents)
            {
                foreach (var bay in CarvedBays)
                {
                    var bayBounds = new Bounds();
                    bayBounds.SetMinMax(new Vector3(bay.xMin, bay.yMin, -1f), new Vector3(bay.xMax, bay.yMax, 1f));
                    Assert.IsFalse(OverlapsAabb(ca.bounds, bayBounds, -0.05f, -0.05f),
                        $"[A20/CeilingAccentBayOverlap] '{ca.gameObject.name}' ({ca.bounds.min:F2}..{ca.bounds.max:F2}) overlaps carved Bay #{bay.bayIdx} ([{bay.xMin}..{bay.xMax}], [{bay.yMin}..{bay.yMax}]).");
                }
            }

            // 6. Task 3.4: Full-height peripheral Foreground_Silhouettes columns (>= 4 FgColumn_* spanning min.y <= -5.8f && max.y >= 8.8f, zero center clutter |x| < 12f)
            var fgRoot = GameObject.Find("Foreground_Silhouettes");
            Assert.IsNotNull(fgRoot, "[A20/ForegroundSilhouettes]");
            var fgColumns = new List<Transform>();
            foreach (Transform child in fgRoot.transform)
            {
                Assert.IsFalse(child.name.StartsWith("FgBeam_", StringComparison.Ordinal) || child.name.StartsWith("FgChain_", StringComparison.Ordinal),
                    $"[A20/NoFgStub] Legacy severed foreground stub '{child.name}' must be removed.");
                if (child.name.StartsWith("FgColumn_", StringComparison.Ordinal))
                    fgColumns.Add(child);
            }
            Assert.GreaterOrEqual(fgColumns.Count, 4, "[A20/FgColumnCount] Expected >= 4 full-height peripheral FgColumn_* columns.");
            foreach (var col in fgColumns)
            {
                Bounds colBounds = GetVisualSpriteBounds(col.gameObject);
                float relCenterX = colBounds.center.x - fgRoot.transform.position.x;
                Assert.GreaterOrEqual(Mathf.Abs(relCenterX), 12.0f,
                    $"[A20/FgColumnClearCenter/{col.name}] FgColumn must stay outside center |x| < 12f, got {relCenterX:F2}.");
                Assert.LessOrEqual(colBounds.min.y, -5.8f,
                    $"[A20/FgColumnMinY/{col.name}] min.y={colBounds.min.y:F2} must extend <= -5.8f.");
                Assert.GreaterOrEqual(colBounds.max.y, 8.8f,
                    $"[A20/FgColumnMaxY/{col.name}] max.y={colBounds.max.y:F2} must extend >= 8.8f.");
            }
            Bounds allFgBounds = GetVisualSpriteBounds(fgRoot);
            Assert.LessOrEqual(allFgBounds.min.x, -36.0f, $"[A20/FgSpanMinX] min.x={allFgBounds.min.x:F2} must be <= -36.0f.");
            Assert.GreaterOrEqual(allFgBounds.max.x, 36.0f, $"[A20/FgSpanMaxX] max.x={allFgBounds.max.x:F2} must be >= 36.0f.");

            // 7. Task 3.5: Global Mixel & Pixel-Density Guard across all Rooms/Room_*/Art SpriteRenderers
            foreach (var prefabName in RoomPrefabNames)
            {
                var artGo = GameObject.Find($"Rooms/Room_{prefabName}/Art");
                Assert.IsNotNull(artGo, $"[A20/MissingArtRoot/{prefabName}]");
                var artRenderers = artGo.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in artRenderers)
                {
                    if (sr == null)
                        continue;
                    string objName = sr.gameObject.name;
                    Assert.IsNotNull(sr.sprite, $"[A20/NullSprite/{prefabName}/{objName}] SpriteRenderer under Art has null sprite.");

                    if (IsExcludedFromMixelScale(objName))
                        continue;

                    float absScaleX = Mathf.Abs(sr.transform.localScale.x);
                    float absScaleY = Mathf.Abs(sr.transform.localScale.y);
                    Assert.That(absScaleX, Is.InRange(0.78f, 1.25f),
                        $"[A20/MixelScaleX/{prefabName}/{objName}] localScale.x={absScaleX:F3} must be in [0.78, 1.25] (sprite='{sr.sprite.name}').");
                    Assert.That(absScaleY, Is.InRange(0.78f, 1.25f),
                        $"[A20/MixelScaleY/{prefabName}/{objName}] localScale.y={absScaleY:F3} must be in [0.78, 1.25] (sprite='{sr.sprite.name}').");
                    Assert.AreEqual(24f, sr.sprite.pixelsPerUnit, 0.01f,
                        $"[A20/PPU24/{prefabName}/{objName}] Sprite '{sr.sprite.name}' pixelsPerUnit={sr.sprite.pixelsPerUnit} must equal 24.");

                    if (IsExcludedFromRoomVerticalBounds(sr))
                        continue;

                    Assert.IsTrue(TryResolveRoomSpan(prefabName, sr, out var room),
                        $"[A20/RoomResolve/{prefabName}/{objName}] Could not resolve owning room for '{objName}' at {sr.bounds.center}.");
                    Assert.GreaterOrEqual(sr.bounds.min.y, room.FloorY - 0.15f,
                        $"[A20/RoomFloorOverflow/{room.Tag}/{objName}] min.y={sr.bounds.min.y:F2} protrudes below Room {room.Tag} floorY={room.FloorY:F2}.");
                    Assert.LessOrEqual(sr.bounds.max.y, room.CeilingY + 0.15f,
                        $"[A20/RoomCeilingOverflow/{room.Tag}/{objName}] max.y={sr.bounds.max.y:F2} protrudes above Room {room.Tag} ceilingY={room.CeilingY:F2}.");
                }
            }
        }

        [Test]
        public void A21_Round9_PlayerReadabilityWindowDepthFogAndPolish()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. Task 3.6a: Player/PlayerRimLight Light2D enabled, intensity in [0.45, 0.75], excludes ParallaxFar/ParallaxMid/Foreground
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "[A21/Player]");
            var rimTr = player.transform.Find("PlayerRimLight");
            Assert.IsNotNull(rimTr, "[A21/PlayerRimLightMissing]");
            var rimLight = rimTr.GetComponent<Light2D>();
            Assert.IsNotNull(rimLight, "[A21/PlayerRimLightComponent]");
            Assert.IsTrue(rimLight.enabled, "[A21/PlayerRimLightEnabled]");
            Assert.AreEqual(Light2D.LightType.Point, rimLight.lightType, "[A21/PlayerRimLightType]");
            Assert.That(rimLight.intensity, Is.InRange(0.45f, 0.75f),
                $"[A21/PlayerRimLightIntensity] Expected [0.45, 0.75], got {rimLight.intensity:F2}.");

            int farLayerId = SortingLayer.NameToID("ParallaxFar");
            int midLayerId = SortingLayer.NameToID("ParallaxMid");
            int fgLayerId = SortingLayer.NameToID("Foreground");
            var rimLayersProp = new SerializedObject(rimLight).FindProperty("m_ApplyToSortingLayers");
            Assert.IsNotNull(rimLayersProp, "[A21/PlayerRimLightSortingLayers]");
            var rimAppliedLayers = Enumerable.Range(0, rimLayersProp.arraySize)
                .Select(i => rimLayersProp.GetArrayElementAtIndex(i).intValue)
                .ToArray();
            CollectionAssert.DoesNotContain(rimAppliedLayers, farLayerId, "[A21/PlayerRimLightExcludesParallaxFar]");
            CollectionAssert.DoesNotContain(rimAppliedLayers, midLayerId, "[A21/PlayerRimLightExcludesParallaxMid]");
            CollectionAssert.DoesNotContain(rimAppliedLayers, fgLayerId, "[A21/PlayerRimLightExcludesForeground]");

            // 2. Task 3.6b: Art_A_Torch clears Bay #0 right edge (x=12) and TP_0_Spawn (x=15.5) by >= 1.2f each
            var torchA = GameObject.Find("Art_A_Torch");
            var tp0 = GameObject.Find("TP_0_Spawn");
            Assert.IsNotNull(torchA, "[A21/Art_A_Torch]");
            Assert.IsNotNull(tp0, "[A21/TP_0_Spawn]");
            Assert.GreaterOrEqual(torchA.transform.position.x - 12f, 1.2f,
                $"[A21/TorchAClearanceBay0] Art_A_Torch x={torchA.transform.position.x:F2} must sit >= 1.2u clear of Bay #0 right edge x=12.");
            Assert.GreaterOrEqual(tp0.transform.position.x - torchA.transform.position.x, 1.2f,
                $"[A21/TorchAClearanceTP0] Art_A_Torch x={torchA.transform.position.x:F2} must sit >= 1.2u clear of TP_0_Spawn x={tp0.transform.position.x:F2}.");

            // 3. Task 3.6b: >= 40 Art_*_SlabStrata_* renderers on Ground / -4
            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            var slabStrata = allRenderers
                .Where(sr => sr != null &&
                             sr.gameObject.name.StartsWith("Art_", StringComparison.Ordinal) &&
                             sr.gameObject.name.Contains("_SlabStrata_", StringComparison.Ordinal))
                .ToArray();
            Assert.GreaterOrEqual(slabStrata.Length, 40,
                $"[A21/SlabStrataCount] Expected >= 40 inter-tier Art_*_SlabStrata_* renderers, found {slabStrata.Length}.");
            foreach (var sr in slabStrata)
            {
                Assert.IsNotNull(sr.sprite, $"[A21/SlabStrataSprite/{sr.name}]");
                Assert.AreEqual("prisonSlabStrata_00", sr.sprite.name, $"[A21/SlabStrataName/{sr.name}]");
                Assert.AreEqual("Ground", sr.sortingLayerName, $"[A21/SlabStrataLayer/{sr.name}]");
                Assert.AreEqual(-4, sr.sortingOrder, $"[A21/SlabStrataOrder/{sr.name}]");
            }

            // 4. Task 3.6b: Art_G_Alcove center (60.5f, 10.4f) has a Point Light2D within <= 0.50f * outerRadius
            var activePointLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
                .Where(l => l.enabled && l.lightType == Light2D.LightType.Point && l.intensity > 0.01f && l.pointLightOuterRadius > 0.1f)
                .ToArray();
            Vector2 alcoveCenter = new Vector2(60.5f, 10.4f);
            Assert.IsTrue(
                activePointLights.Any(l => Vector2.Distance(alcoveCenter, l.transform.position) <= 0.50f * l.pointLightOuterRadius),
                "[A21/Art_G_AlcoveLight] Expected an enabled Point Light2D within <= 0.50f * outerRadius of Art_G_Alcove center (60.5, 10.4).");

            // 5. Task 3.1: Window variants (prisonArchJamb_00..02, prisonArchSill_00..02, prisonArchSpandrel_00, prisonArchMullion_00) & ArchMoonSpill_* across all 16 bays
            var seenJambVariants = new HashSet<string>(StringComparer.Ordinal);
            var seenSillVariants = new HashSet<string>(StringComparer.Ordinal);
            for (int bayIndex = 0; bayIndex <= 15; bayIndex++)
            {
                var spandrelTL = GameObject.Find($"ArchSpandrel_TL_{bayIndex}")?.GetComponent<SpriteRenderer>();
                var spandrelTR = GameObject.Find($"ArchSpandrel_TR_{bayIndex}")?.GetComponent<SpriteRenderer>();
                Assert.AreEqual("prisonArchSpandrel_00", spandrelTL?.sprite?.name, $"[A21/Bay{bayIndex}/SpandrelTL]");
                Assert.AreEqual("prisonArchSpandrel_00", spandrelTR?.sprite?.name, $"[A21/Bay{bayIndex}/SpandrelTR]");

                var mullionMatches = allRenderers
                    .Where(sr => sr != null && sr.gameObject.name.StartsWith($"ArchMullion_{bayIndex}_", StringComparison.Ordinal))
                    .ToArray();
                Assert.IsNotEmpty(mullionMatches, $"[A21/Bay{bayIndex}/MullionMissing]");
                foreach (var msr in mullionMatches)
                    Assert.AreEqual("prisonArchMullion_00", msr.sprite?.name, $"[A21/Bay{bayIndex}/MullionSprite/{msr.name}]");

                var baySills = allRenderers
                    .Where(sr => sr != null &&
                                 (sr.gameObject.name.StartsWith($"ArchSill_{bayIndex}_", StringComparison.Ordinal) ||
                                  sr.gameObject.name.StartsWith($"ArchLintel_{bayIndex}_", StringComparison.Ordinal)))
                    .ToArray();
                Assert.IsNotEmpty(baySills, $"[A21/Bay{bayIndex}/SillLintelMissing]");
                foreach (var ssr in baySills)
                {
                    Assert.IsNotNull(ssr.sprite, $"[A21/Bay{bayIndex}/SillSprite/{ssr.name}]");
                    StringAssert.StartsWith("prisonArchSill_0", ssr.sprite.name, $"[A21/Bay{bayIndex}/SillPrefix/{ssr.name}]");
                    seenSillVariants.Add(ssr.sprite.name);
                }

                var bayJambs = allRenderers
                    .Where(sr => sr != null &&
                                 (sr.gameObject.name.StartsWith($"ArchJamb_L_{bayIndex}_", StringComparison.Ordinal) ||
                                  sr.gameObject.name.StartsWith($"ArchJamb_R_{bayIndex}_", StringComparison.Ordinal)))
                    .ToArray();
                Assert.IsNotEmpty(bayJambs, $"[A21/Bay{bayIndex}/JambMissing]");
                foreach (var jsr in bayJambs)
                {
                    Assert.IsNotNull(jsr.sprite, $"[A21/Bay{bayIndex}/JambSprite/{jsr.name}]");
                    StringAssert.StartsWith("prisonArchJamb_0", jsr.sprite.name, $"[A21/Bay{bayIndex}/JambPrefix/{jsr.name}]");
                    seenJambVariants.Add(jsr.sprite.name);
                }

                var moonLightGo = GameObject.Find($"ArchMoonSpill_{bayIndex}");
                Assert.IsNotNull(moonLightGo, $"[A21/Bay{bayIndex}/ArchMoonSpillMissing]");
                var moonLight = moonLightGo.GetComponent<Light2D>();
                Assert.IsNotNull(moonLight, $"[A21/Bay{bayIndex}/ArchMoonSpillLight]");
                Assert.IsTrue(moonLight.enabled, $"[A21/Bay{bayIndex}/ArchMoonSpillEnabled]");
                Assert.AreEqual(Light2D.LightType.Point, moonLight.lightType, $"[A21/Bay{bayIndex}/ArchMoonSpillType]");
                Assert.Greater(moonLight.intensity, 0.1f, $"[A21/Bay{bayIndex}/ArchMoonSpillIntensity]");
            }
            CollectionAssert.IsSupersetOf(seenSillVariants, new[] { "prisonArchSill_00", "prisonArchSill_01", "prisonArchSill_02" },
                "[A21/SillVariants] All 3 weathered sill variants prisonArchSill_00..02 must appear across bays.");
            CollectionAssert.IsSupersetOf(seenJambVariants, new[] { "prisonArchJamb_00", "prisonArchJamb_01", "prisonArchJamb_02" },
                "[A21/JambVariants] All 3 weathered jamb variants prisonArchJamb_00..02 must appear across bays.");

            // 6. Task 3.2: All 9 GroundMist_R5 strips have vertex color.a >= 0.80f & Mat_GroundMist_Prison.mat _MistColor.a >= 0.55f
            var mistRoot = GameObject.Find("GroundMist_R5");
            Assert.IsNotNull(mistRoot, "[A21/GroundMistRoot]");
            var mistRenderers = mistRoot.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(9, mistRenderers.Length, "[A21/GroundMistCount]");
            foreach (var mr in mistRenderers)
                Assert.GreaterOrEqual(mr.color.a, 0.80f - 1e-4f, $"[A21/MistAlpha/{mr.name}]");

            const string mistMatPath = "Assets/_Project/Art/Materials/Environment/Mat_GroundMist_Prison.mat";
            var mistMat = AssetDatabase.LoadAssetAtPath<Material>(mistMatPath);
            Assert.IsNotNull(mistMat, $"[A21/MistMaterialMissing] {mistMatPath}");
            Assert.GreaterOrEqual(mistMat.GetColor("_MistColor").a, 0.55f - 1e-4f,
                "[A21/MistMaterialAlpha] Mat_GroundMist_Prison _MistColor.a must be >= 0.55f.");
        }

        [Test]
        public void A22_Round9PostReview_SpawnCleanlinessFrustumSilhouettesAndFullGravity()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);

            // 1. PlayerSpawnHead hidden in EditMode + PlayerRimLight preserves m_NormalMapQuality == 0 after BuildAll()
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "[A22/Player]");
            var spawnHead = player.GetComponentInChildren<PlayerSpawnHead>(true);
            Assert.IsNotNull(spawnHead, "[A22/PlayerSpawnHead]");
            var spawnHeadSr = spawnHead.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(spawnHeadSr, "[A22/PlayerSpawnHeadSr]");
            Assert.IsFalse(spawnHeadSr.enabled, "[A22/SpawnHeadHiddenAtRest] PlayerSpawnHead SpriteRenderer must be disabled in EditMode.");
            var spawnTrail = spawnHead.GetComponentInChildren<TrailRenderer>(true);
            if (spawnTrail != null)
                Assert.IsFalse(spawnTrail.emitting, "[A22/SpawnTrailOffAtRest]");

            var rimLight = player.transform.Find("PlayerRimLight")?.GetComponent<Light2D>();
            Assert.IsNotNull(rimLight, "[A22/PlayerRimLight]");
            Assert.AreEqual(0, new SerializedObject(rimLight).FindProperty("m_NormalMapQuality").intValue,
                "[A22/PlayerRimLightNoNormalMap] BuildAll() must preserve m_NormalMapQuality=0 on PlayerRimLight.");

            // 2. Foreground_Silhouettes frustum-edge pinning keeps interior clear across all 15 Round 9 camera shots at 1:1 scale
            var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            var fgLayer = GameObject.Find("Prison_Parallax_R5/Foreground_Silhouettes")?.GetComponent<ParallaxLayer2D>();
            Assert.IsNotNull(fgLayer, "[A22/FgParallax]");
            Assert.IsTrue(new SerializedObject(fgLayer).FindProperty("_pinColumnsToCameraFrustumEdges").boolValue, "[A22/FgFrustumPinEnabled]");

            (Vector3 pos, float ortho)[] shots =
            {
                (new Vector3(18f, 3.0f, -10f), 5.8f), (new Vector3(47.0f, 9.8f, -10f), 10.2f),
                (new Vector3(66f, 5.5f, -10f), 6.2f), (new Vector3(-8f, 19.5f, -10f), 6.0f),
                (new Vector3(8f, 29.5f, -10f), 6.5f), (new Vector3(122f, 11.5f, -10f), 7.0f),
                (new Vector3(198f, -10.5f, -10f), 6.8f), (new Vector3(-66f, 27.5f, -10f), 7.0f),
                (new Vector3(72f, 38.0f, -10f), 7.0f), (new Vector3(14.0f, 12.5f, -10f), 5.8f),
                (new Vector3(9.0f, 2.8f, -10f), 5.2f), (new Vector3(148.5f, 13.5f, -10f), 7.8f),
                (new Vector3(119.0f, 45.0f, -10f), 6.8f), (new Vector3(-49.5f, 26.8f, -10f), 11.2f),
                (new Vector3(58.5f, 31.0f, -10f), 8.8f),
            };
            float origOrtho = cam.orthographicSize;
            Vector3 origCamPos = cam.transform.position;
            Vector3 origFgPos = fgLayer.transform.position;
            try
            {
                cam.aspect = 16f / 9f;
                foreach (var (pos, ortho) in shots)
                {
                    cam.transform.position = pos;
                    cam.orthographicSize = ortho;
                    fgLayer.UpdateParallax(pos);
                    float halfW = ortho * cam.aspect;
                    float clearZoneHalfW = halfW - 0.55f;
                    foreach (Transform col in fgLayer.transform)
                    {
                        if (!col.name.StartsWith("FgColumn_", StringComparison.Ordinal))
                            continue;
                        Assert.AreEqual(Vector3.one, col.localScale, $"[A22/FgNoMixelStretch/{col.name}]");
                        Bounds cb = GetVisualSpriteBounds(col.gameObject);
                        bool intrudes = cb.max.x > pos.x - clearZoneHalfW && cb.min.x < pos.x + clearZoneHalfW;
                        Assert.IsFalse(intrudes,
                            $"[A22/FgIntrudesInterior] '{col.name}' ({cb.min.x:F2}..{cb.max.x:F2}) intrudes [{pos.x - clearZoneHalfW:F2}..{pos.x + clearZoneHalfW:F2}] at cam={pos}, ortho={ortho}.");
                        if (Mathf.Abs(cb.center.x - pos.x) < halfW + 5f)
                        {
                            Assert.LessOrEqual(cb.min.y, pos.y - ortho, $"[A22/FgCoversBottom/{col.name}] ortho={ortho}");
                            Assert.GreaterOrEqual(cb.max.y, pos.y + ortho, $"[A22/FgCoversTop/{col.name}] ortho={ortho}");
                        }
                    }
                }
            }
            finally
            {
                cam.ResetAspect();
                cam.orthographicSize = origOrtho;
                cam.transform.position = origCamPos;
                fgLayer.transform.position = origFgPos;
                fgLayer.ResetFrustumPinnedColumnsToRest();
            }

            // 3. Room A Spawn Vat + Pipes disjoint from Art_A_Cage & ceiling accents + lit by Art_A_VatFillLight
            var pipesGo = GameObject.Find("Art_A_HomonculusPipes");
            var fountainGo = GameObject.Find("Art_A_Fountain");
            var cageAGo = GameObject.Find("Art_A_Cage");
            var vatLight = GameObject.Find("Art_A_VatFillLight")?.GetComponent<Light2D>();
            Assert.IsNotNull(pipesGo, "[A22/Art_A_HomonculusPipes]");
            Assert.IsNotNull(cageAGo, "[A22/Art_A_Cage]");
            Assert.IsNotNull(vatLight, "[A22/Art_A_VatFillLight]");
            Assert.IsTrue(vatLight.enabled && vatLight.intensity >= 0.40f, "[A22/VatFillLightActive]");
            Bounds pipesBounds = GetVisualSpriteBounds(pipesGo);
            Bounds cageBounds = GetVisualSpriteBounds(cageAGo);
            Assert.IsFalse(OverlapsAabb(pipesBounds, cageBounds, -0.02f, -0.02f), "[A22/SpawnVatCageClear]");
            Assert.IsFalse(OverlapsAabb(GetVisualSpriteBounds(fountainGo), cageBounds, -0.02f, -0.02f), "[A22/SpawnFountainCageClear]");

            var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            foreach (var sr in allRenderers.Where(r => r != null && (r.name.StartsWith("Art_A_CeilStamp", StringComparison.Ordinal) || r.name.StartsWith("Art_A_RoofMoss", StringComparison.Ordinal))))
                Assert.IsFalse(OverlapsAabb(sr.bounds, pipesBounds, -0.02f, -0.02f), $"[A22/SpawnPipesCeilAccentClear/{sr.name}]");

            // 4. Every SpawnerPillar column under Rooms/Room_*/Art spans floorY..ceilingY (zero mid-wall truncated pillars)
            (string propName, float floorY, float ceilingY)[] fullHeightSpawnerPillars =
            {
                ("Art_N_Pillar1", 8f, 22.5f),
                ("Art_N_Pillar2", 8f, 22.5f),
                ("Art_O_Pillar1", -16f, -1f),
                ("Art_Q_SkeletonPier", 16f, 23f),
                ("Art_R_Pillar1", 24f, 38.5f),
                ("Art_T_Pillar2", 40f, 62.5f),
            };
            foreach (var (propName, floorY, ceilingY) in fullHeightSpawnerPillars)
            {
                var go = GameObject.Find(propName);
                Assert.IsNotNull(go, $"[A22/PillarMissing/{propName}]");
                Bounds pb = GetVisualSpriteBounds(go);
                Assert.AreEqual(floorY, pb.min.y, 0.15f, $"[A22/PillarFloor/{propName}]");
                Assert.AreEqual(ceilingY, pb.max.y, 0.15f, $"[A22/PillarCeiling/{propName}] Truncated mid-wall pillar detected.");
            }

            // 5. Room S Art_S_AlchemyBibli1 & Art_S_AlchemyBibli2 ceiling chains reach y=39.0 clear of Bay #14 [47..55, 34..38]
            Bounds bay14 = new Bounds(new Vector3(51f, 36f, 0f), new Vector3(8f, 4f, 2f));
            foreach (string bibliName in new[] { "Art_S_AlchemyBibli1", "Art_S_AlchemyBibli2" })
            {
                string chainPrefix = bibliName + "_Chain_";
                var chainSegs = allRenderers.Where(sr => sr != null && sr.gameObject.name.StartsWith(chainPrefix, StringComparison.Ordinal)).ToArray();
                Assert.IsNotEmpty(chainSegs, $"[A22/BibliChainMissing/{bibliName}]");
                Assert.LessOrEqual(chainSegs.Min(s => s.bounds.min.y), 33.15f, $"[A22/BibliChainBottom/{bibliName}]");
                Assert.That(chainSegs.Max(s => s.bounds.max.y), Is.InRange(38.85f, 39.15f), $"[A22/BibliChainCeiling/{bibliName}]");
                foreach (var seg in chainSegs)
                    Assert.IsFalse(OverlapsAabb(seg.bounds, bay14, -0.02f, -0.02f), $"[A22/BibliChainBay14Clash/{seg.name}]");
            }
        }

        private readonly struct RoomVerticalSpan
        {
            public readonly string Tag;
            public readonly float XMin;
            public readonly float XMax;
            public readonly float FloorY;
            public readonly float CeilingY;

            public RoomVerticalSpan(string tag, float xMin, float xMax, float floorY, float ceilingY)
            {
                Tag = tag;
                XMin = xMin;
                XMax = xMax;
                FloorY = floorY;
                CeilingY = ceilingY;
            }
        }

        private static readonly (int bayIdx, float xMin, float xMax, float yMin, float yMax)[] CarvedBays =
        {
            (0, 8f, 12f, 2f, 5f), (1, 46f, 52f, 11f, 16f), (2, 69f, 74f, 2f, 6f), (3, -15f, -9f, 18f, 21f),
            (4, 6f, 10f, 25f, 30f), (5, 118f, 126f, 14f, 19f), (6, 100f, 106f, -13f, -8f), (7, -60f, -54f, 30f, 35f),
            (8, 66f, 74f, 46f, 52f), (9, 97f, 102f, 2f, 6f), (10, 138f, 144f, 18f, 22f), (11, 140f, 148f, -9f, -3f),
            (12, 187f, 195f, -9f, -3f), (13, -75f, -69f, 18f, 22f), (14, 47f, 55f, 34f, 38f), (15, 96f, 104f, 47f, 55f),
        };

        private static readonly string[] RoomPrefabNames =
        {
            "AB", "CD", "G", "HJK", "ELF",
            "W1_EastCourtyard", "W2_SewerCistern", "W3_WestBastion", "U1_UpperPromenade",
        };

        private static readonly Dictionary<string, RoomVerticalSpan[]> RoomSpansByPrefab = new Dictionary<string, RoomVerticalSpan[]>(StringComparer.Ordinal)
        {
            ["AB"] = new[]
            {
                new RoomVerticalSpan("A", 0f, 18f, 0f, 5.5f),
                new RoomVerticalSpan("B", 18f, 42f, 0f, 6f),
            },
            ["CD"] = new[]
            {
                new RoomVerticalSpan("C", 42f, 56f, 0f, 18.5f),
                new RoomVerticalSpan("D", 22f, 42f, 8f, 13.5f),
            },
            ["G"] = new[]
            {
                new RoomVerticalSpan("G", 56f, 76f, 0f, 14.5f),
            },
            ["HJK"] = new[]
            {
                new RoomVerticalSpan("H", 6f, 22f, 8f, 22.5f),
                new RoomVerticalSpan("J", -16f, 6f, 16f, 22.5f),
                new RoomVerticalSpan("K", -30f, -16f, 16f, 30.5f),
            },
            ["ELF"] = new[]
            {
                new RoomVerticalSpan("E", -16f, 22f, 24f, 31f),
                new RoomVerticalSpan("L", 22f, 38f, 24f, 38.5f),
                new RoomVerticalSpan("F", 0f, 22f, 32f, 38f),
            },
            ["W1_EastCourtyard"] = new[]
            {
                new RoomVerticalSpan("M", 76f, 164f, 0f, 7f),
                new RoomVerticalSpan("N", 76f, 164f, 8f, 22.5f),
            },
            ["W2_SewerCistern"] = new[]
            {
                new RoomVerticalSpan("O", 76f, 164f, -16f, -1f),
                new RoomVerticalSpan("P", 164f, 234f, -16f, -1.5f),
            },
            ["W3_WestBastion"] = new[]
            {
                new RoomVerticalSpan("Q", -102f, -30f, 16f, 23f),
                new RoomVerticalSpan("R", -102f, -30f, 24f, 38.5f),
            },
            ["U1_UpperPromenade"] = new[]
            {
                new RoomVerticalSpan("S", 38f, 92f, 24f, 39f),
                new RoomVerticalSpan("T", 38f, 140f, 40f, 62.5f),
            },
        };

        private static bool OverlapsAabb(Bounds a, Bounds b, float padX, float padY)
        {
            return a.min.x < b.max.x + padX && a.max.x > b.min.x - padX &&
                   a.min.y < b.max.y + padY && a.max.y > b.min.y - padY;
        }

        private static bool IsExcludedFromMixelScale(string objName)
        {
            return objName == "GodRay" ||
                   objName == "LanternFlameCore_R5" ||
                   objName.Contains("Glow", StringComparison.Ordinal) ||
                   objName.StartsWith("Beam_", StringComparison.Ordinal) ||
                   objName.StartsWith("Plank_", StringComparison.Ordinal);
        }

        private static bool IsExcludedFromRoomVerticalBounds(SpriteRenderer sr)
        {
            string n = sr.gameObject.name;
            string pn = sr.transform.parent != null ? sr.transform.parent.name : string.Empty;

            if (n.Contains("_BedrockStone_", StringComparison.Ordinal) ||
                n.Contains("_StairCoreStone_", StringComparison.Ordinal) ||
                n.Contains("_SlabStrata_", StringComparison.Ordinal))
            {
                return true;
            }

            if (pn == "Art_I7_Chain" || pn == "Art_I8_Chain" || pn == "Art_I9_Chain" || pn == "Art_I10_Chain")
                return true;

            if (n.Contains("WellBridgeBrace", StringComparison.Ordinal) ||
                n.Contains("WellCapBrace", StringComparison.Ordinal) ||
                n.Contains("ShaftCapBrace", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        private static bool TryResolveRoomSpan(string prefabName, SpriteRenderer sr, out RoomVerticalSpan resolved)
        {
            resolved = default;
            if (!RoomSpansByPrefab.TryGetValue(prefabName, out var candidates) || candidates.Length == 0)
                return false;
            if (candidates.Length == 1)
            {
                resolved = candidates[0];
                return true;
            }

            string n = sr.gameObject.name;
            string pn = sr.transform.parent != null ? sr.transform.parent.name : string.Empty;
            foreach (var c in candidates)
            {
                string prefix = $"Art_{c.Tag}_";
                if (n.StartsWith(prefix, StringComparison.Ordinal) || pn.StartsWith(prefix, StringComparison.Ordinal))
                {
                    resolved = c;
                    return true;
                }
            }

            Vector3 center = sr.bounds.center;
            float bestScore = float.MaxValue;
            bool found = false;
            foreach (var c in candidates)
            {
                if (center.x < c.XMin - 0.5f || center.x > c.XMax + 0.5f)
                    continue;
                float dy = center.y < c.FloorY ? c.FloorY - center.y : (center.y > c.CeilingY ? center.y - c.CeilingY : 0f);
                if (dy < bestScore)
                {
                    bestScore = dy;
                    resolved = c;
                    found = true;
                }
            }
            return found;
        }

        private static Bounds GetVisualSpriteBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(sr => sr != null && sr.sprite != null &&
                             sr.gameObject.name != "GodRay" &&
                             !sr.gameObject.name.StartsWith("ScrollAura_", StringComparison.Ordinal))
                .ToArray();
            Assert.IsNotEmpty(renderers, $"Expected visual SpriteRenderer on '{go.name}'.");
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static void AssertZone(EncounterZone[] zones, string id, Vector2 min, Vector2 max, params string[] enemyNames)
        {
            var z = zones.SingleOrDefault(x => x.ZoneId == id);
            Assert.IsNotNull(z, $"[A07/Zone/{id}] Missing zone.");
            Assert.AreEqual(min.x, z.MinBounds.x, GeometryAuditHelper.Eps, $"[A07/Zone/{id}/minX]");
            Assert.AreEqual(min.y, z.MinBounds.y, GeometryAuditHelper.Eps, $"[A07/Zone/{id}/minY]");
            Assert.AreEqual(max.x, z.MaxBounds.x, GeometryAuditHelper.Eps, $"[A07/Zone/{id}/maxX]");
            Assert.AreEqual(max.y, z.MaxBounds.y, GeometryAuditHelper.Eps, $"[A07/Zone/{id}/maxY]");
            CollectionAssert.AreEquivalent(enemyNames, z.Enemies.Select(e => e.name).ToArray(), $"[A07/Zone/{id}/Members]");
        }
    }
}
