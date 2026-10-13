using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// All player movement tuning. Values marked "DC:" come from Dead Cells' own tuning table
    /// (data.cdb, sheet "truelle"); the rest are Unity-side values tuned by feel.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "DeadCells/Player/Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Run")]
        [Tooltip("Horizontal speed on the ground (units/s).")]
        [SerializeField] private float _runSpeed = 7f;
        [Tooltip("Horizontal speed while airborne (units/s).")]
        [SerializeField] private float _airSpeed = 6.5f;

        [Header("Jump")]
        [Tooltip("Upward velocity applied on the first frame of a jump (units/s). " +
                 "DC: HeroJump 0.36 of a 0.66 full jump, so base ~ 55% of a full jump.")]
        [SerializeField] private float _jumpBaseVelocity = 7.6f;
        [Tooltip("Extra upward velocity added while jump is held during JumpHoldTime (units/s). " +
                 "DC: HeroJumpExtra 0.1 x 3 frames.")]
        [SerializeField] private float _jumpHoldBoost = 6.4f;
        [Tooltip("How long holding jump keeps adding JumpHoldBoost (seconds). DC: 3 frames.")]
        [SerializeField] private float _jumpHoldTime = 0.05f;
        [Tooltip("Rigidbody2D gravity scale while rising / standing.")]
        [SerializeField] private float _gravityScale = 3.5f;
        [Tooltip("Gravity multiplier while falling, for a snappier descent.")]
        [SerializeField] private float _fallGravityMultiplier = 1.5f;
        [Tooltip("Maximum downward speed (units/s).")]
        [SerializeField] private float _maxFallSpeed = 20f;
        [Tooltip("Seconds after leaving a ledge during which a jump is still allowed. DC: HeroInExtremisJump 0.12 s.")]
        [SerializeField] private float _coyoteTime = 0.12f;
        [Tooltip("Seconds a jump press is remembered before landing.")]
        [SerializeField] private float _jumpBufferTime = 0.12f;

        [Header("Air Jump")]
        [SerializeField] private bool _airJumpEnabled = true;
        [Tooltip("Extra jumps allowed while airborne. Refilled on landing or after a ledge climb.")]
        [Min(0)]
        [SerializeField] private int _maxAirJumps = 1;
        [Tooltip("Upward velocity of an air jump (units/s). DC: HeroDoubleJump 0.65, about a full held jump.")]
        [SerializeField] private float _airJumpVelocity = 13.8f;

        [Header("Roll")]
        [Tooltip("Roll length in seconds. DC: DodgeDuration 0.4.")]
        [SerializeField] private float _rollDuration = 0.4f;
        [Tooltip("Seconds after a roll ends before another roll is allowed. DC: DodgeCD 0.37.")]
        [SerializeField] private float _rollCooldown = 0.37f;
        [Tooltip("Horizontal speed at the start of a roll (units/s). Speed eases out to RollEndSpeed.")]
        [SerializeField] private float _rollStartSpeed = 14f;
        [Tooltip("Horizontal speed at the end of a roll (units/s).")]
        [SerializeField] private float _rollEndSpeed = 6f;
        [Tooltip("Normalized time (0-1) after which jump/attack can cancel the roll. 0 = any time.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rollCancelNormalizedTime = 0f;
        [Tooltip("Allow rolling while airborne (Dead Cells wiki: 'Jump + Roll: Mid-air roll').")]
        [SerializeField] private bool _allowAirRoll = true;
        [Tooltip("Air rolls allowed per airtime. Refilled on landing or after a ledge climb.")]
        [Min(0)]
        [SerializeField] private int _maxAirRolls = 1;
        [Tooltip("If off, an air roll glides horizontally with no gravity.")]
        [SerializeField] private bool _airRollKeepsGravity = false;

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
        [Tooltip("Seconds after a roll during which attacking continues the interrupted combo " +
                 "instead of restarting it. DC: WeaponChainLossOnDodge = false.")]
        [SerializeField] private float _comboCarryWindow = 0.3f;

        [Header("Ledge Climb")]
        [Tooltip("Automatically climb a ledge when jumping/falling against it while holding toward it.")]
        [SerializeField] private bool _ledgeClimbEnabled = true;
        [Tooltip("Height above the feet of the 'wall' probe. The ledge top must be above this (units).")]
        [SerializeField] private float _ledgeChestHeight = 0.9f;
        [Tooltip("Height above the feet of the 'free space' probe. The ledge top must be below this (units).")]
        [SerializeField] private float _ledgeHeadHeight = 1.7f;
        [Tooltip("How far beyond the collider side the wall is searched (units).")]
        [SerializeField] private float _ledgeCheckDistance = 0.25f;
        [Tooltip("Maximum vertical difference between the upper ledge probe and a one-way platform top (units).")]
        [Min(0.01f)]
        [SerializeField] private float _oneWayLedgeGrabTolerance = 0.15f;

        [Header("Drop Through / Stomp")]
        [Tooltip("Move Y below minus this counts as holding down.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _downInputThreshold = 0.5f;
        [Tooltip("Seconds collision with a one-way platform stays off after Down + Jump.")]
        [SerializeField] private float _dropThroughDuration = 0.25f;
        [Tooltip("Downward speed of the air stomp (Down + Jump in the air) (units/s).")]
        [SerializeField] private float _stompSpeed = 25f;
        [Tooltip("Seconds the player is locked after a stomp lands.")]
        [SerializeField] private float _stompLandDuration = 0.15f;

        [Header("Chain / Vine Climb")]
        [Tooltip("Vertical speed on a chain or vine (units/s). Tuned by feel: no Dead Cells value found.")]
        [SerializeField] private float _climbSpeed = 4f;

        [Header("Perfect Dodge")]
        [Tooltip("Seconds a ghost stays where a roll started, catching hits as perfect dodges. " +
                 "DC: PerfectDodgeDuration 0.25.")]
        [SerializeField] private float _perfectDodgeDuration = 0.25f;

        [Header("Crouch")]
        [Tooltip("Shrink the collider while crouching. Unverified whether Dead Cells does this.")]
        [SerializeField] private bool _crouchShrinkCollider = false;
        [Tooltip("Collider height while crouching (units), used when CrouchShrinkCollider is on.")]
        [SerializeField] private float _crouchColliderHeight = 0.9f;

        [Header("Hurt")]
        [Tooltip("Seconds of lost control after a hit (longer if the hit stuns). Tuned by feel.")]
        [SerializeField] private float _hurtLockDuration = 0.2f;
        [Tooltip("Minimum horizontal knockback when hit (units/s). Tuned by feel.")]
        [SerializeField] private float _hurtMinKnockback = 4f;
        [Tooltip("Upward pop when hit (units/s). Tuned by feel.")]
        [SerializeField] private float _hurtKnockUp = 4f;

        [Header("Stomp Impact")]
        [Tooltip("Damage of the stomp landing. Tuned by feel: no Dead Cells value found.")]
        [SerializeField] private int _stompDamage = 30;
        [Tooltip("Stun applied by the stomp landing (seconds). DC: DiveAttackStun 1.")]
        [SerializeField] private float _stompStun = 1f;
        [Tooltip("Size of the stomp impact box around the feet (units).")]
        [SerializeField] private Vector2 _stompArea = new Vector2(2.5f, 1f);

        [Header("Ground Check")]
        [Tooltip("How far below the collider the ground is searched (units).")]
        [SerializeField] private float _groundCheckDistance = 0.05f;

        public float RunSpeed => _runSpeed;
        public float AirSpeed => _airSpeed;
        public float JumpBaseVelocity => _jumpBaseVelocity;
        public float JumpHoldBoost => _jumpHoldBoost;
        public float JumpHoldTime => _jumpHoldTime;
        public float GravityScale => _gravityScale;
        public float FallGravityMultiplier => _fallGravityMultiplier;
        public float MaxFallSpeed => _maxFallSpeed;
        public float CoyoteTime => _coyoteTime;
        public float JumpBufferTime => _jumpBufferTime;
        public bool AirJumpEnabled => _airJumpEnabled;
        public int MaxAirJumps => _maxAirJumps;
        public float AirJumpVelocity => _airJumpVelocity;
        public float RollDuration => _rollDuration;
        public float RollCooldown => _rollCooldown;
        public float RollStartSpeed => _rollStartSpeed;
        public float RollEndSpeed => _rollEndSpeed;
        public float RollCancelNormalizedTime => _rollCancelNormalizedTime;
        public bool AllowAirRoll => _allowAirRoll;
        public int MaxAirRolls => _maxAirRolls;
        public bool AirRollKeepsGravity => _airRollKeepsGravity;
        public float ComboChainNormalizedTime => _comboChainNormalizedTime;
        public float AttackMoveSpeedMultiplier => _attackMoveSpeedMultiplier;
        public bool AttackLockFacing => _attackLockFacing;
        public bool AllowAirAttack => _allowAirAttack;
        public float ComboCarryWindow => _comboCarryWindow;
        public bool LedgeClimbEnabled => _ledgeClimbEnabled;
        public float LedgeChestHeight => _ledgeChestHeight;
        public float LedgeHeadHeight => _ledgeHeadHeight;
        public float LedgeCheckDistance => _ledgeCheckDistance;
        public float OneWayLedgeGrabTolerance => Mathf.Max(0.01f, _oneWayLedgeGrabTolerance);
        public float DownInputThreshold => _downInputThreshold;
        public float DropThroughDuration => _dropThroughDuration;
        public float StompSpeed => _stompSpeed;
        public float StompLandDuration => _stompLandDuration;
        public float ClimbSpeed => _climbSpeed;
        public float PerfectDodgeDuration => _perfectDodgeDuration;
        public bool CrouchShrinkCollider => _crouchShrinkCollider;
        public float CrouchColliderHeight => _crouchColliderHeight;
        public float HurtLockDuration => _hurtLockDuration;
        public float HurtMinKnockback => _hurtMinKnockback;
        public float HurtKnockUp => _hurtKnockUp;
        public int StompDamage => _stompDamage;
        public float StompStun => _stompStun;
        public Vector2 StompArea => _stompArea;
        public float GroundCheckDistance => _groundCheckDistance;
    }
}
