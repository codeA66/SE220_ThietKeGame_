using System.Reflection;
using DeadCells.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    /// <summary>
    /// Round 9 physics/combat regressions. Append-only: later tasks add tests below this one.
    /// </summary>
    public class Round9CombatPhysicsTests
    {
        private const int PlayerLayer = 7;
        private const int EnemyLayer = 8;
        private const int PlayerLayerMask = 1 << PlayerLayer;
        private const string ArrowPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Projectile_Arrow.prefab";

        private static readonly string[] EnemyPrefabPaths =
        {
            "Assets/_Project/Features/Enemies/Prefabs/Enemy_Zombie.prefab",
            "Assets/_Project/Features/Enemies/Prefabs/Enemy_Rat.prefab",
            "Assets/_Project/Features/Enemies/Prefabs/Enemy_Shield.prefab",
            "Assets/_Project/Features/Enemies/Prefabs/Enemy_Archer.prefab",
            "Assets/_Project/Features/Enemies/Prefabs/Enemy_Grenadier.prefab"
        };

        /// <summary>
        /// P0-03: the Player must fall through enemy bodies instead of standing on them, while the
        /// Layer 7 (Player) / Layer 8 (Enemies) matrix stays enabled so <c>Projectile_Arrow</c>
        /// (Layer 8, trigger collider) still fires OnTriggerEnter2D on the Player.
        /// </summary>
        [Test]
        public void T01_AllFiveEnemyPrefabsExcludePlayerLayerOnRigidbody_WhileArrowKeepsTriggerContact()
        {
            Assert.AreEqual(PlayerLayer, LayerMask.NameToLayer("Player"),
                "Layer 7 must be named Player for the excludeLayers bit to mean what the fix assumes.");
            Assert.AreEqual(EnemyLayer, LayerMask.NameToLayer("Enemy"),
                "Layer 8 must be named Enemy.");

            // 1. Global matrix must stay enabled: arrows are Layer 8 triggers that need Player contacts.
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(PlayerLayer, EnemyLayer),
                "Physics2DSettings must keep Layer 7 <-> Layer 8 collisions enabled so arrows keep triggering.");

            // 2. Every enemy prefab's own Rigidbody2D must exclude the Player layer per-body.
            foreach (string path in EnemyPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, $"Enemy prefab missing at {path}");
                var body = prefab.GetComponent<Rigidbody2D>();
                Assert.IsNotNull(body, $"{path} has no Rigidbody2D.");
                Assert.AreNotEqual(0, body.excludeLayers.value & PlayerLayerMask,
                    $"{path}: Rigidbody2D.excludeLayers must include Layer {PlayerLayer} (bit {PlayerLayerMask}). " +
                    $"Actual excludeLayers.value = {body.excludeLayers.value}.");
            }

            // 3. Runtime guard: a fresh EnemyController must enforce the exclusion in Awake,
            //    even when its Rigidbody2D starts with no exclusions.
            AssertDefensiveAwakeGuardAppliesPlayerExclusion();

            // 4. Arrow prefab must NOT exclude the Player layer, or it stops hitting the Player.
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            Assert.IsNotNull(arrowPrefab, $"Arrow prefab missing at {ArrowPrefabPath}");
            var arrowBody = arrowPrefab.GetComponent<Rigidbody2D>();
            Assert.IsNotNull(arrowBody, "Projectile_Arrow.prefab has no Rigidbody2D.");
            Assert.AreEqual(0, arrowBody.excludeLayers.value & PlayerLayerMask,
                "Projectile_Arrow must not exclude Layer 7: it needs Player trigger contacts. " +
                $"Actual excludeLayers.value = {arrowBody.excludeLayers.value}.");

            var arrowCollider = arrowPrefab.GetComponent<Collider2D>();
            Assert.IsNotNull(arrowCollider, "Projectile_Arrow.prefab has no Collider2D.");
            Assert.IsTrue(arrowCollider.isTrigger, "Projectile_Arrow collider must stay a trigger.");
            Assert.AreEqual(EnemyLayer, arrowPrefab.layer, "Projectile_Arrow must stay on Layer 8.");
        }

        private static bool AwakeCallsMethod(MethodInfo caller, MethodInfo callee)
        {
            if (caller == null || callee == null)
                return false;
            byte[] il = caller.GetMethodBody()?.GetILAsByteArray();
            if (il == null)
                return false;
            byte[] token = System.BitConverter.GetBytes(callee.MetadataToken);
            for (int i = 0; i + 4 < il.Length; i++)
            {
                // call (0x28) / callvirt (0x6F) followed by the 4-byte metadata token.
                if (il[i] != 0x28 && il[i] != 0x6F)
                    continue;
                if (il[i + 1] == token[0] && il[i + 2] == token[1] && il[i + 3] == token[2] && il[i + 4] == token[3])
                    return true;
            }
            return false;
        }

        private static void AssertDefensiveAwakeGuardAppliesPlayerExclusion()
        {
            var root = new GameObject("__Round9EnemyControllerAwakeGuard");
            try
            {
                root.layer = EnemyLayer;
                root.SetActive(false);

                var body = root.AddComponent<Rigidbody2D>();
                body.excludeLayers = 0;
                root.AddComponent<BoxCollider2D>();
                var controller = root.AddComponent<EnemyController>();
                controller.enabled = false;

                MethodInfo guard = typeof(EnemyController).GetMethod("ApplyPlayerLayerExclusion",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(guard,
                    "EnemyController must expose the Awake() layer guard as ApplyPlayerLayerExclusion().");

                // EditMode does not reliably invoke Awake on AddComponent, so prove the wiring in IL
                // (Awake calls the guard) and then exercise the guard itself.
                Assert.IsTrue(AwakeCallsMethod(typeof(EnemyController).GetMethod("Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic), guard),
                    "EnemyController.Awake must call ApplyPlayerLayerExclusion().");
                guard.Invoke(controller, null);

                var liveBody = root.GetComponent<Rigidbody2D>();
                Assert.AreNotEqual(0, liveBody.excludeLayers.value & PlayerLayerMask,
                    "EnemyController's Awake guard must add Layer 7 to Rigidbody2D.excludeLayers at runtime. " +
                    $"Actual excludeLayers.value = {liveBody.excludeLayers.value}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
