using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Tools.Editor;
using DeadCells.VFX;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public sealed class ParallaxAndAtmosphereTests
    {
        private static readonly string[] ExpectedOrder =
        {
            "Default",
            "ParallaxFar",
            "ParallaxMid",
            "Background",
            "Ground",
            "Actors",
            "VFX",
            "Foreground"
        };

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Object.DestroyImmediate(_spawned[i]);
            }

            _spawned.Clear();
        }

        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        [Test]
        public void SortingLayerSetup_RegistersAllDeadCellsLayersInOrderIdempotently()
        {
            byte[] beforeBytes = File.ReadAllBytes(TagManagerPath);
            try
            {
                SortingLayerSetup.EnsureDeadCellsSortingLayers();
                var firstPass = SortingLayer.layers.Select(l => (l.name, l.id)).ToArray();

                SortingLayerSetup.EnsureDeadCellsSortingLayers();
                var secondPass = SortingLayer.layers.Select(l => (l.name, l.id)).ToArray();
                byte[] afterBytes = File.ReadAllBytes(TagManagerPath);

                CollectionAssert.AreEqual(firstPass, secondPass, "EnsureDeadCellsSortingLayers must be idempotent.");
                Assert.IsTrue(beforeBytes.SequenceEqual(afterBytes),
                    "EnsureDeadCellsSortingLayers must not rewrite ProjectSettings/TagManager.asset when layers are already registered.");

                string[] layerNames = SortingLayer.layers.Select(l => l.name).ToArray();
                Assert.IsNotEmpty(layerNames);
                Assert.AreEqual("Default", layerNames[0], "Default sorting layer must remain at index 0.");
                Assert.AreEqual(layerNames.Distinct().Count(), layerNames.Length, "Sorting layers must not contain duplicates.");

                int previousIndex = -1;
                foreach (string expected in ExpectedOrder)
                {
                    int index = System.Array.IndexOf(layerNames, expected);
                    Assert.GreaterOrEqual(index, 0, $"Missing expected sorting layer '{expected}'.");
                    Assert.Greater(index, previousIndex, $"Sorting layer '{expected}' is out of strict relative order.");
                    previousIndex = index;

                    int id = SortingLayerSetup.GetSortingLayerId(expected);
                    Assert.AreEqual(SortingLayer.NameToID(expected), id);
                    if (expected != "Default")
                        Assert.AreNotEqual(0, id, $"Sorting layer '{expected}' must have a non-zero unique ID.");
                }
            }
            finally
            {
                byte[] currentBytes = File.ReadAllBytes(TagManagerPath);
                if (!currentBytes.SequenceEqual(beforeBytes))
                    File.WriteAllBytes(TagManagerPath, beforeBytes);
            }
        }

        [Test]
        public void ParallaxLayer2D_AppliesFractionalFactorAndPixelSnaps()
        {
            var go = new GameObject("ParallaxTest");
            _spawned.Add(go);
            go.transform.position = Vector3.zero;

            var layer = go.AddComponent<ParallaxLayer2D>();
            layer.Configure(null, new Vector2(0.25f, 0.10f), 0f, pixelSnap: true, pixelsPerUnit: 24f);
            layer.UpdateParallax(new Vector3(10f, 5f, -10f));

            Vector3 pos = go.transform.position;
            Assert.AreEqual(7.5f, pos.x, 1f / 24f, "X displacement should equal camX * (1 - factorX).");
            Assert.AreEqual(4.5f, pos.y, 1f / 24f, "Y displacement should equal camY * (1 - factorY).");

            float snappedX = Mathf.Round(pos.x * 24f) / 24f;
            float snappedY = Mathf.Round(pos.y * 24f) / 24f;
            Assert.AreEqual(snappedX, pos.x, 1e-4f, "X position must snap to 1/24 PPU grid.");
            Assert.AreEqual(snappedY, pos.y, 1e-4f, "Y position must snap to 1/24 PPU grid.");
        }

        [Test]
        public void ParallaxLayer2D_WrapsHorizontallyAcrossFullCorridorWithoutEdgeGap()
        {
            const float wrapWidth = 32f;
            var go = new GameObject("ParallaxWrapTest");
            _spawned.Add(go);
            go.transform.position = Vector3.zero;

            var layer = go.AddComponent<ParallaxLayer2D>();
            layer.Configure(null, new Vector2(0.15f, 0f), wrapWidth, pixelSnap: true, pixelsPerUnit: 24f);

            for (float camX = -10f; camX <= 120f; camX += 2f)
            {
                layer.UpdateParallax(new Vector3(camX, 0f, -10f));
                float distance = Mathf.Abs(layer.transform.position.x - camX);
                Assert.LessOrEqual(
                    distance,
                    wrapWidth * 0.5f + 0.1f,
                    $"Layer drifted outside wrap half-width at camX={camX} (pos.x={layer.transform.position.x}).");
            }
        }

        [Test]
        public void TorchFlicker2D_OscillatesSmoothlyWithinBoundsAndHandlesNullLight()
        {
            var go = new GameObject("TorchFlickerTest");
            _spawned.Add(go);

            const float baseIntensity = 1.2f;
            const float baseOuterRadius = 5.0f;
            const float amplitude = 0.08f;

            var flicker = go.AddComponent<TorchFlicker2D>();
            flicker.Configure(null, baseIntensity, baseOuterRadius, amplitude, speed: 3.2f, phaseOffset: 0f);

            float minIntensity = float.MaxValue;
            float maxIntensity = float.MinValue;
            float minRadius = float.MaxValue;
            float maxRadius = float.MinValue;

            for (int step = 0; step <= 100; step++)
            {
                float t = step * 0.1f;
                float intensity = flicker.EvaluateIntensity(t);
                float radius = flicker.EvaluateOuterRadius(t);

                Assert.GreaterOrEqual(intensity, baseIntensity * (1f - amplitude) - 1e-5f);
                Assert.LessOrEqual(intensity, baseIntensity * (1f + amplitude) + 1e-5f);

                Assert.GreaterOrEqual(radius, baseOuterRadius * (1f - amplitude) - 1e-5f);
                Assert.LessOrEqual(radius, baseOuterRadius * (1f + amplitude) + 1e-5f);

                if (intensity < minIntensity) minIntensity = intensity;
                if (intensity > maxIntensity) maxIntensity = intensity;
                if (radius < minRadius) minRadius = radius;
                if (radius > maxRadius) maxRadius = radius;
            }

            Assert.Greater(maxIntensity - minIntensity, baseIntensity * amplitude * 0.5f);
            Assert.Greater(maxRadius - minRadius, baseOuterRadius * amplitude * 0.5f);
            Assert.DoesNotThrow(() => flicker.ApplyAtTime(1f));
        }

        [Test]
        public void ParallaxLayer2D_VerticalCameraLock_PreservesEditModeOriginAtRefYAndTracksCameraAscentOneToOne()
        {
            var go = new GameObject("FgTest");
            var camGo = new GameObject("CamTest");
            try
            {
                var layer = go.AddComponent<ParallaxLayer2D>();
                layer.Configure(camGo.transform, new Vector2(1.18f, 1.0f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
                layer.ConfigureVerticalCameraLock(true, referenceCameraY: 3.8f);

                layer.UpdateParallax(new Vector3(18.5f, 3.8f, -10f));
                Assert.AreEqual(0f, go.transform.position.y, 1f / 24f, "At referenceCameraY (3.8), vertical offset must be 0.");

                layer.UpdateParallax(new Vector3(18.5f, 8.8f, -10f));
                Assert.AreEqual(5.0f, go.transform.position.y, 1f / 24f, "When camera climbs +5u, locked ceiling foreground must climb +5u.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        [Test]
        public void VertexSwayPin2D_AppliesPerBannerTopWorldYViaPropertyBlock()
        {
            var root = new GameObject("RoomBanner_Test");
            var child = new GameObject("Top");
            child.transform.SetParent(root.transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            try
            {
                var pin = root.AddComponent<VertexSwayPin2D>();
                pin.Configure(pinTopWorldY: 21.8f, swaySpanY: 3.0f, useWorldYPin: true);

                var block = new MaterialPropertyBlock();
                sr.GetPropertyBlock(block);
                Assert.AreEqual(1.0f, block.GetFloat("_UseWorldYPin"), 1e-4f);
                Assert.AreEqual(21.8f, block.GetFloat("_PinTopWorldY"), 1e-4f);
                Assert.AreEqual(3.0f, block.GetFloat("_SwaySpanY"), 1e-4f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimePooledVfxAndProjectiles_DefaultToVfxSortingLayerAboveBackground()
        {
            SortingLayerSetup.EnsureDeadCellsSortingLayers();

            int vfxLayerId = SortingLayer.NameToID("VFX");
            int bgLayerId = SortingLayer.NameToID("Background");
            Assert.AreNotEqual(0, vfxLayerId, "VFX sorting layer must exist with non-zero ID.");
            Assert.Greater(
                SortingLayer.GetLayerValueFromID(vfxLayerId),
                SortingLayer.GetLayerValueFromID(bgLayerId),
                "VFX sorting layer must sort above Background.");

            var vfxDef = ScriptableObject.CreateInstance<VfxDefinition>();
            var vfxGo = new GameObject("PooledVfx_Test");
            var projGo = new GameObject("Projectile_Test");
            var arrowGo = new GameObject("RecoverableArrow_Test");
            try
            {
                // 1. PooledVfx.Init assigns VFX sorting layer
                var vfxSr = vfxGo.AddComponent<SpriteRenderer>();
                var pooledVfx = vfxGo.AddComponent<PooledVfx>();
                pooledVfx.Init(vfxDef, _ => { });
                Assert.AreEqual(vfxLayerId, vfxSr.sortingLayerID, "PooledVfx.Init must assign VFX sortingLayerID.");
                Assert.AreEqual("VFX", vfxSr.sortingLayerName, "PooledVfx.Init must assign VFX sortingLayerName.");

                // 2. Projectile.Setup assigns VFX sorting layer when on Default (0)
                projGo.AddComponent<Rigidbody2D>();
                projGo.AddComponent<BoxCollider2D>();
                var projSr = projGo.AddComponent<SpriteRenderer>();
                var projectile = projGo.AddComponent<Projectile>();
                var rendererField = typeof(Projectile).GetField("_renderer", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(rendererField, "Projectile._renderer field missing.");
                rendererField.SetValue(projectile, projSr);

                projectile.Setup(_ => { }, null, default, default, default);
                Assert.AreEqual(vfxLayerId, projSr.sortingLayerID, "Projectile.Setup must assign VFX sortingLayerID.");
                Assert.AreEqual("VFX", projSr.sortingLayerName, "Projectile.Setup must assign VFX sortingLayerName.");

                // 3. RecoverableArrow.Initialize assigns VFX sorting layer to child SpriteRenderers on Default (0)
                var arrowSr = arrowGo.AddComponent<SpriteRenderer>();
                var arrow = arrowGo.AddComponent<RecoverableArrow>();
                arrow.Initialize(_ => { });
                Assert.AreEqual(vfxLayerId, arrowSr.sortingLayerID, "RecoverableArrow.Initialize must assign VFX sortingLayerID.");
                Assert.AreEqual("VFX", arrowSr.sortingLayerName, "RecoverableArrow.Initialize must assign VFX sortingLayerName.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(vfxDef);
                UnityEngine.Object.DestroyImmediate(vfxGo);
                UnityEngine.Object.DestroyImmediate(projGo);
                UnityEngine.Object.DestroyImmediate(arrowGo);
            }
        }

        [Test]
        public void ParticleMeshSystems_DisableComponentWhenEmpty_AndReEnableOnEmit()
        {
            BloodDrops blood = null;
            Debris debris = null;
            Sparks sparks = null;
            try
            {
                BloodDrops.Emit(Vector2.zero, 1, BloodBurst.Light, 0);
                blood = Object.FindAnyObjectByType<BloodDrops>();
                Assert.IsNotNull(blood, "BloodDrops singleton must be created on Emit.");
                Assert.IsTrue(blood.enabled, "BloodDrops must be enabled after Emit.");

                ClearParticleListsAndTick(blood, "_active");
                Assert.IsFalse(blood.enabled, "BloodDrops must disable after _active drains and LateUpdate clears mesh.");
                Assert.AreEqual(0, blood.GetComponent<MeshFilter>().sharedMesh.vertexCount);

                BloodDrops.Emit(Vector2.zero, 1, BloodBurst.Light, 0);
                Assert.IsTrue(blood.enabled, "BloodDrops must re-enable on subsequent Emit.");

                Debris.Shockwave(Vector2.zero, 0, 0.25f, 4);
                debris = Object.FindAnyObjectByType<Debris>();
                Assert.IsNotNull(debris, "Debris singleton must be created on Shockwave.");
                Assert.IsTrue(debris.enabled, "Debris must be enabled after Shockwave.");

                ClearParticleListsAndTick(debris, "_active");
                Assert.IsFalse(debris.enabled, "Debris must disable after _active drains and LateUpdate clears mesh.");
                Assert.AreEqual(0, debris.GetComponent<MeshFilter>().sharedMesh.vertexCount);

                Debris.Shockwave(Vector2.zero, 0, 0.25f, 4);
                Assert.IsTrue(debris.enabled, "Debris must re-enable on subsequent Shockwave.");

                Sparks.Emit(Vector2.zero, 1, SparkBurst.Shield, 0);
                sparks = Object.FindAnyObjectByType<Sparks>();
                Assert.IsNotNull(sparks, "Sparks singleton must be created on Emit.");
                Assert.IsTrue(sparks.enabled, "Sparks must be enabled after Emit.");

                ClearParticleListsAndTick(sparks, "_active", "_flashes");
                Assert.IsFalse(sparks.enabled, "Sparks must disable after _active and _flashes drain and LateUpdate clears meshes.");

                Sparks.Emit(Vector2.zero, 1, SparkBurst.Shield, 0);
                Assert.IsTrue(sparks.enabled, "Sparks must re-enable on subsequent Emit.");
            }
            finally
            {
                if (blood != null) Object.DestroyImmediate(blood.gameObject);
                if (debris != null) Object.DestroyImmediate(debris.gameObject);
                if (sparks != null) Object.DestroyImmediate(sparks.gameObject);
            }
        }

        [Test]
        public void ParallaxLayer2D_PinColumnsToFrustumEdges_PreservesColumnPositionsWithCachedCameraAndWidths()
        {
            var camGo = new GameObject("CamTest");
            var layerGo = new GameObject("FgLayerTest");
            var tex = new Texture2D(64, 32);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, 64f, 32f), new Vector2(0.5f, 0.5f), 32f);
            _spawned.Add(camGo);
            _spawned.Add(layerGo);

            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6.75f;
                cam.aspect = 16f / 9f;

                var col0 = CreateColumn(layerGo.transform, "FgColumn_0", -30f, 1.5f);
                var col1 = CreateColumn(layerGo.transform, "FgColumn_1", -10f, 1.5f);
                var col2 = CreateColumn(layerGo.transform, "FgColumn_2", 10f, 1.5f);
                var col3 = CreateColumn(layerGo.transform, "FgColumn_3", 30f, 1.5f);

                var sr1 = col1.gameObject.AddComponent<SpriteRenderer>();
                sr1.sprite = sprite;
                col1.localScale = new Vector3(1.5f, 1f, 1f);

                var sr2 = col2.gameObject.AddComponent<SpriteRenderer>();
                sr2.sprite = sprite;
                col2.localScale = new Vector3(0.5f, 1f, 1f);

                var layer = layerGo.AddComponent<ParallaxLayer2D>();
                layer.Configure(camGo.transform, Vector2.one, 0f, pixelSnap: true, pixelsPerUnit: 24f);
                layer.ConfigureFrustumEdgePinning(enabled: true, edgeVisibleInsetUnits: 0.46f, outerGuardOffsetUnits: 26f);

                layer.UpdateParallax(new Vector3(10f, 0f, -10f));
                layer.UpdateParallax(new Vector3(25f, 0f, -10f));

                float halfW = cam.orthographicSize * cam.aspect;
                float expectedLeft = Mathf.Round((halfW + 1.5f - 0.46f) * 24f) / 24f;
                float expectedRight = Mathf.Round((halfW + 0.5f - 0.46f) * 24f) / 24f;
                float expectedOuter = Mathf.Round(Mathf.Max(38f, halfW + 26f) * 24f) / 24f;

                Assert.AreEqual(-expectedOuter, col0.localPosition.x, 1e-4f);
                Assert.AreEqual(-expectedLeft, col1.localPosition.x, 1e-4f);
                Assert.AreEqual(expectedRight, col2.localPosition.x, 1e-4f);
                Assert.AreEqual(expectedOuter, col3.localPosition.x, 1e-4f);
                Assert.AreEqual(1.5f, col1.localPosition.y, 1e-4f);

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Assert.AreSame(cam, typeof(ParallaxLayer2D).GetField("_cachedCamera", flags).GetValue(layer));
                Assert.IsTrue((bool)typeof(ParallaxLayer2D).GetField("_columnWidthsCached", flags).GetValue(layer));
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(tex);
            }
        }

        private static Transform CreateColumn(Transform parent, string name, float localX, float localY)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localX, localY, 0f);
            return go.transform;
        }

        private static void ClearParticleListsAndTick(MonoBehaviour system, params string[] listFieldNames)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            System.Type type = system.GetType();
            for (int i = 0; i < listFieldNames.Length; i++)
            {
                FieldInfo field = type.GetField(listFieldNames[i], flags);
                Assert.IsNotNull(field, $"{type.Name}.{listFieldNames[i]} field missing.");
                var list = (System.Collections.IList)field.GetValue(system);
                list.Clear();
            }

            type.GetMethod("Update", flags)?.Invoke(system, null);
            MethodInfo lateUpdate = type.GetMethod("LateUpdate", flags);
            Assert.IsNotNull(lateUpdate, $"{type.Name}.LateUpdate method missing.");
            lateUpdate.Invoke(system, null);
        }
    }
}
