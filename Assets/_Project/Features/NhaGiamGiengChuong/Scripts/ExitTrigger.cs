using System;
using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Exit arch trigger (I5) in Room F at center (5, 33), size (2, 2).
    /// Submits a win candidate only for the assigned living Player's main collider when Playing and I4 is open.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ExitTrigger : MonoBehaviour
    {
        [SerializeField] private LevelRunController _run;
        [SerializeField] private EncounterGate _gate;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;

        public LevelRunController Run => _run;
        public EncounterGate Gate => _gate;
        public PlayerController Player => _player;

        public event Action<int> WinCandidateSubmitted;

        public void Configure(
            LevelRunController run,
            EncounterGate gate,
            PlayerController player,
            Health playerHealth = null,
            BoxCollider2D playerCollider = null)
        {
            _run = run;
            _gate = gate;
            _player = player;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TrySubmitForCollider(other, Time.frameCount);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TrySubmitForCollider(other, Time.frameCount);
        }

        /// <summary>
        /// Real contact handler for I5. Validates exact main Player collider, alive Health, Playing phase, and open I4.
        /// </summary>
        public bool TrySubmitForCollider(Collider2D candidateCollider, int frame)
        {
            if (candidateCollider == null)
                return false;

            if (_run == null || !_run.IsPlaying)
                return false;

            if (_gate == null || !_gate.IsOpen)
                return false;

            var expectedCollider = ResolvePlayerCollider();
            if (expectedCollider == null || candidateCollider != expectedCollider)
                return false;

            var health = ResolvePlayerHealth();
            if (health == null || !health.IsAlive)
                return false;

            if (!_run.RequestWin(frame))
                return false;

            WinCandidateSubmitted?.Invoke(frame);
            return true;
        }

        private PlayerController ResolvePlayer() =>
            _player != null ? _player : (_run != null ? _run.Player : null);

        private BoxCollider2D ResolvePlayerCollider()
        {
            if (_playerCollider != null)
                return _playerCollider;
            var p = ResolvePlayer();
            return p != null ? p.GetComponent<BoxCollider2D>() : null;
        }

        private Health ResolvePlayerHealth()
        {
            if (_playerHealth != null)
                return _playerHealth;
            var p = ResolvePlayer();
            if (p != null)
                return p.Health != null ? p.Health : p.GetComponent<Health>();
            return _run != null ? _run.PlayerHealth : null;
        }
    }
}
