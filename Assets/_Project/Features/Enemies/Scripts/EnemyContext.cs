using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Data and helpers shared by every enemy state: body, sensors, facing, cooldowns and hit tests.
    /// </summary>
    public class EnemyContext
    {
        private const float GroundProbe = 0.08f;
        private const float WallProbe = 0.15f;
        private const float LedgeProbeDepth = 0.6f;
        private const float PlayerChestHeight = 0.8f;

        private readonly Rigidbody2D _body;
        private readonly BoxCollider2D _collider;
        private readonly LayerMask _groundMask;
        private readonly HitQuery _hits;
        private readonly float[] _skillReadyTime;
        private float _turnTimer;

        public EnemyContext(Rigidbody2D body, BoxCollider2D collider, EnemyAnimator animator, Health health,
            EnemyData data, LayerMask groundMask, LayerMask attackTargets)
        {
            _body = body;
            _collider = collider;
            Animator = animator;
            Health = health;
            Data = data;
            _groundMask = groundMask;
            _hits = new HitQuery(attackTargets);
            _skillReadyTime = new float[data.Skills.Length];
        }

        public EnemyAnimator Animator { get; }
        public Health Health { get; }
        public EnemyData Data { get; }
        public GameObject Owner => _body.gameObject;

        public Transform Player { get; set; }
        public ProjectileSpawner Projectiles { get; set; }
        public VfxSpawner Vfx { get; set; }

        public int Facing { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public DamageInfo LastDamage { get; set; }
        /// <summary>Index into Data.Skills of the skill being performed.</summary>
        public int CurrentSkill { get; set; }
        public float NextBackstepTime { get; set; }
        public float NextTeleportTime { get; set; }

        public Vector2 Position => _body.position;
        public Vector2 Velocity => _body.linearVelocity;
        public Bounds Bounds => _collider.bounds;

        public void RefreshFrame()
        {
            var b = _collider.bounds;
            IsGrounded = Physics2D.BoxCast(new Vector2(b.center.x, b.min.y + 0.02f), new Vector2(b.size.x * 0.9f, 0.04f),
                0f, Vector2.down, GroundProbe, _groundMask);
        }

        // ---------- Skills ----------

        public bool IsSkillReady(int index) => Time.time >= _skillReadyTime[index];

        public void StartSkillCooldown(int index) => _skillReadyTime[index] = Time.time + Data.Skills[index].Cooldown;

        /// <summary>First skill whose range contains the player's distance and whose cooldown is over, or -1.</summary>
        public int PickSkill(float distance)
        {
            var skills = Data.Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                if (IsSkillReady(i) && distance >= skills[i].MinRange && distance <= skills[i].MaxRange)
                    return i;
            }
            return -1;
        }

        // ---------- Player sensing ----------

        public float DistanceToPlayer => Player == null ? float.PositiveInfinity : Mathf.Abs(Player.position.x - Position.x);

        public int DirectionToPlayer => Player == null || Player.position.x >= Position.x ? 1 : -1;

        /// <summary>Within range, roughly the same height, and (unless SeeThroughWalls) not behind a wall.</summary>
        public bool CanSeePlayer(float range)
        {
            if (Player == null || !Player.gameObject.activeInHierarchy)
                return false;
            Vector2 eye = new Vector2(Position.x, _collider.bounds.center.y);
            Vector2 target = new Vector2(Player.position.x, Player.position.y + PlayerChestHeight);
            if (Mathf.Abs(target.x - eye.x) > range || Mathf.Abs(Player.position.y - Position.y) > Data.MaxHeightDiff)
                return false;
            return Data.SeeThroughWalls || !Physics2D.Linecast(eye, target, _groundMask);
        }

        // ---------- Facing ----------

        public void Face(int direction)
        {
            if (direction == 0)
                return;
            Facing = direction;
            _turnTimer = 0f;
            Animator.Renderer.flipX = direction < 0;
        }

        /// <summary>
        /// Turns toward a direction after Data.TurnDelay seconds of wanting to (instant when 0).
        /// Returns true once facing that way.
        /// </summary>
        public bool TurnToward(int direction)
        {
            if (direction == 0 || direction == Facing)
            {
                _turnTimer = 0f;
                return true;
            }
            _turnTimer += Time.deltaTime;
            if (_turnTimer < Data.TurnDelay)
                return false;
            Face(direction);
            return true;
        }

        // ---------- Movement ----------

        public void SetHorizontalVelocity(float x) => _body.linearVelocity = new Vector2(x, _body.linearVelocity.y);

        public void SetVelocity(Vector2 v) => _body.linearVelocity = v;

        public void Teleport(Vector2 feet)
        {
            _body.position = feet;
            _body.linearVelocity = Vector2.zero;
        }

        public bool IsWallAhead(int dir)
        {
            var b = _collider.bounds;
            return Physics2D.BoxCast(b.center, new Vector2(0.05f, b.size.y * 0.8f), 0f, new Vector2(dir, 0f),
                b.extents.x + WallProbe, _groundMask);
        }

        public bool IsGroundAhead(int dir)
        {
            var b = _collider.bounds;
            var origin = new Vector2(b.center.x + dir * (b.extents.x + WallProbe), b.min.y + 0.05f);
            return Physics2D.Raycast(origin, Vector2.down, LedgeProbeDepth, _groundMask);
        }

        /// <summary>Can keep walking that way without hitting a wall or walking off a ledge.</summary>
        public bool CanWalk(int dir) => !IsWallAhead(dir) && IsGroundAhead(dir);

        /// <summary>Solid ground at a point and room for this body to stand there.</summary>
        public bool CanStandAt(Vector2 feet)
        {
            var size = _collider.size;
            if (!Physics2D.Raycast(feet + Vector2.up * 0.2f, Vector2.down, 0.6f, _groundMask))
                return false;
            return !Physics2D.OverlapBox(feet + new Vector2(0f, size.y * 0.5f + 0.05f), size * 0.9f, 0f, _groundMask);
        }

        public void SetPhysicsActive(bool active)
        {
            _body.simulated = active;
            _collider.enabled = active;
        }

        // ---------- Attacks ----------

        public void BeginSwing() => _hits.ResetSwing();

        /// <summary>Damages the player in a box in front of the enemy's centre. Returns hits.</summary>
        public int Strike(EnemyData.Skill skill)
        {
            var b = _collider.bounds;
            var center = new Vector2(b.center.x + Facing * skill.HitReach * 0.5f, b.min.y + skill.HitHeight * 0.5f);
            var info = new DamageInfo(skill.Damage, Facing, skill.Knockback, skill.StunDealt, false, Owner);
            return _hits.Strike(center, new Vector2(skill.HitReach, skill.HitHeight), info);
        }
    }
}
