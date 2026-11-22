using System;
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Core.Audio;
using UnityEngine;
using UnityEngine.Pool;

namespace DeadCells.Player
{
    /// <summary>Owns the bow's ammunition, shot sequence and pooled arrows. Charging lives in the FSM.</summary>
    public class PlayerBow : MonoBehaviour
    {
        [SerializeField] private BowWeaponData _data;
        [SerializeField] private RecoverableArrow _arrowPrefab;

        private readonly List<RecoverableArrow> _activeArrows = new();
        private readonly Collider2D[] _aimResults = new Collider2D[16];
        private ContactFilter2D _aimFilter;
        private ObjectPool<RecoverableArrow> _pool;
        private PlayerRunStats _stats;
        private LayerMask _targets;
        private LayerMask _ground;
        private LayerMask _solidGround;
        private int _nextShot;
        private float _comboExpiresAt = float.NegativeInfinity;
        private float _readyAt = float.NegativeInfinity;

        public BowWeaponData Data => _data;
        public int Ammo { get; private set; }
        public bool CanShoot => _pool != null && Ammo > 0 && Time.time >= _readyAt;
        public event Action<int, int> AmmoChanged;
        public event Action<DamageInfo> HitDealt;

        public void Initialize(LayerMask targets, LayerMask ground, PlayerRunStats stats)
        {
            if (_pool != null)
                throw new InvalidOperationException("PlayerBow is already initialized.");
            if (_data == null || !_data.IsValid || _arrowPrefab == null)
                throw new InvalidOperationException("PlayerBow requires valid bow data and an arrow prefab.");
            _targets = targets;
            _ground = ground;
            int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
            int oneWayBit = oneWayLayer >= 0 ? (1 << oneWayLayer) : (1 << 9);
            _solidGround = ground.value & ~oneWayBit;
            _aimFilter = new ContactFilter2D { useTriggers = true };
            _aimFilter.SetLayerMask(targets);
            _stats = stats;
            _pool = new ObjectPool<RecoverableArrow>(CreateArrow, null, ReleaseArrow, DestroyArrow,
                collectionCheck: true, defaultCapacity: _data.AmmoCapacity, maxSize: _data.AmmoCapacity);
            Ammo = _data.AmmoCapacity;
            // Prewarm without firing or reserving ammunition.
            var arrows = new RecoverableArrow[_data.AmmoCapacity];
            for (int i = 0; i < arrows.Length; i++)
                arrows[i] = _pool.Get();
            foreach (var arrow in arrows)
                _pool.Release(arrow);
        }

        public BowWeaponData.Shot GetNextShot()
        {
            if (Time.time > _comboExpiresAt)
                _nextShot = 0;
            CombatAudioHub.Play(SfxId.BowDraw, 0.55f);
            return _data.GetShot(_nextShot);
        }

        public bool TryFire(Vector2 feet, int facing, bool crouching, BowWeaponData.Shot shot)
        {
            if (!CanShoot)
                return false;
            Vector2 offset = _data.GetMuzzleOffset(crouching);
            offset.x *= facing;
            Vector2 origin = feet + offset;
            Vector2 aimDir = ResolveAimDirection(origin, feet, facing, crouching);
            var arrow = _pool.Get();
            _activeArrows.Add(arrow);
            Ammo--;
            _nextShot = (_nextShot + 1) % _data.ShotCount;
            _comboExpiresAt = Time.time + _data.ComboResetTime;
            _readyAt = Time.time + shot.Cooldown;
            int damage = _stats.ScaleDamage(shot.Damage, _data.ScalingStat);
            arrow.Launch(origin, aimDir, _data, damage, _targets, _ground, gameObject);
            CombatAudioHub.Play(SfxId.BowRelease, 0.80f);
            AmmoChanged?.Invoke(Ammo, _data.AmmoCapacity);
            return true;
        }

        private Vector2 ResolveAimDirection(Vector2 origin, Vector2 feet, int facing, bool crouching)
        {
            Vector2 straight = Vector2.right * facing;
            if (crouching)
                return straight;

            int count = Physics2D.OverlapBox(
                new Vector2(feet.x + facing * 5.0f, feet.y + 0.65f),
                new Vector2(9.6f, 1.3f),
                0f,
                _aimFilter,
                _aimResults);

            Collider2D bestLowTarget = null;
            float bestDistSq = float.PositiveInfinity;
            float minY = origin.y - 0.14f;
            float maxY = origin.y + 0.14f;

            for (int i = 0; i < count; i++)
            {
                var col = _aimResults[i];
                if (col == null || !col.TryGetComponent<Hurtbox>(out var hurtbox) || hurtbox.Target == null || !hurtbox.Target.IsAlive)
                    continue;

                var bounds = col.bounds;
                if ((bounds.center.x - origin.x) * facing <= 0.2f)
                    continue;

                if (Physics2D.Linecast(origin, bounds.center, _solidGround))
                    continue;

                if (bounds.max.y >= minY && bounds.min.y <= maxY)
                    return straight;

                if (bounds.max.y < minY)
                {
                    float distSq = ((Vector2)bounds.center - origin).sqrMagnitude;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestLowTarget = col;
                    }
                }
            }

            if (bestLowTarget != null)
                return ((Vector2)bestLowTarget.bounds.center - origin).normalized;

            return straight;
        }

        public void ResetShots()
        {
            if (_pool == null)
                return;
            while (_activeArrows.Count > 0)
            {
                int last = _activeArrows.Count - 1;
                // Flying arrows live at the scene root, so on scene unload they can be destroyed before this runs.
                if (_activeArrows[last] == null)
                {
                    _activeArrows.RemoveAt(last);
                    continue;
                }
                _activeArrows[last].Recall();
            }
            _nextShot = 0;
            _comboExpiresAt = float.NegativeInfinity;
            _readyAt = float.NegativeInfinity;
            Ammo = _data.AmmoCapacity;
            AmmoChanged?.Invoke(Ammo, _data.AmmoCapacity);
        }

        public int ExtractArrowsFrom(Health victim)
        {
            if (victim == null || _activeArrows.Count == 0)
                return 0;
            int extracted = 0;
            for (int i = _activeArrows.Count - 1; i >= 0; i--)
            {
                var arrow = _activeArrows[i];
                if (arrow != null && arrow.IsEmbeddedIn(victim))
                {
                    arrow.Recall();
                    extracted++;
                }
            }
            return extracted;
        }

        private void OnDisable() => ResetShots();

        private void OnDestroy() => _pool?.Dispose();

        private RecoverableArrow CreateArrow()
        {
            var arrow = Instantiate(_arrowPrefab, transform);
            arrow.gameObject.SetActive(false);
            arrow.Initialize(OnArrowReturned);
            arrow.HitDealt += OnArrowHit;
            return arrow;
        }

        private void OnArrowHit(DamageInfo damage) => HitDealt?.Invoke(damage);

        private void OnArrowReturned(RecoverableArrow arrow)
        {
            if (!_activeArrows.Remove(arrow))
                return;
            Ammo++;
            _pool.Release(arrow);
            AmmoChanged?.Invoke(Ammo, _data.AmmoCapacity);
        }

        private void ReleaseArrow(RecoverableArrow arrow)
        {
            arrow.gameObject.SetActive(false);
            // Unity forbids re-parenting while this object is being deactivated (OnDisable -> ResetShots).
            // The arrow stays hidden at the root; Launch detaches it from any parent anyway.
            if (gameObject.activeInHierarchy)
                arrow.transform.SetParent(transform, false);
        }

        private void DestroyArrow(RecoverableArrow arrow) => Destroy(arrow.gameObject);
    }
}
