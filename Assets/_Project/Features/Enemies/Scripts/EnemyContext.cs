using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Data and helpers shared by every enemy state: body, sensors, facing, attack cooldown and hit tests.
    /// </summary>
    public class EnemyContext
    {
        private const float GroundProbe = 0.08f;
        private const float WallProbe = 0.15f;
        private const float LedgeProbeDepth = 0.6f;

        private readonly Rigidbody2D _body;
        private readonly BoxCollider2D _collider;
        private readonly LayerMask _groundMask;
        private readonly HitQuery _hits;

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
        public float NextAttackTime { get; set; }
        public DamageInfo LastDamage { get; set; }

        public Vector2 Position => _body.position;
        public Vector2 Velocity => _body.linearVelocity;
        public Bounds Bounds => _collider.bounds;

        public bool AttackReady => Time.time >= NextAttackTime;

        public void RefreshFrame()
        {
            var b = _collider.bounds;
            IsGrounded = Physics2D.BoxCast(new Vector2(b.center.x, b.min.y + 0.02f), new Vector2(b.size.x * 0.9f, 0.04f),
                0f, Vector2.down, GroundProbe, _groundMask);
        }

        // ---------- Player sensing ----------

        public float DistanceToPlayer => Player == null ? float.PositiveInfinity : Mathf.Abs(Player.position.x - Position.x);

        public int DirectionToPlayer => Player == null || Player.position.x >= Position.x ? 1 : -1;

        /// <summary>Within range, roughly the same height, and not behind a wall.</summary>
        public bool CanSeePlayer(float range)
        {
            if (Player == null)
                return false;
            Vector2 eye = new Vector2(Position.x, _collider.bounds.center.y);
            Vector2 target = new Vector2(Player.position.x, Player.position.y + 0.8f);
            if (Mathf.Abs(target.x - eye.x) > range || Mathf.Abs(Player.position.y - Position.y) > Data.MaxHeightDiff)
                return false;
            return !Physics2D.Linecast(eye, target, _groundMask);
        }

        // ---------- Movement ----------

        public void Face(int direction)
        {
            if (direction == 0)
                return;
            Facing = direction;
            Animator.Renderer.flipX = direction < 0;
        }

        public void FacePlayer() => Face(DirectionToPlayer);

        public void SetHorizontalVelocity(float x) => _body.linearVelocity = new Vector2(x, _body.linearVelocity.y);

        public void SetVelocity(Vector2 v) => _body.linearVelocity = v;

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

        public void SetPhysicsActive(bool active)
        {
            _body.simulated = active;
            _collider.enabled = active;
        }

        // ---------- Attacks ----------

        public void BeginSwing() => _hits.ResetSwing();

        /// <summary>Damages the player in a box in front of the enemy's centre. Returns hits.</summary>
        public int Strike(float reach, float height, int damage, float knockback, float stun)
        {
            var b = _collider.bounds;
            var center = new Vector2(b.center.x + Facing * reach * 0.5f, b.min.y + height * 0.5f);
            return _hits.Strike(center, new Vector2(reach, height), new DamageInfo(damage, Facing, knockback, stun, false, Owner));
        }
    }
}
