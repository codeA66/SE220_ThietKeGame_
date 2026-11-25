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
            {
                _playerHealth.DamageFilter = BlockAllDamage;
                LockPlayerGameplay(_playerHealth.gameObject);
            }
            Completed?.Invoke();
            return true;
        }

        private static void LockPlayerGameplay(GameObject playerGo)
        {
            // Match by type name so RunFlow stays decoupled from Player assembly (no circular asmdef).
            var behaviours = playerGo.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;
                string typeName = behaviour.GetType().Name;
                if (typeName == "PlayerInputReader" || typeName == "PlayerController")
                    behaviour.enabled = false;
            }

            if (playerGo.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }
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
