using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class ELFGeometryTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A05_ELFPrefab_MatchesFloorsPlatformsOverlapCorridorsSharedCeilingAndOpenings()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("ELF");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            // Solid floor E-L, shared Floor F / Ceiling E, separate Ceiling E [-16,0], Ceilings L & F
            GeometryAuditHelper.AssertSolidBox(cols, "A05/Floor_EL", -16f, 38f, 23f, 24f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05/Floor_F_Ceiling_E", 0f, 22f, 31f, 32f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05/Ceiling_E_West", -16f, 0f, 31f, 31.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05/Ceiling_L", 22f, 38.5f, 38.5f, 39f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05/Ceiling_F", -0.5f, 22f, 38f, 38.5f);

            // Single shared collider check at x=11, y=31..32
            var atX11 = cols.Where(c =>
            {
                if (c.isTrigger || c.gameObject.layer != GeometryAuditHelper.LayerGround) return false;
                var b = GeometryAuditHelper.GetWorldBounds(c);
                return b.min.x < 11f && b.max.x > 11f && b.min.y <= 31.5f && b.max.y >= 31.5f;
            }).ToArray();
            Assert.AreEqual(1, atX11.Length, "[A05/SharedCeilingFloor] Exactly 1 collider must span y=31..32 at x=11.");

            // One-way E combat platform + 4 L platforms
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05/OneWay_E_Combat", -4f, 2f, 26f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05/OneWay_L_1", 32f, 38f, 26f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05/OneWay_L_2", 28f, 34f, 28f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05/OneWay_L_3", 24f, 30f, 30f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05/OneWay_L_4", 22f, 26f, 32f);

            // Verify 3 jump-through overlap corridors in L are unobstructed by solid Ground colliders
            var corridors = new[]
            {
                (label: "L1->L2", xMin: 32f, xMax: 34f, yMin: 26.1f, yMax: 29.8f),
                (label: "L2->L3", xMin: 28f, xMax: 30f, yMin: 28.1f, yMax: 31.8f),
                (label: "L3->L4", xMin: 24f, xMax: 26f, yMin: 30.1f, yMax: 34.0f)
            };
            foreach (var cor in corridors)
            {
                var rect = new Rect(cor.xMin + 0.05f, cor.yMin, (cor.xMax - cor.xMin) - 0.1f, cor.yMax - cor.yMin);
                foreach (var c in cols)
                {
                    if (c.isTrigger || c.gameObject.layer != GeometryAuditHelper.LayerGround) continue;
                    var b = GeometryAuditHelper.GetWorldBounds(c);
                    var r = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
                    Assert.IsFalse(r.Overlaps(rect), $"[A05/Corridor/{cor.label}] Blocked by {c.name}.");
                }
            }

            // Openings at x=22: E<->L y=24..28 (clear for I4) and L<->F y=32..36
            GeometryAuditHelper.AssertOpeningClear(cols, "A05/Open_EL_22", 22f, 24f, 28f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05/Open_LF_22", 22f, 32f, 36f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05");
        }
    }
}
