using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadCells.NhaGiamGiengChuong.Tests.Assets
{
    internal static class GeometryAuditHelper
    {
        public const string ScenePath = "Assets/_Project/Scenes/NhaGiamGiengChuong.unity";
        public const string RoomsFolder = "Assets/_Project/Features/NhaGiamGiengChuong/Rooms";
        public const int LayerGround = 6;
        public const int LayerOneWay = 9;
        public const int LayerClimbable = 10;
        public const float Eps = 0.001f;

        public static string CaptureCurrentScenePath() => SceneManager.GetActiveScene().path;

        public static void RestoreScene(string previousPath)
        {
            if (!string.IsNullOrEmpty(previousPath) && System.IO.File.Exists(previousPath))
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Time.timeScale = 1f;
        }

        public static GameObject LoadRoomPrefabIntoTempScene(string roomName)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = $"{RoomsFolder}/{roomName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"Missing room prefab: {path}");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            inst.transform.localScale = Vector3.one;
            Physics2D.SyncTransforms();
            return inst;
        }

        public static Bounds GetWorldBounds(Collider2D col)
        {
            if (col is BoxCollider2D box)
            {
                Vector3 center = box.transform.TransformPoint(box.offset);
                Vector3 scale = box.transform.lossyScale;
                Vector3 size = new Vector3(
                    Mathf.Abs(box.size.x * scale.x),
                    Mathf.Abs(box.size.y * scale.y),
                    0.1f);
                return new Bounds(center, size);
            }
            if (col is PolygonCollider2D poly)
            {
                var pts = poly.GetPath(0);
                Vector3 min = poly.transform.TransformPoint(pts[0]);
                Vector3 max = min;
                for (int i = 1; i < pts.Length; i++)
                {
                    Vector3 w = poly.transform.TransformPoint(pts[i]);
                    min = Vector3.Min(min, w);
                    max = Vector3.Max(max, w);
                }
                var b = new Bounds();
                b.SetMinMax(min, max);
                return b;
            }
            return col.bounds;
        }

        public static void AssertSolidBox(Collider2D[] all, string label, float xMin, float xMax, float yMin, float yMax)
        {
            var matches = all.Where(c =>
            {
                if (c.isTrigger || c.gameObject.layer != LayerGround) return false;
                var b = GetWorldBounds(c);
                return Mathf.Abs(b.min.x - xMin) <= Eps && Mathf.Abs(b.max.x - xMax) <= Eps
                    && Mathf.Abs(b.min.y - yMin) <= Eps && Mathf.Abs(b.max.y - yMax) <= Eps;
            }).ToArray();

            Assert.AreEqual(1, matches.Length,
                $"[{label}] Expected 1 solid Ground box [{xMin},{xMax}]x[{yMin},{yMax}], found {matches.Length}.");
        }

        public static void AssertOneWayPlatform(Collider2D[] all, string label, float xMin, float xMax, float topY)
        {
            const float thickness = 0.25f;
            float yMin = topY - thickness;
            var matches = all.Where(c =>
            {
                if (c.isTrigger || c.gameObject.layer != LayerOneWay) return false;
                var b = GetWorldBounds(c);
                return Mathf.Abs(b.min.x - xMin) <= Eps && Mathf.Abs(b.max.x - xMax) <= Eps
                    && Mathf.Abs(b.min.y - yMin) <= Eps && Mathf.Abs(b.max.y - topY) <= Eps;
            }).ToArray();

            Assert.AreEqual(1, matches.Length,
                $"[{label}] Expected 1 OneWayPlatform [{xMin},{xMax}] top={topY} (thickness 0.25), found {matches.Length}.");

            var col = matches[0];
            Assert.IsTrue(col.usedByEffector, $"[{label}] {col.name} must enable usedByEffector.");
            var eff = col.GetComponent<PlatformEffector2D>();
            Assert.IsNotNull(eff, $"[{label}] {col.name} must have PlatformEffector2D on same GameObject.");
            Assert.IsTrue(eff.useOneWay, $"[{label}] {col.name} PlatformEffector2D must enable useOneWay.");
        }

        public static void AssertClimbableTrigger(Collider2D[] all, string label, float centerX, float yMin, float yMax)
        {
            float expectedHeight = yMax - yMin;
            float expectedCenterY = (yMin + yMax) * 0.5f;
            var matches = all.Where(c =>
            {
                if (!c.isTrigger || c.gameObject.layer != LayerClimbable) return false;
                var b = GetWorldBounds(c);
                return Mathf.Abs(b.center.x - centerX) <= Eps
                    && Mathf.Abs(b.center.y - expectedCenterY) <= Eps
                    && Mathf.Abs(b.size.x - 0.5f) <= Eps
                    && Mathf.Abs(b.size.y - expectedHeight) <= Eps;
            }).ToArray();

            Assert.AreEqual(1, matches.Length,
                $"[{label}] Expected 1 Climbable trigger center=({centerX},{expectedCenterY}) size=(0.5,{expectedHeight}), found {matches.Length}.");
        }

        public static void AssertOpeningClear(Collider2D[] all, string label, float seamX, float yMin, float yMax, Collider2D exclude = null)
        {
            // Sample across wall thickness [seamX - 0.2, seamX + 0.2] and opening span [yMin + 0.05, yMax - 0.05]
            var testRect = new Rect(seamX - 0.2f, yMin + 0.05f, 0.4f, (yMax - yMin) - 0.1f);
            foreach (var c in all)
            {
                if (c == null || c == exclude || c.isTrigger || !c.enabled || !c.gameObject.activeInHierarchy)
                    continue;
                if (c.gameObject.layer != LayerGround)
                    continue;

                var b = GetWorldBounds(c);
                var r = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
                if (!r.Overlaps(testRect))
                    continue;

                if (c is PolygonCollider2D)
                {
                    bool hitPolygon = false;
                    for (int xi = 0; xi <= 4 && !hitPolygon; xi++)
                    {
                        float sx = Mathf.Lerp(testRect.xMin, testRect.xMax, xi / 4f);
                        for (int yi = 0; yi <= 8; yi++)
                        {
                            float sy = Mathf.Lerp(testRect.yMin, testRect.yMax, yi / 8f);
                            if (c.OverlapPoint(new Vector2(sx, sy)))
                            {
                                hitPolygon = true;
                                break;
                            }
                        }
                    }
                    if (!hitPolygon)
                        continue;
                }

                Assert.Fail($"[{label}] Opening at x={seamX}, y=[{yMin},{yMax}] blocked by '{c.name}' bounds=[{b.min.x:F2},{b.max.x:F2}]x[{b.min.y:F2},{b.max.y:F2}].");
            }
        }

        public static void AssertNoArtColliders(GameObject root, string label)
        {
            var artTransform = root.transform.Find("Art");
            if (artTransform == null)
                return;
            var artCols = artTransform.GetComponentsInChildren<Collider2D>(true);
            Assert.AreEqual(0, artCols.Length, $"[{label}] Decorative Art hierarchy must contain 0 Collider2D components, found {artCols.Length}.");
        }

        public static void AssertFloorUnionCovers(Collider2D[] all, string label, float topY, float expectedXMin, float expectedXMax)
        {
            // Collect all non-trigger Ground/OneWay segments whose top surface is at topY
            var intervals = new List<(float minX, float maxX, string name)>();
            foreach (var c in all)
            {
                if (c == null || c.isTrigger || !c.enabled || !c.gameObject.activeInHierarchy)
                    continue;
                if (c.gameObject.layer != LayerGround && c.gameObject.layer != LayerOneWay)
                    continue;

                if (c is PolygonCollider2D poly)
                {
                    var pts = poly.GetPath(0);
                    for (int i = 0; i < pts.Length; i++)
                    {
                        Vector2 a = poly.transform.TransformPoint(pts[i]);
                        Vector2 b = poly.transform.TransformPoint(pts[(i + 1) % pts.Length]);
                        if (Mathf.Abs(a.y - topY) <= Eps && Mathf.Abs(b.y - topY) <= Eps && Mathf.Abs(a.x - b.x) > Eps)
                            intervals.Add((Mathf.Min(a.x, b.x), Mathf.Max(a.x, b.x), c.name));
                    }
                }
                else
                {
                    var b = GetWorldBounds(c);
                    if (Mathf.Abs(b.max.y - topY) <= Eps && b.size.x >= 1f)
                        intervals.Add((b.min.x, b.max.x, c.name));
                }
            }

            intervals.Sort((a, b) => a.minX.CompareTo(b.minX));
            Assert.IsNotEmpty(intervals, $"[{label}] No floor surfaces found at y={topY}.");

            float cursor = intervals[0].minX;
            Assert.AreEqual(expectedXMin, cursor, Eps, $"[{label}] Floor y={topY} start X mismatch.");
            float coveredMax = intervals[0].maxX;

            for (int i = 1; i < intervals.Count; i++)
            {
                var seg = intervals[i];
                Assert.GreaterOrEqual(seg.minX, coveredMax - Eps,
                    $"[{label}] Overlapping floor segments at y={topY}: prevMax={coveredMax} vs {seg.name} min={seg.minX}.");
                Assert.LessOrEqual(seg.minX, coveredMax + Eps,
                    $"[{label}] Gap in floor at y={topY}: prevMax={coveredMax} vs {seg.name} min={seg.minX}.");
                coveredMax = seg.maxX;
            }

            Assert.AreEqual(expectedXMax, coveredMax, Eps, $"[{label}] Floor y={topY} end X mismatch.");
        }
    }
}
