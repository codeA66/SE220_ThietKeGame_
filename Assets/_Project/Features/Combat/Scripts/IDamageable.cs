namespace DeadCells.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }

        /// <summary>
        /// Applies a hit. Returns true if it was taken (not blocked, dodged or ignored while invulnerable).
        /// </summary>
        bool TakeDamage(in DamageInfo info);
    }
}
