using DeadCells.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public sealed class ArcherArrowTests
    {
        private const string ArrowPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Projectile_Arrow.prefab";
        private const string ArrowSpritePath = "Assets/_Project/Art/Projectiles/Arrow.png";

        [Test]
        public void ArrowPrefab_UsesHorizontalArrowSpriteMatchingPlayerBowScale()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at {ArrowPrefabPath}.");

            var renderer = prefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer, "Projectile_Arrow prefab must have a SpriteRenderer.");
            Assert.IsNotNull(renderer.sprite, "Projectile_Arrow SpriteRenderer must reference a sprite.");

            string spriteAssetPath = AssetDatabase.GetAssetPath(renderer.sprite);
            Assert.AreEqual(ArrowSpritePath, spriteAssetPath, "Projectile_Arrow should use Arrow.png.");
            Assert.AreEqual(26f, renderer.sprite.rect.width, 0.01f, "Archer arrow sprite width should be 26 px.");
            Assert.AreEqual(7f, renderer.sprite.rect.height, 0.01f, "Archer arrow sprite height should be 7 px.");
            Assert.Greater(renderer.sprite.rect.width, renderer.sprite.rect.height,
                "Archer arrow sprite must be wider than tall (horizontal arrow, not square chevron).");
        }

        [Test]
        public void Projectile_LaunchStraight_AlignsIn2DPlaneWithoutYFlip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            Assert.IsNotNull(prefab);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                var projectile = instance.GetComponent<Projectile>();
                var renderer = instance.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(projectile);
                Assert.IsNotNull(renderer);

                projectile.Setup(_ => { }, null, 0, 0);

                // Firing right (+X)
                projectile.LaunchStraight(Vector2.zero, new Vector2(7f, 0f), 12, 3f, null);
                Assert.AreEqual(0f, Mathf.DeltaAngle(0f, instance.transform.eulerAngles.x), 0.1f);
                Assert.AreEqual(0f, Mathf.DeltaAngle(0f, instance.transform.eulerAngles.y), 0.1f);
                Assert.AreEqual(0f, Mathf.DeltaAngle(0f, instance.transform.eulerAngles.z), 0.1f);
                Assert.Greater(instance.transform.forward.z, 0.99f, "2D sprite forward must remain +Z.");
                Assert.IsFalse(renderer.flipY, "Rightward arrow should not flip Y.");

                // Firing left (-X) must rotate 180 deg around Z, NOT 180 deg around Y.
                projectile.LaunchStraight(Vector2.zero, new Vector2(-7f, 0f), 12, 3f, null);
                Assert.AreEqual(0f, Mathf.DeltaAngle(0f, instance.transform.eulerAngles.x), 0.1f);
                Assert.AreEqual(0f, Mathf.DeltaAngle(0f, instance.transform.eulerAngles.y), 0.1f);
                Assert.AreEqual(0f, Mathf.DeltaAngle(180f, instance.transform.eulerAngles.z), 0.1f);
                Assert.Greater(instance.transform.forward.z, 0.99f, "Leftward 2D arrow must not flip around Y axis.");
                Assert.IsTrue(renderer.flipY, "Leftward arrow should flip Y to preserve top-lit orientation.");

                // Lob resets rotation and flipY.
                projectile.LaunchLob(Vector2.zero, new Vector2(-3f, 4f), 12, 1f, 2f, null, null);
                Assert.AreEqual(Quaternion.identity, instance.transform.rotation);
                Assert.IsFalse(renderer.flipY);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
