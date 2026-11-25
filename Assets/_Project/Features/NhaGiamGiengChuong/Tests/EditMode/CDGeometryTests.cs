using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class CDGeometryTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A01_CDPrefab_MatchesFloorCeilingOneWayChainAndOpenings()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("CD");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            // Floors & ceilings
            GeometryAuditHelper.AssertSolidBox(cols, "A01/Floor_C", 42f, 56f, -1f, 0f);
            GeometryAuditHelper.AssertSolidBox(cols, "A01/Ceiling_C", 42f, 56f, 18.5f, 19f);
            GeometryAuditHelper.AssertSolidBox(cols, "A01/Floor_D", 22f, 42f, 7f, 8f);
            GeometryAuditHelper.AssertSolidBox(cols, "A01/Ceiling_D", 22f, 42f, 13.5f, 14f);

            // One-way platforms
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A01/OneWay_C_Top", 42f, 56f, 8f);

            // Chain I1 (P0-02: trigger trimmed to [floorY - 0.20, deckTopY + 0.08])
            GeometryAuditHelper.AssertClimbableTrigger(cols, "A01/I1", 46f, -0.20f, 8.08f);

            // 5 openings owned by CD
            GeometryAuditHelper.AssertOpeningClear(cols, "A01/Open_BC_42", 42f, 0f, 4f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A01/Open_CD_42", 42f, 8f, 12f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A01/Open_CG_Low_56", 56f, 0f, 4f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A01/Open_CG_High_56", 56f, 8f, 12f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A01/Open_DH_22", 22f, 8f, 12f);

            // Decorations must not block Player
            GeometryAuditHelper.AssertNoArtColliders(root, "A01");
        }
    }
}
