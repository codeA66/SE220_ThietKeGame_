using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.VFX
{
    /// <summary>
    /// Object pool for one-shot sprite effects. Instances are created in Awake (prewarm) and only
    /// grow if more effects than the prewarm count play at the same time.
    /// </summary>
    public class VfxSpawner : MonoBehaviour
    {
        [Tooltip("Effects to pre-pool at startup.")]
        [SerializeField] private VfxDefinition[] _prewarm;

        private readonly Dictionary<VfxDefinition, Stack<PooledVfx>> _pools = new();

        private void Awake()
        {
            foreach (var definition in _prewarm)
            {
                if (definition == null)
                    continue;
                var pool = GetPool(definition);
                for (int i = pool.Count; i < definition.PrewarmCount; i++)
                    pool.Push(Create(definition));
            }
        }

        public void Play(VfxDefinition definition, Vector3 position, bool flipX = false)
        {
            if (definition == null)
                return;
            var pool = GetPool(definition);
            var vfx = pool.Count > 0 ? pool.Pop() : Create(definition);
            vfx.Play(position, flipX);
        }

        private Stack<PooledVfx> GetPool(VfxDefinition definition)
        {
            if (!_pools.TryGetValue(definition, out var pool))
            {
                pool = new Stack<PooledVfx>(definition.PrewarmCount);
                _pools.Add(definition, pool);
            }
            return pool;
        }

        private PooledVfx Create(VfxDefinition definition)
        {
            var go = new GameObject(definition.name);
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            go.AddComponent<SpriteRenderer>();
            var vfx = go.AddComponent<PooledVfx>();
            vfx.Init(definition, Release);
            return vfx;
        }

        private void Release(PooledVfx vfx) => GetPool(vfx.Definition).Push(vfx);
    }
}
