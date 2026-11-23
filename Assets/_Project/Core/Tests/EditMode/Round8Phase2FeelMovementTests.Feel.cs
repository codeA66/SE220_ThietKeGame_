using System.Reflection;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public partial class Round8Phase2FeelMovementTests
    {
        private const int FeelGroundLayer = 6;
        private const int FeelPlayerLayer = 7;
        private const int FeelEnemyLayer = 8;
        private const int FeelOneWayLayer = 9;
        private const int FeelClimbableLayer = 10;

        private const string FeelSwordDataPath = "Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset";
        private const string FeelBowDataPath = "Assets/_Project/Features/Combat/Data/Bow_StartBow.asset";
        private const string FeelArrowPrefabPath = "Assets/_Project/Features/Player/Prefabs/Projectile_PlayerArrow.prefab";
        private const string FeelMovementConfigPath = "Assets/_Project/Features/Player/Data/PlayerMovementConfig.asset";
        private const string FeelPlayerControllerPath = "Assets/_Project/Features/Player/Animations/Player.controller";

        [Test]
        public void StartSwordAsset_HasDeadCellsCadence_Knockback_AndFinisherCrit()
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(FeelSwordDataPath);
            Assert.IsNotNull(weapon, $"Missing WeaponData at {FeelSwordDataPath}");
            Assert.AreEqual(3, weapon.ComboLength, "Weapon_StartSword must have 3 combo strikes.");

            // Round 9 hack-and-slash tuning (P0-05): the same cadence, faster, further and heavier.
            var s0 = weapon.GetStrike(0);
            Assert.AreEqual(25, s0.Damage, "Strike 0 damage.");
            Assert.AreEqual(5, s0.HitFrame, "Strike 0 hitFrame.");
            Assert.AreEqual(1.40f, s0.AnimSpeed, 0.001f, "Strike 0 AnimSpeed must be 1.40 for the Round 9 fast opener.");
            Assert.AreEqual(2.9f, s0.RangeWidthCells, 0.001f, "Strike 0 reach must be 2.9 cells.");
            Assert.AreEqual(5.2f, s0.Knockback, 0.001f, "Strike 0 Knockback must be 5.2.");
            Assert.IsFalse(s0.CanCrit, "Strike 0 must not crit.");

            var s1 = weapon.GetStrike(1);
            Assert.AreEqual(30, s1.Damage, "Strike 1 damage.");
            Assert.AreEqual(7, s1.HitFrame, "Strike 1 hitFrame.");
            Assert.AreEqual(1.48f, s1.AnimSpeed, 0.001f, "Strike 1 AnimSpeed must be 1.48 for the Round 9 fast chain.");
            Assert.AreEqual(3.0f, s1.RangeWidthCells, 0.001f, "Strike 1 reach must be 3.0 cells.");
            Assert.AreEqual(6.2f, s1.Knockback, 0.001f, "Strike 1 Knockback must be 6.2.");
            Assert.IsFalse(s1.CanCrit, "Strike 1 must not crit.");

            var s2 = weapon.GetStrike(2);
            Assert.AreEqual(45, s2.Damage, "Strike 2 damage.");
            Assert.AreEqual(9, s2.HitFrame, "Strike 2 hitFrame.");
            Assert.AreEqual(1.05f, s2.AnimSpeed, 0.001f, "Strike 2 AnimSpeed must be 1.05 for heavy finisher weight.");
            Assert.AreEqual(3.85f, s2.RangeWidthCells, 0.001f, "Strike 2 finisher reach must be 3.85 cells.");
            Assert.AreEqual(2.7f, s2.RangeHeightCells, 0.001f, "Strike 2 finisher must sweep 2.7 cells tall.");
            Assert.AreEqual(10.5f, s2.Knockback, 0.001f, "Strike 2 Knockback must be 10.5.");
            Assert.IsTrue(s2.CanCrit, "Strike 2 finisher must have CanCrit = true.");
        }

        [Test]
        public void CameraFollow2D_Punch_ProducesDirectionalKickInPunchDirection()
        {
            var root = new GameObject("__Round8CameraPunchTest");
            try
            {
                var targetGo = new GameObject("Target");
                targetGo.transform.SetParent(root.transform);
                targetGo.transform.position = Vector3.zero;

                var camGo = new GameObject("MainCamera");
                camGo.transform.SetParent(root.transform);
                camGo.transform.position = new Vector3(0f, 1.5f, -10f);
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;

                var follow = camGo.AddComponent<CameraFollow2D>();
                FeelSetPrivateField(follow, "_target", targetGo.transform);
                FeelInvokePrivateMethod(follow, "Awake");
                FeelInvokePrivateMethod(follow, "Start");

                follow.Punch(Vector2.right, 0.24f, 0.12f);
                Vector3 rightPunch = follow.CurrentPunchOffset;
                Assert.Greater(rightPunch.x, 0.15f, "Rightward Punch must produce positive X offset > 0.15.");
                Assert.AreEqual(0f, rightPunch.y, 0.001f, "Horizontal Punch must keep Y offset at 0.");

                FeelInvokePrivateMethod(follow, "LateUpdate");
                Assert.Greater(camGo.transform.position.x, 0.15f,
                    "CameraFollow2D.LateUpdate must add directional Punch offset onto camera position.");

                follow.Punch(Vector2.left, 0.24f, 0.12f);
                Vector3 leftPunch = follow.CurrentPunchOffset;
                Assert.Less(leftPunch.x, -0.15f, "Leftward Punch must produce negative X offset < -0.15.");
                Assert.AreEqual(0f, leftPunch.y, 0.001f, "Leftward horizontal Punch must keep Y offset at 0.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerFeedback_TiersHitstopAndPunch_ForLightCritKillStaggerAndPerfectDodge()
        {
            var root = new GameObject("__Round8PlayerFeedbackTierTest");
            float savedTimeScale = Time.timeScale;
            HitStop hitStop = null;
            try
            {
                Time.timeScale = 1f;

                var camGo = new GameObject("MainCamera");
                camGo.transform.SetParent(root.transform);
                camGo.transform.position = new Vector3(0f, 1.5f, -10f);
                camGo.AddComponent<Camera>();
                var follow = camGo.AddComponent<CameraFollow2D>();

                var hitStopGo = new GameObject("HitStop");
                hitStopGo.transform.SetParent(root.transform);
                hitStop = hitStopGo.AddComponent<HitStop>();

                var controller = CreateFeelPlayerHarness(root.transform, Vector2.zero, out var context, out var sprite);
                FeelSetPrivateField(follow, "_target", controller.transform);
                FeelInvokePrivateMethod(follow, "Awake");
                FeelInvokePrivateMethod(follow, "Start");

                var feedback = controller.gameObject.AddComponent<PlayerFeedback>();
                FeelSetPrivateField(feedback, "_controller", controller);
                FeelSetPrivateField(feedback, "_sprite", sprite);
                FeelSetPrivateField(feedback, "_hitStop", hitStop);
                FeelSetPrivateField(feedback, "_camera", follow);
                FeelInvokePrivateMethod(feedback, "Start");

                var enemyHealth = CreateFeelEnemyTarget(root.transform, new Vector2(1.5f, 0f));
                Physics2D.SyncTransforms();

                // 1. Perfect Dodge: hitStop ~0.055s, flash triggered, no directional punch
                ResetHitStopProbe(hitStop);
                context.BeginRoll();
                Assert.IsTrue(context.TryRegisterPerfectDodge(), "Precondition: perfect dodge window must be open on BeginRoll.");
                float dodgeDuration = FeelGetHitStopRemaining(hitStop);
                Assert.AreEqual(0f, Time.timeScale, 0.001f, "PerfectDodge must trigger HitStop (Time.timeScale == 0).");
                Assert.GreaterOrEqual(dodgeDuration, 0.045f, "PerfectDodge must trigger >= 0.045s HitStop.");
                Assert.Greater(FeelGetPrivateField<float>(feedback, "_flashUntil"), Time.unscaledTime,
                    "PerfectDodge must trigger brief white flash.");
                Assert.AreEqual(Vector3.zero, follow.CurrentPunchOffset,
                    "PerfectDodge must not apply directional camera punch.");
                context.EndRoll();

                // 2. Light melee hit: hitStop ~0.045s, rightward punch ~0.12f
                ResetHitStopProbe(hitStop);
                var lightStrike = new WeaponData.Strike
                {
                    Damage = 10,
                    RangeWidthCells = 3f,
                    RangeHeightCells = 2f,
                    Knockback = 3.5f,
                    CanCrit = false
                };
                context.Combat.BeginSwing();
                Assert.AreEqual(1, context.Combat.PerformStrike(lightStrike, 1), "Light strike must hit enemy.");
                float lightDuration = FeelGetHitStopRemaining(hitStop);
                Assert.AreEqual(0f, Time.timeScale, 0.001f, "Light hit must trigger HitStop.");
                Assert.GreaterOrEqual(lightDuration, 0.040f, "Light hit HitStop must be ~0.045s.");
                Assert.Less(lightDuration, 0.060f, "Light hit HitStop must be shorter than Crit tier.");
                Assert.Greater(follow.CurrentPunchOffset.x, 0.08f, "Light hit facing +1 must kick camera right.");

                // 3. Shield Stagger: hitStop ~0.055s, punch in stagger direction (-1), flash triggered
                ResetHitStopProbe(hitStop);
                FeelSetPrivateField(feedback, "_flashUntil", -1f);
                controller.Stagger(0.4f, -1);
                float staggerDuration = FeelGetHitStopRemaining(hitStop);
                Assert.AreEqual(0f, Time.timeScale, 0.001f, "Shield stagger must trigger HitStop.");
                Assert.GreaterOrEqual(staggerDuration, 0.045f, "Shield stagger HitStop must be ~0.055s.");
                Assert.Less(follow.CurrentPunchOffset.x, -0.10f, "Shield stagger dir=-1 must punch camera left.");
                Assert.Greater(FeelGetPrivateField<float>(feedback, "_flashUntil"), Time.unscaledTime,
                    "Shield stagger must trigger brief white flash.");

                // 4. Critical non-fatal hit: hitStop ~0.075s, stronger punch ~0.22f
                ResetHitStopProbe(hitStop);
                var critStrike = new WeaponData.Strike
                {
                    Damage = 10,
                    RangeWidthCells = 3f,
                    RangeHeightCells = 2f,
                    Knockback = 8f,
                    CanCrit = true
                };
                context.Combat.BeginSwing();
                Assert.AreEqual(1, context.Combat.PerformStrike(critStrike, 1), "Crit strike must hit alive enemy.");
                Assert.IsTrue(enemyHealth.IsAlive, "Precondition: enemy must remain alive after non-fatal crit.");
                float critDuration = FeelGetHitStopRemaining(hitStop);
                Assert.AreEqual(0f, Time.timeScale, 0.001f, "Critical hit must trigger HitStop.");
                Assert.Greater(critDuration, lightDuration + 0.015f, "Crit HitStop (~0.075s) must exceed Light HitStop (~0.045s).");
                Assert.Greater(follow.CurrentPunchOffset.x, 0.16f, "Crit hit must produce stronger directional punch (~0.22).");

                // 5. Fatal kill hit: hitStop ~0.095s, strongest punch ~0.26f
                ResetHitStopProbe(hitStop);
                var killStrike = new WeaponData.Strike
                {
                    Damage = 200,
                    RangeWidthCells = 3f,
                    RangeHeightCells = 2f,
                    Knockback = 8f,
                    CanCrit = true
                };
                context.Combat.BeginSwing();
                Assert.AreEqual(1, context.Combat.PerformStrike(killStrike, 1), "Fatal strike must hit enemy.");
                Assert.IsFalse(enemyHealth.IsAlive, "Precondition: fatal strike must kill enemy.");
                float killDuration = FeelGetHitStopRemaining(hitStop);
                Assert.AreEqual(0f, Time.timeScale, 0.001f, "Fatal kill must trigger HitStop.");
                Assert.Greater(killDuration, critDuration + 0.010f, "Fatal kill HitStop (~0.095s) must exceed Crit HitStop (~0.075s).");
                Assert.Greater(follow.CurrentPunchOffset.x, 0.20f, "Fatal kill must produce strongest directional punch (~0.26).");
            }
            finally
            {
                if (hitStop != null)
                    FeelInvokePrivateMethod(hitStop, "OnDisable");
                Time.timeScale = savedTimeScale;
                Object.DestroyImmediate(root);
            }
        }

        private static PlayerController CreateFeelPlayerHarness(
            Transform parent,
            Vector2 position,
            out PlayerContext context,
            out SpriteRenderer spriteRenderer)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(FeelMovementConfigPath);
            var swordData = AssetDatabase.LoadAssetAtPath<WeaponData>(FeelSwordDataPath);
            var bowData = AssetDatabase.LoadAssetAtPath<BowWeaponData>(FeelBowDataPath);
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<RecoverableArrow>(FeelArrowPrefabPath);
            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(FeelPlayerControllerPath);

            Assert.IsNotNull(config, $"Missing PlayerMovementConfig at {FeelMovementConfigPath}");
            Assert.IsNotNull(swordData, $"Missing WeaponData at {FeelSwordDataPath}");
            Assert.IsNotNull(bowData, $"Missing BowWeaponData at {FeelBowDataPath}");
            Assert.IsNotNull(arrowPrefab, $"Missing arrow prefab at {FeelArrowPrefabPath}");
            Assert.IsNotNull(animController, $"Missing player controller at {FeelPlayerControllerPath}");

            var playerGo = new GameObject("PlayerHarness");
            playerGo.transform.SetParent(parent);
            playerGo.layer = FeelPlayerLayer;
            playerGo.transform.position = position;

            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = config.GravityScale;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.position = position;

            var col = playerGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(playerGo.transform, false);
            spriteRenderer = visualGo.AddComponent<SpriteRenderer>();
            var animator = visualGo.AddComponent<Animator>();
            animator.runtimeAnimatorController = animController;
            animator.Rebind();
            animator.Update(0f);

            var animBridge = playerGo.AddComponent<PlayerAnimatorBridge>();
            FeelSetPrivateField(animBridge, "_animator", animator);
            FeelInvokePrivateMethod(animBridge, "Awake");

            var input = playerGo.AddComponent<PlayerInputReader>();
            var health = playerGo.AddComponent<Health>();
            health.RestoreFull();

            var bow = playerGo.AddComponent<PlayerBow>();
            FeelSetPrivateField(bow, "_data", bowData);
            FeelSetPrivateField(bow, "_arrowPrefab", arrowPrefab);

            var controller = playerGo.AddComponent<PlayerController>();
            FeelSetPrivateField(controller, "_config", config);
            FeelSetPrivateField(controller, "_input", input);
            FeelSetPrivateField(controller, "_animator", animBridge);
            FeelSetPrivateField(controller, "_sprite", spriteRenderer);
            FeelSetPrivateField(controller, "_groundMask", (LayerMask)((1 << FeelGroundLayer) | (1 << FeelOneWayLayer)));
            FeelSetPrivateField(controller, "_rollPassThroughMask", (LayerMask)(1 << FeelEnemyLayer));
            FeelSetPrivateField(controller, "_climbableMask", (LayerMask)(1 << FeelClimbableLayer));
            FeelSetPrivateField(controller, "_health", health);
            FeelSetPrivateField(controller, "_weapon", swordData);
            FeelSetPrivateField(controller, "_bow", bow);
            FeelSetPrivateField(controller, "_attackTargets", (LayerMask)(1 << FeelEnemyLayer));
            FeelSetPrivateField(controller, "_autoRespawn", false);

            FeelInvokePrivateMethod(controller, "Awake");
            context = controller.Context;
            return controller;
        }

        private static Health CreateFeelEnemyTarget(Transform parent, Vector2 position)
        {
            var go = new GameObject("EnemyTarget");
            go.transform.SetParent(parent);
            go.layer = FeelEnemyLayer;
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider2D>();
            col.offset = new Vector2(0f, 0.75f);
            col.size = new Vector2(0.8f, 1.5f);

            var health = go.AddComponent<Health>();
            health.Initialize(100, 0f);

            var hurtbox = go.AddComponent<Hurtbox>();
            FeelSetPrivateField(hurtbox, "_health", health);
            return health;
        }

        private static void ResetHitStopProbe(HitStop hitStop)
        {
            FeelSetPrivateField(hitStop, "_resumeAt", -1f);
            Time.timeScale = 1f;
        }

        private static float FeelGetHitStopRemaining(HitStop hitStop) =>
            FeelGetPrivateField<float>(hitStop, "_resumeAt") - Time.unscaledTime;

        private static void FeelSetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static T FeelGetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void FeelInvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method {target.GetType().Name}.{methodName}");
            method.Invoke(target, args);
        }
    }
}
