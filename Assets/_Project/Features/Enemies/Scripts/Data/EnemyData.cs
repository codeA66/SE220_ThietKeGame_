using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Enemies
{
    public enum EnemyAttackKind
    {
        /// <summary>Stand and swing in front (Zombie "cc").</summary>
        Melee,
        /// <summary>Leap at the player and bite several times (Rat "nibble").</summary>
        Lunge,
        /// <summary>Charge forward over a distance, stunning on hit (Shield "dash").</summary>
        Dash,
        /// <summary>Fire a straight projectile (Archer "shoot").</summary>
        Shoot,
        /// <summary>Lob fused grenades in an arc (Grenader "grenade").</summary>
        Grenade
    }

    /// <summary>
    /// Enemy tuning. Values marked "DC:" come from Dead Cells' mob table (data.cdb, sheets "mob" / "mob@skill").
    /// Distances are entered in Dead Cells cells (1 cell = WeaponData.CellSize units).
    /// Movement speeds are tuned by feel: DC speeds use an internal unit that was not converted.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "DeadCells/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("DC: mob.id")]
        [SerializeField] private string _dcId;
        [Tooltip("Replaces magenta in the sprites. DC: mob.flesh2 (rule from a visual experiment).")]
        [SerializeField] private Color _fleshColor = Color.magenta;

        [Header("Health")]
        [Tooltip("DC: mob.life")]
        [SerializeField] private int _maxHealth = 100;
        [Tooltip("Seconds of stun after each hit taken. DC: directHitStunChance 1 (stun length not found; tuned).")]
        [SerializeField] private float _hitStun = 0.25f;
        [Tooltip("Fraction of incoming knockback ignored (0-1). DC: props.bumpResistance.")]
        [Range(0f, 1f)]
        [SerializeField] private float _knockbackResistance;

        [Header("Movement (tuned by feel)")]
        [SerializeField] private float _patrolSpeed = 1.5f;
        [SerializeField] private float _chaseSpeed = 2.5f;
        [Tooltip("Seconds standing still between patrol walks.")]
        [SerializeField] private Vector2 _idleTimeRange = new Vector2(0.8f, 2f);
        [Tooltip("Seconds of one patrol walk.")]
        [SerializeField] private Vector2 _patrolTimeRange = new Vector2(1.5f, 3f);

        [Header("Sensing (cells)")]
        [Tooltip("Start chasing when the player is this close and visible.")]
        [SerializeField] private float _detectRangeCells = 10f;
        [Tooltip("Give up when the player is farther than this.")]
        [SerializeField] private float _loseRangeCells = 16f;
        [Tooltip("Ignore the player if the height difference is larger (cells).")]
        [SerializeField] private float _maxHeightDiffCells = 4f;
        [Tooltip("Ranged enemies back away when the player is closer than this (cells). 0 = never.")]
        [SerializeField] private float _keepAwayCells;

        [Header("Attack")]
        [SerializeField] private EnemyAttackKind _attackKind;
        [Tooltip("DC: skill.power")]
        [SerializeField] private int _damage = 10;
        [Tooltip("Wind-up before the attack, shown as a telegraph. DC: skill.charge")]
        [SerializeField] private float _charge = 0.8f;
        [Tooltip("Seconds before the next attack. DC: skill.coolDown")]
        [SerializeField] private float _cooldown = 1f;
        [Tooltip("Seconds the AI stays locked after the attack. DC: skill.lockAiAfter")]
        [SerializeField] private float _recover = 0.3f;
        [Tooltip("Frame of the execution clip (30 fps) on which the hit lands. DC: skill.hitFrame")]
        [SerializeField] private int _hitFrame = 3;
        [Tooltip("Start the attack when the player is within this distance (cells). DC: skill.radius when set.")]
        [SerializeField] private float _attackRangeCells = 1.35f;
        [Tooltip("Hit box width in front of the enemy centre (cells).")]
        [SerializeField] private float _hitReachCells = 1.35f;
        [Tooltip("Hit box height (cells).")]
        [SerializeField] private float _hitHeightCells = 2f;
        [Tooltip("Knockback given to the player (units/s).")]
        [SerializeField] private float _knockback = 4f;
        [Tooltip("Stun given to the player (s). DC: dash props.stunTime")]
        [SerializeField] private float _stunDealt;
        [Tooltip("Number of hits per attack. DC: props.count (Rat 2)")]
        [Min(1)]
        [SerializeField] private int _hitCount = 1;

        [Header("Lunge / Dash")]
        [Tooltip("Dash / lunge travel distance (cells). DC: props.distance")]
        [SerializeField] private float _travelCells = 8f;
        [Tooltip("Dash / lunge duration (s). DC: props.duration")]
        [SerializeField] private float _travelDuration = 0.6f;
        [Tooltip("Upward speed of a lunge (units/s).")]
        [SerializeField] private float _lungeUpSpeed = 5f;

        [Header("Projectiles")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 14f;
        [Tooltip("Projectiles per attack. DC: props.count")]
        [Min(1)]
        [SerializeField] private int _projectileCount = 1;
        [Tooltip("Grenade fuse (s). DC: props.triggerDelay")]
        [SerializeField] private float _fuseTime = 1.5f;
        [Tooltip("Grenade blast radius (cells). DC: skill.radius")]
        [SerializeField] private float _blastRadiusCells = 2.9f;
        [Tooltip("Horizontal spread between grenades (units/s of throw speed).")]
        [SerializeField] private float _throwSpread = 1.5f;
        [SerializeField] private VfxDefinition _blastVfx;

        public string DcId => _dcId;
        public Color FleshColor => _fleshColor;
        public int MaxHealth => _maxHealth;
        public float HitStun => _hitStun;
        public float KnockbackResistance => _knockbackResistance;
        public float PatrolSpeed => _patrolSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public Vector2 IdleTimeRange => _idleTimeRange;
        public Vector2 PatrolTimeRange => _patrolTimeRange;
        public float DetectRange => _detectRangeCells * WeaponData.CellSize;
        public float LoseRange => _loseRangeCells * WeaponData.CellSize;
        public float MaxHeightDiff => _maxHeightDiffCells * WeaponData.CellSize;
        public float KeepAway => _keepAwayCells * WeaponData.CellSize;
        public EnemyAttackKind AttackKind => _attackKind;
        public int Damage => _damage;
        public float Charge => _charge;
        public float Cooldown => _cooldown;
        public float Recover => _recover;
        public int HitFrame => _hitFrame;
        public float AttackRange => _attackRangeCells * WeaponData.CellSize;
        public float HitReach => _hitReachCells * WeaponData.CellSize;
        public float HitHeight => _hitHeightCells * WeaponData.CellSize;
        public float Knockback => _knockback;
        public float StunDealt => _stunDealt;
        public int HitCount => _hitCount;
        public float TravelDistance => _travelCells * WeaponData.CellSize;
        public float TravelDuration => _travelDuration;
        public float LungeUpSpeed => _lungeUpSpeed;
        public Projectile ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public int ProjectileCount => _projectileCount;
        public float FuseTime => _fuseTime;
        public float BlastRadius => _blastRadiusCells * WeaponData.CellSize;
        public float ThrowSpread => _throwSpread;
        public VfxDefinition BlastVfx => _blastVfx;
    }
}
