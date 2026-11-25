using System.Reflection;
using DeadCells.Combat;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using DeadCells.Player.StateMachine.States;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public partial class Round8Phase2FeelMovementTests
    {
        private const int MovementGroundLayer = 6;
        private const int MovementPlayerLayer = 7;
        private const int MovementEnemyLayer = 8;
        private const int MovementOneWayLayer = 9;
        private const int MovementClimbableLayer = 10;

        private const string MovementConfigPath = "Assets/_Project/Features/Player/Data/PlayerMovementConfig.asset";
        private const string MovementSwordDataPath = "Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset";
        private const string MovementBowDataPath = "Assets/_Project/Features/Combat/Data/Bow_StartBow.asset";
        private const string MovementArrowPrefabPath = "Assets/_Project/Features/Player/Prefabs/Projectile_PlayerArrow.prefab";
        private const string MovementPlayerControllerPath = "Assets/_Project/Features/Player/Animations/Player.controller";

        // ==================== Task 2.1: Chain Climb Hand Alignment ====================

        [Test]
        public void ChainClimb_AlignsHandsToChainCenter_AndFlipsSideWhenTurningOnChain()
        {
            var root = new GameObject("__Round8Phase2ChainClimbTest");
            try
            {
                var chainGo = new GameObject("Climbable_Chain");
                chainGo.transform.SetParent(root.transform);
                chainGo.layer = MovementClimbableLayer;
                chainGo.transform.position = new Vector3(10f, 4.5f, 0f);
                var chainCol = chainGo.AddComponent<BoxCollider2D>();
                chainCol.isTrigger = true;
                chainCol.size = new Vector2(0.5f, 9f);

                var harness = CreateMovementPlayerHarness(root.transform, new Vector2(10f, 2f));
                Physics2D.SyncTransforms();

                Assert.AreEqual(0.24f, harness.Context.Config.ClimbHandOffsetX, 0.001f,
                    "PlayerMovementConfig.ClimbHandOffsetX must be 0.24.");

                harness.Context.Face(1);
                Assert.IsTrue(harness.Context.TryFindClimbable(), "Precondition: player must overlap chain trigger.");

                harness.Context.BeginClimb();
                Assert.AreEqual(9.76f, harness.Context.Position.x, 0.01f,
                    "Facing +1 on x=10 chain must offset body left to x=9.76 so hands align with chain center.");

                // Sync transform to rigidbody position and confirm the offset collider still overlaps the 0.5u chain trigger
                harness.PlayerGo.transform.position = new Vector3(harness.Context.Position.x, harness.Context.Position.y, 0f);
                Physics2D.SyncTransforms();
                Assert.IsTrue(harness.Context.TryFindClimbable(),
                    "Offset player collider must still overlap the 0.5u chain trigger.");

                ForceMovementState(harness.Machine, harness.Machine.Climb);
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveX), -1f);
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveY), 0f);
                harness.Machine.Tick();

                Assert.AreEqual(-1, harness.Context.FacingDirection, "Pressing left on chain must flip FacingDirection to -1.");
                Assert.AreEqual(10.24f, harness.Context.Position.x, 0.01f,
                    "Turning to -1 on x=10 chain must mirror body offset right to x=10.24.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ==================== Task 2.2: Wall-Cling / Wall-Jump & Ledge Tolerance ====================

        [Test]
        public void WallCling_GripsSolidWall_SlidesAt28_WallJumpsAway_AndEnforcesRegrabLockAnd3JumpCap()
        {
            var root = new GameObject("__Round8Phase2WallClingTest");
            try
            {
                // Solid Ground wall with left face at x = 1.0
                CreateMovementBox(root.transform, "SolidWall", MovementGroundLayer, new Vector2(1.5f, 5.0f), new Vector2(1.0f, 10.0f));

                var harness = CreateMovementPlayerHarness(root.transform, new Vector2(0.52f, 5.0f));
                Physics2D.SyncTransforms();

                harness.Context.RefreshFrame();
                harness.Context.RefillAirMoves();
                Assert.AreEqual(3, harness.Context.WallJumpsRemaining, "MaxWallJumpsPerAir must refill to 3.");

                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveX), 1f);
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                harness.Context.SetVerticalVelocity(-6f);

                Assert.IsTrue(harness.Context.TryFindWall(out int wallDir), "Airborne player pressing into solid wall must detect wall.");
                Assert.AreEqual(1, wallDir);

                ForceMovementState(harness.Machine, harness.Machine.Fall);
                harness.Machine.Tick();
                Assert.AreSame(harness.Machine.WallCling, harness.Machine.Current,
                    "TryAirActions in FallState must transition to WallCling.");

                harness.Machine.FixedTick();
                Assert.GreaterOrEqual(harness.Context.Velocity.y, -2.801f,
                    "WallCling FixedTick must cap downward wall-slide speed at 2.8 u/s.");
                Assert.AreEqual(0f, harness.Context.Velocity.x, 0.001f);

                // Wall Jump #1: kicks away at (-9.5, 13.2) and enforces horizontal & regrab locks
                MovementSetPrivateField(harness.Machine, "_lastTransitionFrame", -1);
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                harness.Machine.Tick();

                Assert.AreSame(harness.Machine.Jump, harness.Machine.Current,
                    "Pressing Jump while WallClinging must transition to Jump.");
                Assert.AreEqual(-9.5f, harness.Context.Velocity.x, 0.01f, "Wall jump must kick away at -9.5 u/s.");
                Assert.AreEqual(13.2f, harness.Context.Velocity.y, 0.01f, "Wall jump must launch upward at 13.2 u/s.");
                Assert.AreEqual(-1, harness.Context.FacingDirection, "Wall jump must face away from wall (-1).");
                Assert.AreEqual(2, harness.Context.WallJumpsRemaining, "First wall jump must decrement WallJumpsRemaining to 2.");
                Assert.IsTrue(harness.Context.IsWallJumpHorizontalLocked,
                    "Wall jump must engage WallJumpHorizontalLockTime.");
                Assert.IsFalse(harness.Context.TryFindWall(out _),
                    "TryFindWall must return false during WallRegrabCooldown.");

                // Horizontal lock keeps -9.5 velocity even while player still holds MoveX = +1 toward wall
                harness.Machine.FixedTick();
                Assert.AreEqual(-9.5f, harness.Context.Velocity.x, 0.01f,
                    "ApplyAirControl must preserve wall-jump horizontal velocity during lock.");

                // AirJump within the 0.12s wall-jump lock must still fire StartAirJump()
                int airJumpsBefore = harness.Context.AirJumpsRemaining;
                MovementSetPrivateField(harness.Machine, "_lastTransitionFrame", -1);
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                harness.Machine.Tick();
                Assert.AreSame(harness.Machine.AirJump, harness.Machine.Current,
                    "Double-jumping during wall-jump horizontal lock must transition to AirJump.");
                Assert.AreEqual(airJumpsBefore - 1, harness.Context.AirJumpsRemaining,
                    "PlayerAirJumpState must execute StartAirJump() even while IsWallJumpHorizontalLocked is true.");
                Assert.AreEqual(harness.Context.Config.AirJumpVelocity, harness.Context.Velocity.y, 0.01f);

                // Exhaust remaining 2 wall jumps (2 -> 1 -> 0) after simulating cooldown expiration
                for (int jump = 2; jump <= 3; jump++)
                {
                    ClearWallTimers(harness.Context);
                    MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveX), 1f);
                    MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                    harness.Context.SetVerticalVelocity(-2f);
                    Assert.IsTrue(harness.Context.TryFindWall(out _));
                    ForceMovementState(harness.Machine, harness.Machine.WallCling);

                    MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                    harness.Machine.Tick();
                    Assert.AreSame(harness.Machine.Jump, harness.Machine.Current, $"Wall jump #{jump} must succeed.");
                    Assert.AreEqual(3 - jump, harness.Context.WallJumpsRemaining);
                }

                // 4th wall jump without touching ground must be blocked
                ClearWallTimers(harness.Context);
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveX), 1f);
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                harness.Context.SetVerticalVelocity(-2f);
                ForceMovementState(harness.Machine, harness.Machine.WallCling);

                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                harness.Machine.Tick();
                Assert.AreEqual(0, harness.Context.WallJumpsRemaining);
                Assert.AreNotSame(harness.Machine.Jump, harness.Machine.Current,
                    "4th wall jump in same airtime must be blocked when WallJumpsRemaining == 0.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WallCling_IgnoresOneWayPlatforms_AndYieldsPriorityToLedgeClimb()
        {
            var root = new GameObject("__Round8Phase2WallPriorityTest");
            try
            {
                // 1. OneWayPlatform adjacent to airborne player must NOT trigger WallCling
                var oneWayGo = CreateMovementBox(root.transform, "OneWayWall", MovementOneWayLayer,
                    new Vector2(1.5f, 5.0f), new Vector2(1.0f, 10.0f));
                var harness = CreateMovementPlayerHarness(root.transform, new Vector2(0.52f, 5.0f));
                Physics2D.SyncTransforms();

                harness.Context.RefreshFrame();
                harness.Context.RefillAirMoves();
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveX), 1f);
                harness.Context.SetVerticalVelocity(-3f);

                Assert.IsFalse(harness.Context.TryFindWall(out _),
                    "OneWayPlatform must not trigger WallCling.");

                // 2. Solid ledge top at feetY + 1.30 (chest ray hits, head ray clears) -> LedgeClimb preempts WallCling
                oneWayGo.SetActive(false);
                CreateMovementBox(root.transform, "SolidLedge", MovementGroundLayer,
                    new Vector2(1.5f, 5.40f), new Vector2(1.0f, 1.80f));
                Physics2D.SyncTransforms();

                Assert.IsTrue(harness.Context.TryFindLedge(out _), "Precondition: TryFindLedge must succeed at solid ledge.");
                Assert.IsTrue(harness.Context.TryFindWall(out _), "Precondition: TryFindWall also sees the solid wall below top.");

                ForceMovementState(harness.Machine, harness.Machine.Fall);
                harness.Machine.Tick();

                Assert.AreSame(harness.Machine.LedgeClimb, harness.Machine.Current,
                    "TryAirActions must give LedgeClimb priority over WallCling when a ledge is climbable.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LedgeClimb_With042Tolerance_CatchesOneWayEdgeAtMaxFallSpeed()
        {
            var root = new GameObject("__Round8Phase2OneWayLedgeTest");
            try
            {
                var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
                Assert.IsNotNull(config);
                Assert.AreEqual(0.42f, config.OneWayLedgeGrabTolerance, 0.001f,
                    "OneWayLedgeGrabTolerance must be widened to 0.42 to catch max-fall-speed frames.");

                // OneWayPlatform top at y = 5.00, left edge at x = 1.00
                var platformGo = CreateMovementBox(root.transform, "OneWayEdge", MovementOneWayLayer,
                    new Vector2(2.5f, 4.875f), new Vector2(3.0f, 0.25f));
                var platformCol = platformGo.GetComponent<BoxCollider2D>();
                platformCol.usedByEffector = true;
                var effector = platformGo.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;

                // Player handY (feetY + 1.70) = 4.67 => 0.33u below platform top (1 frame drop at -20 u/s @ 60fps)
                var harness = CreateMovementPlayerHarness(root.transform, new Vector2(0.50f, 2.97f));
                Physics2D.SyncTransforms();

                harness.Context.RefreshFrame();
                harness.Context.Face(1);
                harness.Context.SetVerticalVelocity(-20f);

                // Case A: MoveX held toward edge
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveX), 1f);
                Assert.IsTrue(harness.Context.TryFindLedge(out var standPoint),
                    "0.42 tolerance must catch OneWay edge across 0.33u single-frame drop at -20 u/s.");
                Assert.AreEqual(5.0f, standPoint.y, 0.05f);

                // Case B: MoveX released 1 frame before edge, horizontal momentum carries player toward edge
                MovementSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                harness.Context.SetHorizontalVelocity(4.0f);
                Assert.IsTrue(harness.Context.TryFindLedge(out _),
                    "TryFindLedge must catch ledge when Velocity.x * FacingDirection > 0.5 even if MoveX is 0.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ==================== Test Harnesses & Helpers ====================

        private readonly struct MovementPlayerHarness
        {
            public readonly GameObject PlayerGo;
            public readonly PlayerController Controller;
            public readonly PlayerContext Context;
            public readonly PlayerStateMachine Machine;
            public readonly PlayerInputReader Input;

            public MovementPlayerHarness(GameObject playerGo, PlayerController controller, PlayerContext context,
                PlayerStateMachine machine, PlayerInputReader input)
            {
                PlayerGo = playerGo;
                Controller = controller;
                Context = context;
                Machine = machine;
                Input = input;
            }
        }

        private static MovementPlayerHarness CreateMovementPlayerHarness(Transform parent, Vector2 position)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
            var swordData = AssetDatabase.LoadAssetAtPath<WeaponData>(MovementSwordDataPath);
            var bowData = AssetDatabase.LoadAssetAtPath<BowWeaponData>(MovementBowDataPath);
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<RecoverableArrow>(MovementArrowPrefabPath);
            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MovementPlayerControllerPath);

            Assert.IsNotNull(config);
            Assert.IsNotNull(swordData);
            Assert.IsNotNull(bowData);
            Assert.IsNotNull(arrowPrefab);
            Assert.IsNotNull(animController);

            var playerGo = new GameObject("PlayerHarness");
            playerGo.transform.SetParent(parent);
            playerGo.layer = MovementPlayerLayer;
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
            var sr = visualGo.AddComponent<SpriteRenderer>();
            var animator = visualGo.AddComponent<Animator>();
            animator.runtimeAnimatorController = animController;
            animator.Rebind();
            animator.Update(0f);

            var animBridge = playerGo.AddComponent<PlayerAnimatorBridge>();
            MovementSetPrivateField(animBridge, "_animator", animator);
            MovementInvokePrivateMethod(animBridge, "Awake");

            var input = playerGo.AddComponent<PlayerInputReader>();
            var health = playerGo.AddComponent<Health>();
            health.RestoreFull();

            var bow = playerGo.AddComponent<PlayerBow>();
            MovementSetPrivateField(bow, "_data", bowData);
            MovementSetPrivateField(bow, "_arrowPrefab", arrowPrefab);

            var controller = playerGo.AddComponent<PlayerController>();
            MovementSetPrivateField(controller, "_config", config);
            MovementSetPrivateField(controller, "_input", input);
            MovementSetPrivateField(controller, "_animator", animBridge);
            MovementSetPrivateField(controller, "_sprite", sr);
            MovementSetPrivateField(controller, "_groundMask", (LayerMask)((1 << MovementGroundLayer) | (1 << MovementOneWayLayer)));
            MovementSetPrivateField(controller, "_rollPassThroughMask", (LayerMask)(1 << MovementEnemyLayer));
            MovementSetPrivateField(controller, "_climbableMask", (LayerMask)(1 << MovementClimbableLayer));
            MovementSetPrivateField(controller, "_health", health);
            MovementSetPrivateField(controller, "_weapon", swordData);
            MovementSetPrivateField(controller, "_bow", bow);
            MovementSetPrivateField(controller, "_attackTargets", (LayerMask)(1 << MovementEnemyLayer));
            MovementSetPrivateField(controller, "_autoRespawn", false);

            MovementInvokePrivateMethod(controller, "Awake");

            var machine = MovementGetPrivateField<PlayerStateMachine>(controller, "_machine");
            return new MovementPlayerHarness(playerGo, controller, controller.Context, machine, input);
        }

        private static GameObject CreateMovementBox(Transform parent, string name, int layer, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.layer = layer;
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            return go;
        }

        private static void ForceMovementState(PlayerStateMachine machine, IPlayerState state)
        {
            machine.ForceChangeState(state);
            MovementSetPrivateField(machine, "_lastTransitionFrame", -1);
        }

        private static void ClearWallTimers(PlayerContext ctx)
        {
            var regrabField = typeof(PlayerContext).GetField("_wallRegrabReadyAt", BindingFlags.Instance | BindingFlags.NonPublic);
            if (regrabField != null)
                regrabField.SetValue(ctx, float.NegativeInfinity);
            var lockField = typeof(PlayerContext).GetField("_wallJumpLockUntil", BindingFlags.Instance | BindingFlags.NonPublic);
            if (lockField != null)
                lockField.SetValue(ctx, float.NegativeInfinity);
        }

        private static void MovementSetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static T MovementGetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void MovementInvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method {target.GetType().Name}.{methodName}");
            method.Invoke(target, args);
        }

        private static void MovementSetInputProperty(PlayerInputReader input, string propertyName, object value)
        {
            var prop = typeof(PlayerInputReader).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(prop, $"Missing property PlayerInputReader.{propertyName}");
            prop.SetValue(input, value);
        }
    }
}
