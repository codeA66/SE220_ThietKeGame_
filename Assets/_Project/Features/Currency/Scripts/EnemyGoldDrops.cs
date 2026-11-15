using DeadCells.Combat;
using DeadCells.Enemies;
using UnityEngine;

namespace DeadCells.Currency
{
    [DisallowMultipleComponent]
    public sealed class EnemyGoldDrops : MonoBehaviour
    {
        [SerializeField] private EnemyController[] _enemies;
        [SerializeField] private GoldPickup _pickupPrefab;
        [SerializeField] private GoldWallet _wallet;
        [SerializeField] private Health _player;
        [SerializeField, Min(1)] private int _goldPerEnemy = 20;

        private void OnEnable()
        {
            foreach (var enemy in _enemies)
                if (enemy != null)
                    enemy.Died += Drop;
        }

        private void OnDisable()
        {
            foreach (var enemy in _enemies)
                if (enemy != null)
                    enemy.Died -= Drop;
        }

        private void Drop(EnemyController enemy)
        {
            Vector3 origin = enemy.transform.position + Vector3.up * 0.65f;
            var pickup = Instantiate(_pickupPrefab, origin, Quaternion.identity, transform);
            pickup.Launch(_wallet, _player, _goldPerEnemy);
        }
    }
}
