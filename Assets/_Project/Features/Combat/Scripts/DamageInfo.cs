using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Everything a hit carries. Passed by value; no allocations.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        /// <summary>Horizontal push direction: -1 or 1.</summary>
        public readonly int Direction;
        /// <summary>Horizontal knockback speed applied to the target (units/s).</summary>
        public readonly float Knockback;
        /// <summary>Seconds the target is stunned.</summary>
        public readonly float Stun;
        public readonly bool IsCritical;
        public readonly GameObject Source;

        public DamageInfo(int amount, int direction, float knockback, float stun, bool isCritical, GameObject source)
        {
            Amount = amount;
            Direction = direction;
            Knockback = knockback;
            Stun = stun;
            IsCritical = isCritical;
            Source = source;
        }
    }
}
