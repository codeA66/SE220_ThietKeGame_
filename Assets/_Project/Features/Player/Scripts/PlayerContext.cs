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
        private const float FeetProbeWidthRatio = 0.9f;
        private const float MinGroundNormalY = 0.7f;
        private const float MaxGroundedRiseSpeed = 0.01f;
        private const float LedgeTopProbeInset = 0.1f;
        private const float LedgeStandInset = 0.05f;
        private const float OneWaySideProbeInset = 0.01f;

        private readonly Rigidbody2D _body;
        private readonly BoxCollider2D _collider;
        private readonly SpriteRenderer _sprite;
        private readonly LayerMask _groundMask;
        private readonly LayerMask _rollPassThroughMask;
        private readonly LayerMask _climbableMask;
        private readonly Vector2 _standingColliderSize;
        private readonly Vector2 _standingColliderOffset;

        private float _lastGroundedTime = float.NegativeInfinity;
        private float _lastJumpPressedTime = float.NegativeInfinity;
        private float _rollReadyTime = float.NegativeInfinity;
        private float _comboCarryUntil = float.NegativeInfinity;
        private int _comboCarryIndex;
        private Collider2D _groundCollider;
        private Collider2D _droppedPlatform;
        private Collider2D _ledgeIgnoredPlatform;
        private float _dropRestoreTime;
        private float _lastAirborneVelocityY;
        private float _perfectDodgeUntil = float.NegativeInfinity;

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
            _rollPassThroughMask = rollPassThroughMask;
            _climbableMask = climbableMask;
            _standingColliderSize = collider.size;
            _standingColliderOffset = collider.offset;
            Config = config;
            Input = input;
            Animator = animator;
        }

        /// <summary>Ground jump started. Argument: feet position.</summary>
        public event Action<Vector2> Jumped;
        /// <summary>Air jump started. Argument: feet position.</summary>
        public event Action<Vector2> AirJumped;
        /// <summary>Landed. Arguments: feet position, downward speed just before impact (positive).</summary>
        public event Action<Vector2, float> Landed;
        /// <summary>A roll started. Argument: feet position where the perfect-dodge ghost stays.</summary>
        public event Action<Vector2> RollStarted;
        /// <summary>A hit landed on the perfect-dodge ghost.</summary>
        public event Action PerfectDodged;

        public PlayerMovementConfig Config { get; }
        public PlayerCombat Combat { get; set; }
        public PlayerBow Bow { get; set; }
        public PlayerWeaponLoadout Weapons { get; set; }
        public PlayerInputReader Input { get; }
        public PlayerAnimatorBridge Animator { get; }

        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public bool IsInvulnerable { get; set; }
        /// <summary>Set by PlayerSpawnHead when the crawling head reaches the body during the spawn intro.</summary>
        public bool SpawnHeadArrived { get; set; }
        public int PendingComboIndex { get; set; }
        public Vector2 LedgeStandPoint { get; set; }
        public int AirJumpsRemaining { get; private set; }
        public int AirRollsRemaining { get; private set; }

        public Vector2 Velocity => _body.linearVelocity;
        public Vector2 Position => _body.position;

        public bool JumpBuffered => Time.time - _lastJumpPressedTime <= Config.JumpBufferTime;
        public bool WithinCoyoteTime => Time.time - _lastGroundedTime <= Config.CoyoteTime;
        public bool HasMoveInput => Mathf.Abs(Input.MoveX) > 0.01f;
        public bool IsDownHeld => Input.MoveY < -Config.DownInputThreshold;
        public bool IsUpHeld => Input.MoveY > Config.DownInputThreshold;
        public Collider2D ClimbTarget { get; private set; }
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
            if (_ledgeIgnoredPlatform != null)
            {
                var platformBounds = _ledgeIgnoredPlatform.bounds;
                if (bounds.max.y < platformBounds.min.y || bounds.max.x < platformBounds.min.x || bounds.min.x > platformBounds.max.x)
                    _ledgeIgnoredPlatform = null;
            }
            var origin = new Vector2(bounds.center.x, bounds.min.y + FeetProbeHeight * 0.5f);
            var size = new Vector2(bounds.size.x * FeetProbeWidthRatio, FeetProbeHeight);
            var hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, Config.GroundCheckDistance, _groundMask);
            IsGrounded = hit.collider != null
                         && hit.collider != _droppedPlatform
                         && hit.normal.y > MinGroundNormalY
                         && _body.linearVelocity.y <= MaxGroundedRiseSpeed;
            _groundCollider = IsGrounded ? hit.collider : null;

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

        // ---------- Velocity / body ----------

        public void SetHorizontalVelocity(float x) => _body.linearVelocity = new Vector2(x, _body.linearVelocity.y);

        public void SetVerticalVelocity(float y) => _body.linearVelocity = new Vector2(_body.linearVelocity.x, y);

        public void AddVerticalVelocity(float dy) => SetVerticalVelocity(_body.linearVelocity.y + dy);

        public void SetGravityScale(float scale) => _body.gravityScale = scale;

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

        public void RefillAirMoves()
        {
            AirJumpsRemaining = Config.MaxAirJumps;
            AirRollsRemaining = Config.MaxAirRolls;
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
            _body.excludeLayers = _rollPassThroughMask;
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
            _body.excludeLayers = 0;
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
            _body.position = new Vector2(ClimbTarget.bounds.center.x, _body.position.y);
            RefillAirMoves();
        }

        public void EndClimb()
        {
            ClimbTarget = null;
            SetGravityScale(Config.GravityScale);
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
            if (!Config.LedgeClimbEnabled || !HasMoveInput || Mathf.Sign(Input.MoveX) != FacingDirection)
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
