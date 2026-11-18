using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Enemies
{
    // Hit spark on the side the blow came from, and a smoke puff when the enemy dies.
    // The spawner is read from the context at play time because EnemyController wires it after Awake.
    public class EnemyVfx : MonoBehaviour
    {
        private const float HitSideOffset = 0.6f;

        private Health _health;
        private EnemyContext _context;
        private VfxDefinition _hitSpark;
        private VfxDefinition _deathPuff;

        public void Setup(Health health, EnemyContext context)
        {
            _health = health;
            _context = context;
            _hitSpark = GeneratedVfx.HitSpark;
            _deathPuff = GeneratedVfx.DeathPuff;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health == null)
                return;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void OnDamaged(DamageInfo info) => PlayHit(info);

        private void OnDied(DamageInfo info)
        {
            PlayHit(info);
            if (_context.Vfx != null)
                _context.Vfx.Play(_deathPuff, _context.Bounds.center);
        }

        private void PlayHit(DamageInfo info)
        {
            if (_context.Vfx == null)
                return;
            Bounds bounds = _context.Bounds;
            // Direction is the way the target is pushed, so the blow came from the opposite side.
            Vector3 position = bounds.center + new Vector3(-info.Direction * bounds.extents.x * HitSideOffset, 0f, 0f);
            _context.Vfx.Play(_hitSpark, position);
        }
    }
}
