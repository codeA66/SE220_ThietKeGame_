using System;
using System.Collections.Generic;
using System.IO;
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
    public partial class Round8Phase1CombatAiTests
    {
        private const int CombatGroundLayer = 6;
        private const int CombatPlayerLayer = 7;
        private const int CombatEnemyLayer = 8;
        private const int CombatOneWayLayer = 9;
        private const int CombatClimbableLayer = 10;

        private const string BowDataPath = "Assets/_Project/Features/Combat/Data/Bow_StartBow.asset";
        private const string SwordDataPath = "Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset";
        private const string PlayerArrowPrefabPath = "Assets/_Project/Features/Player/Prefabs/Projectile_PlayerArrow.prefab";
        private const string PlayerMovementConfigPath = "Assets/_Project/Features/Player/Data/PlayerMovementConfig.asset";
        private const string PlayerAnimatorControllerPath = "Assets/_Project/Features/Player/Animations/Player.controller";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";

        [Serializable]
        private sealed class InputActionAssetJson
        {
            public InputMapJson[] maps;
        }

        [Serializable]
        private sealed class InputMapJson
        {
            public string name;
            public InputActionJson[] actions;
            public InputBindingJson[] bindings;
        }

        [Serializable]
        private sealed class InputActionJson
        {
            public string name;
        }

        [Serializable]
        private sealed class InputBindingJson
        {
            public string path;
            public string action;
        }

        // ==================== Task 1.1: Bow & RecoverableArrow ====================

        [Test]
        public void Bow_StandingAndCrouchingShots_BothHitRatOnFlatGround()
        {
            var root = new GameObject("__Round8BowRatTest");
            try
            {
                LayerMask enemyMask = 1 << CombatEnemyLayer;
                LayerMask groundMask = (1 << CombatGroundLayer) | (1 << CombatOneWayLayer);

                CreateBoxCollider(root.transform, "FlatGround", CombatGroundLayer, new Vector2(4f, -0.5f), new Vector2(20f, 1f));
                var bow = CreateBowHarness(root.transform, Vector2.zero, enemyMask, groundMask);

                // Enemy_Rat collider geometry from Enemy_Rat.prefab: offset=(0, 0.35), size=(1.0, 0.7) => y=[0..0.70]
                CreateCombatTarget(root.transform, "Enemy_Rat", new Vector2(4f, 0f),
                    new Vector2(0f, 0.35f), new Vector2(1f, 0.7f), out var ratHealth, out _);

                Physics2D.SyncTransforms();

                Assert.IsTrue(bow.TryFire(Vector2.zero, 1, crouching: false, bow.GetNextShot()),
                    "Standing shot must fire.");
                StepActiveArrowsFixed(bow, 30);
                Assert.Less(ratHealth.Current, ratHealth.Max,
                    "Standing bow shot must auto-deflect toward low Enemy_Rat on flat ground and deal damage.");

                bow.ResetShots();
                ratHealth.RestoreFull();

                Assert.IsTrue(bow.TryFire(Vector2.zero, 1, crouching: true, bow.GetNextShot()),
                    "Crouching shot must fire.");
                StepActiveArrowsFixed(bow, 30);
                Assert.Less(ratHealth.Current, ratHealth.Max,
                    "Crouching bow shot at y=0.50 must hit Enemy_Rat [0..0.70] on flat ground.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Arrow_IgnoresOneWayPlatform_ClearsAdjacentWallCorner_AndRecallsFromWallIn06s()
        {
            var root = new GameObject("__Round8ArrowWallOneWayTest");
            try
            {
                LayerMask enemyMask = 1 << CombatEnemyLayer;
                LayerMask groundMask = (1 << CombatGroundLayer) | (1 << CombatOneWayLayer);

                var bow = CreateBowHarness(root.transform, Vector2.zero, enemyMask, groundMask);

                // OneWayPlatform across (2, 0..2)
                CreateBoxCollider(root.transform, "OneWayBridge", CombatOneWayLayer, new Vector2(2f, 1f), new Vector2(0.4f, 2f));
                // Solid Ground ceiling corner with bottom edge at y = 1.55 (0.05u above y = 1.50 arrow flight line)
                CreateBoxCollider(root.transform, "AdjacentCorner", CombatGroundLayer, new Vector2(4f, 2.05f), new Vector2(1f, 1f));
                // Solid Ground wall with left face at x = 6.0
                CreateBoxCollider(root.transform, "BackWall", CombatGroundLayer, new Vector2(6.5f, 1.5f), new Vector2(1f, 4f));

                Physics2D.SyncTransforms();

                // Standing muzzle offset y is 1.4375; feet y = 0.0625 puts arrow line at exactly y = 1.50
                var feet = new Vector2(0f, 0.0625f);
                Assert.IsTrue(bow.TryFire(feet, 1, crouching: false, bow.GetNextShot()));
                var activeArrows = CombatGetPrivateField<List<RecoverableArrow>>(bow, "_activeArrows");
                Assert.AreEqual(1, activeArrows.Count);
                var arrow = activeArrows[0];

                StepActiveArrowsFixed(bow, 30);

                Assert.Greater(arrow.transform.position.x, 5.5f,
                    "Arrow must pass OneWayPlatform and clear the 0.05u-adjacent wall corner to impact the x=6 wall.");
                Assert.IsTrue(arrow.gameObject.activeSelf, "Arrow must briefly stick in wall before 0.6s recall.");

                arrow.Tick(0.35f);
                Assert.IsTrue(arrow.gameObject.activeSelf, "Wall-impact arrow must still be active before 0.6s.");

                arrow.Tick(0.30f);
                Assert.IsFalse(arrow.gameObject.activeSelf, "Wall-impact arrow must recall within 0.65s.");
                Assert.AreEqual(bow.Data.AmmoCapacity, bow.Ammo, "Ammo must restore when wall-impact arrow recalls.");
                Assert.AreEqual(0.6f, RecoverableArrow.WallImpactLifetime, 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Arrow_BlockedByShield_RecallsImmediately_WithoutEmbedding()
        {
            var root = new GameObject("__Round8ArrowShieldBlockTest");
            try
            {
                LayerMask enemyMask = 1 << CombatEnemyLayer;
                LayerMask groundMask = (1 << CombatGroundLayer) | (1 << CombatOneWayLayer);

                var bow = CreateBowHarness(root.transform, Vector2.zero, enemyMask, groundMask);
                CreateCombatTarget(root.transform, "ShieldEnemy", new Vector2(3f, 0f),
                    new Vector2(0f, 1f), new Vector2(1f, 2f), out var shieldHealth, out _);
                shieldHealth.DamageFilter = _ => false;

                Physics2D.SyncTransforms();

                Assert.IsTrue(bow.TryFire(Vector2.zero, 1, crouching: false, bow.GetNextShot()));
                var activeArrows = CombatGetPrivateField<List<RecoverableArrow>>(bow, "_activeArrows");
                Assert.AreEqual(1, activeArrows.Count);
                var arrow = activeArrows[0];

                StepActiveArrowsFixed(bow, 20);

                Assert.AreEqual(shieldHealth.Max, shieldHealth.Current, "Blocked hit must deal 0 damage.");
                Assert.IsFalse(arrow.gameObject.activeSelf, "Arrow blocked by shield must recall immediately.");
                Assert.IsFalse(arrow.IsEmbeddedIn(shieldHealth), "Blocked arrow must not embed in shield victim.");
                Assert.AreEqual(bow.Data.AmmoCapacity, bow.Ammo, "Blocked arrow recall must immediately restore ammo.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Arrow_EmbeddedOnEnemy_FlipsWithVictim_AndRecallsOnMeleeExtractOrVictimDeactivate()
        {
            var root = new GameObject("__Round8ArrowEmbedExtractTest");
            try
            {
                LayerMask enemyMask = 1 << CombatEnemyLayer;
                LayerMask groundMask = (1 << CombatGroundLayer) | (1 << CombatOneWayLayer);

                var bow = CreateBowHarness(root.transform, Vector2.zero, enemyMask, groundMask);
                var enemyGo = CreateCombatTarget(root.transform, "VictimEnemy", new Vector2(4f, 0f),
                    new Vector2(0f, 1f), new Vector2(1f, 2f), out var enemyHealth, out var enemySprite);

                Physics2D.SyncTransforms();

                Assert.IsTrue(bow.TryFire(Vector2.zero, 1, crouching: false, bow.GetNextShot()));
                var activeArrows = CombatGetPrivateField<List<RecoverableArrow>>(bow, "_activeArrows");
                Assert.AreEqual(1, activeArrows.Count);
                var arrow = activeArrows[0];

                StepActiveArrowsFixed(bow, 20);

                Assert.IsTrue(arrow.IsEmbeddedIn(enemyHealth), "Arrow must report IsEmbeddedIn(victim) after hitting alive enemy.");
                Assert.Less(arrow.transform.position.x, enemyGo.transform.position.x,
                    "Arrow entering left side of enemy must embed with negative X offset.");

                enemySprite.flipX = true;
                arrow.Tick(0.02f);
                Assert.Greater(arrow.transform.position.x, enemyGo.transform.position.x,
                    "Embedded arrow must mirror X offset when enemy SpriteRenderer.flipX flips.");

                int extracted = bow.ExtractArrowsFrom(enemyHealth);
                Assert.AreEqual(1, extracted, "ExtractArrowsFrom must recall 1 embedded arrow.");
                Assert.IsFalse(arrow.gameObject.activeSelf, "Extracted arrow must deactivate.");
                Assert.IsFalse(arrow.IsEmbeddedIn(enemyHealth), "Extracted arrow must clear victim embedding.");
                Assert.AreEqual(bow.Data.AmmoCapacity, bow.Ammo, "Extracting arrow must restore ammo.");

                // Second arrow: victim deactivation must recall arrow on Update/Tick
                bow.ResetShots();
                enemySprite.flipX = false;
                enemyHealth.RestoreFull();
                Assert.IsTrue(bow.TryFire(Vector2.zero, 1, crouching: false, bow.GetNextShot()));
                var arrow2 = activeArrows[0];

                StepActiveArrowsFixed(bow, 20);
                Assert.IsTrue(arrow2.IsEmbeddedIn(enemyHealth));

                enemyGo.SetActive(false);
                arrow2.Tick(0.02f);
                Assert.IsFalse(arrow2.gameObject.activeSelf, "Embedded arrow must recall when victim GameObject deactivates.");
                Assert.AreEqual(bow.Data.AmmoCapacity, bow.Ammo, "Victim-deactivated arrow recall must restore ammo.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // ==================== Task 1.3: Player Movement & Combat Edge Cases ====================

        [Test]
        public void RollConfig_HasRollCancelNormalizedTime055()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(PlayerMovementConfigPath);
            Assert.IsNotNull(config, $"Missing PlayerMovementConfig at {PlayerMovementConfigPath}");
            Assert.AreEqual(0.55f, config.RollCancelNormalizedTime, 0.001f,
                "PlayerMovementConfig._rollCancelNormalizedTime must be 0.55 so rolls cannot cancel on frame 1.");
        }

        [Test]
        public void BowAttackState_HeldJump_AppliesFullJumpHoldBoost()
        {
            var root = new GameObject("__Round8BowJumpHoldTest");
            try
            {
                CreateBoxCollider(root.transform, "Ground", CombatGroundLayer, new Vector2(0f, -0.5f), new Vector2(10f, 1f));
                var harness = CreatePlayerHarness(root.transform, Vector2.zero);

                Physics2D.SyncTransforms();

                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpHeld), true);
                harness.Context.RefreshFrame();
                harness.Controller.Weapons.Equip(PlayerWeaponSlot.Bow);

                ForceTestState(harness.Machine, harness.Machine.BowAttack);
                var bowState = (PlayerBowAttackState)harness.Machine.Current;

                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                bowState.Tick(0.01f);

                int steps = Mathf.CeilToInt(harness.Context.Config.JumpHoldTime / Time.fixedDeltaTime) + 1;
                for (int i = 0; i < steps; i++)
                    bowState.FixedTick();

                float minExpected = harness.Context.Config.JumpBaseVelocity + harness.Context.Config.JumpHoldBoost * 0.8f;
                Assert.GreaterOrEqual(harness.Context.Velocity.y, minExpected,
                    "Held jump during PlayerBowAttackState must apply full JumpHoldBoost across FixedTick.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AttackState_BlocksPreHitWeaponSwapCancel_AndAllowsPostHitJumpCancel()
        {
            var root = new GameObject("__Round8AttackSwapAndJumpTest");
            try
            {
                CreateBoxCollider(root.transform, "Ground", CombatGroundLayer, new Vector2(0f, -0.5f), new Vector2(10f, 1f));
                var harness = CreatePlayerHarness(root.transform, Vector2.zero);

                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                harness.Context.PendingComboIndex = 1;

                ForceTestState(harness.Machine, harness.Machine.Attack);
                var attackState = (PlayerAttackState)harness.Machine.Current;

                var strike0 = harness.Context.Combat.MeleeWeapon.GetStrike(0);
                float animSpeed = strike0.AnimSpeed > 0f ? strike0.AnimSpeed : 1f;
                float hitTime = strike0.HitFrame / 30f / animSpeed;
                float duration = harness.Context.Animator.GetClipLength(strike0.ClipName) / animSpeed;
                float chainTime = duration * harness.Context.Config.ComboChainNormalizedTime;

                // Pre-hit phase: must block weapon swap cancel
                attackState.Tick(0.01f);
                Assert.IsFalse(attackState.CanWeaponSwapCancel,
                    "AttackState.CanWeaponSwapCancel must be false before HitFrame.");

                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.EquipBowPressed), true);
                CombatInvokePrivateMethod(harness.Controller, "UpdateWeaponSelection");
                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.EquipBowPressed), false);

                Assert.AreEqual(PlayerWeaponSlot.Sword, harness.Controller.Weapons.Equipped,
                    "Pre-hit weapon swap must be blocked to prevent zero-recovery cancel exploit.");
                Assert.AreSame(harness.Machine.Attack, harness.Controller.CurrentState);

                // Post-hit phase: CanWeaponSwapCancel becomes true
                float toPostHit = Mathf.Max(0.01f, hitTime - 0.01f + 0.015f);
                attackState.Tick(toPostHit);
                Assert.IsTrue(attackState.CanWeaponSwapCancel,
                    "AttackState.CanWeaponSwapCancel must become true after HitFrame.");

                // Past ComboChainNormalizedTime: Jump input must cancel Attack into Jump
                float elapsed = 0.01f + toPostHit;
                if (elapsed < chainTime)
                    attackState.Tick(chainTime - elapsed + 0.01f);

                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveY), 0f);
                attackState.Tick(0.005f);

                Assert.AreSame(harness.Machine.Jump, harness.Machine.Current,
                    "Pressing Jump after hit and ComboChainNormalizedTime must cancel Attack into Jump.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Stomp_ScalesDamageWithBrutality_AndAllowsRollCancelOnLanding()
        {
            var root = new GameObject("__Round8StompScaleAndRollTest");
            try
            {
                CreateBoxCollider(root.transform, "Ground", CombatGroundLayer, new Vector2(0f, -0.5f), new Vector2(10f, 1f));
                var harness = CreatePlayerHarness(root.transform, Vector2.zero);

                CreateCombatTarget(root.transform, "StompVictim", new Vector2(0.4f, 0f),
                    new Vector2(0f, 0.5f), new Vector2(1f, 1f), out var victimHealth, out _);

                // Invest 1 Brutality scroll -> StompDamage 30 * 1.15 = 35
                InvestScroll(root.transform, harness.Controller.RunStats, CombatStat.Brutality);
                Assert.AreEqual(2, harness.Controller.RunStats.Level(CombatStat.Brutality));

                Physics2D.SyncTransforms();

                int hits = harness.Context.Combat.PerformStomp(
                    harness.Context.Config.StompDamage,
                    harness.Context.Config.StompStun,
                    harness.Context.Config.StompArea);
                Assert.AreEqual(1, hits, "Stomp must hit nearby enemy.");
                int expectedDamage = Mathf.RoundToInt(harness.Context.Config.StompDamage * (1f + PlayerRunStats.DamageGainPerLevel));
                Assert.AreEqual(victimHealth.Max - expectedDamage, victimHealth.Current,
                    "Stomp damage must scale with Brutality scroll level.");

                // Roll cancel on Stomp landing
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded);

                ForceTestState(harness.Machine, harness.Machine.Stomp);
                var stompState = (PlayerStompState)harness.Machine.Current;

                // First tick registers _landed = true
                stompState.Tick(0.005f);

                // Second tick within StompLandDuration with RollPressed must cancel into Roll
                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.RollPressed), true);
                stompState.Tick(0.005f);

                Assert.AreSame(harness.Machine.Roll, harness.Machine.Current,
                    "Pressing Roll after Stomp landing must cancel Stomp land-lock into Roll.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InputActions_CrouchAndGamepadEquipBindings_AreConfigured()
        {
            Assert.IsTrue(File.Exists(InputActionsPath), $"Missing InputActions asset at {InputActionsPath}");
            var json = File.ReadAllText(InputActionsPath);
            var asset = JsonUtility.FromJson<InputActionAssetJson>(json);
            Assert.IsNotNull(asset?.maps, "Failed to parse InputSystem_Actions.inputactions.");

            InputMapJson playerMap = null;
            foreach (var map in asset.maps)
            {
                if (map.name == "Player")
                {
                    playerMap = map;
                    break;
                }
            }

            Assert.IsNotNull(playerMap, "Player action map must exist in InputSystem_Actions.inputactions.");

            bool hasCrouchAction = false;
            foreach (var action in playerMap.actions)
            {
                if (action.name == "Crouch")
                {
                    hasCrouchAction = true;
                    break;
                }
            }
            Assert.IsTrue(hasCrouchAction, "Player map must define Crouch action.");

            bool hasCrouchCKey = false;
            bool hasGamepadEquipSword = false;
            bool hasGamepadEquipBow = false;
            foreach (var binding in playerMap.bindings)
            {
                if (binding.action == "Crouch" && binding.path == "<Keyboard>/c")
                    hasCrouchCKey = true;
                if (binding.action == "EquipSword" && binding.path == "<Gamepad>/dpad/left")
                    hasGamepadEquipSword = true;
                if (binding.action == "EquipBow" && binding.path == "<Gamepad>/dpad/right")
                    hasGamepadEquipBow = true;
            }

            Assert.IsTrue(hasCrouchCKey, "Crouch action must bind <Keyboard>/c.");
            Assert.IsTrue(hasGamepadEquipSword, "EquipSword action must bind <Gamepad>/dpad/left.");
            Assert.IsTrue(hasGamepadEquipBow, "EquipBow action must bind <Gamepad>/dpad/right.");

            // Also verify PlayerContext.IsCrouchHeld and Crouch state transition honor CrouchHeld
            var root = new GameObject("__Round8CrouchBindingTest");
            try
            {
                CreateBoxCollider(root.transform, "Ground", CombatGroundLayer, new Vector2(0f, -0.5f), new Vector2(10f, 1f));
                var harness = CreatePlayerHarness(root.transform, Vector2.zero);

                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                ForceTestState(harness.Machine, harness.Machine.Idle);

                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.MoveY), 0f);
                CombatSetInputProperty(harness.Input, nameof(PlayerInputReader.CrouchHeld), true);

                Assert.IsTrue(harness.Context.IsCrouchHeld,
                    "PlayerContext.IsCrouchHeld must return true when Input.CrouchHeld is true.");

                harness.Machine.Tick();
                Assert.AreSame(harness.Machine.Crouch, harness.Machine.Current,
                    "Holding Crouch (C) while grounded must transition Idle -> Crouch.");

                harness.Machine.Tick();
                Assert.AreSame(harness.Machine.Crouch, harness.Machine.Current,
                    "Holding Crouch (C) must keep player in PlayerCrouchState.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // ==================== Test Harnesses & Helpers ====================

        private readonly struct PlayerHarness
        {
            public readonly PlayerController Controller;
            public readonly PlayerContext Context;
            public readonly PlayerStateMachine Machine;
            public readonly PlayerInputReader Input;

            public PlayerHarness(PlayerController controller, PlayerContext context, PlayerStateMachine machine, PlayerInputReader input)
            {
                Controller = controller;
                Context = context;
                Machine = machine;
                Input = input;
            }
        }

        private static PlayerBow CreateBowHarness(Transform parent, Vector2 position, LayerMask enemyMask, LayerMask groundMask)
        {
            var bowData = AssetDatabase.LoadAssetAtPath<BowWeaponData>(BowDataPath);
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<RecoverableArrow>(PlayerArrowPrefabPath);
            Assert.IsNotNull(bowData, $"Missing BowWeaponData at {BowDataPath}");
            Assert.IsNotNull(arrowPrefab, $"Missing RecoverableArrow prefab at {PlayerArrowPrefabPath}");

            var playerGo = new GameObject("BowHarness");
            playerGo.transform.SetParent(parent);
            playerGo.layer = CombatPlayerLayer;
            playerGo.transform.position = position;

            var health = playerGo.AddComponent<Health>();
            health.RestoreFull();
            var stats = new PlayerRunStats(health);

            var bow = playerGo.AddComponent<PlayerBow>();
            CombatSetPrivateField(bow, "_data", bowData);
            CombatSetPrivateField(bow, "_arrowPrefab", arrowPrefab);
            bow.Initialize(enemyMask, groundMask, stats);
            return bow;
        }

        private static PlayerHarness CreatePlayerHarness(Transform parent, Vector2 position)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(PlayerMovementConfigPath);
            var swordData = AssetDatabase.LoadAssetAtPath<WeaponData>(SwordDataPath);
            var bowData = AssetDatabase.LoadAssetAtPath<BowWeaponData>(BowDataPath);
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<RecoverableArrow>(PlayerArrowPrefabPath);
            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorControllerPath);

            Assert.IsNotNull(config, $"Missing config at {PlayerMovementConfigPath}");
            Assert.IsNotNull(swordData, $"Missing weapon at {SwordDataPath}");
            Assert.IsNotNull(bowData, $"Missing bow at {BowDataPath}");
            Assert.IsNotNull(arrowPrefab, $"Missing arrow prefab at {PlayerArrowPrefabPath}");
            Assert.IsNotNull(animController, $"Missing player animator controller at {PlayerAnimatorControllerPath}");

            var playerGo = new GameObject("PlayerHarness");
            playerGo.transform.SetParent(parent);
            playerGo.layer = CombatPlayerLayer;
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
            CombatSetPrivateField(animBridge, "_animator", animator);
            CombatInvokePrivateMethod(animBridge, "Awake");

            var input = playerGo.AddComponent<PlayerInputReader>();
            var health = playerGo.AddComponent<Health>();
            health.RestoreFull();

            var bow = playerGo.AddComponent<PlayerBow>();
            CombatSetPrivateField(bow, "_data", bowData);
            CombatSetPrivateField(bow, "_arrowPrefab", arrowPrefab);

            var controller = playerGo.AddComponent<PlayerController>();
            CombatSetPrivateField(controller, "_config", config);
            CombatSetPrivateField(controller, "_input", input);
            CombatSetPrivateField(controller, "_animator", animBridge);
            CombatSetPrivateField(controller, "_sprite", sr);
            CombatSetPrivateField(controller, "_groundMask", (LayerMask)((1 << CombatGroundLayer) | (1 << CombatOneWayLayer)));
            CombatSetPrivateField(controller, "_rollPassThroughMask", (LayerMask)(1 << CombatEnemyLayer));
            CombatSetPrivateField(controller, "_climbableMask", (LayerMask)(1 << CombatClimbableLayer));
            CombatSetPrivateField(controller, "_health", health);
            CombatSetPrivateField(controller, "_weapon", swordData);
            CombatSetPrivateField(controller, "_bow", bow);
            CombatSetPrivateField(controller, "_attackTargets", (LayerMask)(1 << CombatEnemyLayer));
            CombatSetPrivateField(controller, "_autoRespawn", false);

            CombatInvokePrivateMethod(controller, "Awake");

            var machine = CombatGetPrivateField<PlayerStateMachine>(controller, "_machine");
            return new PlayerHarness(controller, controller.Context, machine, input);
        }

        private static GameObject CreateCombatTarget(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 colliderOffset,
            Vector2 colliderSize,
            out Health health,
            out SpriteRenderer spriteRenderer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.layer = CombatEnemyLayer;
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider2D>();
            col.offset = colliderOffset;
            col.size = colliderSize;

            health = go.AddComponent<Health>();
            health.RestoreFull();

            var hurtbox = go.AddComponent<Hurtbox>();
            CombatSetPrivateField(hurtbox, "_health", health);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            spriteRenderer = visual.AddComponent<SpriteRenderer>();
            return go;
        }

        private static BoxCollider2D CreateBoxCollider(Transform parent, string name, int layer, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.layer = layer;
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            return col;
        }

        private static void StepActiveArrowsFixed(PlayerBow bow, int steps)
        {
            var activeArrows = CombatGetPrivateField<List<RecoverableArrow>>(bow, "_activeArrows");
            for (int step = 0; step < steps; step++)
            {
                if (activeArrows.Count == 0)
                    break;
                var snapshot = activeArrows.ToArray();
                foreach (var arrow in snapshot)
                {
                    if (arrow != null && arrow.gameObject.activeSelf)
                        CombatInvokePrivateMethod(arrow, "FixedUpdate");
                }
            }
        }

        private static void ForceTestState(PlayerStateMachine machine, IPlayerState state)
        {
            machine.ForceChangeState(state);
            CombatSetPrivateField(machine, "_lastTransitionFrame", -1);
        }

        private static void InvestScroll(Transform parent, PlayerRunStats stats, CombatStat stat)
        {
            void DummyHandler(PowerScrollPickup _) { }
            stats.ChoiceOpened += DummyHandler;
            try
            {
                var scrollGo = new GameObject("DummyScroll");
                scrollGo.transform.SetParent(parent);
                var promptGo = new GameObject("Prompt");
                promptGo.transform.SetParent(scrollGo.transform);
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(scrollGo.transform);

                var scroll = scrollGo.AddComponent<PowerScrollPickup>();
                CombatSetPrivateField(scroll, "_prompt", promptGo);
                CombatSetPrivateField(scroll, "_icon", iconGo.transform);

                CombatInvokePrivateMethod(stats, "TryBeginChoice", scroll);
                Assert.IsTrue(stats.TryChoose(stat), $"Failed to invest scroll stat {stat}.");
            }
            finally
            {
                stats.ChoiceOpened -= DummyHandler;
            }
        }

        private static void CombatSetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static T CombatGetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void CombatInvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method {target.GetType().Name}.{methodName}");
            method.Invoke(target, args);
        }

        private static void CombatSetInputProperty(PlayerInputReader input, string propertyName, object value)
        {
            var prop = typeof(PlayerInputReader).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(prop, $"Missing property PlayerInputReader.{propertyName}");
            prop.SetValue(input, value);
        }
    }
}
