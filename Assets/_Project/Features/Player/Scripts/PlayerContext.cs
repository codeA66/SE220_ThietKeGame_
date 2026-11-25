using System;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Data and helpers shared by every player state. States read and act through this object
    /// instead of touching each other.
    /// </summary>
    public class PlayerContext
    {
        private const float FeetProbeHeight = 0.05f;
        private const float FeetProbeWidthRatio = 0.96f;
        // ~51.7° of climbable slope. A 45° ramp presents cos(45°) = 0.7071, so this keeps a wide
        // margin instead of the 0.007f the old 0.7f limit left.
        private const float MinGroundNormalY = 0.62f;
        // Flat floors and one-way decks: a rising body has left them (jumps, deck tops).
        private const float FlatGroundNormalY = 0.98f;
        private const float MaxGroundedRiseSpeed = 0.01f;
        // Solid slopes: what matters is the rise measured along the normal, so walking up a 45° ramp
        // (v·n ~ 0) stays grounded while a jump off it (v·n > 4) does not.
        private const float MaxGroundedSlopeRiseSpeed = 0.15f;
        // Keeping the feet probe glued to a slope for one frame after leaving it, so a 45° ramp does
        // not flicker between grounded and airborne while the box slides down the surface.
        private const float SlopeProbeDistance = 0.22f;
        private const float MinSlopeMoveSpeed = 0.01f;
        // Upward grab is refused once the feet are this close to the chain top, because the trigger
        // overhangs the deck it serves and the grab would hang the player in mid air.
        private const float ClimbTopGrabMargin = 0.22f;
        private const float DescendProbeOffsetY = 0.30f;
        private const float DescendProbeSize = 0.55f;
        private const float DescendStartInsetY = 0.30f;
        private const float LedgeTopProbeInset = 0.1f;
        private const float LedgeStandInset = 0.05f;
        private const float OneWaySideProbeInset = 0.01f;

        private readonly Rigidbody2D _body;
        private readonly BoxCollider2D _collider;
        private readonly SpriteRenderer _sprite;
        private readonly LayerMask _groundMask;
        private readonly LayerMask _solidWallMask;
        private readonly LayerMask _rollPassThroughMask;
        private readonly LayerMask _climbableMask;
        private readonly Vector2 _standingColliderSize;
        private readonly Vector2 _standingColliderOffset;
        private readonly LayerMask _defaultExcludeLayers;

        private float _lastGroundedTime = float.NegativeInfinity;
        private float _lastJumpPressedTime = float.NegativeInfinity;
        private float _rollReadyTime = float.NegativeInfinity;
        private float _comboCarryUntil = float.NegativeInfinity;
        private int _comboCarryIndex;
        private Collider2D _groundCollider;
        private Collider2D _droppedPlatform;
        private Collider2D _dismountedClimbable;
        private Collider2D _ledgeIgnoredPlatform;
        private float _dropRestoreTime;
        private float _lastAirborneVelocityY;
        private float _perfectDodgeUntil = float.NegativeInfinity;
        private float _wallJumpLockUntil = float.NegativeInfinity;
        private float _wallRegrabReadyAt = float.NegativeInfinity;
        private bool _wasOnSlope;

        public PlayerContext(
            Rigidbody2D body,
            BoxCollider2D collider,
            SpriteRenderer sprite,
            LayerMask groundMask,
            LayerMask rollPassThroughMask,
            LayerMask climbableMask,
            PlayerMovementConfig config,
            PlayerInputReader input,
            PlayerAnimatorBridge animator)
        {
            _body = body;
            _collider = collider;
            _sprite = sprite;
            _groundMask = groundMask;
            int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
            int oneWayBit = oneWayLayer >= 0 ? (1 << oneWayLayer) : (1 << 9);
            _solidWallMask = _groundMask.value & ~oneWayBit;
            _rollPassThroughMask = rollPassThroughMask;
            _climbableMask = climbableMask;
            _standingColliderSize = collider.size;
            _standingColliderOffset = collider.offset;
            _defaultExcludeLayers = body.excludeLayers;
            Config = config;
            Input = input;
            Animator = animator;
            RefillAirMoves();
        }

        /// <summary>Ground jump started. Argument: feet position.</summary>
        public event Action<Vector2> Jumped;
        /// <summary>Air jump started. Argument: feet position.</summary>
        public event Action<Vector2> AirJumped;
        /// <summary>Wall jump started. Arguments: feet position, kick-off direction.</summary>
        public event Action<Vector2, int> WallJumped;
        /// <summary>Landed. Arguments: feet position, downward speed just before impact (positive).</summary>
        public event Action<Vector2, float> Landed;
        /// <summary>Stomp impact landed. Argument: feet position.</summary>
        public event Action<Vector2> StompLanded;
        /// <summary>A roll started. Argument: feet position where the perfect-dodge ghost stays.</summary>
        public event Action<Vector2> RollStarted;
        /// <summary>A hit landed on the perfect-dodge ghost.</summary>
        public event Action PerfectDodged;
        /// <summary>An air stomp dive started.</summary>
        public event Action StompStarted;
        /// <summary>An air stomp hit the ground. Argument: feet position.</summary>
        public event Action<Vector2> Stomped;
        /// <summary>The stomp state ended, whether it landed or was cut short (a hit, death, anything else).</summary>
        public event Action StompEnded;
        /// <summary>A ledge climb finished and the player is standing on the ledge. Argument: feet position.</summary>
        public event Action<Vector2> LedgeClimbFinished;

        /// <summary>The spawn intro finished and the player is standing up, about to take control.</summary>
        public event Action Awakened;
        /// <summary>The player lost control to a stun. Argument: how long it lasts (seconds).</summary>
        public event Action<float> Stunned;

        public SpriteRenderer Sprite => _sprite;
        public LayerMask GroundMask => _groundMask;

        public void NotifyAwakened() => Awakened?.Invoke();

        public void NotifyStunned(float duration) => Stunned?.Invoke(duration);

        public void NotifyStompStarted() => StompStarted?.Invoke();

        public void NotifyStomped() => Stomped?.Invoke(Position);

        public void NotifyStompEnded() => StompEnded?.Invoke();

        public void NotifyLedgeClimbFinished() => LedgeClimbFinished?.Invoke(Position);

        public PlayerMovementConfig Config { get; }
        public PlayerCombat Combat { get; set; }
        public PlayerBow Bow { get; set; }
        public PlayerWeaponLoadout Weapons { get; set; }
        public PlayerInputReader Input { get; }
        public PlayerAnimatorBridge Animator { get; }

        public bool IsGrounded { get; private set; }
        /// <summary>Surface normal of the ground under the feet while grounded; up otherwise.</summary>
        public Vector2 GroundNormal { get; private set; } = Vector2.up;
        /// <summary>Grounded on a surface that is neither flat floor nor un-climbable steepness.</summary>
        public bool IsOnSlope =>
            IsGrounded && GroundNormal.y < FlatGroundNormalY && GroundNormal.y >= MinGroundNormalY;
        public int FacingDirection { get; private set; } = 1;
        public bool IsInvulnerable { get; set; }
        /// <summary>Set by PlayerSpawnHead when the crawling head reaches the body during the spawn intro.</summary>
        public bool SpawnHeadArrived { get; set; }
        public int PendingComboIndex { get; set; }
        public Vector2 LedgeStandPoint { get; set; }
        public int AirJumpsRemaining { get; private set; }
        public int AirRollsRemaining { get; private set; }
        public int WallJumpsRemaining { get; private set; }
        public int WallDirection { get; private set; }
        public bool IsWallJumpHorizontalLocked => Time.time < _wallJumpLockUntil;

        public Vector2 Velocity => _body.linearVelocity;
        public Vector2 Position => _body.position;

        public bool JumpBuffered => Time.time - _lastJumpPressedTime <= Config.JumpBufferTime;
        public bool WithinCoyoteTime => Time.time - _lastGroundedTime <= Config.CoyoteTime;
        public bool HasMoveInput => Mathf.Abs(Input.MoveX) > 0.01f;
        public bool IsDownHeld => Input.MoveY < -Config.DownInputThreshold;
        public bool IsCrouchHeld => IsDownHeld || Input.CrouchHeld;
        public bool IsUpHeld => Input.MoveY > Config.DownInputThreshold;
        public Collider2D ClimbTarget { get; private set; }
        /// <summary>Collision with the platform under the feet is currently suspended (drop-through or chain descend).</summary>
        public bool IsDroppingThrough => _droppedPlatform != null;
        public Vector2 PerfectDodgeGhostPosition { get; private set; }
        public bool IsPerfectDodgeWindowOpen => Time.time <= _perfectDodgeUntil;

        public bool CanAirJump =>
            Config.AirJumpEnabled && AirJumpsRemaining > 0 && Input.JumpPressed && !IsDownHeld && !WithinCoyoteTime;

        public bool CanRoll =>
            Time.time >= _rollReadyTime &&
            (IsGrounded || (Config.AllowAirRoll && AirRollsRemaining > 0));

        /// <summary>
        /// Called once per frame before the state machine ticks.
        /// </summary>
        public void RefreshFrame()
        {
            if (_droppedPlatform != null && Time.time >= _dropRestoreTime)
            {
                Physics2D.IgnoreCollision(_collider, _droppedPlatform, false);
                _droppedPlatform = null;
            }

            // Thin strip under the feet; only up-facing surfaces count, so touching a wall is not "ground".
            // Rising through a one-way platform is not "ground" either.
            var bounds = _collider.bounds;
            // A chain the player just stepped off keeps its grab blocked only while the body is still
            // inside it; falling or walking clear of the trigger retires the guard.
            if (_dismountedClimbable != null && !_dismountedClimbable.bounds.Intersects(bounds))
                _dismountedClimbable = null;
            if (_ledgeIgnoredPlatform != null)
            {
                var platformBounds = _ledgeIgnoredPlatform.bounds;
                if (bounds.max.y < platformBounds.min.y || bounds.max.x < platformBounds.min.x || bounds.min.x > platformBounds.max.x)
                    _ledgeIgnoredPlatform = null;
            }
            var hit = CastFeet(bounds.center.x, bounds.min.y);

            bool grounded = hit.collider != null
                            && hit.collider != _droppedPlatform
                            && hit.normal.y > MinGroundNormalY;
            if (grounded)
            {
                // Flat floors, deck tops and everything one-way keep the plain "not rising" test so a
                // jump always leaves them. Solid slopes test the rise along the normal instead, which
                // is what lets the player walk up a 45° ramp without being declared airborne.
                grounded = IsOneWayPlatform(hit.collider) || hit.normal.y >= FlatGroundNormalY
                    ? _body.linearVelocity.y <= MaxGroundedRiseSpeed
                    : Vector2.Dot(_body.linearVelocity, hit.normal) <= MaxGroundedSlopeRiseSpeed;
            }
            IsGrounded = grounded;
            _groundCollider = IsGrounded ? hit.collider : null;
            GroundNormal = IsGrounded ? hit.normal : Vector2.up;
            _wasOnSlope = IsOnSlope;

            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
                RefillAirMoves();
            }
            else
            {
                _lastAirborneVelocityY = _body.linearVelocity.y;
            }
            if (Input.JumpPressed)
                _lastJumpPressedTime = Time.time;
        }

        /// <summary>
        /// The feet probe: a thin up-facing box just under a pair of feet. Shared by
        /// <see cref="RefreshFrame"/> and the climb top-dismount footing test, so "is there floor
        /// under these feet" is answered by exactly one query shape.
        /// </summary>
        private RaycastHit2D CastFeet(float centerX, float feetY)
        {
            var size = new Vector2(_collider.bounds.size.x * FeetProbeWidthRatio, FeetProbeHeight);
            var origin = new Vector2(centerX, feetY + FeetProbeHeight * 0.5f);
            // On a slope the box only has to re-acquire the surface it is already standing on.
            float probeDist = _wasOnSlope
                ? Mathf.Max(Config.GroundCheckDistance, SlopeProbeDistance)
                : Config.GroundCheckDistance;
            return Physics2D.BoxCast(origin, size, 0f, Vector2.down, probeDist, _groundMask);
        }

        /// <summary>
        /// The floor under a pair of feet, if there is one: the same <see cref="CastFeet"/> query
        /// <see cref="HasFootingAt"/> answers with, but reporting where that surface actually is. Used
        /// by the climb top dismount, which has to put the feet on the deck it found instead of on a
        /// fixed offset below the climbable trigger (a trigger overhangs its deck by an arbitrary
        /// amount, so the trigger top is not a height the player can stand at).
        /// </summary>
        public bool TryGetFootingAt(Vector2 worldBodyPosition, out float surfaceY)
        {
            var bounds = _collider.bounds;
            float feetY = worldBodyPosition.y;
            var hit = CastFeet(bounds.center.x + (worldBodyPosition.x - _body.position.x), feetY);
            bool footing = hit.collider != null
                           && hit.collider != _droppedPlatform
                           && hit.normal.y > MinGroundNormalY;
            // The probe box starts with its bottom face on the requested feet height, so the surface
            // is exactly `hit.distance` below that. `hit.point` is not used: a shape cast reports the
            // cast shape's contact point, which is the probe's own bottom face, not the surface under
            // the body.
            surfaceY = footing ? feetY - hit.distance : feetY;
            return footing;
        }

        /// <summary>
        /// Would the player find floor with their feet at this body position? Used by the climb top
        /// dismount before handing the player off. Unlike <see cref="RefreshFrame"/> this ignores the
        /// rising-velocity guard: the dismount zeroes the velocity right after, so only the presence
        /// and the slope of the surface matter here.
        /// </summary>
        public bool HasFootingAt(Vector2 worldBodyPosition) => TryGetFootingAt(worldBodyPosition, out _);

        // ---------- Velocity / body ----------

        public void SetHorizontalVelocity(float x) => _body.linearVelocity = new Vector2(x, _body.linearVelocity.y);

        public void SetVerticalVelocity(float y) => _body.linearVelocity = new Vector2(_body.linearVelocity.x, y);

        public void AddVerticalVelocity(float dy) => SetVerticalVelocity(_body.linearVelocity.y + dy);

        public void SetGravityScale(float scale) => _body.gravityScale = scale;

        /// <summary>
        /// Ground locomotion. Flat floors move horizontally as before; a slope moves along its tangent
        /// at the same speed (so the player climbs and descends stairs smoothly) and stands perfectly
        /// still when idle, with gravity switched off so the player does not creep downhill.
        /// </summary>
        public void ApplyGroundedMove(float speedX)
        {
            if (IsOnSlope)
            {
                if (Mathf.Abs(speedX) < MinSlopeMoveSpeed)
                {
                    _body.gravityScale = 0f;
                    _body.linearVelocity = Vector2.zero;
                    return;
                }

                _body.gravityScale = Config.GravityScale;
                var tangent = new Vector2(GroundNormal.y, -GroundNormal.x).normalized;
                _body.linearVelocity = tangent * speedX;
                return;
            }

            _body.gravityScale = Config.GravityScale;
            SetHorizontalVelocity(speedX);
        }

        public void ClampFallSpeed()
        {
            if (_body.linearVelocity.y < -Config.MaxFallSpeed)
                SetVerticalVelocity(-Config.MaxFallSpeed);
        }

        public void SetKinematic(bool kinematic)
        {
            _body.linearVelocity = Vector2.zero;
            _body.bodyType = kinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        }

        public void MovePosition(Vector2 position) => _body.MovePosition(position);

        // ---------- Jumps ----------

        public void StartJump()
        {
            SetVerticalVelocity(Config.JumpBaseVelocity);
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
            Jumped?.Invoke(Position);
        }

        public void StartAirJump()
        {
            AirJumpsRemaining--;
            SetVerticalVelocity(Config.AirJumpVelocity);
            _lastJumpPressedTime = float.NegativeInfinity;
            AirJumped?.Invoke(Position);
        }

        /// <summary>
        /// Called by states when the player touches down after being airborne.
        /// </summary>
        public void NotifyLanded()
        {
            Landed?.Invoke(Position, Mathf.Max(0f, -_lastAirborneVelocityY));
            _lastAirborneVelocityY = 0f;
        }

        public void NotifyStompLanded() => StompLanded?.Invoke(Position);

        public void RefillAirMoves()
        {
            AirJumpsRemaining = Config.MaxAirJumps;
            AirRollsRemaining = Config.MaxAirRolls;
            WallJumpsRemaining = Config.MaxWallJumpsPerAir;
        }

        // ---------- Roll ----------

        /// <summary>
        /// Begins a roll: spends an air roll if airborne and stops collisions with enemies.
        /// </summary>
        public void BeginRoll()
        {
            if (!IsGrounded)
                AirRollsRemaining--;
            IsInvulnerable = true;
            _body.excludeLayers = _defaultExcludeLayers | _rollPassThroughMask;
            PerfectDodgeGhostPosition = Position;
            _perfectDodgeUntil = Time.time + Config.PerfectDodgeDuration;
            RollStarted?.Invoke(PerfectDodgeGhostPosition);
        }

        /// <summary>
        /// Called by an attack that hits the perfect-dodge ghost (enemy attacks, not implemented yet).
        /// Returns true if the hit counts as a perfect dodge.
        /// </summary>
        public bool TryRegisterPerfectDodge()
        {
            if (!IsPerfectDodgeWindowOpen)
                return false;
            _perfectDodgeUntil = float.NegativeInfinity;
            PerfectDodged?.Invoke();
            return true;
        }

        public void EndRoll()
        {
            IsInvulnerable = false;
            _body.excludeLayers = _defaultExcludeLayers;
            _rollReadyTime = Time.time + Config.RollCooldown;
        }

        // ---------- Combo carry over a roll ----------

        /// <summary>
        /// Remembers the next combo hit when a swing is interrupted by a roll.
        /// </summary>
        public void StoreComboCarry(int nextIndex)
        {
            _comboCarryIndex = nextIndex;
            _comboCarryUntil = float.PositiveInfinity; // open until the roll ends
        }

        public void OpenComboCarryWindow()
        {
            if (_comboCarryIndex > 0)
                _comboCarryUntil = Time.time + Config.ComboCarryWindow;
        }

        /// <summary>
        /// Combo hit to start with: the carried hit if still valid, otherwise 1. Consumes the carry.
        /// </summary>
        public int TakeComboStartIndex()
        {
            int index = _comboCarryIndex > 0 && Time.time <= _comboCarryUntil ? _comboCarryIndex : 1;
            _comboCarryIndex = 0;
            _comboCarryUntil = float.NegativeInfinity;
            return index;
        }

        public void ClearComboCarry()
        {
            _comboCarryIndex = 0;
            _comboCarryUntil = float.NegativeInfinity;
        }

        // ---------- One-way platforms ----------

        /// <summary>
        /// Down + Jump on a one-way platform: ignore it briefly so the player falls through.
        /// </summary>
        public bool TryDropThrough()
        {
            if (!IsDownHeld || !Input.JumpPressed || _groundCollider == null)
                return false;
            if (!_groundCollider.TryGetComponent<PlatformEffector2D>(out _))
                return false;

            _droppedPlatform = _groundCollider;
            _ledgeIgnoredPlatform = _groundCollider;
            _dropRestoreTime = Time.time + Config.DropThroughDuration;
            Physics2D.IgnoreCollision(_collider, _droppedPlatform, true);
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
            IsGrounded = false;
            return true;
        }

        // ---------- Chains / vines ----------

        /// <summary>
        /// Finds a climbable (trigger on the Climbable layer) overlapping the player's body and
        /// remembers it as ClimbTarget.
        /// </summary>
        public bool TryFindClimbable()
        {
            var bounds = _collider.bounds;
            var climbable = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, _climbableMask);
            if (climbable == null)
                return false;
            ClimbTarget = climbable;
            return true;
        }

        public void BeginClimb()
        {
            SetGravityScale(0f);
            _body.linearVelocity = Vector2.zero;
            AlignToClimbTarget();
            RefillAirMoves();
        }

        public void EndClimb()
        {
            ClimbTarget = null;
            SetGravityScale(Config.GravityScale);
        }

        public void AlignToClimbTarget()
        {
            if (ClimbTarget != null)
                _body.position = new Vector2(ClimbTarget.bounds.center.x - FacingDirection * Config.ClimbHandOffsetX, _body.position.y);
        }

        /// <summary>
        /// Upward grab check. The climbable trigger overhangs the deck it serves, so grabbing from
        /// the deck (or from the frame right after a top dismount) would hang the player in mid air.
        /// Rejection clears ClimbTarget so the next frame re-probes cleanly.
        /// </summary>
        public bool CanMountClimbableUpward()
        {
            if (!TryFindClimbable())
                return false;

            // The chain this player just stepped off at its top must not take them straight back.
            if (_dismountedClimbable == ClimbTarget)
            {
                ClimbTarget = null;
                return false;
            }

            if (Position.y < ClimbTarget.bounds.max.y - ClimbTopGrabMargin)
                return true;

            ClimbTarget = null;
            return false;
        }

        /// <summary>
        /// Remembers the chain a top dismount just handed the player off from, for as long as the body
        /// still overlaps it. A trigger that overhangs its deck (every untrimmed chain does) leaves the
        /// dismount point above the floor, and without this the next Up grab would loop
        /// Climb -> Idle -> Fall -> Climb for as long as Up is held.
        /// </summary>
        public void LatchDismountedClimbable(Collider2D climbable) => _dismountedClimbable = climbable;

        /// <summary>
        /// Looks for a chain directly under a one-way deck slot the player is standing on, which is
        /// what a Down-hold descend needs.
        /// </summary>
        public bool TryFindClimbableBelowDeck(out Collider2D climbable)
        {
            climbable = null;
            if (!IsGrounded || _groundCollider == null || !IsOneWayPlatform(_groundCollider))
                return false;

            var bounds = _collider.bounds;
            var probe = new Vector2(bounds.center.x, bounds.min.y - DescendProbeOffsetY);
            var size = new Vector2(DescendProbeSize, DescendProbeSize);
            climbable = Physics2D.OverlapBox(probe, size, 0f, _climbableMask);
            return climbable != null;
        }

        /// <summary>
        /// Starts a descent: suspends the deck collision for the drop-through window, slides the body
        /// below the deck top and grips the chain.
        /// </summary>
        public void BeginDescendClimb(Collider2D climbable)
        {
            if (climbable == null)
                return;

            var deck = _groundCollider;
            if (deck != null)
            {
                _droppedPlatform = deck;
                _ledgeIgnoredPlatform = deck;
                _dropRestoreTime = Time.time + Config.DropThroughDuration;
                Physics2D.IgnoreCollision(_collider, deck, true);
            }

            ClimbTarget = climbable;
            _body.position = new Vector2(
                climbable.bounds.center.x - FacingDirection * Config.ClimbHandOffsetX,
                climbable.bounds.max.y - DescendStartInsetY);
            _groundCollider = null;
            IsGrounded = false;
        }

        /// <summary>Teleports the feet to a world point and kills the velocity (flush dismounts).</summary>
        public void SnapPosition(Vector2 worldFeet)
        {
            _body.position = worldFeet;
            _body.linearVelocity = Vector2.zero;
        }

        public bool TryFindWall(out int wallDir)
        {
            wallDir = 0;
            if (!Config.WallClingEnabled || IsGrounded || Time.time < _wallRegrabReadyAt || Velocity.y > 2.0f || IsDownHeld)
                return false;
            int inputDir = Mathf.Abs(Input.MoveX) > 0.1f ? (Input.MoveX > 0f ? 1 : -1) : 0;
            if (inputDir == 0)
                return false;

            var b = _collider.bounds;
            float dist = b.extents.x + Config.WallCheckDistance;
            var dir = new Vector2(inputDir, 0f);
            var lowHit = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.45f), dir, dist, _solidWallMask);
            var highHit = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 1.25f), dir, dist, _solidWallMask);
            if (lowHit.collider == null || highHit.collider == null)
                return false;
            if (Mathf.Abs(lowHit.normal.x) < 0.9f || Mathf.Abs(highHit.normal.x) < 0.9f)
                return false;

            wallDir = inputDir;
            WallDirection = inputDir;
            return true;
        }

        public void StartWallJump(int awayDir)
        {
            SetHorizontalVelocity(awayDir * Config.WallJumpVelocity.x);
            SetVerticalVelocity(Config.WallJumpVelocity.y);
            Face(awayDir);
            WallJumpsRemaining = Mathf.Max(0, WallJumpsRemaining - 1);
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
            _wallJumpLockUntil = Time.time + Config.WallJumpHorizontalLockTime;
            _wallRegrabReadyAt = Time.time + Config.WallRegrabCooldown;
            WallJumped?.Invoke(Position, awayDir);
        }

        // ---------- Crouch ----------

        public void SetCrouchCollider(bool crouching)
        {
            if (!Config.CrouchShrinkCollider)
                return;
            if (crouching)
            {
                float h = Config.CrouchColliderHeight;
                _collider.size = new Vector2(_standingColliderSize.x, h);
                _collider.offset = new Vector2(_standingColliderOffset.x, h * 0.5f);
            }
            else
            {
                _collider.size = _standingColliderSize;
                _collider.offset = _standingColliderOffset;
            }
        }

        // ---------- Ledge ----------

        /// <summary>
        /// Looks for a climbable ledge in the facing direction while the player holds toward it:
        /// Solid ground needs a wall at chest height and free space above it. One-way platforms
        /// require a real side edge near the upper probe while the player is no longer rising.
        /// standPoint is where the feet (transform pivot) should end up.
        /// </summary>
        public bool TryFindLedge(out Vector2 standPoint)
        {
            standPoint = default;
            bool movingToward = (HasMoveInput && Mathf.Sign(Input.MoveX) == FacingDirection)
                || (!HasMoveInput && Velocity.x * FacingDirection > 0.5f);
            if (!Config.LedgeClimbEnabled || !movingToward)
                return false;

            var bounds = _collider.bounds;
            var dir = new Vector2(FacingDirection, 0f);
            float reach = bounds.extents.x + Config.LedgeCheckDistance;
            float feetY = bounds.min.y;

            if (TryFindOneWayLedge(bounds, dir, reach, out standPoint))
                return true;

            var chest = Physics2D.Raycast(new Vector2(bounds.center.x, feetY + Config.LedgeChestHeight), dir, reach, _groundMask);
            if (chest.collider == null || IsOneWayPlatform(chest.collider))
                return false;

            var head = Physics2D.Raycast(new Vector2(bounds.center.x, feetY + Config.LedgeHeadHeight), dir, reach, _groundMask);
            if (head.collider != null)
                return false;

            // Find the ledge top just past the wall face.
            float probeX = chest.point.x + FacingDirection * LedgeTopProbeInset;
            float probeTop = feetY + Config.LedgeHeadHeight;
            var top = Physics2D.Raycast(new Vector2(probeX, probeTop), Vector2.down,
                Config.LedgeHeadHeight - Config.LedgeChestHeight + LedgeTopProbeInset, _groundMask);
            if (top.collider == null || IsOneWayPlatform(top.collider) || top.normal.y < MinGroundNormalY)
                return false;

            return TryGetLedgeStandPoint(bounds, chest.point.x, top.point.y, out standPoint);
        }

        private bool TryFindOneWayLedge(Bounds bounds, Vector2 dir, float reach, out Vector2 standPoint)
        {
            standPoint = default;
            // Let upward jumps pass through naturally; Down + Jump must not grab the platform again.
            if (Velocity.y > MaxGroundedRiseSpeed || IsDownHeld || _droppedPlatform != null)
                return false;

            float tolerance = Config.OneWayLedgeGrabTolerance;
            float handY = bounds.min.y + Config.LedgeHeadHeight;
            var probe = new Vector2(bounds.center.x + FacingDirection * reach, handY + tolerance);
            var top = Physics2D.Raycast(probe, Vector2.down, tolerance * 2f, _groundMask);
            if (top.collider == null || !IsOneWayPlatform(top.collider)
                || top.collider == _ledgeIgnoredPlatform || top.normal.y < MinGroundNormalY)
                return false;

            // A real side hit is required: a ray starting inside the platform is not an edge.
            var side = Physics2D.Raycast(new Vector2(bounds.center.x, top.point.y - OneWaySideProbeInset),
                dir, reach, _groundMask);
            if (side.collider != top.collider || side.distance <= 0f
                || side.normal.x * FacingDirection > -MinGroundNormalY)
                return false;

            return TryGetLedgeStandPoint(bounds, side.point.x, top.point.y, out standPoint);
        }

        private static bool IsOneWayPlatform(Collider2D collider) =>
            collider.usedByEffector && collider.TryGetComponent<PlatformEffector2D>(out var effector)
            && effector.isActiveAndEnabled && effector.useOneWay;

        private bool TryGetLedgeStandPoint(Bounds bounds, float edgeX, float topY, out Vector2 standPoint)
        {
            standPoint = default;
            var stand = new Vector2(edgeX + FacingDirection * (bounds.extents.x + LedgeStandInset), topY);
            var standCenter = stand + new Vector2(0f, bounds.size.y * 0.5f + LedgeStandInset);
            if (Physics2D.OverlapBox(standCenter, bounds.size * 0.95f, 0f, _groundMask) != null)
                return false;

            standPoint = stand;
            return true;
        }

        // ---------- Facing ----------

        public void FaceInput()
        {
            if (HasMoveInput)
                Face(Input.MoveX > 0f ? 1 : -1);
        }

        public void Face(int direction)
        {
            FacingDirection = direction;
            _sprite.flipX = direction < 0;
        }
    }
}
