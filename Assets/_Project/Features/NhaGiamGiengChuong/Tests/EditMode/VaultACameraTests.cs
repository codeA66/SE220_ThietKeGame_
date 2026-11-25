using System.Collections.Generic;
using System.Reflection;
using DeadCells.CameraSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class VaultACameraTests
    {
        private readonly List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
        }

        private CameraFollow2D CreateCameraFixture(out Camera cam, out Transform target)
        {
            var targetGo = new GameObject("Target_Test");
            targetGo.transform.position = new Vector3(3f, 0f, 0f);
            _created.Add(targetGo);
            target = targetGo.transform;

            var camGo = new GameObject("Cam_Test");
            camGo.transform.position = new Vector3(3f, 1.5f, -10f);
            _created.Add(camGo);

            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.aspect = 16f / 9f;

            var follow = camGo.AddComponent<CameraFollow2D>();
            var so = new SerializedObject(follow);
            so.FindProperty("_target").objectReferenceValue = target;
            so.FindProperty("_offset").vector2Value = new Vector2(0f, 1.5f);
            so.FindProperty("_deadzone").vector2Value = new Vector2(0.55f, 0.35f);
            so.FindProperty("_smoothTime").floatValue = 0.09f;
            so.FindProperty("_useBounds").boolValue = true;
            so.FindProperty("_bounds").rectValue = new Rect(-104f, -20f, 340f, 86f);
            so.ApplyModifiedPropertiesWithoutUndo();

            typeof(CameraFollow2D)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(follow, null);
            return follow;
        }

        [Test]
        public void SetBoundsAndSnapToTarget_MovesCameraImmediatelyInsideVaultABounds()
        {
            var follow = CreateCameraFixture(out _, out var target);
            var vaultBounds = new Rect(176f, 27f, 28f, 24f);

            target.position = new Vector3(180f, 29f, 0f);
            follow.SetBounds(true, vaultBounds);
            follow.SnapToTarget();

            Assert.AreEqual(vaultBounds, follow.CurrentBounds);
            Assert.IsTrue(follow.UseBounds);
            Assert.GreaterOrEqual(follow.transform.position.x, 176f);
            Assert.LessOrEqual(follow.transform.position.x, 204f);
            Assert.AreEqual(34f, follow.transform.position.y, 0.05f);
        }

        [Test]
        public void BeginAndEndFocusOverride_FramesVaultATapestryWithoutBoundsSkew_AndRestoresOrthoSize()
        {
            var follow = CreateCameraFixture(out var cam, out var target);
            var vaultBounds = new Rect(176f, 27f, 28f, 24f);
            target.position = new Vector3(190f, 29f, 0f);
            follow.SetBounds(true, vaultBounds);
            follow.SnapToTarget();

            var tapestryCenter = new Vector2(191.5f, 37.2f);
            follow.BeginFocusOverride(tapestryCenter, 0.5f, 5.2f, bypassBounds: false);
            Assert.IsTrue(follow.IsFocusOverrideActive);

            for (int i = 0; i < 180; i++)
                follow.StepCamera(1f / 60f);

            Assert.AreEqual(191.5f, follow.transform.position.x, 0.20f);
            Assert.AreEqual(37.2f, follow.transform.position.y, 0.20f);
            Assert.AreEqual(5.2f, cam.orthographicSize, 0.05f);

            follow.EndFocusOverride(0.4f);
            Assert.IsFalse(follow.IsFocusOverrideActive);

            for (int i = 0; i < 180; i++)
                follow.StepCamera(1f / 60f);

            Assert.AreEqual(7f, cam.orthographicSize, 0.05f);
            Assert.AreEqual(34f, follow.transform.position.y, 0.25f);
        }
    }
}
