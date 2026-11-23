using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    public sealed class HJKGeometryTests
    {
        private string _previousScene;

        [SetUp]
        public void SetUp() => _previousScene = GeometryAuditHelper.CaptureCurrentScenePath();

        [TearDown]
        public void TearDown() => GeometryAuditHelper.RestoreScene(_previousScene);

        [Test]
        public void A04_HJKPrefab_MatchesStairPolylineFloorsOneWayChainAndOpenings()
        {
            var root = GeometryAuditHelper.LoadRoomPrefabIntoTempScene("HJK");
            var cols = root.GetComponentsInChildren<Collider2D>(true);

            // Verify single 45-degree Dead Cells staircase PolygonCollider2D H: bottom y=7,
            // flat bottom landing [18,22]@8, continuous 45-degree ramp (18,8)->(10,16), flat top landing [6,10]@16
            var poly = cols.OfType<PolygonCollider2D>().SingleOrDefault();
            Assert.IsNotNull(poly, "[A04/Stairs_H] Expected exactly 1 PolygonCollider2D.");
            Assert.AreEqual(GeometryAuditHelper.LayerGround, poly.gameObject.layer);

            var expectedRamp = new[]
            {
                new Vector2(6f, 7f),
                new Vector2(22f, 7f),
                new Vector2(22f, 8f),
                new Vector2(18f, 8f),
                new Vector2(10f, 16f),
                new Vector2(6f, 16f)
            };

            var pts = poly.GetPath(0).Select(p => (Vector2)poly.transform.TransformPoint(p)).ToArray();
            Assert.AreEqual(expectedRamp.Length, pts.Length,
                "[A04/Stairs_H] Expected the 6-vertex 45-degree ramp polyline.");
            for (int i = 0; i < expectedRamp.Length; i++)
            {
                Assert.AreEqual(expectedRamp[i].x, pts[i].x, GeometryAuditHelper.Eps,
                    $"[A04/Stairs_H] Vertex {i} X must be {expectedRamp[i].x}.");
                Assert.AreEqual(expectedRamp[i].y, pts[i].y, GeometryAuditHelper.Eps,
                    $"[A04/Stairs_H] Vertex {i} Y must be {expectedRamp[i].y}.");
            }

            float minY = pts.Min(p => p.y);
            Assert.AreEqual(7f, minY, GeometryAuditHelper.Eps, "[A04/Stairs_H] Common bottom must be y=7.");

            Assert.IsTrue(HasHorizontalEdge(pts, 18f, 22f, 8f),
                "[A04/Stairs_H] Missing flat bottom landing [18,22]@y=8.");
            Assert.IsTrue(HasHorizontalEdge(pts, 6f, 10f, 16f),
                "[A04/Stairs_H] Missing flat top landing [6,10]@y=16.");

            bool has45Ramp = false;
            for (int i = 0; i < pts.Length; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % pts.Length];
                float dx = Mathf.Abs(b.x - a.x);
                float dy = Mathf.Abs(b.y - a.y);
                if (Mathf.Abs(dx - 8f) <= GeometryAuditHelper.Eps && Mathf.Abs(dy - 8f) <= GeometryAuditHelper.Eps
                    && Mathf.Abs(Mathf.Min(a.x, b.x) - 10f) <= GeometryAuditHelper.Eps
                    && Mathf.Abs(Mathf.Min(a.y, b.y) - 8f) <= GeometryAuditHelper.Eps)
                {
                    has45Ramp = true;
                    break;
                }
            }
            Assert.IsTrue(has45Ramp, "[A04/Stairs_H] Missing continuous 45-degree ramp edge (18,8)->(10,16) rising 8u over 8u.");

            Assert.IsNotNull(poly.sharedMaterial, "[A04/Stairs_H] Ramp must carry a zero-friction PhysicsMaterial2D.");
            Assert.LessOrEqual(poly.sharedMaterial.friction, GeometryAuditHelper.Eps,
                "[A04/Stairs_H] Ramp PhysicsMaterial2D friction must be zero so Floor_D -> ramp -> Floor_KJ is seamless.");

            // Floors, one-way K top, ceilings
            GeometryAuditHelper.AssertSolidBox(cols, "A04/Floor_KJ", -30f, 6f, 15f, 16f);
            GeometryAuditHelper.AssertOneWayPlatform(cols, "A04/OneWay_K_Top", -30f, -16f, 24f);
            GeometryAuditHelper.AssertSolidBox(cols, "A04/Ceiling_HJ", -16f, 22f, 22.5f, 23f);
            GeometryAuditHelper.AssertSolidBox(cols, "A04/Ceiling_K", -30.5f, -16f, 30.5f, 31f);

            // Chain I6 (P0-02: trigger trimmed to [floorY - 0.20, deckTopY + 0.08])
            GeometryAuditHelper.AssertClimbableTrigger(cols, "A04/I6", -24f, 15.80f, 24.08f);

            // 3 openings owned by HJK
            GeometryAuditHelper.AssertOpeningClear(cols, "A04/Open_HJ_6", 6f, 16f, 20f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A04/Open_JK_Neg16", -16f, 16f, 20f);
            GeometryAuditHelper.AssertOpeningClear(cols, "A04/Open_KE_Neg16", -16f, 24f, 28f);
            GeometryAuditHelper.AssertNoArtColliders(root, "A04");
        }

        private static bool HasHorizontalEdge(Vector2[] pts, float xMin, float xMax, float y)
        {
            for (int i = 0; i < pts.Length; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % pts.Length];
                if (Mathf.Abs(a.y - y) <= GeometryAuditHelper.Eps
                    && Mathf.Abs(b.y - y) <= GeometryAuditHelper.Eps
                    && Mathf.Abs(Mathf.Min(a.x, b.x) - xMin) <= GeometryAuditHelper.Eps
                    && Mathf.Abs(Mathf.Max(a.x, b.x) - xMax) <= GeometryAuditHelper.Eps)
                    return true;
            }
            return false;
        }
    }
}
