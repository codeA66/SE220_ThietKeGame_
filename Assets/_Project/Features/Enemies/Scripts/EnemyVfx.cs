using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Enemies
{
    // Hit spark on the side the blow came from; the smoke puff plays when the dead enemy vanishes.
    // The spawner is read from the context at play time because EnemyController wires it after Awake.
    // Positions come from the collider's offset and size, not its bounds: a dead enemy's collider is
    // switched off, and a disabled collider reports empty bounds.
    public class EnemyVfx : MonoBehaviour
    {
        private const float HitSideOffset = 0.6f;
        // Each hit flare is tilted by a random angle up to this many degrees either way around the line
        // of the blow, so it always follows the attack direction but no two hits look the same.
        private const float MaxHitTilt = 25f;
        // Crit kills fan four extra heavy bursts across this many degrees either side of the gore burst.
        private const float GoreScatter = 35f;

        private Health _health;
        private EnemyContext _context;
        private BoxCollider2D _collider;
        private VfxDefinition _hitSpark;
        private VfxDefinition _critSpark;
        private VfxDefinition _bloodMist;
        private VfxDefinition _deathPuff;

        public void Setup(Health health, EnemyContext context, BoxCollider2D collider)
        {
            _health = health;
            _context = context;
            _collider = collider;
            _hitSpark = GeneratedVfx.HitSpark;
            _critSpark = GeneratedVfx.CritSpark;
            _bloodMist = GeneratedVfx.BloodMist;
            _deathPuff = GeneratedVfx.DeathPuff;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            _context.Vanished += OnVanished;
        }

        private void OnDestroy()
        {
            if (_health == null)
                return;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
            _context.Vanished -= OnVanished;
        }

        private void OnDamaged(DamageInfo info) => PlayHit(info, false);

        // The killing blow always gets the heavy spray.
        private void OnDied(DamageInfo info) => PlayHit(info, true);

        private void OnVanished()
        {
            if (_context.Vfx != null)
                _context.Vfx.Play(_deathPuff, KeepAboveGround(Center(), GeneratedVfx.DeathPuffExtentBelow));
        }

        private void PlayHit(DamageInfo info, bool killingBlow)
        {
            if (_context.Vfx == null)
                return;
            // Direction is the way the target is pushed, so the blow came from the opposite side.
            float halfWidth = _collider.size.x * 0.5f * Mathf.Abs(transform.lossyScale.x);
            Vector3 position = Center() + new Vector3(-info.Direction * halfWidth * HitSideOffset, 0f, 0f);

            float tilt = Random.Range(-MaxHitTilt, MaxHitTilt);
            if (info.IsCritical)
                _context.Vfx.Play(_critSpark, KeepAboveGround(position, GeneratedVfx.CritSparkExtentBelow), info.Direction < 0, tilt);
            else
                _context.Vfx.Play(_hitSpark, KeepAboveGround(position, GeneratedVfx.HitSparkExtentBelow), info.Direction < 0, tilt);

            // The flare and the blood both go the way the enemy is pushed. Blood drops are real
            // particles that land on floors and run down walls.
            LayerMask ground = _context.GroundMask;
            if (info.IsCritical && killingBlow)
            {
                // Crit kill: a gore burst plus four heavy bursts fanned across the cone, and a blood mist.
                BloodDrops.Emit(position, info.Direction, BloodBurst.Gore, ground, Random.Range(-10f, 10f));
                for (int i = 0; i < 4; i++)
                {
                    float from = Mathf.Lerp(-GoreScatter, GoreScatter, i / 4f);
                    float to = Mathf.Lerp(-GoreScatter, GoreScatter, (i + 1) / 4f);
                    BloodDrops.Emit(position, info.Direction, BloodBurst.Heavy, ground, Random.Range(from, to));
                }
                _context.Vfx.Play(_bloodMist, position, info.Direction < 0, Random.Range(-10f, 10f));
                return;
            }
            bool heavy = info.IsCritical || killingBlow;
            BloodDrops.Emit(position, info.Direction, heavy ? BloodBurst.Heavy : BloodBurst.Light, ground);
        }

        private Vector3 Center() => transform.TransformPoint(_collider.offset);

        // Lifts the effect when the floor is closer than the effect reaches below its centre, but lets
        // GroundSink of that reach dip into the floor so it looks planted instead of floating.
        private Vector3 KeepAboveGround(Vector3 position, float extentBelow)
        {
            float reach = extentBelow * (1f - GeneratedVfx.GroundSink);
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, reach, _context.GroundMask);
            if (hit.collider != null)
                position.y += reach - hit.distance;
            return position;
        }
    }
}
