using System;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Sweeps the travelled segment to avoid tunnelling. An embedded arrow returns on target death
    /// or expiry; a missed arrow returns when its flight lifetime ends. The pool owns ammunition.
    /// Visuals: a streak trails the arrow in flight (gold for a heavy shot), and an arrow that sticks
    /// in a wall quivers for a moment.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RecoverableArrow : MonoBehaviour
    {
        private enum Phase { Inactive, Flying, WorldImpact, Embedded }

        private const float TrailDuration = 0.1f;
        private const float QuiverTime = 0.3f;
        private const float QuiverSpeed = 55f;
        private const float QuiverAngle = 7f;
        private const float Px = 1f / 32f;

        private static readonly Color32 TrailHead = new Color32(255, 245, 215, 255);
        private static readonly Color32 TrailTail = new Color32(255, 200, 120, 255);
        private static readonly Color32 HeavyTrailHead = new Color32(255, 236, 150, 255);
        private static readonly Color32 HeavyTrailTail = new Color32(255, 150, 40, 255);

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];
        private Action<RecoverableArrow> _release;
        private ContactFilter2D _filter;
        private LayerMask _targets;
        private DamageInfo _damage;
        private Health _victim;
        private Vector2 _victimOffset;
        private Vector2 _velocity;
        private float _remainingLifetime;
        private float _embeddedLifetime;
        private Phase _phase;
        private TrailRibbon _trail;
        private bool _isHeavy;
        private float _impactTime;
        private Quaternion _impactRotation;

        public event Action<DamageInfo> HitDealt;
        /// <summary>Hit a wall or floor. Arguments: impact point, flight direction (-1 or 1), heavy shot.</summary>
        public event Action<Vector2, int, bool> WorldHit;

        public void Initialize(Action<RecoverableArrow> release)
        {
            _release = release;
            _trail = gameObject.AddComponent<TrailRibbon>();
            _trail.Setup(TrailHead, TrailTail, 2f * Px, TrailDuration, GetComponent<SpriteRenderer>());
        }

        public void Launch(Vector2 origin, int direction, BowWeaponData data, int damage,
            LayerMask targets, LayerMask ground, GameObject owner, bool heavy = false)
        {
            DetachVictim();
            transform.SetParent(null, true);
            transform.position = origin;
            transform.right = Vector3.right * direction;
            _targets = targets;
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(targets.value | ground.value);
            _damage = new DamageInfo(damage, direction, 0f, 0f, false, owner, isProjectile: true);
            _velocity = Vector2.right * (direction * data.ProjectileSpeed);
            _remainingLifetime = data.FlightLifetime;
            _embeddedLifetime = data.EmbeddedLifetime;
            _phase = Phase.Flying;
            _isHeavy = heavy;
            _trail.Clear();
            if (heavy)
                _trail.SetStyle(HeavyTrailHead, HeavyTrailTail, 3.5f * Px);
            else
                _trail.SetStyle(TrailHead, TrailTail, 2f * Px);
            _trail.Emitting = true;
            gameObject.SetActive(true);
        }

        public void Recall()
        {
            if (_phase == Phase.Inactive)
                return;
            _phase = Phase.Inactive;
            DetachVictim();
            _release(this);
        }

        private void Update()
        {
            if (_phase == Phase.Inactive)
                return;
            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f || (_phase == Phase.Embedded && (_victim == null || !_victim.IsAlive)))
            {
                Recall();
                return;
            }
            if (_phase == Phase.Embedded)
                transform.position = (Vector2)_victim.transform.position + _victimOffset;
            else if (_phase == Phase.WorldImpact && _impactTime < QuiverTime)
                Quiver();
        }

        private void FixedUpdate()
        {
            if (_phase != Phase.Flying)
                return;

            Vector2 origin = transform.position;
            Vector2 movement = _velocity * Time.fixedDeltaTime;
            int count = Physics2D.Raycast(origin, movement.normalized, _filter, _hits, movement.magnitude);
            int nearest = -1;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].collider.gameObject == _damage.Source || _hits[i].distance >= nearestDistance)
                    continue;
                nearest = i;
                nearestDistance = _hits[i].distance;
            }

            if (nearest < 0)
            {
                transform.position = origin + movement;
                return;
            }

            var hit = _hits[nearest];
            transform.position = hit.point;
            _trail.Emitting = false;
            if ((_targets.value & (1 << hit.collider.gameObject.layer)) != 0
                && hit.collider.TryGetComponent<Hurtbox>(out var hurtbox) && hurtbox.Target is Health health)
            {
                bool damaged = health.TakeDamage(_damage);
                if (damaged)
                    HitDealt?.Invoke(_damage);
                if (!health.IsAlive)
                {
                    Recall();
                    return;
                }
                _victim = health;
                _victimOffset = (Vector2)transform.position - (Vector2)health.transform.position;
                _victim.Died += OnVictimDied;
                _remainingLifetime = _embeddedLifetime;
                _phase = Phase.Embedded;
            }
            else
            {
                _phase = Phase.WorldImpact;
                _impactTime = 0f;
                _impactRotation = transform.rotation;
                WorldHit?.Invoke(hit.point, _velocity.x >= 0f ? 1 : -1, _isHeavy);
            }
        }

        private void OnDestroy() => DetachVictim();

        // Wobbles around the stuck angle, dying out over QuiverTime.
        private void Quiver()
        {
            _impactTime += Time.deltaTime;
            float strength = 1f - Mathf.Clamp01(_impactTime / QuiverTime);
            float angle = Mathf.Sin(_impactTime * QuiverSpeed) * QuiverAngle * strength;
            transform.rotation = _impactRotation * Quaternion.Euler(0f, 0f, angle);
        }

        private void OnVictimDied(DamageInfo damage) => Recall();

        private void DetachVictim()
        {
            if (_victim != null)
                _victim.Died -= OnVictimDied;
            _victim = null;
        }
    }
}
