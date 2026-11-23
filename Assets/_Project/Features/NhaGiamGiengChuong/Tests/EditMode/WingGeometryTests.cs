using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class WingGeometryTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A05b_W1EastCourtyardPrefab_MatchesRoomsMNCollidersChainsOpeningsAndNoArtColliders()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("W1_EastCourtyard");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            // Room M (x=76..164, y=0..7)
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Floor_M_West", 76f, 115f, -1f, 0f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_M_WellBridge", 115f, 121f, 0f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Floor_M_East", 121f, 164f, -1f, 0f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_M_Perch1", 88f, 96f, 3.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_M_Perch2", 134f, 142f, 3.5f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05b/FloorM_Union", 0f, 76f, 164f);

            // Room N (x=76..164, y=8..22.5)
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Floor_N_West", 76f, 101f, 7f, 8f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_N_WellCap", 101f, 107f, 8f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Floor_N_East", 107f, 164f, 7f, 8f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05b/FloorN_Union", 8f, 76f, 164f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_N_Mid1", 86f, 96f, 12.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05b/OneWay_N_Mid2", 116f, 128f, 12.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Ceiling_N", 76f, 164.5f, 22.5f, 23f);

            // Walls & Headers
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Wall_MN_East", 164f, 164.5f, -1f, 22.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Wall_N_WestUpper_76", 76f, 76.5f, 15f, 22.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05b/Wall_M_VaultHeader_150", 149.75f, 150.25f, 4f, 7f);

            // Chain I7 & Openings
            GeometryAuditHelper.AssertClimbableTrigger(cols, "A05b/I7", 104f, -0.20f, 8.08f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05b/Open_GM_Low_76", 76f, 0f, 4f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05b/Open_GN_High_76", 76f, 8f, 12f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05b/Open_M_Vault_150", 150f, 0f, 4f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05b");
        }

        [Test]
        public void A05c_W2SewerCisternPrefab_MatchesRoomsOPCollidersChainOpeningsAndNoArtColliders()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("W2_SewerCistern");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Floor_O", 76f, 164f, -17f, -16f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Floor_P", 164f, 234f, -17f, -16f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05c/FloorOP_Union", -16f, 76f, 234f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Ceiling_P", 164f, 234.5f, -1.5f, -1f);

            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_O_ShaftStep1", 112f, 124f, -11f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_O_ShaftStep2", 112f, 124f, -5.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_O_East", 138f, 150f, -11.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05c/OneWay_P_Mid", 184f, 198f, -11.5f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Wall_O_West", 75.5f, 76f, -17f, -1f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Wall_OP_Header_164", 163.75f, 164.25f, -12f, -1f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Wall_P_SewerHeader_212", 211.75f, 212.25f, -12f, -1.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05c/Wall_P_East", 234f, 234.5f, -17f, -1.5f);

            GeometryAuditHelper.AssertClimbableTrigger(cols, "A05c/I8", 118f, -16.20f, 0.08f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05c/Open_OP_164", 164f, -16f, -12f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05c/Open_P_SewerGate_212", 212f, -16f, -12f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05c");
        }

        [Test]
        public void A05d_W3WestBastionPrefab_MatchesRoomsQRCollidersChainOpeningsAndNoArtColliders()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("W3_WestBastion");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Floor_Q", -102f, -30f, 15f, 16f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05d/OneWay_Q_Mid", -88f, -76f, 19.5f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Floor_R_West", -102f, -69f, 23f, 24f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05d/OneWay_R_WellCap", -69f, -63f, 24f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Floor_R_East", -63f, -30f, 23f, 24f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05d/FloorR_Union", 24f, -102f, -30f);

            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05d/OneWay_R_Mid1", -54f, -42f, 28.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05d/OneWay_R_Mid2", -82f, -70f, 28.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Ceiling_R", -102.5f, -30f, 38.5f, 39f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Wall_QR_West", -102.5f, -102f, 15f, 38.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Wall_R_EastUpper_Neg30", -30.5f, -30f, 31f, 38.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05d/Wall_R_ArmoryHeader_Neg88", -88.25f, -87.75f, 28f, 38.5f);

            GeometryAuditHelper.AssertClimbableTrigger(cols, "A05d/I9", -66f, 15.80f, 24.08f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05d/Open_KQ_Low_Neg30", -30f, 16f, 20f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05d/Open_KR_High_Neg30", -30f, 24f, 28f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05d/Open_R_Armory_Neg88", -88f, 24f, 28f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05d");
        }

        [Test]
        public void A05e_U1UpperPromenadePrefab_MatchesRoomsSTCollidersChainOpeningsAndNoArtColliders()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("U1_UpperPromenade");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Floor_S", 38f, 92f, 23f, 24f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Floor_T_West", 38f, 77f, 39f, 40f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05e/OneWay_T_ShaftCap", 77f, 83f, 40f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Floor_T_East", 83f, 140f, 39f, 40f);
            GeometryAuditHelper.AssertFloorUnionCovers(cols, "A05e/FloorT_Union", 40f, 38f, 140f);

            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05e/OneWay_T_Mid1", 52f, 64f, 44.5f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A05e/OneWay_T_Mid2", 92f, 106f, 44.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Ceiling_T", 37.5f, 140.5f, 62.5f, 63f);

            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Wall_S_East", 92f, 92.5f, 23f, 39f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Wall_T_West", 37.5f, 38f, 39f, 62.5f);
            GeometryAuditHelper.AssertSolidBox(cols, "A05e/Wall_T_East", 140f, 140.5f, 39f, 62.5f);

            GeometryAuditHelper.AssertClimbableTrigger(cols, "A05e/I10", 80f, 23.80f, 40.08f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A05e/Open_LS_38", 38f, 24f, 28f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A05e");
        }
    }
}
