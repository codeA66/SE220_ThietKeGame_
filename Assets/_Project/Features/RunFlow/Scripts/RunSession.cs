using System;
using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    public sealed class RunSession : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;

        public bool IsDefeated { get; private set; }
        public event Action Defeated;

        private void OnEnable() => _playerHealth.Died += OnPlayerDied;

        private void OnDisable()
        {
            if (_playerHealth != null)
                _playerHealth.Died -= OnPlayerDied;
        }

        private void OnPlayerDied(DamageInfo damage)
        {
            if (IsDefeated)
                return;

            IsDefeated = true;
            Defeated?.Invoke();
        }
    }
}
