using System;
using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Sweeps the travelled segment to avoid tunnelling. An embedded arrow returns on target death
    /// or expiry; a missed arrow returns when its flight lifetime ends. The pool owns ammunition.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RecoverableArrow : MonoBehaviour
    {
        private enum Phase { Inactive, Flying, WorldImpact, Embedded }

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
        public event Action<DamageInfo> HitDealt;

        public void Initialize(Action<RecoverableArrow> release) => _release = release;

        public void Launch(Vector2 origin, int direction, BowWeaponData data, BowWeaponData.Shot shot,
            LayerMask targets, LayerMask ground, GameObject owner)
        {
            DetachVictim();
            transform.SetParent(null, true);
            transform.position = origin;
            transform.right = Vector3.right * direction;
            _targets = targets;
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(targets.value | ground.value);
            _damage = new DamageInfo(shot.Damage, direction, 0f, 0f, false, owner, isProjectile: true);
            _velocity = Vector2.right * (direction * data.ProjectileSpeed);
            _remainingLifetime = data.FlightLifetime;
            _embeddedLifetime = data.EmbeddedLifetime;
            _phase = Phase.Flying;
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
            }
        }

        private void OnDestroy() => DetachVictim();

        private void OnVictimDied(DamageInfo damage) => Recall();

        private void DetachVictim()
        {
            if (_victim != null)
                _victim.Died -= OnVictimDied;
            _victim = null;
        }
    }
}
