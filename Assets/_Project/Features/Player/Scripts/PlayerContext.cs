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
        private const float LedgeTopProbeInset = 0.1f;
        private const float LedgeStandInset = 0.05f;

        private readonly Rigidbody2D _body;
        private readonly BoxCollider2D _collider;
        private readonly SpriteRenderer _sprite;
        private readonly LayerMask _groundMask;

        private float _lastGroundedTime = float.NegativeInfinity;
        private float _lastJumpPressedTime = float.NegativeInfinity;

        public PlayerContext(
            Rigidbody2D body,
            BoxCollider2D collider,
            SpriteRenderer sprite,
            LayerMask groundMask,
            PlayerMovementConfig config,
            PlayerInputReader input,
            PlayerAnimatorBridge animator)
        {
            _body = body;
            _collider = collider;
            _sprite = sprite;
            _groundMask = groundMask;
            Config = config;
            Input = input;
            Animator = animator;
        }

        public PlayerMovementConfig Config { get; }
        public PlayerInputReader Input { get; }
        public PlayerAnimatorBridge Animator { get; }

        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public bool IsInvulnerable { get; set; }
        public int PendingComboIndex { get; set; }
        public Vector2 LedgeStandPoint { get; set; }
        public Vector2 Velocity => _body.linearVelocity;

        public bool JumpBuffered => Time.time - _lastJumpPressedTime <= Config.JumpBufferTime;
        public bool WithinCoyoteTime => Time.time - _lastGroundedTime <= Config.CoyoteTime;
        public bool HasMoveInput => Mathf.Abs(Input.MoveX) > 0.01f;

        /// <summary>
        /// Called once per frame before the state machine ticks.
        /// </summary>
        public void RefreshFrame()
        {
            // Thin strip under the feet; only surfaces facing up count, so touching a wall is not "ground".
            var bounds = _collider.bounds;
            var origin = new Vector2(bounds.center.x, bounds.min.y + FeetProbeHeight * 0.5f);
            var size = new Vector2(bounds.size.x * FeetProbeWidthRatio, FeetProbeHeight);
            var hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, Config.GroundCheckDistance, _groundMask);
            IsGrounded = hit.collider != null && hit.normal.y > MinGroundNormalY;

            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
                RefillAirJumps();
            }
            if (Input.JumpPressed)
                _lastJumpPressedTime = Time.time;
        }

        public Vector2 Position => _body.position;

        /// <summary>
        /// Looks for a climbable ledge in the facing direction while the player holds toward it:
        /// a wall at chest height, free space at head height, and room to stand on top.
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

            var chest = Physics2D.Raycast(new Vector2(bounds.center.x, feetY + Config.LedgeChestHeight), dir, reach, _groundMask);
            if (chest.collider == null)
                return false;

            var head = Physics2D.Raycast(new Vector2(bounds.center.x, feetY + Config.LedgeHeadHeight), dir, reach, _groundMask);
            if (head.collider != null)
                return false;

            // Find the ledge top just past the wall face.
            float probeX = chest.point.x + FacingDirection * LedgeTopProbeInset;
            float probeTop = feetY + Config.LedgeHeadHeight;
            var top = Physics2D.Raycast(new Vector2(probeX, probeTop), Vector2.down,
                Config.LedgeHeadHeight - Config.LedgeChestHeight + LedgeTopProbeInset, _groundMask);
            if (top.collider == null || top.normal.y < MinGroundNormalY)
                return false;

            var stand = new Vector2(chest.point.x + FacingDirection * (bounds.extents.x + LedgeStandInset), top.point.y);
            var standCenter = stand + new Vector2(0f, bounds.size.y * 0.5f + LedgeStandInset);
            if (Physics2D.OverlapBox(standCenter, bounds.size * 0.95f, 0f, _groundMask) != null)
                return false;

            standPoint = stand;
            return true;
        }

        public void SetKinematic(bool kinematic)
        {
            _body.linearVelocity = Vector2.zero;
            _body.bodyType = kinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        }

        public void MovePosition(Vector2 position) => _body.MovePosition(position);

        public void SetHorizontalVelocity(float x) => _body.linearVelocity = new Vector2(x, _body.linearVelocity.y);

        public void SetVerticalVelocity(float y) => _body.linearVelocity = new Vector2(_body.linearVelocity.x, y);

        public void SetGravityScale(float scale) => _body.gravityScale = scale;

        public void ClampFallSpeed()
        {
            if (_body.linearVelocity.y < -Config.MaxFallSpeed)
                SetVerticalVelocity(-Config.MaxFallSpeed);
        }

        public void StartJump()
        {
            SetVerticalVelocity(Config.JumpVelocity);
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
        }

        public int AirJumpsRemaining { get; private set; }

        public bool CanAirJump =>
            Config.AirJumpEnabled && AirJumpsRemaining > 0 && Input.JumpPressed && !WithinCoyoteTime;

        public void RefillAirJumps() => AirJumpsRemaining = Config.MaxAirJumps;

        public void StartAirJump()
        {
            AirJumpsRemaining--;
            SetVerticalVelocity(Config.AirJumpVelocity);
            _lastJumpPressedTime = float.NegativeInfinity;
        }

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
