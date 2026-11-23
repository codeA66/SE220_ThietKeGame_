using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class MapGeometryTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A06_AssembledScene_MatchesEnvelopeFloorUnionsAll11OpeningsAndZeroSeamOverlaps()
        {
            EditorSceneManager.OpenScene(GeometryAuditHelper.ScenePath, OpenSceneMode.Single);
            Physics2D.SyncTransforms();

            var gate = Object.FindAnyObjectByType<EncounterGate>();
            Assert.IsNotNull(gate, "[A06] Missing EncounterGate I4.");
            var gateBlocker = gate.GateBarrierRoot != null ? gate.GateBarrierRoot.GetComponent<BoxCollider2D>() : null;
            Assert.IsNotNull(gateBlocker, "[A06] Missing I4 GateBarrier BoxCollider2D.");

            var switchDoors = Object.FindObjectsByType<SwitchDoor>(FindObjectsInactive.Include);
            Assert.AreEqual(3, switchDoors.Length, "[A06] Expected 3 SwitchDoor instances (SD_1, SD_2, SD_3).");
            var sd1 = switchDoors.SingleOrDefault(sd => sd.name == "SD_1_EastVault");
            var sd2 = switchDoors.SingleOrDefault(sd => sd.name == "SD_2_SewerExit");
            var sd3 = switchDoors.SingleOrDefault(sd => sd.name == "SD_3_WestArmory");
            Assert.IsNotNull(sd1, "[A06] Missing SwitchDoor SD_1_EastVault.");
            Assert.IsNotNull(sd2, "[A06] Missing SwitchDoor SD_2_SewerExit.");
            Assert.IsNotNull(sd3, "[A06] Missing SwitchDoor SD_3_WestArmory.");
            var sd1Blocker = sd1.DoorBarrierRoot != null ? sd1.DoorBarrierRoot.GetComponent<BoxCollider2D>() : null;
            var sd2Blocker = sd2.DoorBarrierRoot != null ? sd2.DoorBarrierRoot.GetComponent<BoxCollider2D>() : null;
            var sd3Blocker = sd3.DoorBarrierRoot != null ? sd3.DoorBarrierRoot.GetComponent<BoxCollider2D>() : null;
            Assert.IsNotNull(sd1Blocker, "[A06] Missing SD_1_EastVault DoorBarrier BoxCollider2D.");
            Assert.IsNotNull(sd2Blocker, "[A06] Missing SD_2_SewerExit DoorBarrier BoxCollider2D.");
            Assert.IsNotNull(sd3Blocker, "[A06] Missing SD_3_WestArmory DoorBarrier BoxCollider2D.");

            // Collect all static world geometry colliders (Ground + OneWayPlatform)
            var worldCols = Object.FindObjectsByType<Collider2D>()
                .Where(c => !c.isTrigger && (c.gameObject.layer == GeometryAuditHelper.LayerGround || c.gameObject.layer == GeometryAuditHelper.LayerOneWay))
                .ToArray();

            // 1. Outer physical envelope: (-102.5, -17.0) to (234.5, 63.0)
            float minX = worldCols.Min(c => GeometryAuditHelper.GetWorldBounds(c).min.x);
            float maxX = worldCols.Max(c => GeometryAuditHelper.GetWorldBounds(c).max.x);
            float minY = worldCols.Min(c => GeometryAuditHelper.GetWorldBounds(c).min.y);
            float maxY = worldCols.Max(c => GeometryAuditHelper.GetWorldBounds(c).max.y);
            Assert.AreEqual(-102.5f, minX, GeometryAuditHelper.Eps, "[A06/Envelope/minX]");
            Assert.AreEqual(234.5f, maxX, GeometryAuditHelper.Eps, "[A06/Envelope/maxX]");
            Assert.AreEqual(-17.0f, minY, GeometryAuditHelper.Eps, "[A06/Envelope/minY]");
            Assert.AreEqual(63.0f, maxY, GeometryAuditHelper.Eps, "[A06/Envelope/maxY]");

            // 2. Seven floor unions (T0..T6)
            GeometryAuditHelper.AssertFloorUnionCovers(worldCols, "A06/T0", -16f, 76f, 234f);
            GeometryAuditHelper.AssertFloorUnionCovers(worldCols, "A06/T1", 0f, 0f, 164f);
            GeometryAuditHelper.AssertFloorUnionCovers(worldCols, "A06/T2", 8f, 18f, 164f);
            GeometryAuditHelper.AssertFloorUnionCovers(worldCols, "A06/T3", 16f, -102f, 10f);
            GeometryAuditHelper.AssertFloorUnionCovers(worldCols, "A06/T4", 24f, -102f, 92f);
            GeometryAuditHelper.AssertFloorUnionCovers(worldCols, "A06/T5", 32f, 0f, 26f);
            GeometryAuditHelper.AssertFloorUnionCovers(worldCols, "A06/T6", 40f, 38f, 140f);

            // 3. All 17 inter-room openings (11 core + 6 wing) + 3 SwitchDoor openings
            var openings = new (string name, float x, float yMin, float yMax, Collider2D exclude)[]
            {
                ("A<->B", 18f, 0f, 4f, null),
                ("B<->C", 42f, 0f, 4f, null),
                ("C<->G_low", 56f, 0f, 4f, null),
                ("C<->G_high", 56f, 8f, 12f, null),
                ("C<->D", 42f, 8f, 12f, null),
                ("D<->H", 22f, 8f, 12f, null),
                ("H<->J", 6f, 16f, 20f, null),
                ("J<->K", -16f, 16f, 20f, null),
                ("K<->E", -16f, 24f, 28f, null),
                ("E<->L", 22f, 24f, 28f, gateBlocker),
                ("L<->F", 22f, 32f, 36f, null),
                ("G<->M_low", 76f, 0f, 4f, null),
                ("G<->N_high", 76f, 8f, 12f, null),
                ("O<->P", 164f, -16f, -12f, null),
                ("K<->Q_low", -30f, 16f, 20f, null),
                ("K<->R_high", -30f, 24f, 28f, null),
                ("L<->S", 38f, 24f, 28f, null),
                ("M_Vault", 150f, 0f, 4f, sd1Blocker),
                ("P_SewerGate", 212f, -16f, -12f, sd2Blocker),
                ("R_Armory", -88f, 24f, 28f, sd3Blocker),
            };
            foreach (var (name, x, yMin, yMax, exclude) in openings)
            {
                GeometryAuditHelper.AssertOpeningClear(worldCols, "A06/Opening/" + name, x, yMin, yMax, exclude);
            }

            // 4. Verify I4 gate blocker seals x=22, y=24..28 and SwitchDoor blockers seal their respective doorways
            var gb = GeometryAuditHelper.GetWorldBounds(gateBlocker);
            Assert.AreEqual(22f, gb.center.x, GeometryAuditHelper.Eps, "[A06/I4/centerX]");
            Assert.AreEqual(24f, gb.min.y, GeometryAuditHelper.Eps, "[A06/I4/minY]");
            Assert.AreEqual(28f, gb.max.y, GeometryAuditHelper.Eps, "[A06/I4/maxY]");

            var sd1Bounds = GeometryAuditHelper.GetWorldBounds(sd1Blocker);
            Assert.AreEqual(150f, sd1Bounds.center.x, GeometryAuditHelper.Eps, "[A06/SD1/centerX]");
            Assert.AreEqual(0f, sd1Bounds.min.y, GeometryAuditHelper.Eps, "[A06/SD1/minY]");
            Assert.AreEqual(4f, sd1Bounds.max.y, GeometryAuditHelper.Eps, "[A06/SD1/maxY]");

            var sd2Bounds = GeometryAuditHelper.GetWorldBounds(sd2Blocker);
            Assert.AreEqual(212f, sd2Bounds.center.x, GeometryAuditHelper.Eps, "[A06/SD2/centerX]");
            Assert.AreEqual(-16f, sd2Bounds.min.y, GeometryAuditHelper.Eps, "[A06/SD2/minY]");
            Assert.AreEqual(-12f, sd2Bounds.max.y, GeometryAuditHelper.Eps, "[A06/SD2/maxY]");

            var sd3Bounds = GeometryAuditHelper.GetWorldBounds(sd3Blocker);
            Assert.AreEqual(-88f, sd3Bounds.center.x, GeometryAuditHelper.Eps, "[A06/SD3/centerX]");
            Assert.AreEqual(24f, sd3Bounds.min.y, GeometryAuditHelper.Eps, "[A06/SD3/minY]");
            Assert.AreEqual(28f, sd3Bounds.max.y, GeometryAuditHelper.Eps, "[A06/SD3/maxY]");

            // 5. Verify all 7 climbable chains exist in assembled scene (P0-02: trimmed to [floorY - 0.20, deckTopY + 0.08])
            var allCols = Object.FindObjectsByType<Collider2D>();
            GeometryAuditHelper.AssertClimbableTrigger(allCols, "A06/I1", 46f, -0.20f, 8.08f);
            GeometryAuditHelper.AssertClimbableTrigger(allCols, "A06/I2", 68f, -0.20f, 8.08f);
            GeometryAuditHelper.AssertClimbableTrigger(allCols, "A06/I6", -24f, 15.80f, 24.08f);
            GeometryAuditHelper.AssertClimbableTrigger(allCols, "A06/I7", 104f, -0.20f, 8.08f);
            GeometryAuditHelper.AssertClimbableTrigger(allCols, "A06/I8", 118f, -16.20f, 0.08f);
            GeometryAuditHelper.AssertClimbableTrigger(allCols, "A06/I9", -66f, 15.80f, 24.08f);
            GeometryAuditHelper.AssertClimbableTrigger(allCols, "A06/I10", 80f, 23.80f, 40.08f);

            // 6. Verify no duplicate colliders on seam x=22 encroaching across tiers or at E-F x=11 y=31..32
            var efShared = worldCols.Where(c =>
            {
                if (c.gameObject.layer != GeometryAuditHelper.LayerGround) return false;
                var b = GeometryAuditHelper.GetWorldBounds(c);
                return b.min.x < 11f && b.max.x > 11f && b.min.y <= 31.5f && b.max.y >= 31.5f;
            }).ToArray();
            Assert.AreEqual(1, efShared.Length, "[A06/EF_Shared] Exactly 1 collider at x=11, y=31..32.");
        }
    }
}
