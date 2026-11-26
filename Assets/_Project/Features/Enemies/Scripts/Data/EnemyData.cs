using System;
using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;
using UnityEngine.Serialization;

namespace DeadCells.Enemies
{
    public enum EnemyAttackKind
    {
        /// <summary>Stand and swing in front (Zombie scratch).</summary>
        Melee,
        /// <summary>Leap forward, damaging on contact (Zombie leap, Rat pounce).</summary>
        Lunge,
        /// <summary>Charge forward over a distance, stunning on contact (Shieldbearer ram).</summary>
        Dash,
        /// <summary>Fire a straight projectile (Undead Archer).</summary>
        Shoot,
        /// <summary>Lob bombs at the player's position (Grenadier).</summary>
        Grenade
    }

    /// <summary>
    /// Enemy tuning. Values marked "DC:" come from Dead Cells' mob table (data.cdb, sheets "mob" / "mob@skill");
    /// behaviour notes marked "Wiki:" come from deadcells.wiki.gg. Distances are in Dead Cells cells
    /// (1 cell = WeaponData.CellSize units). Speeds and timings without a source are tuned by feel.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "DeadCells/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Serializable]
        public class Skill
        {
            [Tooltip("DC: skill id (reference only).")]
            [FormerlySerializedAs("DcId"), SerializeField] private string _dcId;
            [FormerlySerializedAs("Kind"), SerializeField] private EnemyAttackKind _kind;
            [Tooltip("Animator state played during the wind-up.")]
            [FormerlySerializedAs("ChargeAnim"), SerializeField] private string _chargeAnim = "AtkLoad";
            [Tooltip("Animator state played during the execution.")]
            [FormerlySerializedAs("ExecuteAnim"), SerializeField] private string _executeAnim = "Atk";
            [Tooltip("Usable when the player is at least this far (cells).")]
            [FormerlySerializedAs("MinRangeCells"), SerializeField] private float _minRangeCells;
            [Tooltip("Usable when the player is at most this far (cells). DC: skill.radius when set.")]
            [FormerlySerializedAs("MaxRangeCells"), SerializeField] private float _maxRangeCells = 1.35f;
            [Tooltip("DC: skill.power")]
            [FormerlySerializedAs("Damage"), SerializeField] private int _damage = 10;
            [Tooltip("Wind-up shown as a telegraph (s). DC: skill.charge")]
            [FormerlySerializedAs("Charge"), SerializeField] private float _charge = 0.8f;
            [Tooltip("Seconds before this skill can be used again. DC: skill.coolDown")]
            [FormerlySerializedAs("Cooldown"), SerializeField] private float _cooldown = 1f;
            [Tooltip("Seconds the AI stays locked after the skill. DC: skill.lockAiAfter")]
            [FormerlySerializedAs("Recover"), SerializeField] private float _recover = 0.2f;
            [Tooltip("Frame of the execution clip (30 fps) on which the hit lands. DC: skill.hitFrame")]
            [FormerlySerializedAs("HitFrame"), SerializeField] private int _hitFrame;
            [Tooltip("Hit box width in front of the enemy centre (cells).")]
            [FormerlySerializedAs("HitReachCells"), SerializeField] private float _hitReachCells = 1.35f;
            [Tooltip("Hit box height (cells).")]
            [FormerlySerializedAs("HitHeightCells"), SerializeField] private float _hitHeightCells = 2f;
            [Tooltip("Knockback given to the player (units/s).")]
            [FormerlySerializedAs("Knockback"), SerializeField] private float _knockback = 4f;
            [Tooltip("Stun given to the player (s). DC: props.stunTime")]
            [FormerlySerializedAs("StunDealt"), SerializeField] private float _stunDealt;

            [Header("Lunge / Dash")]
            [Tooltip("Travel distance (cells). DC: props.distance")]
            [FormerlySerializedAs("TravelCells"), SerializeField] private float _travelCells = 4f;
            [Tooltip("Travel duration (s). DC: props.duration")]
            [FormerlySerializedAs("TravelDuration"), SerializeField] private float _travelDuration = 0.4f;
            [Tooltip("Upward speed of a lunge (units/s).")]
            [FormerlySerializedAs("LungeUpSpeed"), SerializeField] private float _lungeUpSpeed = 5f;

            [Header("Projectiles")]
            [FormerlySerializedAs("ProjectilePrefab"), SerializeField] private Projectile _projectilePrefab;
            [FormerlySerializedAs("ProjectileSpeed"), SerializeField] private float _projectileSpeed = 7f;
            [Tooltip("Height above the feet the projectile leaves from (units).")]
            [FormerlySerializedAs("MuzzleHeight"), SerializeField] private float _muzzleHeight = 1f;
            [Tooltip("DC: props.count")]
            [FormerlySerializedAs("ProjectileCount"), SerializeField, Min(1)] private int _projectileCount = 1;
            [Tooltip("Bomb fuse after landing (s). DC: props.triggerDelay")]
            [FormerlySerializedAs("FuseTime"), SerializeField] private float _fuseTime = 1.5f;
            [Tooltip("Blast radius (cells). DC: skill.radius")]
            [FormerlySerializedAs("BlastRadiusCells"), SerializeField] private float _blastRadiusCells = 2.9f;
            [Tooltip("Spread of bomb landing points around the player (cells).")]
            [FormerlySerializedAs("LandingSpreadCells"), SerializeField] private float _landingSpreadCells = 1.5f;
            [FormerlySerializedAs("BlastVfx"), SerializeField] private VfxDefinition _blastVfx;

            public Skill() { }

            public Skill(
                EnemyAttackKind kind,
                string dcId = null,
                string chargeAnim = "AtkLoad",
                string executeAnim = "Atk",
                float minRangeCells = 0f,
                float maxRangeCells = 1.35f,
                int damage = 10,
                float charge = 0.8f,
                float cooldown = 1f,
                float recover = 0.2f,
                int hitFrame = 0,
                float hitReachCells = 1.35f,
                float hitHeightCells = 2f,
                float knockback = 4f,
                float stunDealt = 0f,
                float travelCells = 4f,
                float travelDuration = 0.4f,
                float lungeUpSpeed = 5f,
                Projectile projectilePrefab = null,
                float projectileSpeed = 7f,
                float muzzleHeight = 1f,
                int projectileCount = 1,
                float fuseTime = 1.5f,
                float blastRadiusCells = 2.9f,
                float landingSpreadCells = 1.5f,
                VfxDefinition blastVfx = null)
            {
                _kind = kind;
                _dcId = dcId;
                _chargeAnim = chargeAnim;
                _executeAnim = executeAnim;
                _minRangeCells = minRangeCells;
                _maxRangeCells = maxRangeCells;
                _damage = damage;
                _charge = charge;
                _cooldown = cooldown;
                _recover = recover;
                _hitFrame = hitFrame;
                _hitReachCells = hitReachCells;
                _hitHeightCells = hitHeightCells;
                _knockback = knockback;
                _stunDealt = stunDealt;
                _travelCells = travelCells;
                _travelDuration = travelDuration;
                _lungeUpSpeed = lungeUpSpeed;
                _projectilePrefab = projectilePrefab;
                _projectileSpeed = projectileSpeed;
                _muzzleHeight = muzzleHeight;
                _projectileCount = projectileCount;
                _fuseTime = fuseTime;
                _blastRadiusCells = blastRadiusCells;
                _landingSpreadCells = landingSpreadCells;
                _blastVfx = blastVfx;
            }

            public string DcId => _dcId;
            public EnemyAttackKind Kind => _kind;
            public string ChargeAnim => _chargeAnim;
            public string ExecuteAnim => _executeAnim;
            public float MinRangeCells => _minRangeCells;
            public float MaxRangeCells => _maxRangeCells;
            public int Damage => _damage;
            public float Charge => _charge;
            public float Cooldown => _cooldown;
            public float Recover => _recover;
            public int HitFrame => _hitFrame;
            public float HitReachCells => _hitReachCells;
            public float HitHeightCells => _hitHeightCells;
            public float Knockback => _knockback;
            public float StunDealt => _stunDealt;
            public float TravelCells => _travelCells;
            public float TravelDuration => _travelDuration;
            public float LungeUpSpeed => _lungeUpSpeed;
            public Projectile ProjectilePrefab => _projectilePrefab;
            public float ProjectileSpeed => _projectileSpeed;
            public float MuzzleHeight => _muzzleHeight;
            public int ProjectileCount => _projectileCount;
            public float FuseTime => _fuseTime;
            public float BlastRadiusCells => _blastRadiusCells;
            public float LandingSpreadCells => _landingSpreadCells;
            public VfxDefinition BlastVfx => _blastVfx;

            public float MinRange => _minRangeCells * WeaponData.CellSize;
            public float MaxRange => _maxRangeCells * WeaponData.CellSize;
            public float HitReach => _hitReachCells * WeaponData.CellSize;
            public float HitHeight => _hitHeightCells * WeaponData.CellSize;
            public float TravelDistance => _travelCells * WeaponData.CellSize;
            public float BlastRadius => _blastRadiusCells * WeaponData.CellSize;
            public float LandingSpread => _landingSpreadCells * WeaponData.CellSize;
        }

        [Header("Identity")]
        [Tooltip("DC: mob.id")]
        [SerializeField] private string _dcId;
        [Tooltip("Replaces magenta in the sprites. DC: mob.flesh2 (rule from a visual experiment).")]
        [SerializeField] private Color _fleshColor = Color.magenta;
        [Tooltip("Telegraph glow during wind-ups. DC: mob.glowInnerColor (white when the mob has none).")]
        [SerializeField] private Color _glowColor = Color.white;

        [Header("Health")]
        [Tooltip("DC: mob.life")]
        [SerializeField] private int _maxHealth = 100;
        [Tooltip("Seconds of stun after each hit taken. DC: directHitStunChance 1 (length tuned).")]
        [SerializeField] private float _hitStun = 0.25f;
        [Tooltip("Fraction of incoming knockback ignored (0-1). DC: props.bumpResistance.")]
        [Range(0f, 1f)]
        [SerializeField] private float _knockbackResistance;

        [Header("Shield (Wiki: Shieldbearer)")]
        [Tooltip("Blocks hits coming from the front unless they stun.")]
        [SerializeField] private bool _hasFrontShield;
        [Tooltip("Stagger given to a player whose melee hit lands on the shield (s).")]
        [SerializeField] private float _shieldStaggerDuration = 0.4f;

        [Header("Movement (tuned by feel)")]
        [SerializeField] private float _patrolSpeed = 1.5f;
        [SerializeField] private float _chaseSpeed = 2.5f;
        [Tooltip("Seconds needed to turn around (Wiki: Shieldbearer is slow to turn around).")]
        [SerializeField] private float _turnDelay;
        [SerializeField] private Vector2 _idleTimeRange = new Vector2(0.8f, 2f);
        [SerializeField] private Vector2 _patrolTimeRange = new Vector2(1.5f, 3f);

        [Header("Sensing (cells)")]
        [SerializeField] private float _detectRangeCells = 10f;
        [SerializeField] private float _loseRangeCells = 16f;
        [SerializeField] private float _maxHeightDiffCells = 4f;
        [Tooltip("Detect the player through walls (Wiki: Grenadier).")]
        [SerializeField] private bool _seeThroughWalls;

        [Header("Backstep (Wiki: Shieldbearer, Archer, Grenadier)")]
        [Tooltip("Back off when the player is closer than this (cells). 0 = never.")]
        [SerializeField] private float _backstepTriggerCells;
        [SerializeField] private float _backstepCells = 2.5f;
        [SerializeField] private float _backstepDuration = 0.3f;
        [SerializeField] private float _backstepCooldown = 1f;

        [Header("Teleport (Wiki: Archer teleports behind you when cornered)")]
        [SerializeField] private bool _canTeleport;
        [Tooltip("DC: swap coolDown 3.5")]
        [SerializeField] private float _teleportCooldown = 3.5f;
        [Tooltip("Lands this far behind the player (cells).")]
        [SerializeField] private float _teleportBehindCells = 2.5f;

        [Header("Skills (checked in order; the first usable one is used)")]
        [SerializeField] private Skill[] _skills = Array.Empty<Skill>();

        [Header("Loot")]
        [Tooltip("Scales gold and gem drops on top of max health (gold ~ health / 10, gem chance ~ health / 200).")]
        [Min(0f)]
        [SerializeField] private float _lootMultiplier = 1f;

        public string DcId => _dcId;
        public Color FleshColor => _fleshColor;
        public Color GlowColor => _glowColor;
        public int MaxHealth => _maxHealth;
        public float HitStun => _hitStun;
        public float KnockbackResistance => _knockbackResistance;
        public bool HasFrontShield => _hasFrontShield;
        public float ShieldStaggerDuration => _shieldStaggerDuration;
        public float PatrolSpeed => _patrolSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float TurnDelay => _turnDelay;
        public Vector2 IdleTimeRange => _idleTimeRange;
        public Vector2 PatrolTimeRange => _patrolTimeRange;
        public float DetectRange => _detectRangeCells * WeaponData.CellSize;
        public float LoseRange => _loseRangeCells * WeaponData.CellSize;
        public float MaxHeightDiff => _maxHeightDiffCells * WeaponData.CellSize;
        public bool SeeThroughWalls => _seeThroughWalls;
        public float BackstepTrigger => _backstepTriggerCells * WeaponData.CellSize;
        public float BackstepDistance => _backstepCells * WeaponData.CellSize;
        public float BackstepDuration => _backstepDuration;
        public float BackstepCooldown => _backstepCooldown;
        public bool CanTeleport => _canTeleport;
        public float TeleportCooldown => _teleportCooldown;
        public float TeleportBehind => _teleportBehindCells * WeaponData.CellSize;
        public Skill[] Skills => _skills;
        public float LootMultiplier => _lootMultiplier;
    }
}
