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
        private readonly PlayerRunStats _stats;
        private readonly Action<IDamageable, DamageInfo> _onTargetHit;
        private bool _anyFatalInStrike;

        public PlayerCombat(Transform owner, BoxCollider2D collider, WeaponData weapon, LayerMask targets, PlayerRunStats stats, LayerMask solidObstructionMask = default)
        {
            _owner = owner;
            _collider = collider;
            MeleeWeapon = weapon;
            _query = new HitQuery(targets, solidObstructionMask);
            _stats = stats;
            _onTargetHit = (target, hitInfo) =>
            {
                if (target != null && !target.IsAlive)
                    _anyFatalInStrike = true;
                if (target is Health health)
                    TargetHit?.Invoke(health, hitInfo);
            };
        }

        /// <summary>Arguments: number of targets hit, whether any hit was critical.</summary>
        public event Action<int, bool> StrikeHit;
        /// <summary>Arguments: hits, isCritical, anyFatal, direction.</summary>
        public event Action<int, bool, bool, int> ImpactDealt;
        /// <summary>A combo hit lands (hit or miss). Arguments: strike, feet position, facing.</summary>
        public event Action<WeaponData.Strike, Vector2, int> StrikePerformed;
        /// <summary>Raised for each Health target damaged by a melee strike.</summary>
        public event Action<Health, DamageInfo> TargetHit;

        public WeaponData MeleeWeapon { get; }
        public DamageInfo LastDamageTaken { get; set; }

        public void BeginSwing() => _query.ResetSwing();

        /// <summary>
        /// Resolves one combo hit in front of the player. The hit box starts at the player's centre
        /// (closest match to the Dead Cells swing effects; the original anchor is not verified).
        /// The swing is re-resolved every frame of the multi-frame active window, so the swing event
        /// (slash VFX + sword whoosh) is opt-out: <c>HitQuery._alreadyHit</c> already stops a target
        /// taking damage twice, but the event would fire a fresh effect on every active frame.
        /// </summary>
        public int PerformStrike(in WeaponData.Strike strike, int facing, bool emitSwingEvent = true)
        {
            var bounds = _collider.bounds;
            if (emitSwingEvent)
                StrikePerformed?.Invoke(strike, new Vector2(bounds.center.x, bounds.min.y), facing);

            float height = strike.RangeHeight > 0f ? strike.RangeHeight : bounds.size.y;
            var size = new Vector2(strike.RangeWidth, height);
            var center = new Vector2(
                bounds.center.x + facing * (strike.RangeWidth * 0.5f + strike.RangeOffset.x),
                bounds.min.y + height * 0.5f + strike.RangeOffset.y);

            int damage = _stats.ScaleDamage(strike.Damage, MeleeWeapon.ScalingStat);
            var info = new DamageInfo(damage, facing, strike.Knockback, strike.Stun, strike.CanCrit, _owner.gameObject);
            _anyFatalInStrike = false;
            int hits = _query.Strike(center, size, info, _onTargetHit, lineOfSightOrigin: (Vector2)bounds.center);
            if (hits > 0)
            {
                StrikeHit?.Invoke(hits, info.IsCritical);
                ImpactDealt?.Invoke(hits, info.IsCritical, _anyFatalInStrike, facing);
            }
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
            int scaledDamage = _stats != null
                ? _stats.ScaleDamage(damage, MeleeWeapon != null ? MeleeWeapon.ScalingStat : CombatStat.Brutality)
                : damage;
            var info = new DamageInfo(scaledDamage, 0, 0f, stun, false, _owner.gameObject, isStomp: true);
            _anyFatalInStrike = false;
            int hits = _query.Strike(center, area, info, _onTargetHit, lineOfSightOrigin: new Vector2(bounds.center.x, bounds.min.y + 0.15f));
            if (hits > 0)
            {
                StrikeHit?.Invoke(hits, false);
                ImpactDealt?.Invoke(hits, false, _anyFatalInStrike, 0);
            }
            return hits;
        }
    }
}
