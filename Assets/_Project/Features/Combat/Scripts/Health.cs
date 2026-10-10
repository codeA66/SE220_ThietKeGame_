using System;
using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Hit points with a short invulnerability window after each hit (DC: OnHitGlobalShield 0.5 s).
    /// Owners can veto hits through DamageFilter (rolling, perfect dodge, shields...).
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Tooltip("Starting and maximum hit points. DC hero: HeroLife 100.")]
        [Min(1)]
        [SerializeField] private int _maxHealth = 100;
        [Tooltip("Seconds of invulnerability after taking a hit. DC: OnHitGlobalShield 0.5.")]
        [SerializeField] private float _invulnerabilityAfterHit = 0.5f;

        private float _invulnerableUntil = float.NegativeInfinity;

        /// <summary>Return false to ignore a hit before it is applied.</summary>
        public Func<DamageInfo, bool> DamageFilter { get; set; }

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        /// <summary>Arguments: current, max.</summary>
        public event Action<int, int> Changed;

        public int Current { get; private set; }
        public int Max => _maxHealth;
        public bool IsAlive => Current > 0;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        private void Awake() => Current = _maxHealth;

        public bool TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || IsInvulnerable || info.Amount <= 0)
                return false;
            if (DamageFilter != null && !DamageFilter(info))
                return false;

            Current = Mathf.Max(0, Current - info.Amount);
            _invulnerableUntil = Time.time + _invulnerabilityAfterHit;
            Changed?.Invoke(Current, _maxHealth);

            if (IsAlive)
                Damaged?.Invoke(info);
            else
                Died?.Invoke(info);
            return true;
        }

        public void RestoreFull()
        {
            Current = _maxHealth;
            _invulnerableUntil = float.NegativeInfinity;
            Changed?.Invoke(Current, _maxHealth);
        }
    }
}
