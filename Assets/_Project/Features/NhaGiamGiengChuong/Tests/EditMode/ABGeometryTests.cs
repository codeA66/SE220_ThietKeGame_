using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class ABGeometryTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A02_ABPrefab_MatchesFloorCeilingPracticeClearanceAndOpenings()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("AB");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            GeometryAuditHelper.AssertSolidBox(cols, "A02/Floor_AB", 0f, 42f, -1f, 0f);
            GeometryAuditHelper.AssertSolidBox(cols, "A02/Ceiling_A", -0.5f, 18f, 5.5f, 6f);
            GeometryAuditHelper.AssertSolidBox(cols, "A02/Ceiling_B", 18f, 42f, 6f, 6.5f);

            // P0-04 Part A: Room AB is a pure transition corridor, so the zero-purpose practice ledge is gone.
            var oneWays = cols.Where(c => !c.isTrigger && c.gameObject.layer == GeometryAuditHelper.LayerOneWay).ToArray();
            Assert.IsEmpty(oneWays,
                "[A02/NoOneWay] Room AB must carry zero OneWay platforms, found: " +
                string.Join(", ", oneWays.Select(c => c.name)));

            // Openings: A<->B at x=18 y=0..4 and B east end at x=42 y=0..4
            GeometryAuditHelper.AssertOpeningClear(cols, "A02/Open_AB_18", 18f, 0f, 4f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A02/Open_BC_42", 42f, 0f, 4f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A02");
        }
    }
}
