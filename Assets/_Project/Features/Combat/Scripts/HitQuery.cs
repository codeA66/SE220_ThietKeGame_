using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Allocation-free box hit test: finds Hurtboxes in an area and damages each target once per swing.
    /// One instance per attacker; buffers are reused.
    /// </summary>
    public class HitQuery
    {
        private const int MaxResults = 16;

        private readonly Collider2D[] _results = new Collider2D[MaxResults];
        private readonly HashSet<IDamageable> _alreadyHit = new();
        private readonly LayerMask _solidObstructionMask;
        private ContactFilter2D _filter;

        public HitQuery(LayerMask targets, LayerMask solidObstructionMask = default)
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(targets);
            _solidObstructionMask = solidObstructionMask;
        }

        /// <summary>Call at the start of each swing so targets can be hit again.</summary>
        public void ResetSwing() => _alreadyHit.Clear();

        /// <summary>Returns how many targets took the hit.</summary>
        public int Strike(Vector2 center, Vector2 size, in DamageInfo info, Action<IDamageable, DamageInfo> onHit = null, Vector2? lineOfSightOrigin = null)
        {
            int count = Physics2D.OverlapBox(center, size, 0f, _filter, _results);
            int hits = 0;
            for (int i = 0; i < count; i++)
            {
                if (!_results[i].TryGetComponent<Hurtbox>(out var hurtbox))
                    continue;
                var target = hurtbox.Target;
                if (target == null || !target.IsAlive || _alreadyHit.Contains(target))
                    continue;
                if (lineOfSightOrigin.HasValue && _solidObstructionMask.value != 0)
                {
                    var obstruction = Physics2D.Linecast(lineOfSightOrigin.Value, _results[i].bounds.center, _solidObstructionMask);
                    if (obstruction.collider != null && obstruction.collider != _results[i])
                        continue;
                }
                _alreadyHit.Add(target);
                if (target.TakeDamage(info))
                {
                    hits++;
                    onHit?.Invoke(target, info);
                }
            }
            return hits;
        }
    }
}
