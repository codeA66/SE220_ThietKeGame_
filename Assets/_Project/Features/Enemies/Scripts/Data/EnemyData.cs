using System;
using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

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
            public string DcId;
            public EnemyAttackKind Kind;
            [Tooltip("Animator state played during the wind-up.")]
            public string ChargeAnim = "AtkLoad";
            [Tooltip("Animator state played during the execution.")]
            public string ExecuteAnim = "Atk";
            [Tooltip("Usable when the player is at least this far (cells).")]
            public float MinRangeCells;
            [Tooltip("Usable when the player is at most this far (cells). DC: skill.radius when set.")]
            public float MaxRangeCells = 1.35f;
            [Tooltip("DC: skill.power")]
            public int Damage = 10;
            [Tooltip("Wind-up shown as a telegraph (s). DC: skill.charge")]
            public float Charge = 0.8f;
            [Tooltip("Seconds before this skill can be used again. DC: skill.coolDown")]
            public float Cooldown = 1f;
            [Tooltip("Seconds the AI stays locked after the skill. DC: skill.lockAiAfter")]
            public float Recover = 0.2f;
            [Tooltip("Frame of the execution clip (30 fps) on which the hit lands. DC: skill.hitFrame")]
            public int HitFrame;
            [Tooltip("Hit box width in front of the enemy centre (cells).")]
            public float HitReachCells = 1.35f;
            [Tooltip("Hit box height (cells).")]
            public float HitHeightCells = 2f;
            [Tooltip("Knockback given to the player (units/s).")]
            public float Knockback = 4f;
            [Tooltip("Stun given to the player (s). DC: props.stunTime")]
            public float StunDealt;

            [Header("Lunge / Dash")]
            [Tooltip("Travel distance (cells). DC: props.distance")]
            public float TravelCells = 4f;
            [Tooltip("Travel duration (s). DC: props.duration")]
            public float TravelDuration = 0.4f;
            [Tooltip("Upward speed of a lunge (units/s).")]
            public float LungeUpSpeed = 5f;

            [Header("Projectiles")]
            public Projectile ProjectilePrefab;
            public float ProjectileSpeed = 7f;
            [Tooltip("Height above the feet the projectile leaves from (units).")]
            public float MuzzleHeight = 1f;
            [Tooltip("DC: props.count")]
            [Min(1)] public int ProjectileCount = 1;
            [Tooltip("Bomb fuse after landing (s). DC: props.triggerDelay")]
            public float FuseTime = 1.5f;
            [Tooltip("Blast radius (cells). DC: skill.radius")]
            public float BlastRadiusCells = 2.9f;
            [Tooltip("Spread of bomb landing points around the player (cells).")]
            public float LandingSpreadCells = 1.5f;
            public VfxDefinition BlastVfx;

            public float MinRange => MinRangeCells * WeaponData.CellSize;
            public float MaxRange => MaxRangeCells * WeaponData.CellSize;
            public float HitReach => HitReachCells * WeaponData.CellSize;
            public float HitHeight => HitHeightCells * WeaponData.CellSize;
            public float TravelDistance => TravelCells * WeaponData.CellSize;
            public float BlastRadius => BlastRadiusCells * WeaponData.CellSize;
            public float LandingSpread => LandingSpreadCells * WeaponData.CellSize;
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
    }
}
