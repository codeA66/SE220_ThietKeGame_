using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>Spawns a DamagePopup above the enemy for every hit it takes, including the killing blow.</summary>
    public class EnemyDamageNumbers : MonoBehaviour
    {
        private Health _health;
        private Transform _anchor;
        private Vector2 _offset;

        public void Setup(Health health, BoxCollider2D collider)
        {
            _health = health;
            _anchor = transform;
            _offset = collider.offset + new Vector2(0f, collider.size.y * 0.5f);
            DamagePopup.Prewarm();
            _health.Damaged += OnHit;
            _health.Died += OnHit;
        }

        private void OnDestroy()
        {
            if (_health == null)
                return;
            _health.Damaged -= OnHit;
            _health.Died -= OnHit;
        }

        private void OnHit(DamageInfo info)
        {
            Vector3 position = _anchor.TransformPoint(_offset);
            DamagePopup.Spawn(position, info.Amount, info.Direction, info.IsCritical);
        }
    }
}
