using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class GGeometryTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A03_GPrefab_MatchesLowerFloorThreeSegmentUpperFloorChainAndOpenings()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("G");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            GeometryAuditHelper.AssertSolidBox(cols, "A03/Floor_G_Lower", 56f, 76f, -1f, 0f);
            GeometryAuditHelper.AssertSolidBox(cols, "A03/Ceiling_G", 56f, 76.5f, 14.5f, 15f);

            // Three flush upper floor segments at y=8
            GeometryAuditHelper.AssertSolidBox(cols, "A03/UpperWest", 56f, 65f, 7f, 8f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A03/UpperMidOneWay", 65f, 72f, 8f);
            GeometryAuditHelper.AssertSolidBox(cols, "A03/UpperEast", 72f, 76f, 7f, 8f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A03/UpperUnion", 8f, 56f, 76f);

            // Chain I2 (P0-02: trigger trimmed to [floorY - 0.20, deckTopY + 0.08])
            GeometryAuditHelper.AssertClimbableTrigger(cols, "A03/I2", 68f, -0.20f, 8.08f);

            // G side openings at x=56
            GeometryAuditHelper.AssertOpeningClear(cols, "A03/Open_CG_Low_56", 56f, 0f, 4f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A03/Open_CG_High_56", 56f, 8f, 12f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A03");
        }
    }
}
