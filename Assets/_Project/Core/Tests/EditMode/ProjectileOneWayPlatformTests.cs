using System.Reflection;
using DeadCells.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public sealed class ProjectileOneWayPlatformTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp() => _root = new GameObject("__ProjectileOneWayTestRoot");

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void StraightArrow_WhenEnteringOneWayPlatformTrigger_DoesNotDespawn_ButDespawnsOnSolidGround()
        {
            int groundLayer = 6;         // Ground
            int playerLayer = 7;         // Player
            int oneWayPlatformLayer = 9; // OneWayPlatform
            LayerMask targetMask = 1 << playerLayer;
            LayerMask combinedGroundMask = (1 << groundLayer) | (1 << oneWayPlatformLayer); // 576 in Main_Level / Sandbox_Player

            var arrowGo = new GameObject("ArrowProjectile");
            arrowGo.transform.SetParent(_root.transform);
            var rb = arrowGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var arrowCol = arrowGo.AddComponent<BoxCollider2D>();
            arrowCol.isTrigger = true;
            var sr = arrowGo.AddComponent<SpriteRenderer>();
            var projectile = arrowGo.AddComponent<Projectile>();
            SetPrivateField(projectile, "_mode", Projectile.Mode.Straight);
            SetPrivateField(projectile, "_renderer", sr);

            bool released = false;
            projectile.Setup(_ => released = true, vfx: null, targets: targetMask, ground: combinedGroundMask);
            projectile.LaunchStraight(Vector2.zero, new Vector2(12f, 0f), damage: 15, knockback: 2f, owner: _root);
            Assert.IsTrue(projectile.IsLive);

            // 1. Passing across Room E center OneWayPlatform (layer 9) MUST NOT destroy Straight arrow
            var oneWayGo = new GameObject("RoomE_CenterOneWayPlatform");
            oneWayGo.transform.SetParent(_root.transform);
            oneWayGo.layer = oneWayPlatformLayer;
            var oneWayCol = oneWayGo.AddComponent<BoxCollider2D>();

            InvokeOnTriggerEnter2D(projectile, oneWayCol);
            Assert.IsFalse(released, "Horizontal Straight arrow must pass through OneWayPlatform (layer 9) without despawning.");
            Assert.IsTrue(projectile.IsLive, "Straight arrow must remain live after intersecting OneWayPlatform.");

            // 2. Hitting solid Ground wall (layer 6) MUST destroy Straight arrow
            var solidWallGo = new GameObject("SolidStoneWall");
            solidWallGo.transform.SetParent(_root.transform);
            solidWallGo.layer = groundLayer;
            var solidWallCol = solidWallGo.AddComponent<BoxCollider2D>();

            InvokeOnTriggerEnter2D(projectile, solidWallCol);
            Assert.IsTrue(released, "Straight arrow hitting solid Ground (layer 6) must despawn and return to pool.");
            Assert.IsFalse(projectile.IsLive);
        }

        private static void InvokeOnTriggerEnter2D(Projectile projectile, Collider2D other)
        {
            var method = typeof(Projectile).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(projectile, new object[] { other });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field {fieldName}");
            field.SetValue(target, value);
        }
    }
}
