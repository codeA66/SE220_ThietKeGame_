using System.Reflection;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Enemies.States;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public partial class Round8Phase1CombatAiTests
    {
        private const int EnemyGroundLayer = 6;
        private const int EnemyPlayerLayer = 7;
        private const int EnemyActorLayer = 8;
        private const int EnemyOneWayLayer = 9;
        private const string ZombieControllerPath = "Assets/_Project/Features/Enemies/Animations/zombie/zombie.controller";

        [Test]
        public void EnemyContext_OneWayPlatform_SupportsStanding_ButDoesNotBlockSightWallOrTeleportOverlap()
        {
            var root = new GameObject("__Round8EnemyContextOneWayTest");
            EnemyData data = null;
            try
            {
                LayerMask groundMask = (1 << EnemyGroundLayer) | (1 << EnemyOneWayLayer);
                LayerMask targetMask = 1 << EnemyPlayerLayer;

                // OneWay floor supporting the enemy at y = 0
                var oneWayFloor = new GameObject("OneWay_Floor");
                oneWayFloor.transform.SetParent(root.transform);
                oneWayFloor.layer = EnemyOneWayLayer;
                oneWayFloor.transform.position = new Vector3(1.5f, -0.125f, 0f);
                var floorCol = oneWayFloor.AddComponent<BoxCollider2D>();
                floorCol.size = new Vector2(10f, 0.25f);

                // Torso-height OneWayPlatform ahead of enemy between enemy (0,0) and player (3,0) / teleport spot (2,0)
                var oneWayTorso = new GameObject("OneWay_Torso");
                oneWayTorso.transform.SetParent(root.transform);
                oneWayTorso.layer = EnemyOneWayLayer;
                oneWayTorso.transform.position = new Vector3(2.24f, 0.725f, 0f);
                var torsoCol = oneWayTorso.AddComponent<BoxCollider2D>();
                torsoCol.size = new Vector2(3.52f, 0.25f);

                // Solid Ground wall at x = 5 to confirm solid Ground still blocks CanStandAt
                var solidWall = new GameObject("Solid_Wall");
                solidWall.transform.SetParent(root.transform);
                solidWall.layer = EnemyGroundLayer;
                solidWall.transform.position = new Vector3(5.0f, 0.75f, 0f);
                var wallCol = solidWall.AddComponent<BoxCollider2D>();
                wallCol.size = new Vector2(0.5f, 1.5f);

                var playerGo = new GameObject("Player");
                playerGo.transform.SetParent(root.transform);
                playerGo.layer = EnemyPlayerLayer;
                playerGo.transform.position = new Vector3(3.0f, 0f, 0f);

                var ctx = CreateEnemyContextHarness(root.transform, Vector2.zero, groundMask, targetMask,
                    out var enemyGo, out _, out _, out data);
                ctx.Player = playerGo.transform;
                ctx.Face(1);

                Physics2D.SyncTransforms();
                ctx.RefreshFrame();

                Assert.IsTrue(ctx.IsGrounded, "OneWayPlatform beneath feet must support EnemyContext.IsGrounded.");
                Assert.IsTrue(ctx.IsGroundAhead(1), "OneWayPlatform ahead must count as walkable floor in IsGroundAhead.");
                Assert.IsFalse(ctx.IsWallAhead(1), "OneWayPlatform at torso height must NOT block EnemyContext.IsWallAhead.");
                Assert.IsTrue(ctx.CanSeePlayer(10f), "OneWayPlatform between enemy and player must NOT block EnemyContext.CanSeePlayer.");
                Assert.IsTrue(ctx.CanStandAt(new Vector2(2.0f, 0f)),
                    "OneWayPlatform at torso height must NOT block EnemyContext.CanStandAt body overlap.");
                Assert.IsFalse(ctx.CanStandAt(new Vector2(5.0f, 0f)),
                    "Solid Ground wall must still block EnemyContext.CanStandAt.");

                // Suppress unused local warning
                Assert.IsNotNull(enemyGo);
            }
            finally
            {
                if (data != null)
                    Object.DestroyImmediate(data);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyLunge_StopsAtPlatformLedge_WhenGroundedOrDescending()
        {
            var root = new GameObject("__Round8EnemyLungeLedgeTest");
            EnemyData data = null;
            try
            {
                LayerMask groundMask = (1 << EnemyGroundLayer) | (1 << EnemyOneWayLayer);
                LayerMask targetMask = 1 << EnemyPlayerLayer;

                // Solid platform ending at x = 0.45f (top at y = 0)
                var platform = new GameObject("Platform_Ledge");
                platform.transform.SetParent(root.transform);
                platform.layer = EnemyGroundLayer;
                platform.transform.position = new Vector3(-1.275f, -0.5f, 0f);
                var platformCol = platform.AddComponent<BoxCollider2D>();
                platformCol.size = new Vector2(3.45f, 1.0f);

                var playerGo = new GameObject("Player");
                playerGo.transform.SetParent(root.transform);
                playerGo.layer = EnemyPlayerLayer;
                playerGo.transform.position = new Vector3(3.0f, 0f, 0f);

                var ctx = CreateEnemyContextHarness(root.transform, Vector2.zero, groundMask, targetMask,
                    out var enemyGo, out var rb, out _, out data, new[]
                    {
                        new EnemyData.Skill
                        {
                            Kind = EnemyAttackKind.Lunge,
                            ChargeAnim = "LungeLoad",
                            ExecuteAnim = "Lunge",
                            Charge = 0f,
                            TravelCells = 4f,
                            TravelDuration = 0.5f,
                            LungeUpSpeed = 6f,
                            Recover = 0.2f
                        }
                    });
                ctx.Player = playerGo.transform;
                ctx.Face(1);

                var machine = new EnemyStateMachine(ctx);
                var lunge = (EnemyLungeAttackState)machine.Skills[0];

                // 1. Grounded at ledge (IsGrounded == true, IsGroundAhead == false)
                Physics2D.SyncTransforms();
                ctx.RefreshFrame();
                Assert.IsTrue(ctx.IsGrounded, "Precondition: enemy must be grounded on platform.");
                Assert.IsFalse(ctx.IsGroundAhead(1), "Precondition: no floor ahead past platform ledge.");

                lunge.Enter();
                lunge.Tick();
                lunge.FixedTick();
                Assert.AreEqual(0f, ctx.Velocity.x, 0.001f,
                    "Grounded enemy lunging at a platform ledge must clamp horizontal velocity to 0.");

                // 2. Descending off ledge (IsGrounded == false, Velocity.y <= 0.5f, IsGroundAhead == false)
                enemyGo.transform.position = new Vector3(1.2f, 0.3f, 0f);
                rb.position = new Vector2(1.2f, 0.3f);
                Physics2D.SyncTransforms();
                ctx.RefreshFrame();
                Assert.IsFalse(ctx.IsGrounded, "Precondition: enemy past ledge must not be grounded.");
                Assert.IsFalse(ctx.IsGroundAhead(1), "Precondition: no floor ahead while descending off ledge.");

                ctx.SetVelocity(new Vector2(6f, -2f));
                lunge.FixedTick();
                Assert.AreEqual(0f, ctx.Velocity.x, 0.001f,
                    "Descending enemy lunging with no floor ahead must clamp horizontal velocity to 0.");
            }
            finally
            {
                if (data != null)
                    Object.DestroyImmediate(data);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyArrow_PassesThroughInvulnerableRollingPlayer()
        {
            var root = new GameObject("__Round8EnemyArrowRollTest");
            try
            {
                LayerMask groundMask = (1 << EnemyGroundLayer) | (1 << EnemyOneWayLayer);
                LayerMask targetMask = 1 << EnemyPlayerLayer;

                var arrowGo = new GameObject("EnemyArrow");
                arrowGo.transform.SetParent(root.transform);
                var rb = arrowGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                var arrowCol = arrowGo.AddComponent<BoxCollider2D>();
                arrowCol.isTrigger = true;
                var sr = arrowGo.AddComponent<SpriteRenderer>();
                var projectile = arrowGo.AddComponent<Projectile>();
                EnemySetPrivateField(projectile, "_mode", Projectile.Mode.Straight);
                EnemySetPrivateField(projectile, "_renderer", sr);

                bool released = false;
                projectile.Setup(_ => released = true, vfx: null, targets: targetMask, ground: groundMask);
                projectile.LaunchStraight(Vector2.zero, new Vector2(12f, 0f), damage: 15, knockback: 2f, owner: root);

                var playerHurtbox = CreateBlastTarget(root.transform, "RollingPlayer", new Vector2(2f, 0f),
                    new Vector2(0.8f, 1.5f), out var playerHealth);
                playerHealth.DamageFilter = _ => false; // Rolling / i-frames veto damage

                EnemyInvokePrivateMethod(projectile, "OnTriggerEnter2D", playerHurtbox);

                Assert.IsTrue(projectile.IsLive, "Enemy straight arrow must remain live when passing through rolling player.");
                Assert.IsTrue(projectile.gameObject.activeSelf, "Enemy straight arrow must stay active after blocked hit.");
                Assert.IsFalse(released, "Enemy straight arrow must not be released to pool on invulnerable player.");
                Assert.AreEqual(100, playerHealth.Current, "Rolling player must take 0 damage.");

                // Non-invulnerable hit afterward still connects and finishes projectile
                playerHealth.DamageFilter = null;
                EnemyInvokePrivateMethod(projectile, "OnTriggerEnter2D", playerHurtbox);

                Assert.IsFalse(projectile.IsLive, "Enemy arrow must finish after hitting vulnerable player.");
                Assert.IsTrue(released, "Enemy arrow must return to pool on valid hit.");
                Assert.AreEqual(85, playerHealth.Current, "Vulnerable player must take arrow damage.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GrenadeExplode_IsCircular_BlockedBySolidWall_AndAppliesDirectionalKnockback()
        {
            var root = new GameObject("__Round8GrenadeBlastTest");
            try
            {
                LayerMask groundMask = (1 << EnemyGroundLayer) | (1 << EnemyOneWayLayer);
                LayerMask targetMask = 1 << EnemyPlayerLayer;

                // Solid Ground wall at x = 1.0 blocking Target B at (1.8, 0)
                var wallGo = new GameObject("SolidWall");
                wallGo.transform.SetParent(root.transform);
                wallGo.layer = EnemyGroundLayer;
                wallGo.transform.position = new Vector3(1.0f, 0f, 0f);
                var wallCol = wallGo.AddComponent<BoxCollider2D>();
                wallCol.size = new Vector2(0.4f, 4.0f);

                // Target A: diagonal corner (2.5, 2.5), dist = 3.535u > 2.9u (inside old 5.8x5.8 square box)
                CreateBlastTarget(root.transform, "TargetA_Corner", new Vector2(2.5f, 2.5f),
                    new Vector2(0.4f, 0.4f), out var targetAHealth);

                // Target B: at (1.8, 0) within radius 2.9u, but occluded by SolidWall at x = 1.0
                CreateBlastTarget(root.transform, "TargetB_BehindWall", new Vector2(1.8f, 0f),
                    new Vector2(0.4f, 0.4f), out var targetBHealth);

                // Target C: at (-1.8, 0) unobstructed within radius 2.9u
                CreateBlastTarget(root.transform, "TargetC_Exposed", new Vector2(-1.8f, 0f),
                    new Vector2(0.4f, 0.4f), out var targetCHealth);
                DamageInfo lastDamageC = default;
                bool damagedC = false;
                targetCHealth.Damaged += info =>
                {
                    damagedC = true;
                    lastDamageC = info;
                };

                var grenadeGo = new GameObject("GrenadeProjectile");
                grenadeGo.transform.SetParent(root.transform);
                var rb = grenadeGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                var col = grenadeGo.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.25f, 0.25f);
                var sr = grenadeGo.AddComponent<SpriteRenderer>();
                var projectile = grenadeGo.AddComponent<Projectile>();
                EnemySetPrivateField(projectile, "_mode", Projectile.Mode.Lob);
                EnemySetPrivateField(projectile, "_renderer", sr);

                projectile.Setup(_ => { }, vfx: null, targets: targetMask, ground: groundMask);
                projectile.LaunchLob(Vector2.zero, Vector2.zero, damage: 15, fuse: 0.5f,
                    blastRadius: 2.9f, blastVfx: null, owner: root);

                Physics2D.SyncTransforms();
                EnemyInvokePrivateMethod(projectile, "Explode");

                Assert.AreEqual(100, targetAHealth.Current,
                    "Target at (2.5, 2.5) with distance 3.53u > 2.9u must NOT be hit by circular grenade blast.");
                Assert.AreEqual(100, targetBHealth.Current,
                    "Target behind solid Ground wall at x = 1.0 must NOT take grenade splash damage.");
                Assert.IsTrue(damagedC, "Unobstructed target within blast radius must take grenade damage.");
                Assert.AreEqual(85, targetCHealth.Current, "Unobstructed target must lose grenade damage HP.");
                Assert.AreEqual(-1, lastDamageC.Direction,
                    "Target on left side of grenade (-1.8 < 0) must receive knockback direction -1.");
                Assert.GreaterOrEqual(lastDamageC.Knockback, 4f,
                    "Grenade blast must apply directional knockback >= 4.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static EnemyContext CreateEnemyContextHarness(
            Transform parent,
            Vector2 position,
            LayerMask groundMask,
            LayerMask targetMask,
            out GameObject enemyGo,
            out Rigidbody2D rb,
            out BoxCollider2D col,
            out EnemyData data,
            EnemyData.Skill[] skills = null)
        {
            enemyGo = new GameObject("EnemyHarness");
            enemyGo.transform.SetParent(parent);
            enemyGo.layer = EnemyActorLayer;
            enemyGo.transform.position = position;

            rb = enemyGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.position = position;

            col = enemyGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.5f);
            col.offset = new Vector2(0f, 0.75f);

            var sr = enemyGo.AddComponent<SpriteRenderer>();
            var animator = enemyGo.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ZombieControllerPath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.Rebind();
                animator.Update(0f);
            }

            var enemyAnim = enemyGo.AddComponent<EnemyAnimator>();
            EnemySetPrivateField(enemyAnim, "_animator", animator);
            EnemySetPrivateField(enemyAnim, "_renderer", sr);
            EnemyInvokePrivateMethod(enemyAnim, "Awake");

            var health = enemyGo.AddComponent<Health>();
            health.RestoreFull();

            data = ScriptableObject.CreateInstance<EnemyData>();
            if (skills != null)
                EnemySetPrivateField(data, "_skills", skills);

            return new EnemyContext(rb, col, enemyAnim, health, data, groundMask, targetMask);
        }

        private static BoxCollider2D CreateBlastTarget(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            out Health health)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.layer = EnemyPlayerLayer;
            go.transform.position = position;

            health = go.AddComponent<Health>();
            health.RestoreFull();

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            var hurtbox = go.AddComponent<Hurtbox>();
            EnemySetPrivateField(hurtbox, "_health", health);
            return col;
        }

        private static void EnemySetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static void EnemyInvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method {target.GetType().Name}.{methodName}");
            method.Invoke(target, args);
        }
    }
}
