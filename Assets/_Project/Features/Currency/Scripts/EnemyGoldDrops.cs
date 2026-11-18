using DeadCells.Combat;
using DeadCells.Enemies;
using UnityEngine;

namespace DeadCells.Currency
{
    // Superseded: enemies now drop coins and gems themselves through LootDropper (EnemyController.OnDied),
    // which credits DeadCells.Core.Wallet. This component no longer spawns GoldPickups, so kills do not drop
    // gold twice. It stays only so the copy in Main_Level keeps its serialized references; remove it from the
    // scene, then delete it, together with GoldPickup and its prefab.
    [DisallowMultipleComponent]
    public sealed class EnemyGoldDrops : MonoBehaviour
    {
        // Kept unused so the scene data still deserialises; silence the never-used warnings.
#pragma warning disable CS0169, CS0414
        [SerializeField] private EnemyController[] _enemies;
        [SerializeField] private GoldPickup _pickupPrefab;
        [SerializeField] private GoldWallet _wallet;
        [SerializeField] private Health _player;
        [SerializeField, Min(1)] private int _goldPerEnemy = 20;
#pragma warning restore CS0169, CS0414
    }
}
