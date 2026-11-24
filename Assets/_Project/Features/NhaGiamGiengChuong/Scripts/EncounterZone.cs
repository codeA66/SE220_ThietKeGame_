using System;
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Keeps assigned enemies visible at spawn yet dormant and damage-immune until the living assigned
    /// Player's feet enter the inclusive [MinBounds, MaxBounds] rect during Playing phase.
    /// Opens at most once per run; preserves each enemy's original Health.DamageFilter (e.g. Shield).
    /// </summary>
    public sealed class EncounterZone : MonoBehaviour
    {
        [SerializeField] private string _zoneId;
        [SerializeField] private Vector2 _minBounds;
        [SerializeField] private Vector2 _maxBounds;
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private EnemyController[] _enemies = Array.Empty<EnemyController>();
        [SerializeField] private bool _sleepEnemiesOnStart = true;

        private Func<DamageInfo, bool>[] _savedDamageFilters;
        private bool _asleep;
        private bool _activated;

        public string ZoneId => _zoneId;
        public Vector2 MinBounds => _minBounds;
        public Vector2 MaxBounds => _maxBounds;
        public bool IsActivated => _activated;
        public bool IsAsleep => _asleep;
        public IReadOnlyList<EnemyController> Enemies => _enemies;

        public event Action<EncounterZone> ZoneActivated;
        public event Action<EnemyController> EnemyWoken;

        public void Configure(
            string zoneId,
            Vector2 minBounds,
            Vector2 maxBounds,
            LevelRunController run,
            PlayerController player,
            BoxCollider2D playerCollider,
            EnemyController[] enemies,
            Health playerHealth = null)
        {
            _zoneId = zoneId;
            _minBounds = minBounds;
            _maxBounds = maxBounds;
            _run = run;
            _player = player;
            _playerCollider = playerCollider;
            _playerHealth = playerHealth;
            _enemies = enemies ?? Array.Empty<EnemyController>();
            _activated = false;
            _asleep = false;
            _savedDamageFilters = null;
        }

        private void Start()
        {
            if (_sleepEnemiesOnStart && !_activated)
                PutEnemiesToSleep();
        }

        private void Update()
        {
            if (!_activated)
                Evaluate();
        }

        private void FixedUpdate()
        {
            if (!_activated)
                Evaluate();
        }

        /// <summary>
        /// Captures each enemy's post-Awake DamageFilter (preserving Shield's FilterShieldBlock),
        /// disables AI and physics simulation, and blocks all incoming damage until the zone activates.
        /// </summary>
        public void PutEnemiesToSleep()
        {
            if (_activated || _asleep)
                return;

            int count = _enemies != null ? _enemies.Length : 0;
            _savedDamageFilters = new Func<DamageInfo, bool>[count];

            for (int i = 0; i < count; i++)
            {
                var enemy = _enemies[i];
                if (enemy == null)
                    continue;

                enemy.enabled = false;
                if (enemy.TryGetComponent<Rigidbody2D>(out var body))
                {
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                    body.simulated = false;
                }

                if (enemy.TryGetComponent<Health>(out var health))
                {
                    _savedDamageFilters[i] = health.DamageFilter;
                    health.DamageFilter = static _ => false;
                }
            }

            _asleep = true;
        }

        public bool ContainsPoint(Vector2 feet) =>
            feet.x >= _minBounds.x && feet.x <= _maxBounds.x &&
            feet.y >= _minBounds.y && feet.y <= _maxBounds.y;

        public static Vector2 GetColliderFeet(BoxCollider2D collider)
        {
            float scaleSignY = collider.transform.lossyScale.y < 0f ? -1f : 1f;
            float localHalfHeight = collider.size.y * 0.5f * scaleSignY;
            Vector3 localFeet = new Vector3(collider.offset.x, collider.offset.y - localHalfHeight, 0f);
            return collider.transform.TransformPoint(localFeet);
        }

        /// <summary>
        /// Evaluates the assigned Player's main collider feet against the inclusive zone bounds.
        /// </summary>
        public bool Evaluate()
        {
            var expectedCollider = ResolvePlayerCollider();
            var expectedHealth = ResolvePlayerHealth();
            return TryActivateFor(expectedCollider, expectedHealth);
        }

        /// <summary>
        /// Validates that candidateCollider is the exact assigned Player main collider, Player is alive,
        /// run is Playing, and feet are inside [MinBounds, MaxBounds].
        /// </summary>
        public bool TryActivateFor(Collider2D candidateCollider, Health candidateHealth = null)
        {
            if (_activated)
                return false;

            if (_run == null || !_run.IsPlaying)
                return false;

            var expectedCollider = ResolvePlayerCollider();
            if (expectedCollider == null || candidateCollider != expectedCollider)
                return false;

            var health = candidateHealth != null ? candidateHealth : ResolvePlayerHealth();
            if (health == null || !health.IsAlive)
                return false;

            Vector2 feet = GetColliderFeet(expectedCollider);
            if (!ContainsPoint(feet))
                return false;

            WakeEnemies();
            return true;
        }

        private void WakeEnemies()
        {
            // Mark opened BEFORE enabling any enemy (Plan §4 W1).
            _activated = true;
            _asleep = false;

            int count = _enemies != null ? _enemies.Length : 0;
            for (int i = 0; i < count; i++)
            {
                var enemy = _enemies[i];
                if (enemy == null)
                    continue;

                if (enemy.TryGetComponent<Health>(out var health))
                {
                    if (_savedDamageFilters != null && i < _savedDamageFilters.Length)
                        health.DamageFilter = _savedDamageFilters[i];
                    else
                        health.DamageFilter = null;
                }

                if (enemy.TryGetComponent<Rigidbody2D>(out var body))
                    body.simulated = true;

                enemy.enabled = true;
                EnemyWoken?.Invoke(enemy);
            }

            ZoneActivated?.Invoke(this);
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
