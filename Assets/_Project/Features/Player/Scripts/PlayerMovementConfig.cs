using UnityEngine;

namespace DeadCells.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "DeadCells/Player/Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Run")]
        [Tooltip("Horizontal speed on the ground (units/s).")]
        [SerializeField] private float _runSpeed = 7f;
        [Tooltip("Horizontal speed while airborne (units/s).")]
        [SerializeField] private float _airSpeed = 6.5f;

        [Header("Jump")]
        [Tooltip("Upward velocity applied when the jump starts (units/s).")]
        [SerializeField] private float _jumpVelocity = 14f;
        [Tooltip("Rigidbody2D gravity scale while rising / standing.")]
        [SerializeField] private float _gravityScale = 3.5f;
        [Tooltip("Gravity multiplier while falling, for a snappier descent.")]
        [SerializeField] private float _fallGravityMultiplier = 1.5f;
        [Tooltip("Maximum downward speed (units/s).")]
        [SerializeField] private float _maxFallSpeed = 20f;
        [Tooltip("Upward velocity is multiplied by this when jump is released early (variable jump height).")]
        [Range(0f, 1f)]
        [SerializeField] private float _jumpCutMultiplier = 0.5f;
        [Tooltip("Seconds after leaving a ledge during which a jump is still allowed.")]
        [SerializeField] private float _coyoteTime = 0.1f;
        [Tooltip("Seconds a jump press is remembered before landing.")]
        [SerializeField] private float _jumpBufferTime = 0.12f;

        [Header("Air Jump")]
        [SerializeField] private bool _airJumpEnabled = true;
        [Tooltip("Extra jumps allowed while airborne. Refilled on landing or after a ledge climb.")]
        [Min(0)]
        [SerializeField] private int _maxAirJumps = 1;
        [Tooltip("Upward velocity of an air jump (units/s).")]
        [SerializeField] private float _airJumpVelocity = 12f;

        [Header("Roll")]
        [Tooltip("Horizontal speed during a roll (units/s).")]
        [SerializeField] private float _rollSpeed = 12f;
        [Tooltip("Roll length in seconds. Default matches Player_Roll clip: 19 frames at 30 fps.")]
        [SerializeField] private float _rollDuration = 0.633f;

        [Header("Attack")]
        [Tooltip("Normalized time (0-1) of the current swing after which a queued attack chains into the next hit.")]
        [Range(0f, 1f)]
        [SerializeField] private float _comboChainNormalizedTime = 0.6f;
        [Tooltip("Horizontal speed while attacking, as a fraction of run speed (ground) or air speed (air).")]
        [Range(0f, 1f)]
        [SerializeField] private float _attackMoveSpeedMultiplier = 0.4f;
        [Tooltip("Keep facing direction during a swing; it only updates when a new hit starts.")]
        [SerializeField] private bool _attackLockFacing = true;
        [Tooltip("Allow starting attacks while jumping or falling.")]
        [SerializeField] private bool _allowAirAttack = true;

        [Header("Ledge Climb")]
        [Tooltip("Automatically climb a ledge when jumping/falling against it while holding toward it.")]
        [SerializeField] private bool _ledgeClimbEnabled = true;
        [Tooltip("Height above the feet of the 'wall' probe. The ledge top must be above this (units).")]
        [SerializeField] private float _ledgeChestHeight = 0.9f;
        [Tooltip("Height above the feet of the 'free space' probe. The ledge top must be below this (units).")]
        [SerializeField] private float _ledgeHeadHeight = 1.7f;
        [Tooltip("How far beyond the collider side the wall is searched (units).")]
        [SerializeField] private float _ledgeCheckDistance = 0.25f;

        [Header("Ground Check")]
        [Tooltip("How far below the collider the ground is searched (units).")]
        [SerializeField] private float _groundCheckDistance = 0.05f;

        public float RunSpeed => _runSpeed;
        public float AirSpeed => _airSpeed;
        public float JumpVelocity => _jumpVelocity;
        public float GravityScale => _gravityScale;
        public float FallGravityMultiplier => _fallGravityMultiplier;
        public float MaxFallSpeed => _maxFallSpeed;
        public float JumpCutMultiplier => _jumpCutMultiplier;
        public float CoyoteTime => _coyoteTime;
        public float JumpBufferTime => _jumpBufferTime;
        public bool AirJumpEnabled => _airJumpEnabled;
        public int MaxAirJumps => _maxAirJumps;
        public float AirJumpVelocity => _airJumpVelocity;
        public float RollSpeed => _rollSpeed;
        public float RollDuration => _rollDuration;
        public float ComboChainNormalizedTime => _comboChainNormalizedTime;
        public float AttackMoveSpeedMultiplier => _attackMoveSpeedMultiplier;
        public bool AttackLockFacing => _attackLockFacing;
        public bool AllowAirAttack => _allowAirAttack;
        public bool LedgeClimbEnabled => _ledgeClimbEnabled;
        public float LedgeChestHeight => _ledgeChestHeight;
        public float LedgeHeadHeight => _ledgeHeadHeight;
        public float LedgeCheckDistance => _ledgeCheckDistance;
        public float GroundCheckDistance => _groundCheckDistance;
    }
}
