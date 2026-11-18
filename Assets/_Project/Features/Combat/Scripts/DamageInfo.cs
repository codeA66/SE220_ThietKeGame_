using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Everything a hit carries. Passed by value; no allocations.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        /// <summary>Horizontal push direction: -1 or 1 (0 = none, e.g. explosions and stomps).</summary>
        public readonly int Direction;
        /// <summary>Horizontal knockback speed applied to the target (units/s).</summary>
        public readonly float Knockback;
        /// <summary>Seconds the target is stunned.</summary>
        public readonly float Stun;
        public readonly bool IsCritical;
        public readonly GameObject Source;
        /// <summary>True for arrows, grenades and other projectiles (shields don't stagger their shooter).</summary>
        public readonly bool IsProjectile;

        public DamageInfo(int amount, int direction, float knockback, float stun, bool isCritical, GameObject source,
            bool isProjectile = false)
        {
            Amount = amount;
            Direction = direction;
            Knockback = knockback;
            Stun = stun;
            IsCritical = isCritical;
            Source = source;
            IsProjectile = isProjectile;
        }
    }
}
