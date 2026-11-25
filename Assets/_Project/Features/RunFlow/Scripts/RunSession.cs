using System;
using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    public sealed class RunSession : MonoBehaviour
    {
        private static readonly Func<DamageInfo, bool> BlockAllDamage = static _ => false;

        [SerializeField] private Health _playerHealth;

        public bool IsDefeated { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsEnded => IsDefeated || IsCompleted;

        public event Action Defeated;
        public event Action Completed;

        private void OnEnable()
        {
            if (_playerHealth != null)
                _playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Died -= OnPlayerDied;
                if (ReferenceEquals(_playerHealth.DamageFilter, BlockAllDamage))
                    _playerHealth.DamageFilter = null;
            }
        }

        public bool CompleteRun()
        {
            if (IsEnded || (_playerHealth != null && !_playerHealth.IsAlive))
                return false;

            IsCompleted = true;
            if (_playerHealth != null)
                _playerHealth.DamageFilter = BlockAllDamage;
            Completed?.Invoke();
            return true;
        }

        private void OnPlayerDied(DamageInfo damage)
        {
            if (IsEnded)
                return;

            IsDefeated = true;
            Defeated?.Invoke();
        }
    }
}
