using System.Collections.Generic;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Scene-level pool for enemy projectiles, one stack per prefab. Prewarmed so firing never allocates.
    /// </summary>
    public class ProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private Projectile[] _prewarm;
        [SerializeField] private int _prewarmCount = 6;
        [SerializeField] private VfxSpawner _vfx;
        [Tooltip("Layers projectiles can damage (the player).")]
        [SerializeField] private LayerMask _targets;
        [Tooltip("Layers that stop arrows and that grenades bounce on.")]
        [SerializeField] private LayerMask _ground;

        private readonly Dictionary<Projectile, Stack<Projectile>> _pools = new();
        private readonly Dictionary<Projectile, Projectile> _prefabOf = new();

        private void Awake()
        {
            foreach (var prefab in _prewarm)
            {
                if (prefab == null)
                    continue;
                var pool = GetPool(prefab);
                for (int i = pool.Count; i < _prewarmCount; i++)
                    pool.Push(Create(prefab));
            }
        }

        public Projectile Get(Projectile prefab)
        {
            var pool = GetPool(prefab);
            return pool.Count > 0 ? pool.Pop() : Create(prefab);
        }

        private Stack<Projectile> GetPool(Projectile prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<Projectile>(_prewarmCount);
                _pools.Add(prefab, pool);
            }
            return pool;
        }

        private Projectile Create(Projectile prefab)
        {
            var p = Instantiate(prefab, transform);
            p.gameObject.SetActive(false);
            _prefabOf.Add(p, prefab);
            p.Setup(Release, _vfx, _targets, _ground);
            return p;
        }

        private void Release(Projectile p) => GetPool(_prefabOf[p]).Push(p);
    }
}
