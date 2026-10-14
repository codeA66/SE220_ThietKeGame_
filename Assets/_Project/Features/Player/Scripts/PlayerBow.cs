using System;
using System.Collections.Generic;
using DeadCells.Combat;
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
        private ObjectPool<RecoverableArrow> _pool;
        private LayerMask _targets;
        private LayerMask _ground;
        private int _nextShot;
        private float _comboExpiresAt = float.NegativeInfinity;
        private float _readyAt = float.NegativeInfinity;

        public BowWeaponData Data => _data;
        public int Ammo { get; private set; }
        public bool CanShoot => _pool != null && Ammo > 0 && Time.time >= _readyAt;
        public event Action<int, int> AmmoChanged;
        public event Action<DamageInfo> HitDealt;

        public void Initialize(LayerMask targets, LayerMask ground)
        {
            if (_pool != null)
                throw new InvalidOperationException("PlayerBow is already initialized.");
            if (_data == null || !_data.IsValid || _arrowPrefab == null)
                throw new InvalidOperationException("PlayerBow requires valid bow data and an arrow prefab.");
            _targets = targets;
            _ground = ground;
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
            return _data.GetShot(_nextShot);
        }

        public bool TryFire(Vector2 feet, int facing, bool crouching, BowWeaponData.Shot shot)
        {
            if (!CanShoot)
                return false;
            Vector2 offset = _data.GetMuzzleOffset(crouching);
            offset.x *= facing;
            var arrow = _pool.Get();
            _activeArrows.Add(arrow);
            Ammo--;
            _nextShot = (_nextShot + 1) % _data.ShotCount;
            _comboExpiresAt = Time.time + _data.ComboResetTime;
            _readyAt = Time.time + shot.Cooldown;
            arrow.Launch(feet + offset, facing, _data, shot, _targets, _ground, gameObject);
            AmmoChanged?.Invoke(Ammo, _data.AmmoCapacity);
            return true;
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
