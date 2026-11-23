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

            // 3. Runtime behaviour: Awake() on a real enemy instance must enforce the exclusion,
            //    even when its Rigidbody2D starts with no exclusions.
            AssertAwakeOnLiveEnemyInstanceAppliesPlayerExclusion();

            // 4. Arrow prefab must NOT exclude the Player layer, or it stops hitting the Player.
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            Assert.IsNotNull(arrowPrefab, $"Arrow prefab missing at {ArrowPrefabPath}");
            var arrowBody = arrowPrefab.GetComponent<Rigidbody2D>();
            Assert.IsNotNull(arrowBody, "Projectile_Arrow.prefab has no Rigidbody2D.");
            Assert.AreEqual(0, arrowBody.excludeLayers.value & PlayerLayerMask,
                "Projectile_Arrow must not exclude Layer 7: it needs Player trigger contacts. " +
                $"Actual excludeLayers.value = {arrowBody.excludeLayers.value}.");

            // A prefab may carry several colliders; only one needs to be the damage trigger, so scan
            // them all instead of trusting GetComponent's arbitrary pick.
            var arrowColliders = arrowPrefab.GetComponents<Collider2D>();
            Assert.Greater(arrowColliders.Length, 0, "Projectile_Arrow.prefab has no Collider2D.");
            bool hasTrigger = false;
            foreach (var collider in arrowColliders)
                if (collider.isTrigger)
                    hasTrigger = true;
            Assert.IsTrue(hasTrigger,
                "At least one Projectile_Arrow collider must stay a trigger so OnTriggerEnter2D still fires on the Player.");
            Assert.AreEqual(EnemyLayer, arrowPrefab.layer, "Projectile_Arrow must stay on Layer 8.");
        }

        /// <summary>
        /// Behavioural version of the Awake wiring assert (brief Step 2): a real enemy prefab instance
        /// gets its <c>excludeLayers</c> zeroed, then the same <c>Awake()</c> Unity would run is driven
        /// through reflection. The bit must be back afterwards. EditMode does not invoke Awake on its
        /// own, but prefab instances already carry <c>_data</c>, <c>_health</c> and <c>_animator</c>, so
        /// the production Awake body (guard + Health.Initialize + EnemyContext + FSM) executes for real.
        /// Refactor-agnostic: inlining the guard back into Awake, moving it behind a condition, or
        /// reordering it are all judged on the resulting Rigidbody2D state instead of on IL.
        /// </summary>
        private static void AssertAwakeOnLiveEnemyInstanceAppliesPlayerExclusion()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPaths[0]);
            Assert.IsNotNull(prefab, $"Enemy prefab missing at {EnemyPrefabPaths[0]}");

            var instance = Object.Instantiate(prefab);
            try
            {
                var body = instance.GetComponent<Rigidbody2D>();
                Assert.IsNotNull(body, "Instantiated enemy prefab has no Rigidbody2D.");

                // Strip the serialized bit so the only thing that can set it is Awake itself.
                body.excludeLayers = 0;
                Assert.AreEqual(0, body.excludeLayers.value & PlayerLayerMask,
                    "Test precondition: the instance's excludeLayers must start without Layer 7.");

                var controller = instance.GetComponent<EnemyController>();
                Assert.IsNotNull(controller,
                    "Enemy prefab must carry an EnemyController so the runtime Awake path can be exercised.");

                MethodInfo awake = typeof(EnemyController).GetMethod("Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(awake, "EnemyController must declare a private Awake().");

                try
                {
                    awake.Invoke(controller, null);
                }
                catch (TargetInvocationException ex)
                {
                    Assert.Fail($"EnemyController.Awake() threw on a live prefab instance: {ex.InnerException}");
                }

                var liveBody = instance.GetComponent<Rigidbody2D>();
                Assert.AreNotEqual(0, liveBody.excludeLayers.value & PlayerLayerMask,
                    "EnemyController.Awake() must add Layer 7 to Rigidbody2D.excludeLayers on a live instance. " +
                    $"Actual excludeLayers.value = {liveBody.excludeLayers.value}.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
