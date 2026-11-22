using System;
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Pooled enemy projectile. Straight: flies until it hits a target or a wall (arrow).
    /// Lob: ballistic with gravity, bounces, explodes when the fuse runs out (grenade).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        public enum Mode { Straight, Lob }

        [SerializeField] private Mode _mode;
        [SerializeField] private SpriteRenderer _renderer;
        [Tooltip("Lob: blink colour during the last part of the fuse.")]
        [SerializeField] private Color _warnColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private float _maxLifetime = 4f;

        private const int MaxBlastResults = 16;

        private Rigidbody2D _body;
        private Action<Projectile> _release;
        private VfxSpawner _vfx;
        private readonly Collider2D[] _blastResults = new Collider2D[MaxBlastResults];
        private readonly HashSet<IDamageable> _blastHitTargets = new();
        private ContactFilter2D _blastFilter;
        private LayerMask _targets;
        private LayerMask _ground;
        private LayerMask _straightObstacles;
        private GameObject _owner;
        private int _damage;
        private float _knockback;
        private float _blastRadius;
        private VfxDefinition _blastVfx;
        private float _timer;
        private float _fuse;
        private float _fuseTimer;
        private bool _landed;
        private bool _live;
        private Color _baseColor;

        public Mode Kind => _mode;
        public bool IsLive => _live;

        public void Setup(Action<Projectile> release, VfxSpawner vfx, LayerMask targets, LayerMask ground)
        {
            int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
            int oneWayBit = oneWayLayer >= 0 ? (1 << oneWayLayer) : (1 << 9);
            LayerMask straightObstacles = ground.value & ~oneWayBit;
            Setup(release, vfx, targets, ground, straightObstacles);
        }

        public void Setup(Action<Projectile> release, VfxSpawner vfx, LayerMask targets, LayerMask ground, LayerMask straightObstacles)
        {
            _body = GetComponent<Rigidbody2D>();
            _release = release;
            _vfx = vfx;
            _targets = targets;
            _ground = ground;
            _straightObstacles = straightObstacles;
            _blastFilter = new ContactFilter2D { useTriggers = true };
            _blastFilter.SetLayerMask(targets);
            _baseColor = _renderer != null ? _renderer.color : Color.white;
            if (_renderer != null && _renderer.sortingLayerID == 0)
            {
                int vfxLayerId = SortingLayer.NameToID("VFX");
                if (vfxLayerId != 0)
                    _renderer.sortingLayerID = vfxLayerId;
            }
        }

        public void LaunchStraight(Vector2 position, Vector2 velocity, int damage, float knockback, GameObject owner)
        {
            Begin(position, velocity, damage, knockback, owner);
            _body.gravityScale = 0f;
            transform.right = velocity.sqrMagnitude > 0f ? (Vector3)velocity.normalized : Vector3.right;
        }

        public void LaunchLob(Vector2 position, Vector2 velocity, int damage, float fuse, float blastRadius,
            VfxDefinition blastVfx, GameObject owner)
        {
            Begin(position, velocity, damage, 0f, owner);
            _body.gravityScale = 1f;
            _fuse = fuse;
            _blastRadius = blastRadius;
            _blastVfx = blastVfx;
            transform.rotation = Quaternion.identity;
        }

        private void Begin(Vector2 position, Vector2 velocity, int damage, float knockback, GameObject owner)
        {
            transform.position = position;
            _damage = damage;
            _knockback = knockback;
            _owner = owner;
            _timer = 0f;
            _fuseTimer = 0f;
            _landed = false;
            _live = true;
            if (_renderer != null)
                _renderer.color = _baseColor;
            gameObject.SetActive(true);
            _body.linearVelocity = velocity;
            _body.angularVelocity = 0f;
        }

        private void Update()
        {
            if (!_live)
                return;
            _timer += Time.deltaTime;

            if (_mode == Mode.Lob)
            {
                // Wiki: bombs "detonate after a delay upon landing". Blink while the fuse burns.
                if (_landed)
                {
                    _fuseTimer += Time.deltaTime;
                    if (_renderer != null)
                        _renderer.color = Mathf.Repeat(_fuseTimer * 8f, 1f) < 0.5f ? _warnColor : _baseColor;
                    if (_fuseTimer >= _fuse)
                        Explode();
                }
                else if (_timer >= _maxLifetime)
                {
                    Explode();
                }
            }
            else if (_timer >= _maxLifetime)
            {
                Finish();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_live && _mode == Mode.Lob && (_ground.value & (1 << collision.gameObject.layer)) != 0)
                _landed = true;
        }

        private void FixedUpdate()
        {
            if (_live && _mode == Mode.Straight && _body.linearVelocity.sqrMagnitude > 0.01f)
                transform.right = _body.linearVelocity.normalized;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_live || _mode != Mode.Straight)
                return;

            int layerBit = 1 << other.gameObject.layer;
            if ((_targets.value & layerBit) != 0 && other.TryGetComponent<Hurtbox>(out var hurtbox) && hurtbox.Target != null)
            {
                int dir = _body.linearVelocity.x >= 0f ? 1 : -1;
                bool hit = hurtbox.Target.TakeDamage(new DamageInfo(_damage, dir, _knockback, 0f, false, _owner, isProjectile: true));
                if (hit)
                    Finish();
            }
            else if ((_straightObstacles.value & layerBit) != 0)
            {
                Finish();
            }
        }

        private void Explode()
        {
            _blastHitTargets.Clear();
            Vector2 center = transform.position;
            int count = Physics2D.OverlapCircle(center, _blastRadius, _blastFilter, _blastResults);
            for (int i = 0; i < count; i++)
            {
                if (!_blastResults[i].TryGetComponent<Hurtbox>(out var hurtbox))
                    continue;
                var target = hurtbox.Target;
                if (target == null || !target.IsAlive)
                    continue;
                Vector2 targetPos = _blastResults[i].bounds.center;
                if (Physics2D.Linecast(center, targetPos, _straightObstacles))
                    continue;
                if (!_blastHitTargets.Add(target))
                    continue;
                int dir = targetPos.x >= center.x ? 1 : -1;
                target.TakeDamage(new DamageInfo(_damage, dir, 5f, 0f, false, _owner, isProjectile: true));
            }
            if (_vfx != null)
                _vfx.Play(_blastVfx, center);
            Finish();
        }

        private void Finish()
        {
            if (!_live)
                return;
            _live = false;
            _body.linearVelocity = Vector2.zero;
            gameObject.SetActive(false);
            _release(this);
        }
    }
}
