using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Enemies.States;
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
        private const string EnemyAnimatorPath = "Assets/_Project/Features/Enemies/Animations/zombie/zombie.controller";

        /// <summary>
        /// <c>m_GravityScale</c> carried by every enemy prefab (Zombie, Rat, Shield, Archer, Grenadier).
        /// With <c>Physics2D.gravity.y = -9.81</c> that is a downward acceleration of 34.335 u/s².
        /// </summary>
        private const float PrefabGravityScale = 3.5f;

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

        /// <summary>
        /// Pre-trim chain geometry — the overhang the Task 1.2 latch exists to survive. Box 0.5 x 9.0
        /// centred at y = 4.0, so the trigger spans [-0.50 .. 8.50] over the same 8.00 deck.
        /// The mandated -0.06 top dismount therefore puts the feet at 8.44, which is 0.44u of air above
        /// the deck and far outside the reach of the 0.05u feet probe, so a fixture built on this trigger
        /// exercises the <b>no footing</b> branch of <c>PlayerClimbState</c>'s top dismount.
        /// </summary>
        private const float OverhangChainCenterY = 4f;
        private const float OverhangChainHeight = 9f;
        private const float OverhangChainMaxY = OverhangChainCenterY + OverhangChainHeight * 0.5f; // 8.50
        private const float OverhangDismountFeetY = OverhangChainMaxY - 0.06f; // 8.44

        /// <summary>Simulated frame length for the multi-frame climb/fall runs (120 Hz).</summary>
        private const float FrameDeltaTime = 1f / 120f;

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
        /// P0-02 regression on an overhanging chain (trigger top 8.50 over the 8.00 deck — the shape
        /// every untrimmed chain still has, and the shape the Task 1.2 latch was written against).
        /// The mandated -0.06 snap puts the feet at 8.44, which is 0.44u above the deck and beyond the
        /// reach of the 0.05u feet probe, so the top dismount finds <b>no footing</b>. Handing the
        /// player to Idle there made a held <c>W</c> loop Climb -> Idle -> Fall -> Climb roughly every
        /// 0.1s: Fall 8.44 -> 8.27, grab allowed (8.27 &lt; 8.50 - 0.22), Climb, dismount snap back to
        /// 8.44, Idle, not grounded, Fall. The hand-off must not depend on Task 2.2 trimming the
        /// trigger, so a top dismount with no footing under the feet has to stay off the chain it just
        /// left instead of handing it straight back.
        /// </summary>
        [Test]
        public void T02b_ClimbState_TopDismountWithoutFootingDoesNotOscillateWithUpHeld()
        {
            var root = new GameObject("__Round9T02bOverhangChainTop");
            try
            {
                CreateOverhangChain(root.transform);
                CreateOneWayDeck(root.transform);
                // Exactly where the -0.06 snap lands on the overhanging trigger: 8.50 - 0.06 = 8.44.
                var harness = CreatePlayerHarness(root.transform, new Vector2(ChainX, OverhangDismountFeetY));
                Physics2D.SyncTransforms();

                Assert.AreEqual(0.44f, OverhangDismountFeetY - DeckTopY, 0.001f,
                    "Precondition: the overhanging trigger dismounts the feet at 8.44, 0.44u above the 8.00 deck.");

                // Pin the branch this test owns. If the fixture geometry ever drifts back inside the
                // feet probe this fails loudly, instead of the guard quietly sliding onto T02c's
                // footed path and leaving the Climb -> Fall oscillation untested.
                Assert.IsFalse(harness.Context.TryGetFootingAt(new Vector2(ChainX, OverhangDismountFeetY), out _),
                    "T02b must run the no-footing branch: with the feet at 8.44 the 0.05u feet probe cannot reach the " +
                    "8.00 deck. It reported a surface, so this fixture is no longer a top dismount with no footing.");

                Assert.IsTrue(harness.Context.TryFindClimbable(),
                    "Precondition: the dismount point still overlaps the overhanging chain trigger.");

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveY), 1f); // W held for the whole run
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpHeld), false);

                ForceState(harness.Machine, harness.Machine.Climb);

                int climbEntries = 1;
                bool landed = false;
                IPlayerState previous = harness.Machine.Current;
                for (int frame = 0; frame < 360 && !landed; frame++)
                {
                    harness.Context.RefreshFrame();
                    harness.Machine.Tick();
                    harness.Machine.FixedTick();

                    IPlayerState current = harness.Machine.Current;
                    if (!ReferenceEquals(current, previous))
                    {
                        if (current is PlayerClimbState)
                            climbEntries++;
                        previous = current;
                    }

                    landed = harness.Context.IsGrounded
                             && !(current is PlayerClimbState) && !(current is PlayerFallState);
                    if (landed)
                        break;

                    AdvanceBody(harness, FrameDeltaTime);
                    BeginSimulatedFrame(harness.Machine);
                }

                Assert.LessOrEqual(climbEntries, 2,
                    "Holding W at the top of an overhanging chain must not re-enter PlayerClimbState over and over: " +
                    $"a Climb -> Idle -> Fall -> Climb cycle was counted {climbEntries} times in 3s.");
                Assert.IsTrue(landed || harness.Machine.Current is PlayerClimbState,
                    "Holding W at the top must settle either on the chain or on the deck, not in a per-frame loop. " +
                    $"Ended in {harness.Machine.Current?.GetType().Name ?? "None"} at y={harness.Context.Position.y:F2}.");
                if (landed)
                {
                    Assert.AreEqual(DeckTopY, harness.Context.Position.y, 0.3f,
                        "A top dismount with no footing must fall onto the deck the chain serves, not hang in mid air.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// The other side of the same dismount: on the geometry Task 2.2 actually ships (trigger top
        /// 8.08 over the 8.00 deck) the -0.06 snap lands the feet at 8.02, which is inside the 0.05u
        /// feet probe, so <c>PlayerClimbState</c> takes the <b>footed</b> branch — it snaps onto the
        /// surface it found, a release gap above it, and hands the player to Fall so the drop resolves
        /// a real landing. This is the branch that ships, so it gets its own fixture instead of riding
        /// on T02b's no-footing one.
        /// </summary>
        [Test]
        public void T02c_ClimbState_TopDismountOnTrimmedTriggerLandsAndDoesNotRegrab()
        {
            var root = new GameObject("__Round9T02cTrimmedChainTop");
            try
            {
                CreateChain(root.transform);
                CreateOneWayDeck(root.transform);
                var harness = CreatePlayerHarness(root.transform, new Vector2(ChainX, TopDismountFeetY));
                Physics2D.SyncTransforms();

                // Pin the branch: the trimmed dismount point is under the deck, inside the probe.
                Assert.IsTrue(harness.Context.TryGetFootingAt(new Vector2(ChainX, TopDismountFeetY), out float surfaceY),
                    "On the trimmed trigger the dismount point at 8.02 must find the 8.00 deck under it; if it does " +
                    "not, this fixture is testing the no-footing branch that T02b already owns.");
                // The probe reports the contact-offset band rather than the analytic collider top, so this
                // stays inside a probe-width of the deck instead of pinning a float that is not a contract.
                Assert.AreEqual(DeckTopY, surfaceY, 0.01f,
                    "The footing found under the trimmed dismount point must be the deck the chain serves.");
                Assert.IsTrue(harness.Context.TryFindClimbable(),
                    "Precondition: the dismount point still overlaps the trimmed chain trigger.");

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveY), 1f); // W held for the whole run
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpHeld), false);

                ForceState(harness.Machine, harness.Machine.Climb);

                harness.Machine.Tick();

                Assert.AreNotSame(harness.Machine.Climb, harness.Machine.Current,
                    "Holding W at the trimmed chain top must step the player off the chain.");
                Assert.AreEqual(surfaceY + 0.02f, harness.Context.Position.y, 0.001f,
                    "The footed dismount must snap the feet to the surface it found plus the 0.02u release gap, " +
                    "not to a fixed offset below the trigger top.");
                Assert.AreEqual(0f, harness.Context.Velocity.sqrMagnitude, 0.0001f,
                    "The footed dismount must zero the velocity so the short drop starts from rest.");

                // Hold W through the drop: the footed dismount must land on the deck and never climb again.
                int climbEntries = 1;
                bool landed = false;
                Vector2 landingPosition = harness.Context.Position;
                IPlayerState previous = harness.Machine.Current;
                for (int frame = 0; frame < 360 && !landed; frame++)
                {
                    harness.Context.RefreshFrame();
                    harness.Machine.Tick();
                    harness.Machine.FixedTick();

                    IPlayerState current = harness.Machine.Current;
                    if (!ReferenceEquals(current, previous))
                    {
                        if (current is PlayerClimbState)
                            climbEntries++;
                        previous = current;
                    }

                    landed = harness.Context.IsGrounded
                             && !(current is PlayerClimbState) && !(current is PlayerFallState);
                    if (landed)
                        landingPosition = harness.Context.Position;
                    else
                    {
                        AdvanceBody(harness, FrameDeltaTime);
                        BeginSimulatedFrame(harness.Machine);
                    }
                }

                Assert.IsTrue(landed,
                    "A footed top dismount must land on the deck it found; the player never became grounded in 3s. " +
                    $"Ended in {harness.Machine.Current?.GetType().Name ?? "None"} at y={harness.Context.Position.y:F2}.");
                Assert.AreEqual(DeckTopY, landingPosition.y, 0.05f,
                    $"The footed dismount must land on the 8.00 deck, not at y={landingPosition.y:F2}.");
                Assert.LessOrEqual(climbEntries, 1,
                    "A held W must not re-enter PlayerClimbState after a footed top dismount: " +
                    $"climb was entered {climbEntries} time(s).");

                // Standing on the deck with W held: the chain tops out 0.08u overhead, so the grab must
                // stay refused for as long as the player stands there. The body is pinned so this only
                // measures the state machine, not the EditMode integrator's lack of contact resolution.
                harness.Body.position = new Vector2(ChainX, DeckTopY);
                harness.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded, "Precondition: the pinned body stands on the 8.00 deck.");
                Assert.IsTrue(harness.Context.TryFindClimbable(),
                    "Precondition: the body standing on the deck still overlaps the trimmed chain trigger.");

                ForceState(harness.Machine, harness.Machine.Idle);
                for (int frame = 0; frame < 60; frame++)
                {
                    harness.Machine.Tick();
                    Assert.AreNotSame(harness.Machine.Climb, harness.Machine.Current,
                        "Holding W while standing on the deck below a trimmed chain must not mount it: " +
                        $"the grab succeeded on frame {frame}.");
                    BeginSimulatedFrame(harness.Machine);
                }
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

        /// <summary>
        /// P0-02: "block mid-air chain catch while dropping through" was a named requirement with no
        /// test on it. <c>S</c> grips a chain while falling, but not while the player is inside the
        /// deliberately-suspended drop-through window — that one has to stay a drop. Everything else
        /// about the input and the geometry is identical in the two halves, so the guard is the only
        /// thing that can flip the result.
        /// </summary>
        [Test]
        public void T04c_FallState_BlocksMidAirChainCatchWhileDroppingThrough()
        {
            var root = new GameObject("__Round9T04cDropThroughCatch");
            try
            {
                CreateChain(root.transform);
                CreateOneWayDeck(root.transform);
                var harness = CreatePlayerHarness(root.transform, new Vector2(ChainX, DeckTopY));
                Physics2D.SyncTransforms();

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveY), -1f); // S held
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpHeld), false);

                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded, "Precondition: grounded on the one-way deck.");
                Assert.IsTrue(harness.Context.TryDropThrough(),
                    "Precondition: Down + Jump on a one-way deck must open the drop-through window.");
                Assert.IsTrue(harness.Context.IsDroppingThrough,
                    "Precondition: IsDroppingThrough must report the suspended deck collision.");

                // Falling mid-chain, well clear of the 0.25u descend-grab margin, with the
                // drop-through window still open.
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                harness.Body.position = new Vector2(ChainX, ChainTriggerMaxY - 0.48f);
                harness.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                Assert.IsTrue(harness.Context.TryFindClimbable(),
                    "Precondition: the falling body must overlap the chain, or the guard proves nothing.");
                Assert.Less(harness.Context.Position.y, ChainTriggerMaxY - 0.25f,
                    "Precondition: the fall must be inside the descend-grab vertically, or the guard proves nothing.");

                ForceState(harness.Machine, harness.Machine.Fall);
                harness.Machine.Tick();

                Assert.AreNotSame(harness.Machine.Climb, harness.Machine.Current,
                    "While a drop-through is in flight the same S input must not catch the chain: the player asked to drop.");
                Assert.AreSame(harness.Machine.Fall, harness.Machine.Current,
                    "A blocked mid-air catch must leave the player falling.");

                // Close the window. The duration is real-time (DropThroughDuration), which an EditMode
                // test cannot wait out, so the suspended platform is cleared directly — this is fixture
                // setup, and IsDroppingThrough is asserted right after to prove the state really flipped.
                SetPrivateField(harness.Context, "_droppedPlatform", null);
                Assert.IsFalse(harness.Context.IsDroppingThrough,
                    "Precondition: the drop-through window must be closed for the control half.");

                BeginSimulatedFrame(harness.Machine);
                harness.Machine.Tick();

                Assert.AreSame(harness.Machine.Climb, harness.Machine.Current,
                    "With the drop-through window closed, the same S input must grip the chain.");
            }
            finally
            {
                Object.DestroyImmediate(root);
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

                // 6. Leaving slope-idle must hand gravity back. Slope idle switches gravity off, so a
                //    state that inherited gravityScale == 0 would float — the one property this design
                //    makes fragile. The probe target is a GROUNDED roll, whose Enter() deliberately does
                //    not touch gravity (only the air-roll branch does), so a restored gravityScale can
                //    only have come from the Exit() of the slope state being left.
                harness.Body.position = new Vector2(0f, surface.point.y + 0.03f);
                Physics2D.SyncTransforms();
                harness.Body.linearVelocity = tangent * harness.Context.Config.RunSpeed;
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsOnSlope, "Precondition: back on the 45° ramp for the gravity-restore probes.");
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);

                // (a) Idle on the slope -> grounded roll.
                ForceState(harness.Machine, harness.Machine.Idle);
                harness.Machine.FixedTick();
                Assert.AreEqual(0f, harness.Body.gravityScale, 0.0001f,
                    "Precondition: slope idle must be the zero-gravity state being left.");
                ForceState(harness.Machine, harness.Machine.Roll);
                Assert.AreSame(harness.Machine.Roll, harness.Machine.Current, "Precondition: rolling from slope idle.");
                Assert.AreEqual(harness.Context.Config.GravityScale, harness.Body.gravityScale, 0.0001f,
                    "Leaving slope idle must restore gravity to Config.GravityScale; a grounded roll never sets it, " +
                    "so only Idle.Exit() can explain a non-zero value here.");

                // (b) Run standing still on the slope -> grounded roll.
                ForceState(harness.Machine, harness.Machine.Run);
                harness.Machine.FixedTick();
                Assert.AreEqual(0f, harness.Body.gravityScale, 0.0001f,
                    "Precondition: a stationary Run on the slope is the same zero-gravity state.");
                ForceState(harness.Machine, harness.Machine.Roll);
                Assert.AreEqual(harness.Context.Config.GravityScale, harness.Body.gravityScale, 0.0001f,
                    "Leaving slope idle in Run must restore gravity through Run.Exit().");

                // (c) Crouch on the slope -> grounded roll.
                ForceState(harness.Machine, harness.Machine.Crouch);
                harness.Machine.FixedTick();
                Assert.AreEqual(0f, harness.Body.gravityScale, 0.0001f,
                    "Precondition: slope crouch is the same zero-gravity state.");
                ForceState(harness.Machine, harness.Machine.Roll);
                Assert.AreEqual(harness.Context.Config.GravityScale, harness.Body.gravityScale, 0.0001f,
                    "Leaving slope crouch must restore gravity through Crouch.Exit() as well.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ==================== Task 1.3: Dead Cells melee overhaul (P0-05 Part A) ====================

        /// <summary>
        /// P0-05: the ground strike handed the body a one-frame 3.8 u/s poke that the very next
        /// <c>FixedTick</c> replaced with the input-only attack walk, so the swing had no forward
        /// travel. The lunge now starts on the hit frame and survives <c>FixedTick</c> for its whole
        /// duration, decaying down to (but never below) the player's own attack walk, so walking
        /// backwards during the swing cannot cancel it. The hit box also re-resolves for the whole
        /// active window, but the slash VFX / sword whoosh must fire exactly once per swing while
        /// each enemy still takes damage at most once.
        /// </summary>
        [Test]
        public void T05_AttackState_PreservesLungeAcrossFixedTick_AndEmitsSwingEventOnlyOnceAcrossActiveWindow()
        {
            var root = new GameObject("__Round9T05StrikeLunge");
            try
            {
                CreateFlatGround(root.transform, new Vector2(0f, -0.5f));
                var harness = CreatePlayerHarness(root.transform, new Vector2(0f, 0f));
                var target = CreateRound9EnemyTarget(root.transform, "Zombie", new Vector2(1.1f, 0f));
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded, "Precondition: the strike lunge is a grounded move.");

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);

                int swingEvents = 0;
                harness.Context.Combat.StrikePerformed += (_, _, _) => swingEvents++;

                // ---------- Hit 1: lunge + one swing event across the whole active window ----------
                harness.Context.PendingComboIndex = 1;
                ForceState(harness.Machine, harness.Machine.Attack);
                var attack = (PlayerAttackState)harness.Machine.Current;

                var strike = harness.Context.Combat.MeleeWeapon.GetStrike(0);
                float speed = strike.AnimSpeed > 0f ? strike.AnimSpeed : 1f;
                float hitTime = strike.HitFrame / 30f / speed;

                attack.Tick(Mathf.Max(0.001f, hitTime - 0.01f));
                Assert.AreEqual(0, swingEvents, "No swing event may fire before the hit frame.");
                int healthBefore = target.Current;

                attack.Tick(0.015f); // crosses HitFrame
                Assert.AreEqual(1, swingEvents, "The hit frame must raise StrikePerformed exactly once.");
                Assert.Less(target.Current, healthBefore,
                    "Precondition: the hit frame must connect, or the at-most-once check proves nothing.");
                int healthAfterHit = target.Current;
                Assert.AreEqual(7.2f, harness.Body.linearVelocity.x, 0.001f,
                    "The hit frame must immediately apply the hit-1 forward lunge (7.2 u/s).");

                // Walking backwards must not wipe the lunge: the blend takes the greater magnitude.
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), -1f);
                harness.Machine.FixedTick();
                float lungeVx = harness.Body.linearVelocity.x;
                Assert.Greater(lungeVx, 0f,
                    "FixedTick must preserve the strike lunge: the next fixed step may not replace it with the " +
                    $"input-only attack walk when the player holds backwards. Actual vx = {lungeVx:F3}.");
                Assert.LessOrEqual(lungeVx, 7.2f + 0.001f,
                    "The lunge must never exceed its configured speed.");

                // Drive the rest of the active window (0.065s) plus a margin.
                for (int frame = 0; frame < 12; frame++)
                    attack.Tick(0.01f);

                Assert.AreEqual(1, swingEvents,
                    "One swing must raise exactly one StrikePerformed across its whole multi-frame active " +
                    $"window, or the slash VFX and sword SFX stack. Actual count = {swingEvents}.");
                Assert.AreEqual(healthAfterHit, target.Current,
                    "A target hit once must not take damage again while the same swing re-resolves across the " +
                    "active window: HitQuery._alreadyHit must hold for the whole swing.");

                // The lunge must END: it decays to zero inside its 0.095s window and then hands the
                // horizontal velocity back to the attack walk. A "lunge never ends" regression would
                // otherwise keep the body sliding at 7.2 u/s for the whole clip and still pass nothing.
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                bool lungeDecayed = false;
                for (int frame = 0; frame < 10 && !lungeDecayed; frame++)
                {
                    harness.Machine.FixedTick();
                    lungeDecayed = Mathf.Abs(harness.Body.linearVelocity.x) <= 0.0001f;
                }
                Assert.IsTrue(lungeDecayed,
                    "The strike lunge must decay to zero within its 0.095s window; it cannot push the body " +
                    $"for the whole clip. Actual vx = {harness.Body.linearVelocity.x:F3} after 10 fixed steps.");

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), -1f);
                harness.Machine.FixedTick();
                float handedBackVx = -harness.Context.Config.RunSpeed * harness.Context.Config.AttackMoveSpeedMultiplier;
                Assert.AreEqual(handedBackVx, harness.Body.linearVelocity.x, 0.001f,
                    "Once the lunge ends the attack walk must own the horizontal velocity again " +
                    "(-RunSpeed * AttackMoveSpeedMultiplier), so a dead lunge cannot freeze the body.");

                // ---------- Hit 3 (finisher): the heavier lunge ----------
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                harness.Context.PendingComboIndex = harness.Context.Combat.MeleeWeapon.ComboLength;
                ForceState(harness.Machine, harness.Machine.Attack);
                attack = (PlayerAttackState)harness.Machine.Current;
                var finisher = harness.Context.Combat.MeleeWeapon.GetStrike(harness.Context.Combat.MeleeWeapon.ComboLength - 1);
                float finisherSpeed = finisher.AnimSpeed > 0f ? finisher.AnimSpeed : 1f;
                attack.Tick(finisher.HitFrame / 30f / finisherSpeed + 0.005f);
                Assert.AreEqual(9.5f, harness.Body.linearVelocity.x, 0.001f,
                    "The combo finisher must lunge harder (9.5 u/s) than the opening hits (7.2 u/s).");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// P0-05: a swing that is <em>not</em> a chain continuation is a fresh swing and must start from a
        /// clean slate. <c>Enter()</c> zeroes the lunge/stall fields, but until now nothing distinguished
        /// "a new swing starts with zero lunge" from "the previous lunge had already decayed", because the
        /// only other re-entry (<c>T05</c>'s finisher block) deliberately lets the lunge run out first.
        /// Here the swing is interrupted while the 0.095s step-in is still live and still pushing the body,
        /// and the next opener is started from that state: a leaked window would give the fresh swing a
        /// step the player never earned.
        /// </summary>
        [Test]
        public void T05b_AttackState_FreshSwingAfterInterruptStartsWithZeroLungeWhileTheOldStepIsStillLive()
        {
            var root = new GameObject("__Round9T05bFreshSwingStartsClean");
            try
            {
                CreateFlatGround(root.transform, new Vector2(0f, -0.5f));
                var harness = CreatePlayerHarness(root.transform, new Vector2(0f, 0f));
                CreateRound9EnemyTarget(root.transform, "Zombie", new Vector2(1.1f, 0f));
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded, "Precondition: the step-in is a grounded move.");

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);

                var weapon = harness.Context.Combat.MeleeWeapon;
                harness.Context.PendingComboIndex = 1;
                ForceState(harness.Machine, harness.Machine.Attack);
                var attack = (PlayerAttackState)harness.Machine.Current;

                var strike = weapon.GetStrike(0);
                float speed = strike.AnimSpeed > 0f ? strike.AnimSpeed : 1f;
                attack.Tick(strike.HitFrame / 30f / speed + 0.005f); // opens the 0.095s step-in

                float liveLungeTimer = GetPrivateField<float>(attack, "_lungeTimer");
                Assert.Greater(liveLungeTimer, 0f,
                    "Precondition: the hit frame must leave the step-in live, or there is nothing to leak.");
                Assert.AreEqual(7.2f, harness.Body.linearVelocity.x, 0.001f,
                    "Precondition: the step-in must be pushing the body at 7.2 u/s right now.");

                // Interrupt the swing while that step is still live. Roll's Enter()/Exit() never touches the
                // attack state's lunge fields, so they are still live when the next swing's Enter() runs —
                // which is exactly the state the "fresh swing starts clean" guarantee has to survive.
                ForceState(harness.Machine, harness.Machine.Roll);
                Assert.AreSame(harness.Machine.Roll, harness.Machine.Current,
                    "Precondition: the swing must actually be interrupted for this to prove anything.");
                Assert.AreEqual(liveLungeTimer, GetPrivateField<float>(attack, "_lungeTimer"), 0.0001f,
                    "Precondition: interrupting the swing must leave its step-in live, so the next Enter() is the " +
                    "only thing that can clear it.");

                // A fresh opener, not a chain continuation: nothing hands momentum over this transition.
                harness.Context.PendingComboIndex = 1;
                ForceState(harness.Machine, harness.Machine.Attack);
                var fresh = (PlayerAttackState)harness.Machine.Current;

                Assert.AreEqual(0f, GetPrivateField<float>(fresh, "_lungeTimer"), 0.0001f,
                    "A fresh swing must start with no step-in window: Enter() zeroes _lungeTimer, so a swing that " +
                    $"is not a chain continuation cannot inherit the previous one. Actual _lungeTimer = {GetPrivateField<float>(fresh, "_lungeTimer"):F4}s.");
                Assert.AreEqual(0f, GetPrivateField<float>(fresh, "_lungeSpeed"), 0.0001f,
                    "A fresh swing must start with no carried lunge speed.");
                Assert.AreEqual(0f, GetPrivateField<float>(fresh, "_airStallTimer"), 0.0001f,
                    "A fresh swing must start with no carried air hit-stall window.");
                Assert.IsFalse(GetPrivateField<bool>(fresh, "_airStallApplied"),
                    "A fresh swing must start without the air hit-stall already marked applied.");
                Assert.IsFalse(GetPrivateField<bool>(fresh, "_hitDone"),
                    "A fresh swing must start before its hit frame.");

                harness.Machine.FixedTick();
                Assert.AreEqual(0f, harness.Body.linearVelocity.x, 0.0001f,
                    "A fresh swing with no move input must not move the body at all: a carried lunge would push " +
                    $"it to 7.2 u/s the moment FixedTick runs. Actual vx = {harness.Body.linearVelocity.x:F3}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// P0-05: the combo buffer was gated on <c>_comboIndex &lt; ComboLength</c>, so a press during the
        /// finisher (hit 3) was dropped and the chain fell back to Idle instead of looping back to hit 1.
        /// An air swing that connects also has to stall the fall long enough to read as a hit, then hand
        /// gravity back exactly.
        /// </summary>
        [Test]
        public void T06_AttackState_ChainsHit3BackIntoHit1WhenBuffered_AndAppliesAirHitStall()
        {
            // ---------- (a) Hit 3 buffered wraps into hit 1 ----------
            var comboRoot = new GameObject("__Round9T06ComboLoop");
            try
            {
                CreateFlatGround(comboRoot.transform, new Vector2(0f, -0.5f));
                var harness = CreatePlayerHarness(comboRoot.transform, new Vector2(0f, 0f));
                // A live target in front of the player, so the finisher's own damage has to resolve before
                // the chain hands over to hit 1.
                var target = CreateRound9EnemyTarget(comboRoot.transform, "Zombie", new Vector2(1.1f, 0f));
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded, "Precondition: the combo loop is driven on the ground.");

                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);

                var weapon = harness.Context.Combat.MeleeWeapon;
                harness.Context.PendingComboIndex = weapon.ComboLength;
                ForceState(harness.Machine, harness.Machine.Attack);
                var attack = (PlayerAttackState)harness.Machine.Current;
                Assert.AreEqual(weapon.ComboLength, GetPrivateField<int>(attack, "_comboIndex"),
                    "Precondition: the swing must start on the finisher (hit 3).");

                var finisherStrike = weapon.GetStrike(weapon.ComboLength - 1);
                float finisherAnimSpeed = finisherStrike.AnimSpeed > 0f ? finisherStrike.AnimSpeed : 1f;
                float finisherHitTime = finisherStrike.HitFrame / 30f / finisherAnimSpeed;
                int expectedFinisherDamage = harness.PlayerGo.GetComponent<PlayerController>().RunStats
                    .ScaleDamage(finisherStrike.Damage, weapon.ScalingStat);
                int healthBefore = target.Current;

                // Buffer the follow-up during the finisher, then hold it while the swing plays out.
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), true);
                attack.Tick(0.005f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);

                float elapsed = 0.005f;
                float elapsedAtWrap = -1f;
                for (int frame = 0; frame < 200 && elapsedAtWrap < 0f; frame++)
                {
                    attack.Tick(0.01f);
                    elapsed += 0.01f;
                    if (GetPrivateField<int>(attack, "_comboIndex") == 1)
                        elapsedAtWrap = elapsed;
                }

                bool wrapped = elapsedAtWrap >= 0f;
                Assert.IsTrue(wrapped,
                    "Pressing attack during the finisher must loop the combo back into hit 1 instead of " +
                    $"ending the chain in {harness.Machine.Current?.GetType().Name ?? "None"}.");
                Assert.AreSame(harness.Machine.Attack, harness.Machine.Current,
                    "A wrapped combo must still be inside the attack state, ready to swing hit 1.");
                Assert.AreEqual(1, harness.Context.PendingComboIndex,
                    "PendingComboIndex must wrap (comboIndex % ComboLength) + 1, so hit 3 -> hit 1.");

                // The finisher must actually deal its damage before handing the chain over. A regression
                // that skipped PerformStrike whenever the next swing was already buffered would pass every
                // other assertion in this test, so the 45 is asserted explicitly - exactly once, not twice
                // across the finisher's multi-frame active window.
                Assert.AreEqual(healthBefore - expectedFinisherDamage, target.Current,
                    $"A buffered finisher must still resolve its scaled {expectedFinisherDamage} damage exactly " +
                    $"once before the chain wraps. Health went {healthBefore} -> {target.Current}, i.e. " +
                    $"{healthBefore - target.Current} damage.");

                // The active window must not be cut by the chain: the swing keeps resolving past the hit
                // frame until ActiveHitWindow closes, and only then may the next swing start.
                float windowBeforeChain = elapsedAtWrap - finisherHitTime;
                Assert.GreaterOrEqual(windowBeforeChain, 0.065f - 0.0001f,
                    "The chained swing may not start before the previous swing's 0.065s active window has " +
                    $"closed; it was cut to {windowBeforeChain:F4}s.");

                // The finisher's step-in must survive the chain re-entry instead of being wiped by the next
                // Enter(): hit 1 has not reached its own hit frame yet, so the only thing that can push the
                // body here is the carried finisher lunge.
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                harness.Machine.FixedTick();
                float carriedVx = harness.Body.linearVelocity.x;
                Assert.Greater(carriedVx, 0f,
                    "The finisher lunge must carry across the chained re-entry: the next FixedTick after the " +
                    $"wrap must still push the body forward. Actual vx = {carriedVx:F3}.");
            }
            finally
            {
                Object.DestroyImmediate(comboRoot);
            }

            // ---------- (b) Air hit-stall ----------
            var airRoot = new GameObject("__Round9T06AirHitStall");
            try
            {
                var harness = CreatePlayerHarness(airRoot.transform, new Vector2(0f, 5f));
                var target = CreateRound9EnemyTarget(airRoot.transform, "Zombie", new Vector2(1.1f, 5f));
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsFalse(harness.Context.IsGrounded, "Precondition: the air hit-stall needs an airborne swing.");

                harness.Body.linearVelocity = new Vector2(0f, -6f); // falling fast
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);
                harness.Context.PendingComboIndex = 1;
                ForceState(harness.Machine, harness.Machine.Attack);
                var attack = (PlayerAttackState)harness.Machine.Current;

                var strike = harness.Context.Combat.MeleeWeapon.GetStrike(0);
                float speed = strike.AnimSpeed > 0f ? strike.AnimSpeed : 1f;
                int healthBefore = target.Current;

                attack.Tick(strike.HitFrame / 30f / speed + 0.005f);
                Assert.Less(target.Current, healthBefore,
                    "Precondition: the airborne swing must connect, or the stall proves nothing.");
                Assert.GreaterOrEqual(harness.Body.linearVelocity.y, 2.2f - 0.001f,
                    "A landed air hit must stall the fall by lifting the vertical velocity to at least 2.2 u/s.");

                harness.Machine.FixedTick();
                Assert.AreEqual(harness.Context.Config.GravityScale * 0.35f, harness.Body.gravityScale, 0.0001f,
                    "During the air hit-stall gravity must run at 35% of GravityScale so the body hangs on the hit.");

                // A buffered air swing must keep that stall: the reduced-gravity window belongs to the whole
                // air sequence, not to one clip. The check runs before the next swing's own hit frame, so the
                // only thing that can still be holding gravity down here is the carried stall.
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), true);
                attack.Tick(0.005f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);

                int comboBefore = GetPrivateField<int>(attack, "_comboIndex");
                bool chainedInAir = false;
                for (int frame = 0; frame < 200 && !chainedInAir; frame++)
                {
                    attack.Tick(0.01f);
                    chainedInAir = GetPrivateField<int>(attack, "_comboIndex") != comboBefore;
                }
                Assert.IsTrue(chainedInAir,
                    "Precondition: a buffered air swing must chain into the next hit for the stall carry to matter.");

                harness.Machine.FixedTick();
                Assert.AreEqual(harness.Context.Config.GravityScale * 0.35f, harness.Body.gravityScale, 0.0001f,
                    "A chained air swing must keep the hit-stall running: the next swing's Enter() may not wipe " +
                    "the 0.12s reduced-gravity window the previous hit opened.");

                // The stall must expire and hand normal air gravity back, not stick forever.
                for (int frame = 0; frame < 10; frame++)
                    harness.Machine.FixedTick();
                Assert.AreEqual(harness.Context.Config.GravityScale, harness.Body.gravityScale, 0.0001f,
                    "Once the 0.12s air hit-stall expires, gravity must return to GravityScale so the player " +
                    "resumes falling instead of floating.");
            }
            finally
            {
                Object.DestroyImmediate(airRoot);
            }

            // ---------- (c) Landing mid-stall must clear the reduced gravity ----------
            var landingRoot = new GameObject("__Round9T06LandingClearsStall");
            try
            {
                CreateFlatGround(landingRoot.transform, new Vector2(0f, -0.5f));
                var harness = CreatePlayerHarness(landingRoot.transform, new Vector2(0f, 5f));
                var target = CreateRound9EnemyTarget(landingRoot.transform, "Zombie", new Vector2(1.1f, 5f));
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsFalse(harness.Context.IsGrounded, "Precondition: the swing opens the stall in mid air.");

                harness.Body.linearVelocity = new Vector2(0f, -6f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);
                harness.Context.PendingComboIndex = 1;
                ForceState(harness.Machine, harness.Machine.Attack);
                var attack = (PlayerAttackState)harness.Machine.Current;

                var strike = harness.Context.Combat.MeleeWeapon.GetStrike(0);
                float speed = strike.AnimSpeed > 0f ? strike.AnimSpeed : 1f;
                attack.Tick(strike.HitFrame / 30f / speed + 0.005f);
                harness.Machine.FixedTick();
                Assert.AreEqual(harness.Context.Config.GravityScale * 0.35f, harness.Body.gravityScale, 0.0001f,
                    "Precondition: the air hit-stall is running at 35% gravity before the landing.");

                // Land while the stall is still running: the stall lifts the body, so it falls back onto the
                // deck well inside the 0.12s window. Set the fall by hand and put the feet on the deck.
                harness.Body.linearVelocity = new Vector2(0f, -1f);
                harness.Body.position = new Vector2(0f, 0f);
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsTrue(harness.Context.IsGrounded,
                    "Precondition: the body must be standing to prove that the landing clears the stall.");

                harness.Machine.FixedTick();
                Assert.AreEqual(harness.Context.Config.GravityScale, harness.Body.gravityScale, 0.0001f,
                    "Landing during the air hit-stall must restore gravity straight away. Leaving 0.35x " +
                    "GravityScale alive while grounded would make the next walk off the ledge resume the " +
                    "fall at 35% gravity for the remainder of the stall.");
                Assert.AreEqual(0f, GetPrivateField<float>(attack, "_airStallTimer"), 0.0001f,
                    "Landing must clear the air hit-stall timer, so no reduced-gravity window survives it.");
            }
            finally
            {
                Object.DestroyImmediate(landingRoot);
            }

            // ---------- (d) A chained air swing opens its OWN stall once the predecessor's expired ----------
            var expiredRoot = new GameObject("__Round9T06ExpiredStallCarry");
            try
            {
                var harness = CreatePlayerHarness(expiredRoot.transform, new Vector2(0f, 5f));
                var target = CreateRound9EnemyTarget(expiredRoot.transform, "Zombie", new Vector2(1.1f, 5f));
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsFalse(harness.Context.IsGrounded, "Precondition: the air hit-stall needs an airborne swing.");

                harness.Body.linearVelocity = new Vector2(0f, -6f);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveX), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);

                var weapon = harness.Context.Combat.MeleeWeapon;
                harness.Context.PendingComboIndex = 1;
                ForceState(harness.Machine, harness.Machine.Attack);
                var attack = (PlayerAttackState)harness.Machine.Current;

                var strike = weapon.GetStrike(0);
                float speed = strike.AnimSpeed > 0f ? strike.AnimSpeed : 1f;
                int healthBefore = target.Current;

                attack.Tick(strike.HitFrame / 30f / speed + 0.005f);
                Assert.Less(target.Current, healthBefore,
                    "Precondition: the first airborne swing must connect, or there is no stall to expire.");
                Assert.GreaterOrEqual(harness.Body.linearVelocity.y, 2.2f - 0.001f,
                    "Precondition: the first airborne hit must open the lift before it can expire.");

                // Let the whole 0.12s stall run out BEFORE the chain is allowed to fire, and never let the
                // stall flag be the thing under test here: the point is that the window is dead by the time
                // the next swing starts.
                for (int frame = 0; frame < 40 && GetPrivateField<float>(attack, "_airStallTimer") > 0f; frame++)
                {
                    attack.Tick(0.01f);
                    harness.Machine.FixedTick();
                }

                Assert.AreEqual(0f, GetPrivateField<float>(attack, "_airStallTimer"), 0.0001f,
                    "Precondition: the predecessor's air hit-stall must be fully expired before the chain fires.");
                Assert.AreSame(harness.Machine.Attack, harness.Machine.Current,
                    "Precondition: the combo must still be inside the swing when its stall expires.");
                Assert.AreEqual(harness.Context.Config.GravityScale, harness.Body.gravityScale, 0.0001f,
                    "Precondition: with the stall expired the body must already be back on normal air gravity.");

                // Chain into hit 2 now that the predecessor's reduced-gravity window is long gone.
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), true);
                attack.Tick(0.005f);
                SetInput(harness.Input, nameof(PlayerInputReader.AttackPressed), false);

                bool chained = false;
                for (int frame = 0; frame < 200 && !chained; frame++)
                {
                    attack.Tick(0.01f);
                    chained = GetPrivateField<int>(attack, "_comboIndex") != 1;
                }

                Assert.IsTrue(chained,
                    "Precondition: a buffered air swing must chain into hit 2 for the carry to be exercised.");
                Assert.AreEqual(2, GetPrivateField<int>(attack, "_comboIndex"),
                    "Precondition: hit 1 must chain into hit 2 (not straight to the finisher).");

                var hit2 = weapon.GetStrike(1);
                float hit2Speed = hit2.AnimSpeed > 0f ? hit2.AnimSpeed : 1f;
                harness.Body.linearVelocity = new Vector2(0f, -6f); // still falling onto the new swing
                int healthBeforeHit2 = target.Current;

                attack.Tick(hit2.HitFrame / 30f / hit2Speed + 0.005f);
                Assert.Less(target.Current, healthBeforeHit2,
                    "Precondition: the chained hit 2 must connect, or the stall path is never reached.");
                Assert.GreaterOrEqual(harness.Body.linearVelocity.y, 2.2f - 0.001f,
                    "A chained air swing whose predecessor's stall has already expired must get its OWN lift: " +
                    "carrying a spent stall flag into the next swing leaves every chained hit after the first " +
                    $"with zero lift and zero slow-fall. Actual vy = {harness.Body.linearVelocity.y:F3}.");

                harness.Machine.FixedTick();
                Assert.AreEqual(harness.Context.Config.GravityScale * 0.35f, harness.Body.gravityScale, 0.0001f,
                    "The chained hit must open its own 0.12s reduced-gravity window, so every chained airborne " +
                    "swing reads with the same hit-stop as the first.");
            }
            finally
            {
                Object.DestroyImmediate(expiredRoot);
            }
        }

        /// <summary>
        /// P0-05 Part A: the tuning the overhaul depends on. Run speed and the attack walk scale up so
        /// the swing carries the player, the combo chain point comes in earlier (0.42) for a continuous
        /// loop, and the start sword gains the Dead Cells hack-and-slash cadence (faster clips, longer
        /// reach, heavier knockback, one crit finisher).
        /// </summary>
        [Test]
        public void T07_MovementConfigAndSwordAsset_MeetDeadCellsHackAndSlashTargets()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath);
            Assert.IsNotNull(config, $"Missing {MovementConfigPath}");
            Assert.AreEqual(8.6f, config.RunSpeed, 0.001f, "Round 9 run speed must be 8.6 u/s.");
            Assert.AreEqual(7.4f, config.AirSpeed, 0.001f, "Round 9 air speed must be 7.4 u/s.");
            Assert.AreEqual(0.42f, config.ComboChainNormalizedTime, 0.001f,
                "The combo must chain at 0.42 of the clip so the 3-hit loop stays continuous.");
            Assert.AreEqual(0.5f, config.AttackMoveSpeedMultiplier, 0.001f,
                "The attack walk must keep half of the run speed.");

            var sword = AssetDatabase.LoadAssetAtPath<WeaponData>(SwordDataPath);
            Assert.IsNotNull(sword, $"Missing {SwordDataPath}");
            Assert.AreEqual(3, sword.ComboLength, "Weapon_StartSword must keep a 3-hit combo.");

            var hit1 = sword.GetStrike(0);
            Assert.AreEqual(25, hit1.Damage, "Hit 1 damage.");
            Assert.AreEqual(5, hit1.HitFrame, "Hit 1 hit frame.");
            Assert.AreEqual(1.40f, hit1.AnimSpeed, 0.001f, "Hit 1 must play at 1.40x for a fast opener.");
            Assert.AreEqual(2.9f, hit1.RangeWidthCells, 0.001f, "Hit 1 reach must be 2.9 cells.");
            Assert.AreEqual(5.2f, hit1.Knockback, 0.001f, "Hit 1 knockback must be 5.2.");
            Assert.IsFalse(hit1.CanCrit, "Hit 1 must not crit.");

            var hit2 = sword.GetStrike(1);
            Assert.AreEqual(30, hit2.Damage, "Hit 2 damage.");
            Assert.AreEqual(7, hit2.HitFrame, "Hit 2 hit frame.");
            Assert.AreEqual(1.48f, hit2.AnimSpeed, 0.001f, "Hit 2 must play at 1.48x.");
            Assert.AreEqual(3.0f, hit2.RangeWidthCells, 0.001f, "Hit 2 reach must be 3.0 cells.");
            Assert.AreEqual(6.2f, hit2.Knockback, 0.001f, "Hit 2 knockback must be 6.2.");
            Assert.IsFalse(hit2.CanCrit, "Hit 2 must not crit.");

            var hit3 = sword.GetStrike(2);
            Assert.AreEqual(45, hit3.Damage, "Hit 3 damage.");
            Assert.AreEqual(9, hit3.HitFrame, "Hit 3 hit frame.");
            Assert.AreEqual(1.05f, hit3.AnimSpeed, 0.001f, "Hit 3 must play at 1.05x so the finisher has weight.");
            Assert.AreEqual(3.85f, hit3.RangeWidthCells, 0.001f, "The finisher reach must be 3.85 cells.");
            Assert.AreEqual(2.7f, hit3.RangeHeightCells, 0.001f, "The finisher must sweep 2.7 cells tall.");
            Assert.AreEqual(10.5f, hit3.Knockback, 0.001f, "The finisher knockback must be 10.5.");
            Assert.IsTrue(hit3.CanCrit, "Hit 3 is the crit finisher.");
        }

        /// <summary>
        /// P0-05 Part B: a grounded hit with weight pops the enemy upward so the hit registers on the
        /// body itself, the pop is a floor on the vertical arc (never a cut), light hits stay flat,
        /// knockback resistance scales the push before the 3.5 gate, and the horizontal carry bleeds
        /// at 18 u/s² instead of 30 so the slide is still readable after the hit-stop ends.
        /// </summary>
        [Test]
        public void T08_EnemyStunState_AppliesUpwardPopAnd18uSlideDamping()
        {
            var root = new GameObject("__Round9T08EnemyPop");
            EnemyStunHarness harness = default;
            EnemyStunHarness gravityHarness = default;
            try
            {
                // Flat floor whose top face is y = 0, so an enemy with feet at y = 0 is grounded.
                CreateFlatGround(root.transform, new Vector2(0f, -0.5f));
                harness = CreateEnemyStunHarness(root.transform, Vector2.zero);
                var ctx = harness.Context;
                var stun = harness.Stun;

                void Arm(Vector2 position, Vector2 velocity, float resistance)
                {
                    harness.Go.transform.position = position;
                    harness.Body.position = position;
                    harness.Body.linearVelocity = velocity;
                    Physics2D.SyncTransforms();
                    ctx.RefreshFrame();
                    SetPrivateField(harness.Data, "_knockbackResistance", resistance);
                }

                // 1. Hit 1 of Weapon_StartSword (knockback 5.2, no crit) on the ground: slide + a 2.2 pop.
                Arm(Vector2.zero, Vector2.zero, 0f);
                Assert.IsTrue(ctx.IsGrounded, "Precondition: the enemy must stand on the floor.");
                ctx.LastDamage = new DamageInfo(25, 1, 5.2f, 0.25f, false, null);
                stun.Enter();
                Assert.AreEqual(5.2f, ctx.Velocity.x, 0.001f,
                    "A grounded hit must slide at the full reduced knockback (5.2).");
                Assert.AreEqual(2.2f, ctx.Velocity.y, 0.001f,
                    "A grounded non-crit 5.2 hit must pop the enemy to 2.2 u/s.");
                Assert.AreEqual(-1, ctx.Facing, "The enemy must turn to face the attacker (opposite the push).");

                // 2. The crit finisher (knockback 10.5, crit) gets the finisher's pop.
                Arm(Vector2.zero, Vector2.zero, 0f);
                ctx.LastDamage = new DamageInfo(45, 1, 10.5f, 0.25f, true, null);
                stun.Enter();
                Assert.AreEqual(10.5f, ctx.Velocity.x, 0.001f, "The finisher must slide at 10.5.");
                Assert.AreEqual(3.8f, ctx.Velocity.y, 0.001f, "A grounded crit hit must pop to 3.8 u/s.");
                Assert.AreEqual(-1, ctx.Facing, "A +1 push must leave the enemy facing -1, toward the attacker.");

                // 3. Under the 3.5 gate: no pop, just the slide.
                Arm(Vector2.zero, Vector2.zero, 0f);
                ctx.LastDamage = new DamageInfo(10, 1, 3.0f, 0.25f, false, null);
                stun.Enter();
                Assert.AreEqual(3.0f, ctx.Velocity.x, 0.001f, "A light hit must still slide.");
                Assert.AreEqual(0f, ctx.Velocity.y, 0.001f, "A 3.0 push is under the gate and must not pop.");

                // 3b. The gate is inclusive: exactly 3.5 pops.
                Arm(Vector2.zero, Vector2.zero, 0f);
                ctx.LastDamage = new DamageInfo(10, -1, 3.5f, 0.25f, false, null);
                stun.Enter();
                Assert.AreEqual(2.2f, ctx.Velocity.y, 0.001f, "The gate is `push >= 3.5`, so exactly 3.5 must pop.");
                Assert.AreEqual(1, ctx.Facing, "A -1 push must turn the enemy toward +1.");

                // 4. Airborne: no pop, and the existing vertical arc is left alone.
                Arm(new Vector2(0f, 3f), new Vector2(0f, -1f), 0f);
                Assert.IsFalse(ctx.IsGrounded, "Precondition: the enemy is airborne above the floor.");
                ctx.LastDamage = new DamageInfo(30, 1, 6.2f, 0.25f, false, null);
                stun.Enter();
                Assert.AreEqual(6.2f, ctx.Velocity.x, 0.001f, "An airborne hit still slides.");
                Assert.AreEqual(-1f, ctx.Velocity.y, 0.001f, "An airborne hit must not rewrite the vertical arc.");

                // 5. Already rising faster than the pop floor: the floor never cuts the arc.
                Arm(Vector2.zero, new Vector2(0f, 5f), 0f);
                ctx.LastDamage = new DamageInfo(25, 1, 5.2f, 0.25f, false, null);
                stun.Enter();
                Assert.AreEqual(5f, ctx.Velocity.y, 0.001f, "The pop is a floor on Velocity.y, never a clamp down.");

                // 6. Knockback resistance scales the push (Shieldbearer 0.3): 10.5 -> 7.35.
                Arm(Vector2.zero, Vector2.zero, 0.3f);
                ctx.LastDamage = new DamageInfo(45, 1, 10.5f, 0.25f, true, null);
                stun.Enter();
                Assert.AreEqual(7.35f, ctx.Velocity.x, 0.001f,
                    "Resistance 0.3 must leave 7.35 of a 10.5 knockback.");
                Assert.AreEqual(3.8f, ctx.Velocity.y, 0.001f, "A resisted 7.35 still clears the pop gate.");

                // 6b. Resistance dropping the push under the gate suppresses the pop: the gate reads
                // the resisted push, not the raw knockback (raw 4.5 would have cleared 3.5).
                Arm(Vector2.zero, Vector2.zero, 0.3f);
                ctx.LastDamage = new DamageInfo(10, 1, 4.5f, 0.25f, false, null);
                stun.Enter();
                Assert.AreEqual(3.15f, ctx.Velocity.x, 0.001f, "4.5 resisted by 0.3 gives a 3.15 slide.");
                Assert.AreEqual(0f, ctx.Velocity.y, 0.001f,
                    "A resisted 3.15 is under the gate: the raw 4.5 must not decide the pop.");

                // 6c. The Shieldbearer's hit-1 is the tightest real margin in the game: 5.2 × (1 − 0.3)
                // = 3.64 against the 3.5 gate — 0.14 u/s, a 4% margin. The other enemies sit at 6.2 and
                // 10.5, which resist to 4.34 and 7.35 and have room to spare. A resistance retune to
                // 0.33 or a gate raised to 3.7 would silently stop the Shieldbearer popping on hit 1;
                // this case pins the 3.64 > 3.5 relationship so that retune cannot land unseen.
                Arm(Vector2.zero, Vector2.zero, 0.3f);
                ctx.LastDamage = new DamageInfo(25, 1, 5.2f, 0.25f, false, null);
                stun.Enter();
                Assert.AreEqual(3.64f, ctx.Velocity.x, 0.001f,
                    "5.2 resisted by 0.3 must leave a 3.64 slide (Shieldbearer hit 1).");
                Assert.AreEqual(2.2f, ctx.Velocity.y, 0.001f,
                    "3.64 clears the 3.5 pop gate by only 0.14 u/s: the Shieldbearer light hit must still pop.");

                // 7. Directionless hits (blasts, stomps) neither push nor turn.
                Arm(Vector2.zero, Vector2.zero, 0f);
                ctx.Face(1);
                ctx.LastDamage = new DamageInfo(20, 0, 8f, 0.25f, false, null);
                stun.Enter();
                Assert.AreEqual(0f, ctx.Velocity.x, 0.001f, "A directionless hit must not add horizontal velocity.");
                Assert.AreEqual(0f, ctx.Velocity.y, 0.001f, "A directionless hit must not pop either.");
                Assert.AreEqual(1, ctx.Facing, "A directionless hit must not turn the enemy.");

                // 8. The carry: 18 u/s² bleeds the slide slower than the old 30 u/s² did.
                float newBleed = 18f * Time.fixedDeltaTime;
                float oldBleed = 30f * Time.fixedDeltaTime;
                Assert.Less(newBleed, oldBleed, "Sanity: 18 u/s² bleeds less per step than 30 u/s².");

                Arm(Vector2.zero, new Vector2(5.2f, 0f), 0f);
                stun.FixedTick();
                Assert.AreEqual(5.2f - newBleed, ctx.Velocity.x, 0.001f,
                    "FixedTick must bleed horizontal recoil at 18 u/s² so the slide survives the hit-stop.");

                Arm(Vector2.zero, new Vector2(5.2f, 0f), 0f);
                for (int i = 0; i < 3; i++)
                    stun.FixedTick();
                Assert.AreEqual(5.2f - newBleed * 3f, ctx.Velocity.x, 0.002f,
                    "Three FixedTicks must bleed exactly 3 x 18 * fixedDeltaTime.");
                Assert.Greater(ctx.Velocity.x, 5.2f - oldBleed * 3f,
                    "After 0.06 s the slide must still carry more speed than the old 30 u/s² decay left.");

                // 8b. Termination, not just magnitude: the decay must reach exactly 0 and stay there.
                // A recoil smaller than one step's bleed has to land on the clamp, never on a residue
                // or a sign flip, otherwise it would creep towards zero forever without arriving.
                Arm(Vector2.zero, new Vector2(0.1f, 0f), 0f);
                int ticksToStop = 0;
                for (; ticksToStop < 10 && ctx.Velocity.x != 0f; ticksToStop++)
                    stun.FixedTick();
                Assert.AreEqual(0f, ctx.Velocity.x, 0f,
                    "A 0.1 recoil must bleed to exactly 0, not to a small residue just above it.");
                Assert.LessOrEqual(ticksToStop, 10,
                    "The zero clamp must terminate inside a bounded number of fixed ticks.");

                // 9. Real gravity: how high the pop actually carries the body, so the Phase 4
                // GPU/playtest gate has a measured number instead of an opinion. Shipped physics are
                // Physics2D gravity y = -9.81 (ProjectSettings/Physics2DSettings.asset) with
                // m_GravityScale 3.5 on every enemy prefab, so g = 34.335 u/s². Analytic apex
                // vy0² / (2g) for the shipped pop speeds:
                //   non-crit 2.2 u/s -> 0.0705 u = 0.094 cell = 2.3 px @ 32 PPU, airtime 0.128 s
                //   crit     3.8 u/s -> 0.2103 u = 0.280 cell = 6.7 px @ 32 PPU, airtime 0.221 s
                // Measured here, one real fixed step per iteration (also recorded in the task report):
                //   non-crit 0.0496 u (~1.6 px), crit 0.1740 u (~5.6 px). The horizontal slide is what
                // sells the hit; the vertical pop is a nudge. PopSpeed/CritPopSpeed stay at the plan's
                // values — their feel is the visual gate's decision, not this fixture's.
                gravityHarness = CreateEnemyStunHarness(root.transform, new Vector2(6f, 0f), PrefabGravityScale);
                Rigidbody2D gBody = gravityHarness.Body;
                EnemyContext gCtx = gravityHarness.Context;
                EnemyStunState gStun = gravityHarness.Stun;

                void ArmGravity(float resistance)
                {
                    gBody.position = new Vector2(6f, 0f);
                    gBody.linearVelocity = Vector2.zero;
                    Physics2D.SyncTransforms();
                    gCtx.RefreshFrame();
                    SetPrivateField(gravityHarness.Data, "_knockbackResistance", resistance);
                }

                float gravity = -Physics2D.gravity.y * gBody.gravityScale;
                Assert.AreEqual(34.335f, gravity, 0.01f,
                    "Fixture: -9.81 x the prefab's gravityScale 3.5 must give 34.335 u/s².");

                ArmGravity(0f);
                Assert.IsTrue(gCtx.IsGrounded, "Precondition: the gravity enemy must stand on the floor.");
                gCtx.LastDamage = new DamageInfo(25, 1, 5.2f, 0.25f, false, null);
                gStun.Enter();
                Assert.AreEqual(2.2f, gBody.linearVelocity.y, 0.001f,
                    "Precondition: the grounded pop launches at 2.2 u/s before gravity gets a vote.");
                float nonCritApex = MeasurePopApex(gravityHarness);

                ArmGravity(0f);
                gCtx.LastDamage = new DamageInfo(45, 1, 10.5f, 0.25f, true, null);
                gStun.Enter();
                Assert.AreEqual(3.8f, gBody.linearVelocity.y, 0.001f,
                    "Precondition: the grounded crit pop launches at 3.8 u/s.");
                float critApex = MeasurePopApex(gravityHarness);

                // The measurement, in the log, for whoever gates the feel later.
                Debug.Log($"[T08] pop apex under g={gravity:F3} u/s² (fixedDeltaTime {Time.fixedDeltaTime:F5} s): " +
                          $"non-crit {nonCritApex:F4} u = {nonCritApex * 32f:F2} px @ 32 PPU, " +
                          $"crit {critApex:F4} u = {critApex * 32f:F2} px @ 32 PPU.");

                AssertPopApex(nonCritApex, 2.2f, gravity, "non-crit");
                AssertPopApex(critApex, 3.8f, gravity, "crit");
            }
            finally
            {
                if (harness.Data != null)
                    Object.DestroyImmediate(harness.Data);
                if (gravityHarness.Data != null)
                    Object.DestroyImmediate(gravityHarness.Data);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Combat_DoorAndWallOcclusion_BlocksMeleeStompAndBowMuzzle()
        {
            var root = new GameObject("__Round9DoorOcclusionTest");
            try
            {
                CreateFlatGround(root.transform, new Vector2(0f, -0.5f));
                var harness = CreatePlayerHarness(root.transform, Vector2.zero);
                var enemy = CreateRound9EnemyTarget(root.transform, "OccludedEnemy", new Vector2(1.35f, 0f));

                var doorGo = new GameObject("ClosedDoor_050");
                doorGo.transform.SetParent(root.transform);
                doorGo.layer = GroundLayer;
                doorGo.transform.position = new Vector2(0.65f, 1.0f);
                var doorCol = doorGo.AddComponent<BoxCollider2D>();
                doorCol.size = new Vector2(0.50f, 2.0f);

                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();

                var strike = harness.Context.Combat.MeleeWeapon.GetStrike(0);
                harness.Context.Combat.BeginSwing();
                Assert.AreEqual(0, harness.Context.Combat.PerformStrike(strike, facing: 1),
                    "0.50u solid door between player and enemy must occlude melee swing.");
                Assert.AreEqual(enemy.Max, enemy.Current);

                Assert.AreEqual(0, harness.Context.Combat.PerformStomp(30, 1f, new Vector2(4f, 2f)),
                    "0.50u solid door between player and enemy must occlude stomp.");
                Assert.AreEqual(enemy.Max, enemy.Current);

                Vector2 firedMuzzle = default;
                int worldHitCount = 0;
                harness.Context.Bow.Fired += (muzzle, _, _) => firedMuzzle = muzzle;
                harness.Context.Bow.ArrowWorldHit += (_, _, _) => worldHitCount++;

                Assert.IsTrue(harness.Context.Bow.TryFire(Vector2.zero, 1, crouching: false, harness.Context.Bow.GetNextShot()));
                Assert.Less(firedMuzzle.x, 0.40f,
                    "Bow muzzle must clamp in front of the 0.50u door face at x=0.40.");

                var activeArrows = GetPrivateField<List<RecoverableArrow>>(harness.Context.Bow, "_activeArrows");
                for (int step = 0; step < 10 && activeArrows.Count > 0; step++)
                {
                    foreach (var arrow in activeArrows.ToArray())
                    {
                        if (arrow != null && arrow.gameObject.activeSelf)
                            InvokePrivateMethod(arrow, "FixedUpdate");
                    }
                }

                Assert.AreEqual(1, worldHitCount, "Clamped bow shot must hit the closed door face.");
                Assert.AreEqual(enemy.Max, enemy.Current, "Clamped bow shot must not tunnel past the closed door.");

                // One-way platform must not occlude melee or stomp.
                doorGo.layer = OneWayLayer;
                Physics2D.SyncTransforms();

                harness.Context.Combat.BeginSwing();
                Assert.AreEqual(1, harness.Context.Combat.PerformStrike(strike, facing: 1),
                    "OneWayPlatform must not occlude melee swings.");
                enemy.RestoreFull();
                Assert.AreEqual(1, harness.Context.Combat.PerformStomp(30, 1f, new Vector2(4f, 2f)),
                    "OneWayPlatform must not occlude stomps.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Combat_StarterSwordThirdHitCrit_IsBlockedByFrontShield()
        {
            var root = new GameObject("__Round9ShieldCritBlockTest");
            GameObject shieldGo = null;
            try
            {
                CreateFlatGround(root.transform, new Vector2(0f, -0.5f));
                var harness = CreatePlayerHarness(root.transform, Vector2.zero);
                var playerController = harness.PlayerGo.GetComponent<PlayerController>();

                var shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPaths[2]);
                Assert.IsNotNull(shieldPrefab, $"Missing {EnemyPrefabPaths[2]}");
                shieldGo = Object.Instantiate(shieldPrefab, new Vector3(1.5f, 0f, 0f), Quaternion.identity);

                var shieldAnim = shieldGo.GetComponent<EnemyAnimator>();
                var shieldController = shieldGo.GetComponent<EnemyController>();
                var shieldHealth = shieldGo.GetComponent<Health>();
                InvokePrivateMethod(shieldAnim, "Awake");
                InvokePrivateMethod(shieldController, "Awake");
                InvokePrivateMethod(shieldController, "Start");

                var shieldContext = GetPrivateField<EnemyContext>(shieldController, "_context");
                shieldContext.Face(-1);

                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();

                var hit3 = harness.Context.Combat.MeleeWeapon.GetStrike(2);
                Assert.IsTrue(hit3.CanCrit, "Precondition: starter sword 3rd hit must have CanCrit = true.");
                Assert.AreEqual(0f, hit3.Stun, 0.0001f, "Precondition: starter sword 3rd hit must have Stun = 0.");

                int staggeredDir = 0;
                playerController.Staggered += dir => staggeredDir = dir;

                harness.Context.Combat.BeginSwing();
                int hits = harness.Context.Combat.PerformStrike(hit3, facing: 1);

                Assert.AreEqual(0, hits, "Front shield must block starter sword 3rd hit despite CanCrit = true.");
                Assert.AreEqual(shieldHealth.Max, shieldHealth.Current, "Shieldbearer HP must stay unchanged.");
                Assert.AreEqual(-1, staggeredDir, "Blocked front melee strike must stagger the player backward.");
                Assert.AreSame(harness.Machine.Hurt, harness.Machine.Current, "Shield stagger must push player into Hurt state.");
            }
            finally
            {
                if (shieldGo != null)
                    Object.DestroyImmediate(shieldGo);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Player_LowCeilingCrouch_PreventsStandingUpUntilClear_AndBowJumpClearsCrouch()
        {
            var root = new GameObject("__Round9LowCeilingCrouchTest");
            try
            {
                CreateFlatGround(root.transform, new Vector2(0f, -0.5f));
                var ceilingGo = new GameObject("LowCeiling");
                ceilingGo.transform.SetParent(root.transform);
                ceilingGo.layer = GroundLayer;
                ceilingGo.transform.position = new Vector2(0f, 1.60f);
                ceilingGo.AddComponent<BoxCollider2D>().size = new Vector2(4f, 1.0f);

                var harness = CreatePlayerHarness(root.transform, Vector2.zero);
                var col = harness.PlayerGo.GetComponent<BoxCollider2D>();
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();

                Assert.IsFalse(harness.Context.CanStandUp(), "Precondition: 1.10u solid ceiling blocks standing up.");

                SetInput(harness.Input, nameof(PlayerInputReader.CrouchHeld), true);
                ForceState(harness.Machine, harness.Machine.Crouch);
                var crouchState = (PlayerCrouchState)harness.Machine.Crouch;
                Assert.AreEqual(0.90f, col.size.y, 0.001f, "Crouching must shrink collider to 0.90u.");

                // Releasing crouch and pressing jump under ceiling must stay crouched.
                SetInput(harness.Input, nameof(PlayerInputReader.CrouchHeld), false);
                SetInput(harness.Input, nameof(PlayerInputReader.MoveY), 0f);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                harness.Context.RefreshFrame();
                BeginSimulatedFrame(harness.Machine);
                TickCrouch(crouchState, 0.02f);

                Assert.AreSame(harness.Machine.Crouch, harness.Machine.Current,
                    "Under 1.10u ceiling, releasing crouch and pressing jump must keep PlayerCrouchState.");
                Assert.AreEqual(0.90f, col.size.y, 0.001f, "Collider must remain 0.90u under low ceiling.");

                // Finishing a bow shot under the ceiling must return to Crouch with 0.90u collider.
                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), false);
                harness.Context.Weapons.Equip(PlayerWeaponSlot.Bow);
                ForceState(harness.Machine, harness.Machine.BowAttack);
                var bowState = (PlayerBowAttackState)harness.Machine.Current;
                Assert.AreEqual(0.90f, col.size.y, 0.001f, "Entering BowAttack under ceiling must keep 0.90u collider.");

                var shot0 = harness.Context.Bow.Data.GetShot(0);
                bowState.Tick(shot0.ChargeTime + 0.01f);
                BeginSimulatedFrame(harness.Machine);
                bowState.Tick(0.5f);

                Assert.AreSame(harness.Machine.Crouch, harness.Machine.Current,
                    "Finishing BowAttack under 1.10u ceiling must transition to Crouch instead of standing.");
                Assert.AreEqual(0.90f, col.size.y, 0.001f, "Collider must stay 0.90u on returning to Crouch.");

                // Removing ceiling allows standing up cleanly to 1.60u.
                ceilingGo.SetActive(false);
                Physics2D.SyncTransforms();
                Assert.IsTrue(harness.Context.CanStandUp());
                BeginSimulatedFrame(harness.Machine);
                TickCrouch(crouchState, 0.02f);

                Assert.AreSame(harness.Machine.Idle, harness.Machine.Current,
                    "Once ceiling clears, releasing crouch must stand up into Idle.");
                Assert.AreEqual(1.60f, col.size.y, 0.001f, "Standing up must restore 1.60u collider.");

                // Open-air jump during bow recovery must clear _crouching and restore 1.60u collider.
                harness.Context.Bow.ResetShots();
                SetInput(harness.Input, nameof(PlayerInputReader.CrouchHeld), true);
                harness.Context.RefreshFrame();
                ForceState(harness.Machine, harness.Machine.BowAttack);
                bowState.Tick(shot0.ChargeTime + 0.01f);
                Assert.AreEqual(0.90f, col.size.y, 0.001f);

                SetInput(harness.Input, nameof(PlayerInputReader.JumpPressed), true);
                SetInput(harness.Input, nameof(PlayerInputReader.JumpHeld), true);
                harness.Context.RefreshFrame();
                bowState.Tick(0.01f);

                Assert.IsFalse(GetPrivateField<bool>(bowState, "_crouching"),
                    "Open-air jump during bow recovery must clear _crouching.");
                Assert.AreEqual(1.60f, col.size.y, 0.001f,
                    "Open-air jump during bow recovery must restore 1.60u collider.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RecoverableArrow_Quiver_UsesPassedDeltaTime()
        {
            var root = new GameObject("__Round9ArrowQuiverDeltaTimeTest");
            try
            {
                var bowData = AssetDatabase.LoadAssetAtPath<BowWeaponData>(BowDataPath);
                var arrowPrefab = AssetDatabase.LoadAssetAtPath<RecoverableArrow>(PlayerArrowPrefabPath);
                Assert.IsNotNull(bowData, $"Missing {BowDataPath}");
                Assert.IsNotNull(arrowPrefab, $"Missing {PlayerArrowPrefabPath}");

                var wallGo = new GameObject("SolidWall");
                wallGo.transform.SetParent(root.transform);
                wallGo.layer = GroundLayer;
                wallGo.transform.position = new Vector2(1.0f, 0f);
                wallGo.AddComponent<BoxCollider2D>().size = new Vector2(1.0f, 4f);

                var arrow = Object.Instantiate(arrowPrefab);
                arrow.transform.SetParent(root.transform, false);
                bool worldHit = false;
                arrow.Initialize(_ => { });
                arrow.WorldHit += (_, _, _) => worldHit = true;
                arrow.Launch(Vector2.zero, 1, bowData, 10, (LayerMask)(1 << EnemyLayer), (LayerMask)(1 << GroundLayer), null);

                Physics2D.SyncTransforms();
                for (int i = 0; i < 5 && !worldHit; i++)
                    InvokePrivateMethod(arrow, "FixedUpdate");

                Assert.IsTrue(worldHit, "Precondition: arrow must impact wall into WorldImpact phase.");
                Quaternion initialRot = arrow.transform.rotation;

                arrow.Tick(0.05f);

                Assert.AreEqual(0.05f, GetPrivateField<float>(arrow, "_impactTime"), 0.0001f,
                    "Quiver must advance _impactTime using the deltaTime passed to Tick(dt).");
                Assert.AreNotEqual(initialRot, arrow.transform.rotation,
                    "Quiver must rotate the stuck arrow on Tick(dt).");
                float expectedAngle = Mathf.Sin(0.05f * 55f) * 7f * (1f - 0.05f / 0.3f);
                Assert.AreEqual(expectedAngle, Mathf.DeltaAngle(0f, arrow.transform.eulerAngles.z), 0.01f,
                    "Arrow quiver angle must match elapsed deltaTime.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void T09_EndRoll_RestoresInitialBodyExcludeLayersInsteadOfZeroing()
        {
            var root = new GameObject("T09_Root");
            try
            {
                var harness = CreatePlayerHarness(root.transform, Vector2.zero);
                LayerMask customInitialMask = 1 << GroundLayer;
                harness.Body.excludeLayers = customInitialMask;

                var ctx = new PlayerContext(
                    harness.Body,
                    harness.PlayerGo.GetComponent<BoxCollider2D>(),
                    harness.PlayerGo.GetComponentInChildren<SpriteRenderer>(),
                    (LayerMask)((1 << GroundLayer) | (1 << OneWayLayer)),
                    (LayerMask)(1 << EnemyLayer),
                    ClimbProbeMask,
                    AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(MovementConfigPath),
                    harness.Input,
                    harness.PlayerGo.GetComponent<PlayerAnimatorBridge>());

                ctx.BeginRoll();
                Assert.AreEqual(((1 << GroundLayer) | (1 << EnemyLayer)), harness.Body.excludeLayers.value);
                ctx.EndRoll();
                Assert.AreEqual(customInitialMask.value, harness.Body.excludeLayers.value,
                    "EndRoll must restore the Rigidbody2D's initial excludeLayers rather than hardcoding 0.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void T10_ScrollPickupPresentation_DestroyingGameObjectMidSequenceDestroysSnapshotTexture()
        {
            var go = new GameObject("T10_ScrollPresentation");
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            try
            {
                var pres = go.AddComponent<DeadCells.HUD.ScrollPickupPresentation>();
                SetPrivateField(pres, "_snapshot", tex);
                InvokePrivateMethod(pres, "OnDestroy");
                Object.DestroyImmediate(go);
                Assert.IsTrue(tex == null, "OnDestroy must destroy an active _snapshot Texture2D when the GameObject is torn down.");
            }
            finally
            {
                if (tex != null)
                    Object.DestroyImmediate(tex);
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void T11_ParallaxLayer2D_FrustumEdgePinningKeepsInteriorClearAcrossOrthoSizes()
        {
            var root = new GameObject("T11_Root");
            var tex = new Texture2D(63, 121, TextureFormat.RGBA32, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, 63, 121), new Vector2(0.5f, 0.5f), 24f);
            try
            {
                var camGo = new GameObject("Cam");
                camGo.transform.SetParent(root.transform, false);
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.aspect = 16f / 9f;

                var fgGo = new GameObject("Foreground_Silhouettes");
                fgGo.transform.SetParent(root.transform, false);
                var layer = fgGo.AddComponent<DeadCells.CameraSystem.ParallaxLayer2D>();
                float[] restXs = { -38f, -12.85f, 12.85f, 38f };
                for (int i = 0; i < restXs.Length; i++)
                {
                    var col = new GameObject($"FgColumn_{i}");
                    col.transform.SetParent(fgGo.transform, false);
                    col.transform.localPosition = new Vector3(restXs[i], 0f, 0f);
                    var seg = new GameObject("Seg_0");
                    seg.transform.SetParent(col.transform, false);
                    seg.AddComponent<SpriteRenderer>().sprite = sp;
                }

                layer.Configure(cam.transform, new Vector2(0f, 1f), 0f, pixelSnap: true, pixelsPerUnit: 24f);
                layer.ConfigureVerticalCameraLock(true, 1.5f);
                layer.ConfigureFrustumEdgePinning(true, 0.46f, 26f);

                foreach (var (cx, ortho) in new[] { (18f, 5.8f), (47f, 10.2f), (-49.5f, 11.2f), (119f, 6.8f) })
                {
                    cam.transform.position = new Vector3(cx, 3f, -10f);
                    cam.orthographicSize = ortho;
                    layer.UpdateParallax(cam.transform.position);

                    float halfW = ortho * cam.aspect;
                    float clearHalfW = halfW - 0.55f;
                    foreach (Transform col in fgGo.transform)
                    {
                        Assert.AreEqual(Vector3.one, col.localScale, "FgColumn localScale must remain 1:1 (no vertical mixel stretch).");
                        var b = col.GetComponentInChildren<SpriteRenderer>().bounds;
                        bool intrudes = b.max.x > cx - clearHalfW && b.min.x < cx + clearHalfW;
                        Assert.IsFalse(intrudes, $"{col.name} ({b.min.x:F2}..{b.max.x:F2}) intrudes [{cx - clearHalfW:F2}..{cx + clearHalfW:F2}] at ortho={ortho}.");
                    }
                }

                layer.ResetFrustumPinnedColumnsToRest();
                for (int i = 0; i < restXs.Length; i++)
                    Assert.AreEqual(restXs[i], fgGo.transform.GetChild(i).localPosition.x, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(sp);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyShield_PlayerBehindWithinBackstepTrigger_RespectsTurnDelayWithoutInstantSnapTurn()
        {
            var root = new GameObject("__Round9ShieldTurnDelayTest");
            GameObject shieldGo = null;
            try
            {
                CreateFlatGround(root.transform, new Vector2(0f, -0.5f));

                var playerGo = new GameObject("Player");
                playerGo.transform.SetParent(root.transform);
                playerGo.layer = PlayerLayer;
                playerGo.transform.position = new Vector3(-0.8f, 0f, 0f);

                var shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPaths[2]);
                Assert.IsNotNull(shieldPrefab, $"Missing {EnemyPrefabPaths[2]}");
                shieldGo = Object.Instantiate(shieldPrefab, Vector3.zero, Quaternion.identity);

                var shieldAnim = shieldGo.GetComponent<EnemyAnimator>();
                var shieldController = shieldGo.GetComponent<EnemyController>();
                InvokePrivateMethod(shieldAnim, "Awake");
                InvokePrivateMethod(shieldController, "Awake");
                InvokePrivateMethod(shieldController, "Start");

                var ctx = GetPrivateField<EnemyContext>(shieldController, "_context");
                var machine = GetPrivateField<EnemyStateMachine>(shieldController, "_machine");
                ctx.Player = playerGo.transform;
                ctx.Face(1);

                Physics2D.SyncTransforms();
                ctx.RefreshFrame();

                Assert.IsTrue(ctx.IsGrounded, "Precondition: Enemy_Shield must be grounded.");
                Assert.AreEqual(0.6f, ctx.Data.TurnDelay, 0.001f, "Precondition: Enemy_Shield TurnDelay must be 0.6s.");
                Assert.Less(ctx.DistanceToPlayer, ctx.Data.BackstepTrigger,
                    "Precondition: player at 0.8u must be inside BackstepTrigger.");
                Assert.IsTrue(ctx.CanSeePlayer(ctx.Data.LoseRange), "Precondition: Enemy_Shield must see player.");
                Assert.IsTrue(ctx.CanWalk(-ctx.DirectionToPlayer), "Precondition: room behind must allow Backstep.");

                machine.ForceChangeState(machine.Chase);
                SetPrivateField(machine, "_lastTransitionFrame", -1);

                var chase = (EnemyChaseState)machine.Chase;
                for (int i = 0; i < 3; i++)
                    chase.Tick(0.05f);

                Assert.AreSame(machine.Chase, machine.Current,
                    "Within TurnDelay (0.15s < 0.6s), Enemy_Shield must stay in Chase instead of snapping into Backstep.");
                Assert.AreEqual(1, ctx.Facing,
                    "Within TurnDelay (0.15s < 0.6s), Enemy_Shield must keep facing +1 (right) instead of snap-turning.");

                // Once TurnDelay completes, Enemy_Shield turns toward player and enters Backstep normally.
                SetPrivateField(machine, "_lastTransitionFrame", -1);
                chase.Tick(0.50f);
                Assert.AreEqual(-1, ctx.Facing, "After TurnDelay elapses, Enemy_Shield must turn toward player (-1).");
                Assert.AreSame(machine.Backstep, machine.Current,
                    "After TurnDelay completes while player is inside BackstepTrigger, Enemy_Shield must transition to Backstep.");
            }
            finally
            {
                if (shieldGo != null)
                    Object.DestroyImmediate(shieldGo);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyContext_RefreshFrame_DetectsGroundAtOuterFivePercentLedgeEdge()
        {
            var root = new GameObject("__Round9EnemyLedgeEdgeGroundTest");
            EnemyData data = null;
            try
            {
                LayerMask groundMask = (1 << GroundLayer) | (1 << OneWayLayer);

                var ledgeGo = new GameObject("LedgePlatform");
                ledgeGo.transform.SetParent(root.transform);
                ledgeGo.layer = GroundLayer;
                ledgeGo.transform.position = new Vector3(-5f, -0.5f, 0f);
                ledgeGo.AddComponent<BoxCollider2D>().size = new Vector2(10f, 1f);

                var harness = CreateEnemyHarness(root.transform, Vector2.zero, groundMask, out data);
                float width = harness.Collider.size.x; // 0.8u
                float fourPercentOverlap = width * 0.04f;
                float edgeX = width * 0.5f - fourPercentOverlap; // b.min.x = -0.032f over platform ending at x = 0

                harness.Go.transform.position = new Vector3(edgeX, 0f, 0f);
                harness.Body.position = new Vector2(edgeX, 0f);
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();

                Assert.IsTrue(harness.Context.IsGrounded,
                    "Enemy resting on outer 4% corner of a platform edge must still be detected as grounded.");

                // Negative control: clearly past the ledge edge is airborne.
                float offLedgeX = width * 0.5f + 0.05f;
                harness.Go.transform.position = new Vector3(offLedgeX, 0f, 0f);
                harness.Body.position = new Vector2(offLedgeX, 0f);
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();

                Assert.IsFalse(harness.Context.IsGrounded,
                    "Enemy positioned completely past the ledge edge must report airborne.");

                // Rising through a OneWayPlatform must not report IsGrounded == true on frame 1.
                ledgeGo.SetActive(false);
                var oneWayGo = new GameObject("OneWayPlatform");
                oneWayGo.transform.SetParent(root.transform);
                oneWayGo.layer = OneWayLayer;
                oneWayGo.transform.position = new Vector3(0f, -0.125f, 0f);
                oneWayGo.AddComponent<BoxCollider2D>().size = new Vector2(6f, 0.25f);

                harness.Go.transform.position = Vector3.zero;
                harness.Body.position = Vector2.zero;
                harness.Body.linearVelocity = new Vector2(0f, 3f);
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();

                Assert.IsFalse(harness.Context.IsGrounded,
                    "Rising (vy > 0.05) through a OneWayPlatform must not falsely report IsGrounded == true.");

                harness.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();

                Assert.IsTrue(harness.Context.IsGrounded,
                    "Resting on a OneWayPlatform (vy <= 0.05) must report IsGrounded == true.");
            }
            finally
            {
                if (data != null)
                    Object.DestroyImmediate(data);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnemyBackstepAndLunge_ExceedingMaxGroundedWait_ExitSafelyWithoutDeadlock()
        {
            var root = new GameObject("__Round9EnemyMaxGroundedWaitTest");
            EnemyData data = null;
            try
            {
                LayerMask groundMask = (1 << GroundLayer) | (1 << OneWayLayer);
                var skills = new[]
                {
                    new EnemyData.Skill
                    {
                        Kind = EnemyAttackKind.Lunge,
                        ChargeAnim = "AtkLoad",
                        ExecuteAnim = "Atk",
                        Charge = 0f,
                        TravelCells = 4f,
                        TravelDuration = 0.4f,
                        LungeUpSpeed = 5f,
                        Recover = 0.2f
                    }
                };
                var harness = CreateEnemyHarness(root.transform, new Vector2(0f, 5f), groundMask, out data, skills);
                SetPrivateField(data, "_backstepDuration", 0.25f);

                var machine = new EnemyStateMachine(harness.Context);
                Physics2D.SyncTransforms();
                harness.Context.RefreshFrame();
                Assert.IsFalse(harness.Context.IsGrounded, "Precondition: enemy in mid-air must start ungrounded.");

                // 1. Backstep exits cleanly once BackstepDuration + MaxGroundedWait (1.0s) is exceeded.
                machine.ForceChangeState(machine.Backstep);
                SetPrivateField(machine, "_lastTransitionFrame", -1);
                var backstep = (EnemyBackstepState)machine.Backstep;

                backstep.Tick(0.25f);
                Assert.AreSame(machine.Backstep, machine.Current,
                    "Airborne Backstep should wait for ground right after BackstepDuration.");

                backstep.Tick(0.90f);
                Assert.AreSame(machine.Backstep, machine.Current,
                    "Airborne Backstep must still be waiting before BackstepDuration + 1.0s.");

                backstep.Tick(0.15f);
                Assert.AreSame(machine.Chase, machine.Current,
                    "Airborne Backstep exceeding MaxGroundedWait (1.0s) must exit to Chase without deadlock.");

                // 2. Lunge exits cleanly through Recover -> Chase once TravelDuration + MaxGroundedWait (1.0s) is exceeded.
                var lunge = (EnemyLungeAttackState)machine.Skills[0];
                machine.ForceChangeState(lunge);
                SetPrivateField(machine, "_lastTransitionFrame", -1);

                lunge.Tick(0.01f); // Charge (0f) -> Execute
                lunge.Tick(0.40f); // TravelDuration elapsed, still airborne
                Assert.AreSame(lunge, machine.Current,
                    "Airborne Lunge should wait for ground right after TravelDuration.");

                lunge.Tick(1.05f); // Exceeds TravelDuration + MaxGroundedWait (1.0s) -> enters Recover
                lunge.Tick(0.25f); // Exceeds Recover (0.2f) -> exits to Chase
                Assert.AreSame(machine.Chase, machine.Current,
                    "Airborne Lunge exceeding MaxGroundedWait (1.0s) must exit Recover -> Chase without deadlock.");
            }
            finally
            {
                if (data != null)
                    Object.DestroyImmediate(data);
                Object.DestroyImmediate(root);
            }
        }

        // ==================== Fixtures & helpers ====================

        /// <summary>A hittable dummy on the Enemy layer with a live Health (0s invulnerability).</summary>
        private static Health CreateRound9EnemyTarget(Transform parent, string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.layer = EnemyLayer;
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider2D>();
            col.offset = new Vector2(0f, 0.75f);
            col.size = new Vector2(0.8f, 1.5f);

            var health = go.AddComponent<Health>();
            health.Initialize(100, 0f);

            var hurtbox = go.AddComponent<Hurtbox>();
            SetPrivateField(hurtbox, "_health", health);
            return health;
        }

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
        /// A climbable trigger on layer 10 at an explicit geometry. Every fixture that needs a different
        /// overhang goes through here, so the trigger shape lives in one place instead of a second copy
        /// of the constants drifting away from the first.
        /// </summary>
        private static BoxCollider2D CreateChainAt(Transform parent, string name, float centerY, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.layer = ClimbableLayer;
            go.transform.position = new Vector3(ChainX, centerY, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, height);
            return col;
        }

        /// <summary>
        /// Chain trigger in the shape Task 2.2 ships: y = [-0.20 .. 8.08] on x = 46, overhanging the
        /// 8.00 deck it serves by 0.08u, so a top dismount lands inside the feet probe.
        /// </summary>
        private static BoxCollider2D CreateChain(Transform parent) =>
            CreateChainAt(parent, "I1_Chain_C", ChainTriggerCenterY, ChainTriggerHeight);

        /// <summary>
        /// Pre-trim chain trigger: y = [-0.50 .. 8.50] on x = 46. Overhangs the same 8.00 deck by
        /// 0.50u, which is what puts a top dismount 0.44u into the air with no footing to find.
        /// </summary>
        private static BoxCollider2D CreateOverhangChain(Transform parent) =>
            CreateChainAt(parent, "I1_Chain_C_Overhang", OverhangChainCenterY, OverhangChainHeight);

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

        private readonly struct EnemyStunHarness
        {
            public readonly GameObject Go;
            public readonly Rigidbody2D Body;
            public readonly EnemyContext Context;
            public readonly EnemyStunState Stun;
            public readonly EnemyData Data;

            public EnemyStunHarness(GameObject go, Rigidbody2D body, EnemyContext context, EnemyStunState stun,
                EnemyData data)
            {
                Go = go;
                Body = body;
                Context = context;
                Stun = stun;
                Data = data;
            }
        }

        /// <summary>
        /// A real <see cref="EnemyContext"/> + <see cref="EnemyStateMachine"/> on a dynamic body with the
        /// zombie animator, so <c>EnemyStunState</c> runs exactly the code the prefabs run. Gravity defaults
        /// to 0 because the velocity assertions care what the state writes, not the arc the body then flies;
        /// pass <see cref="PrefabGravityScale"/> to measure that arc.
        /// </summary>
        private static EnemyStunHarness CreateEnemyStunHarness(Transform parent, Vector2 position,
            float gravityScale = 0f)
        {
            var harness = CreateEnemyHarness(parent, position, (LayerMask)(1 << GroundLayer), out var data,
                gravityScale: gravityScale);
            var stun = (EnemyStunState)new EnemyStateMachine(harness.Context).Stun;
            return new EnemyStunHarness(harness.Go, harness.Body, harness.Context, stun, data);
        }

        private readonly struct EnemyHarness
        {
            public readonly GameObject Go;
            public readonly Rigidbody2D Body;
            public readonly BoxCollider2D Collider;
            public readonly EnemyContext Context;

            public EnemyHarness(GameObject go, Rigidbody2D body, BoxCollider2D collider, EnemyContext context)
            {
                Go = go;
                Body = body;
                Collider = collider;
                Context = context;
            }
        }

        private static EnemyHarness CreateEnemyHarness(Transform parent, Vector2 position, LayerMask groundMask,
            out EnemyData data, EnemyData.Skill[] skills = null, float gravityScale = 0f)
        {
            var go = new GameObject("EnemyHarness");
            go.transform.SetParent(parent);
            go.layer = EnemyLayer;
            go.transform.position = position;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = gravityScale;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.position = position;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.5f);
            col.offset = new Vector2(0f, 0.75f);

            var sprite = go.AddComponent<SpriteRenderer>();
            var animator = go.AddComponent<Animator>();
            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(EnemyAnimatorPath);
            Assert.IsNotNull(animController, $"Missing {EnemyAnimatorPath}");
            animator.runtimeAnimatorController = animController;
            animator.Rebind();
            animator.Update(0f);

            var enemyAnimator = go.AddComponent<EnemyAnimator>();
            SetPrivateField(enemyAnimator, "_animator", animator);
            SetPrivateField(enemyAnimator, "_renderer", sprite);
            InvokePrivateMethod(enemyAnimator, "Awake");

            var health = go.AddComponent<Health>();
            health.RestoreFull();

            data = ScriptableObject.CreateInstance<EnemyData>();
            if (skills != null)
                SetPrivateField(data, "_skills", skills);

            var context = new EnemyContext(body, col, enemyAnimator, health, data, groundMask, (LayerMask)(1 << PlayerLayer));
            return new EnemyHarness(go, body, col, context);
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

        /// <summary>
        /// EditMode never runs a physics step of its own, and a synchronous test body does not advance
        /// <c>Time.frameCount</c> either. These two helpers model exactly what the engine would do
        /// between two <c>Update</c> calls: integrate the body under the gravity scale the current
        /// state set, then open a fresh transition slot for the next frame. Without them a multi-frame
        /// climb / fall hand-off cannot be observed at all.
        /// </summary>
        private static void AdvanceBody(PlayerHarness harness, float deltaTime)
        {
            var body = harness.Body;
            var velocity = body.linearVelocity;
            velocity.y += Physics2D.gravity.y * body.gravityScale * deltaTime;
            body.linearVelocity = velocity;
            body.position += velocity * deltaTime;
            Physics2D.SyncTransforms();
        }

        /// <summary>
        /// Peak height a launched body reaches above where it started, stepping the body the way the
        /// engine would between two fixed frames: gravity into velocity first, then position — semi-implicit
        /// Euler, the same integration <see cref="AdvanceBody"/> models. Stops when the body stops rising and
        /// never runs past <paramref name="maxSteps"/>, so a pop that never lands cannot hang the suite.
        /// </summary>
        private static float MeasurePopApex(EnemyStunHarness harness, int maxSteps = 60)
        {
            Rigidbody2D body = harness.Body;
            float startY = body.position.y;
            float peak = 0f;
            for (int step = 0; step < maxSteps; step++)
            {
                Vector2 velocity = body.linearVelocity;
                velocity.y += Physics2D.gravity.y * body.gravityScale * Time.fixedDeltaTime;
                body.linearVelocity = velocity;
                body.position += velocity * Time.fixedDeltaTime;
                Physics2D.SyncTransforms();

                float rise = body.position.y - startY;
                if (rise > peak)
                    peak = rise;
                if (velocity.y <= 0f)
                    break;
            }

            return peak;
        }

        /// <summary>
        /// The measured apex must match the analytic arc <c>vy0² / (2g)</c> inside the band a real
        /// fixed step can actually reach.
        /// </summary>
        /// <remarks>
        /// Semi-implicit Euler loses roughly <c>vy0 · dt / 2</c> against the continuous apex — 31% of
        /// 0.0705 u at 2.2 u/s and 18% of 0.2103 u at 3.8 u/s at dt = 0.02. The floor is 60% of
        /// analytic: slack for that deficit, still tight enough that a zero or truncated pop fails.
        /// The ceiling is the continuous apex itself, which no integrator can beat.
        /// </remarks>
        private static void AssertPopApex(float measured, float launchSpeed, float gravity, string label)
        {
            float predicted = launchSpeed * launchSpeed / (2f * gravity);
            Assert.Greater(measured, predicted * 0.6f,
                $"The {label} pop must actually rise: analytic apex {predicted:F4} u, measured {measured:F4} u.");
            Assert.LessOrEqual(measured, predicted * 1.001f,
                $"The {label} pop cannot exceed the analytic apex {predicted:F4} u; measured {measured:F4} u.");
        }

        /// <summary>Opens the one-transition-per-frame slot that a real frame boundary provides.</summary>
        private static void BeginSimulatedFrame(PlayerStateMachine machine) =>
            SetPrivateField(machine, "_lastTransitionFrame", -1);

        private static void ForceState(PlayerStateMachine machine, IPlayerState state)
        {
            machine.ForceChangeState(state);
            SetPrivateField(machine, "_lastTransitionFrame", -1);
        }

        private static FieldInfo FindPrivateField(System.Type type, string fieldName)
        {
            for (var cursor = type; cursor != null; cursor = cursor.BaseType)
            {
                var field = cursor.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }
            return null;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = FindPrivateField(target.GetType(), fieldName);
            Assert.IsNotNull(field, $"Missing private field {target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = FindPrivateField(target.GetType(), fieldName);
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
