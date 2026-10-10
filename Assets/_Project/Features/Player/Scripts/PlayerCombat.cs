using System;
using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Player-side combat helpers used by states: weapon strikes, the stomp impact, and the last hit taken.
    /// </summary>
    public class PlayerCombat
    {
        private readonly Transform _owner;
        private readonly BoxCollider2D _collider;
        private readonly HitQuery _query;

        public PlayerCombat(Transform owner, BoxCollider2D collider, WeaponData weapon, LayerMask targets)
        {
            _owner = owner;
            _collider = collider;
            Weapon = weapon;
            _query = new HitQuery(targets);
        }

        /// <summary>Arguments: number of targets hit, whether any hit was critical.</summary>
        public event Action<int, bool> StrikeHit;
        /// <summary>A combo hit lands (hit or miss). Arguments: strike, feet position, facing.</summary>
        public event Action<WeaponData.Strike, Vector2, int> StrikePerformed;

        public WeaponData Weapon { get; }
        public DamageInfo LastDamageTaken { get; set; }

        public void BeginSwing() => _query.ResetSwing();

        /// <summary>
        /// Resolves one combo hit in front of the player. The hit box starts at the player's centre
        /// (closest match to the Dead Cells swing effects; the original anchor is not verified).
        /// </summary>
        public int PerformStrike(in WeaponData.Strike strike, int facing)
        {
            var bounds = _collider.bounds;
            StrikePerformed?.Invoke(strike, new Vector2(bounds.center.x, bounds.min.y), facing);

            float height = strike.RangeHeight > 0f ? strike.RangeHeight : bounds.size.y;
            var size = new Vector2(strike.RangeWidth, height);
            var center = new Vector2(
                bounds.center.x + facing * (strike.RangeWidth * 0.5f + strike.RangeOffset.x),
                bounds.min.y + height * 0.5f + strike.RangeOffset.y);

            var info = new DamageInfo(strike.Damage, facing, strike.Knockback, 0f, false, _owner.gameObject);
            int hits = _query.Strike(center, size, info);
            if (hits > 0)
                StrikeHit?.Invoke(hits, info.IsCritical);
            return hits;
        }

        /// <summary>
        /// Stomp landing: damages and stuns everything in a box around the feet.
        /// </summary>
        public int PerformStomp(int damage, float stun, Vector2 area)
        {
            _query.ResetSwing();
            var bounds = _collider.bounds;
            var center = new Vector2(bounds.center.x, bounds.min.y + area.y * 0.5f);
            var info = new DamageInfo(damage, 0, 0f, stun, false, _owner.gameObject);
            int hits = _query.Strike(center, area, info);
            if (hits > 0)
                StrikeHit?.Invoke(hits, false);
            return hits;
        }
    }
}
