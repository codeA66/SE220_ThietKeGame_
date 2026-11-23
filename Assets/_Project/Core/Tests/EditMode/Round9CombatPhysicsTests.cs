using System.Reflection;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using DeadCells.Player.StateMachine.States;
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

        // Task 1.2 fixture geometry.
        private const int GroundLayer = 6;
        private const int OneWayLayer = 9;
        private const int ClimbableLayer = 10;

        private const string MovementConfigPath = "Assets/_Project/Features/Player/Data/PlayerMovementConfig.asset";
        private const string SwordDataPath = "Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset";
        private const string BowDataPath = "Assets/_Project/Features/Combat/Data/Bow_StartBow.asset";
        private const string PlayerArrowPrefabPath = "Assets/_Project/Features/Player/Prefabs/Projectile_PlayerArrow.prefab";
        private const string PlayerAnimatorPath = "Assets/_Project/Features/Player/Animations/Player.controller";

        /// <summary>
        /// Chain geometry as Task 2.2 trims it: the trigger tops out 0.08u above the deck it serves,
        /// so a flush top dismount lands 0.06u below the trigger top (8.02 on the 8.00 deck).
        /// </summary>
        private const float ChainX = 46f;
        private const float ChainTriggerCenterY = 3.94f;
        private const float ChainTriggerHeight = 8.28f;
        private const float ChainTriggerMaxY = ChainTriggerCenterY + ChainTriggerHeight * 0.5f; // 8.08
        private const float DeckTopY = 8f;
        private const float DeckThickness = 0.25f;
        private const float TopDismountFeetY = ChainTriggerMaxY - 0.06f; // 8.02

        private static readonly LayerMask ClimbProbeMask = 1 << ClimbableLayer;

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

        // ==================== Task 1.2: Chain Climb Precision (P0-02) ====================

        /// <summary>
        /// P0-02: the chain trigger overhangs the deck it serves, so <c>W</c> used to re-grab thin air
        /// above the deck — both while standing on the deck and during the one-frame settle after a top
        /// dismount. A grab that genuinely starts below the deck must still work.
        /// </summary>
        [Test]
        public void T02_ClimbState_RejectsUpGrabAtDeckTopWhetherGroundedOrAirborne()
        {
            var root = new GameObject("__Round9T02ChainTopGrab");
            try
            {
                CreateChain(root.transform);
                var deck = CreateOneWayDeck(root.transform);
                var harness = CreatePlayerHarness(root.transform, new Vector2(ChainX, DeckTopY));
                Physics2D.SyncTransforms();

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveY), 1f);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), false);

                // 1. Grounded on the deck the chain serves.
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded, "Precondition: feet on the deck top must count as grounded.");
                Assert.IsTrue(harness.Context.TryFindClimbable(),
                    "Precondition: a body standing on the deck still overlaps the overhanging chain trigger.");

                ForceState(harness.Machine, harness.Machine.Idle);
                harness.Machine.Tick();

                Assert.AreNotSame(harness.Machine.Climb, harness.Machine.Current,
                    "Holding W while standing on the deck must NOT mount the chain: the trigger overhangs the deck by 0.08u " +
                    "and the grab would hang the player in mid air.");
                Assert.IsNull(harness.Context.ClimbTarget,
                    "A rejected upward grab must clear ClimbTarget so the next frame re-probes cleanly.");

                // 2. One frame after a top dismount: feet at 8.02, still overlapping the trigger.
                deck.SetActive(false);
                harness.Body.position = new Vector2(ChainX, TopDismountFeetY);
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsFalse(harness.Context.IsGrounded, "Precondition: the settle frame after a top dismount is airborne.");
                Assert.IsTrue(harness.Context.TryFindClimbable(),
                    "Precondition: the settle-frame body still overlaps the overhanging trigger.");

                ForceState(harness.Machine, harness.Machine.Fall);
                harness.Machine.Tick();
                Assert.AreNotSame(harness.Machine.Climb, harness.Machine.Current,
                    "The one-frame settle after a top dismount must not re-grab the chain either.");

                // 3. Control: a grab that really starts below the deck still works.
                harness.Body.position = new Vector2(ChainX, DeckTopY - 0.60f);
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsFalse(harness.Context.IsGrounded, "Precondition: 0.60u below the deck is airborne.");

                ForceState(harness.Machine, harness.Machine.Fall);
                harness.Machine.Tick();
                Assert.AreSame(harness.Machine.Climb, harness.Machine.Current,
                    "A player 0.60u below the deck must still grab the chain while holding W.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// P0-02: climbing past the top of a chain must step the feet onto the deck (trigger top - 0.06)
        /// instead of letting go while still 0.50u above the deck in mid air.
        /// </summary>
        [Test]
        public void T03_ClimbState_TopDismountSnapsFlushAtDeckWithoutMidAirFloat()
        {
            var root = new GameObject("__Round9T03TopDismount");
            try
            {
                CreateChain(root.transform);
                var harness = CreatePlayerHarness(root.transform, new Vector2(ChainX, TopDismountFeetY - 0.03f));
                Physics2D.SyncTransforms();

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveY), 1f);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), false);

                Assert.IsTrue(harness.Context.TryFindClimbable(), "Precondition: the player must overlap the chain trigger.");
                ForceState(harness.Machine, harness.Machine.Climb);

                harness.Machine.Tick();

                Assert.AreNotSame(harness.Machine.Climb, harness.Machine.Current,
                    "Holding W at the chain top must step the player off the chain instead of hanging there.");
                Assert.AreEqual(TopDismountFeetY, harness.Context.Position.y, 0.001f,
                    "Top dismount must snap the feet to trigger top - 0.06 (8.02 on the trimmed 8.08 trigger), " +
                    "not leave the +0.50u mid-air float.");
                Assert.AreEqual(0f, harness.Context.Velocity.sqrMagnitude, 0.0001f,
                    "Snapping onto the deck must zero the velocity so the player neither launches nor slides.");

                // The snap must land inside GroundCheckDistance (0.05) of the deck, so the next frame is grounded.
                CreateOneWayDeck(root.transform);
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded,
                    "After the top dismount the feet must sit within GroundCheckDistance of the 8.00u deck top.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// P0-02: <c>S</c> must descend a chain, but the frame-0 crouch is load bearing —
        /// <c>PilotPlayTests.P01</c> presses S and then Space on the same breath and expects a
        /// drop-through. A descend therefore only starts after 0.18s of S with no Jump.
        /// </summary>
        [Test]
        public void T04_CrouchState_PreservesFrame0CrouchAndMountsChainAfter018sHold()
        {
            // ---- (a) P01 contract: S + Space on frame 0 still drops through the one-way deck. ----
            var frame0Root = new GameObject("__Round9T04Frame0Crouch");
            try
            {
                CreateChain(frame0Root.transform);
                CreateOneWayDeck(frame0Root.transform);
                var harness = CreatePlayerHarness(frame0Root.transform, new Vector2(ChainX, DeckTopY));
                Physics2D.SyncTransforms();

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveY), -1f);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpHeld), true);
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded, "Precondition: grounded on the one-way deck.");

                ForceState(harness.Machine, harness.Machine.Crouch);
                harness.Machine.Tick();

                Assert.AreSame(harness.Machine.Fall, harness.Machine.Current,
                    "S followed immediately by Space must still drop through on frame 0: PilotPlayTests.P01 is load bearing.");
            }
            finally
            {
                Object.DestroyImmediate(frame0Root);
            }

            // ---- (b) Holding S with no Jump for 0.18s mounts the chain under the deck. ----
            var descendRoot = new GameObject("__Round9T04ChainDescend");
            try
            {
                var chain = CreateChain(descendRoot.transform);
                CreateOneWayDeck(descendRoot.transform);
                var harness = CreatePlayerHarness(descendRoot.transform, new Vector2(ChainX, DeckTopY));
                Physics2D.SyncTransforms();

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveY), -1f);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpHeld), false);
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded, "Precondition: grounded on the one-way deck above the chain.");

                ForceState(harness.Machine, harness.Machine.Crouch);
                var crouch = (PlayerCrouchState)harness.Machine.Crouch;

                TickCrouch(crouch, 0.05f);
                TickCrouch(crouch, 0.05f);
                TickCrouch(crouch, 0.05f);
                Assert.AreSame(harness.Machine.Crouch, harness.Machine.Current,
                    "Holding S for 0.15s (< 0.18s) must stay a plain crouch, not a chain mount.");

                TickCrouch(crouch, 0.05f);
                Assert.AreSame(harness.Machine.Climb, harness.Machine.Current,
                    "Holding S for 0.20s (>= 0.18s) on a one-way deck above a chain must mount the chain downward.");
                Assert.AreEqual(chain.bounds.max.y - 0.30f, harness.Context.Position.y, 0.01f,
                    "A descent must start the body 0.30u below the chain top so the grip takes immediately.");
                Assert.AreEqual(ChainX - harness.Context.Config.ClimbHandOffsetX, harness.Context.Position.x, 0.01f,
                    "A descent must still centre the hands on the chain.");
            }
            finally
            {
                Object.DestroyImmediate(descendRoot);
            }
        }

        // ==================== Task 1.2: 45° Slope Grounding & Walk Support (P0-01) ====================

        /// <summary>
        /// P0-01: a 45° ramp presents a ground normal of cos(45°) = 0.7071 and walking up it produces
        /// a positive vertical velocity, so the old flat-only ground test (vy &lt;= 0.01) dropped the
        /// player off the ground on the first frame of every step up. Grounding must be judged along
        /// the surface normal, slope locomotion must follow the tangent, and an idle player must not
        /// creep downhill.
        /// </summary>
        [Test]
        public void T04b_SlopeGrounding_StaysGroundedWalkingUp45DegreeRampAndZeroIdleSlide()
        {
            var root = new GameObject("__Round9T04bSlopeGrounding");
            try
            {
                var rampMask = (LayerMask)((1 << GroundLayer) | (1 << OneWayLayer));
                CreateRamp45(root.transform);
                Physics2D.SyncTransforms();

                // Feet probe half width: collider width (0.8) * FeetProbeWidthRatio (0.96) / 2. The probe's
                // up-slope corner is what makes first contact with the incline, so measure the surface there.
                const float ProbeHalfWidth = 0.8f * 0.96f * 0.5f;
                var surface = Physics2D.Raycast(new Vector2(ProbeHalfWidth, 6f), Vector2.down, 20f, rampMask);
                Assert.IsNotNull(surface.collider, "Precondition: the 45° ramp must be probeable.");
                Assert.AreEqual(Mathf.Sqrt(0.5f), surface.normal.y, 0.01f,
                    "Precondition: the fixture must present a real 45° ground normal (0.7071).");

                Vector2 normal = surface.normal;
                Vector2 tangent = new Vector2(normal.y, -normal.x).normalized;
                Assert.Greater(tangent.x, 0.5f, "Precondition: the ramp must rise toward +x so 'up the slope' is +x.");
                Assert.AreEqual(tangent.x, tangent.y, 0.001f, "Precondition: a 45° tangent has equal components.");

                var harness = CreatePlayerHarness(root.transform, new Vector2(0f, surface.point.y + 0.03f));
                Physics2D.SyncTransforms();

                // 1. Walking UP the ramp: velocity along the tangent means vy > 0.01 but v·n ~ 0, so stay grounded.
                harness.Body.linearVelocity = tangent * harness.Context.Config.RunSpeed;
                Assert.Greater(harness.Context.Velocity.y, 0.01f,
                    "Precondition: walking up a 45° ramp really does produce a rising velocity, which is what broke the old test.");
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded,
                    "Walking up a 45° ramp must stay grounded (grounding is judged along the surface normal, not on vy).");

                // 2. Slope locomotion follows the tangent at a constant speed instead of pressing into the slope.
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 1f);
                ForceState(harness.Machine, harness.Machine.Run);
                harness.Machine.FixedTick();
                Vector2 runVelocity = harness.Context.Velocity;
                Assert.AreEqual(harness.Context.Config.RunSpeed, runVelocity.magnitude, 0.01f,
                    "Running on a 45° ramp must keep the configured speed along the surface.");
                Assert.Greater(Vector2.Dot(runVelocity.normalized, tangent), 0.99f,
                    "Running up a 45° ramp must move along the surface tangent, not horizontally into the slope.");

                // 3. Idle on the ramp: zero slide, so standing on stairs does not creep downhill.
                harness.Body.linearVelocity = tangent * harness.Context.Config.RunSpeed;
                harness.Context.RefreshFrame();
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                ForceState(harness.Machine, harness.Machine.Idle);
                harness.Machine.FixedTick();
                Assert.AreEqual(0f, harness.Body.gravityScale, 0.0001f,
                    "Idling on a slope must switch gravity off so the player does not slide downhill.");
                Assert.AreEqual(0f, harness.Context.Velocity.sqrMagnitude, 0.0001f,
                    "Idling on a slope must zero the velocity.");

                // 4. Jumping off the ramp must still leave the ground (v·n is well above the rise tolerance).
                harness.Body.linearVelocity = tangent * harness.Context.Config.RunSpeed
                                             + new Vector2(0f, harness.Context.Config.JumpBaseVelocity);
                harness.Context.RefreshFrame();
                Assert.IsFalse(harness.Context.IsGrounded,
                    "A jump started from a 45° ramp must leave the ground immediately, not stick to the slope.");

                // 5. Flat ground keeps the original rising guard, so ledges and one-way decks still release.
                var flatRoot = new GameObject("__Round9T04bFlatGround");
                try
                {
                    CreateFlatGround(flatRoot.transform, new Vector2(50f, -2.5f));
                    Physics2D.SyncTransforms();
                    harness.Body.position = new Vector2(50f, -1.97f);
                    Physics2D.SyncTransforms();
                    harness.Body.linearVelocity = new Vector2(harness.Context.Config.RunSpeed, 5f);
                    harness.Context.RefreshFrame();
                    Assert.IsFalse(harness.Context.IsGrounded,
                        "On flat ground a rising player must still count as airborne, or jumps and one-way decks break.");
                }
                finally
                {
                    Object.DestroyImmediate(flatRoot);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ==================== Fixtures & helpers ====================

        private readonly struct PlayerHarness
        {
            public readonly GameObject PlayerGo;
            public readonly Rigidbody2D Body;
            public readonly PlayerContext Context;
            public readonly PlayerStateMachine Machine;
            public readonly PlayerInputReader Input;

            public PlayerHarness(GameObject playerGo, Rigidbody2D body, PlayerContext context,
                PlayerStateMachine machine, PlayerInputReader input)
            {
                PlayerGo = playerGo;
                Body = body;
                Context = context;
                Machine = machine;
                Input = input;
            }
        }

        /// <summary>
        /// Chain trigger trimmed to the Task 2.2 shape: y = [0.08 .. 8.08] on x = 46, so it overhangs
        /// the 8.00 deck it serves by 0.08u.
        /// </summary>
        private static BoxCollider2D CreateChain(Transform parent)
        {
            var go = new GameObject("I1_Chain_C");
            go.transform.SetParent(parent);
            go.layer = ClimbableLayer;
            go.transform.position = new Vector3(ChainX, ChainTriggerCenterY, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, ChainTriggerHeight);
            return col;
        }

        private static GameObject CreateOneWayDeck(Transform parent)
        {
            var go = new GameObject("OneWay_Deck_C");
            go.transform.SetParent(parent);
            go.layer = OneWayLayer;
            go.transform.position = new Vector3(ChainX, DeckTopY - DeckThickness * 0.5f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(6f, DeckThickness);
            col.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            return go;
        }

        private static void CreateRamp45(Transform parent)
        {
            var go = new GameObject("Stairs_H_45");
            go.transform.SetParent(parent);
            go.layer = GroundLayer;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(10f, 1f);
        }

        private static void CreateFlatGround(Transform parent, Vector2 position)
        {
            var go = new GameObject("FlatGround");
            go.transform.SetParent(parent);
            go.layer = GroundLayer;
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(20f, 1f);
        }

        private static PlayerHarness CreatePlayerHarness(Transform parent, Vector2 feetPosition)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
            var swordData = AssetDatabase.LoadAssetAtPath<WeaponData>(SwordDataPath);
            var bowData = AssetDatabase.LoadAssetAtPath<BowWeaponData>(BowDataPath);
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<RecoverableArrow>(PlayerArrowPrefabPath);
            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorPath);

            Assert.IsNotNull(config, $"Missing {MovementConfigPath}");
            Assert.IsNotNull(swordData, $"Missing {SwordDataPath}");
            Assert.IsNotNull(bowData, $"Missing {BowDataPath}");
            Assert.IsNotNull(arrowPrefab, $"Missing {PlayerArrowPrefabPath}");
            Assert.IsNotNull(animController, $"Missing {PlayerAnimatorPath}");

            var playerGo = new GameObject("PlayerHarness");
            playerGo.transform.SetParent(parent);
            playerGo.layer = PlayerLayer;
            playerGo.transform.position = feetPosition;

            var body = playerGo.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = config.GravityScale;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.position = feetPosition;

            var col = playerGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(playerGo.transform, false);
            var sprite = visualGo.AddComponent<SpriteRenderer>();
            var animator = visualGo.AddComponent<Animator>();
            animator.runtimeAnimatorController = animController;
            animator.Rebind();
            animator.Update(0f);

            var animBridge = playerGo.AddComponent<PlayerAnimatorBridge>();
            SetPrivateField(animBridge, "_animator", animator);
            InvokePrivateMethod(animBridge, "Awake");

            var input = playerGo.AddComponent<PlayerInputReader>();
            var health = playerGo.AddComponent<Health>();
            health.RestoreFull();

            var bow = playerGo.AddComponent<PlayerBow>();
            SetPrivateField(bow, "_data", bowData);
            SetPrivateField(bow, "_arrowPrefab", arrowPrefab);

            var controller = playerGo.AddComponent<PlayerController>();
            SetPrivateField(controller, "_config", config);
            SetPrivateField(controller, "_input", input);
            SetPrivateField(controller, "_animator", animBridge);
            SetPrivateField(controller, "_sprite", sprite);
            SetPrivateField(controller, "_groundMask", (LayerMask)((1 << GroundLayer) | (1 << OneWayLayer)));
            SetPrivateField(controller, "_rollPassThroughMask", (LayerMask)(1 << EnemyLayer));
            SetPrivateField(controller, "_climbableMask", ClimbProbeMask);
            SetPrivateField(controller, "_health", health);
            SetPrivateField(controller, "_weapon", swordData);
            SetPrivateField(controller, "_bow", bow);
            SetPrivateField(controller, "_attackTargets", (LayerMask)(1 << EnemyLayer));
            SetPrivateField(controller, "_autoRespawn", false);
            InvokePrivateMethod(controller, "Awake");

            var machine = GetPrivateField<PlayerStateMachine>(controller, "_machine");
            return new PlayerHarness(playerGo, body, controller.Context, machine, input);
        }

        /// <summary>
        /// Deterministic clock injection: <c>PlayerCrouchState</c> must expose a <c>Tick(float)</c>
        /// overload so the 0.18s descend window can be driven without depending on
        /// <c>Time.deltaTime</c> inside a batchmode EditMode run. Only the clock is injected — every
        /// assertion below is on state transitions and body transforms.
        /// </summary>
        private static void TickCrouch(PlayerCrouchState crouch, float deltaTime)
        {
            var method = typeof(PlayerCrouchState).GetMethod("Tick",
                BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(float) }, null);
            Assert.IsNotNull(method,
                "PlayerCrouchState must expose a public Tick(float deltaTime) overload so the S-hold descend window is testable.");
            method.Invoke(crouch, new object[] { deltaTime });
        }

        private static void ForceState(PlayerStateMachine machine, IPlayerState state)
        {
            machine.ForceChangeState(state);
            SetPrivateField(machine, "_lastTransitionFrame", -1);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method {target.GetType().Name}.{methodName}");
            method.Invoke(target, args);
        }

        private static void SetInput(PlayerInputReader input, string propertyName, object value)
        {
            var prop = typeof(PlayerInputReader).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(prop, $"Missing property PlayerInputReader.{propertyName}");
            prop.SetValue(input, value);
        }
    }
}
