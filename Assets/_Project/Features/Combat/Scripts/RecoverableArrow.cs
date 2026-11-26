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

        public const float WallImpactLifetime = 0.6f;
        private static readonly Vector2 TargetBoxSize = new Vector2(0.24f, 0.28f);

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
        private ContactFilter2D _groundFilter;
        private ContactFilter2D _targetFilter;
        private DamageInfo _damage;
        private Health _victim;
        private SpriteRenderer _victimSprite;
        private bool _victimInitialFlipX;
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
            int vfxLayerId = SortingLayer.NameToID("VFX");
            if (vfxLayerId != 0)
            {
                foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr != null && sr.sortingLayerID == 0)
                        sr.sortingLayerID = vfxLayerId;
                }
            }
            if (_trail == null)
            {
                _trail = gameObject.AddComponent<TrailRibbon>();
                _trail.Setup(TrailHead, TrailTail, 2f * Px, TrailDuration, GetComponent<SpriteRenderer>());
            }
        }

        public void Launch(Vector2 origin, int direction, BowWeaponData data, int damage,
            LayerMask targets, LayerMask ground, GameObject owner, bool heavy = false) =>
            Launch(origin, Vector2.right * direction, data, damage, targets, ground, owner, heavy);

        public void Launch(Vector2 origin, Vector2 direction, BowWeaponData data, int damage,
            LayerMask targets, LayerMask ground, GameObject owner, bool heavy = false)
        {
            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            int hitDirection = dir.x >= 0f ? 1 : -1;

            DetachVictim();
            transform.SetParent(null, true);
            transform.position = origin;
            transform.right = new Vector3(dir.x, dir.y, 0f);

            int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
            int oneWayBit = oneWayLayer >= 0 ? (1 << oneWayLayer) : (1 << 9);
            LayerMask solidGround = ground.value & ~oneWayBit;

            _groundFilter = new ContactFilter2D { useTriggers = false };
            _groundFilter.SetLayerMask(solidGround);
            _targetFilter = new ContactFilter2D { useTriggers = true };
            _targetFilter.SetLayerMask(targets);

            _damage = new DamageInfo(damage, hitDirection, 0f, 0f, false, owner, isProjectile: true);
            _velocity = dir * data.ProjectileSpeed;
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

        public bool IsEmbeddedIn(Health victim) => _phase == Phase.Embedded && victim != null && _victim == victim;

        public void Recall()
        {
            if (_phase == Phase.Inactive)
                return;
            _phase = Phase.Inactive;
            if (_trail != null)
                _trail.Emitting = false;
            DetachVictim();
            _release(this);
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (_phase == Phase.Inactive)
                return;
            _remainingLifetime -= deltaTime;
            if (_remainingLifetime <= 0f || (_phase == Phase.Embedded && (_victim == null || !_victim.IsAlive || !_victim.gameObject.activeInHierarchy)))
            {
                Recall();
                return;
            }
            if (_phase == Phase.Embedded)
            {
                bool flipped = _victimSprite != null && _victimSprite.flipX != _victimInitialFlipX;
                Vector2 offset = flipped ? new Vector2(-_victimOffset.x, _victimOffset.y) : _victimOffset;
                transform.position = (Vector2)_victim.transform.position + offset;
            }
            else if (_phase == Phase.WorldImpact && _impactTime < QuiverTime)
                Quiver(deltaTime);
        }

        private void FixedUpdate()
        {
            if (_phase != Phase.Flying)
                return;

            Vector2 origin = transform.position;
            Vector2 movement = _velocity * Time.fixedDeltaTime;
            float dist = movement.magnitude;
            if (dist <= 0.00001f)
                return;
            Vector2 dir = movement / dist;

            int groundCount = Physics2D.Raycast(origin, dir, _groundFilter, _hits, dist);
            bool hasGroundHit = false;
            RaycastHit2D nearestGround = default;
            float nearestGroundDist = float.PositiveInfinity;
            for (int i = 0; i < groundCount; i++)
            {
                if (_hits[i].collider == null || _hits[i].collider.gameObject == _damage.Source || _hits[i].distance >= nearestGroundDist)
                    continue;
                hasGroundHit = true;
                nearestGround = _hits[i];
                nearestGroundDist = _hits[i].distance;
            }

            int targetCount = Physics2D.BoxCast(origin, TargetBoxSize, 0f, dir, _targetFilter, _hits, dist);
            bool hasTargetHit = false;
            RaycastHit2D nearestTarget = default;
            Health nearestHealth = null;
            float nearestTargetDist = nearestGroundDist;
            for (int i = 0; i < targetCount; i++)
            {
                if (_hits[i].collider == null || _hits[i].collider.gameObject == _damage.Source)
                    continue;
                float hitDist = _hits[i].distance;
                if (hasTargetHit ? hitDist >= nearestTargetDist : hitDist > nearestGroundDist)
                    continue;
                if (!_hits[i].collider.TryGetComponent<Hurtbox>(out var hurtbox) || hurtbox.Target is not Health health)
                    continue;
                hasTargetHit = true;
                nearestTarget = _hits[i];
                nearestHealth = health;
                nearestTargetDist = hitDist;
            }

            if (hasTargetHit)
            {
                transform.position = nearestTarget.distance > 0f ? nearestTarget.point : origin;
                if (_trail != null)
                    _trail.Emitting = false;
                bool damaged = nearestHealth.TakeDamage(_damage);
                if (!damaged || !nearestHealth.IsAlive)
                {
                    if (damaged)
                        HitDealt?.Invoke(_damage);
                    Recall();
                    return;
                }

                HitDealt?.Invoke(_damage);
                _victim = nearestHealth;
                _victimSprite = nearestHealth.GetComponentInChildren<SpriteRenderer>();
                _victimInitialFlipX = _victimSprite != null && _victimSprite.flipX;
                _victimOffset = (Vector2)transform.position - (Vector2)nearestHealth.transform.position;
                _victim.Died += OnVictimDied;
                _remainingLifetime = _embeddedLifetime;
                _phase = Phase.Embedded;
            }
            else if (hasGroundHit)
            {
                transform.position = nearestGround.point;
                if (_trail != null)
                    _trail.Emitting = false;
                _remainingLifetime = Mathf.Min(_remainingLifetime, WallImpactLifetime);
                _phase = Phase.WorldImpact;
                _impactTime = 0f;
                _impactRotation = transform.rotation;
                WorldHit?.Invoke(nearestGround.point, _velocity.x >= 0f ? 1 : -1, _isHeavy);
            }
            else
            {
                transform.position = origin + movement;
            }
        }

        private void OnDestroy() => DetachVictim();

        // Wobbles around the stuck angle, dying out over QuiverTime.
        private void Quiver(float deltaTime)
        {
            _impactTime += deltaTime;
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
            _victimSprite = null;
            _victimInitialFlipX = false;
        }
    }
}
