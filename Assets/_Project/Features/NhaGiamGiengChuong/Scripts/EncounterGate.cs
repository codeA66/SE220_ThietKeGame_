using System;
using System.Collections.Generic;
using DeadCells.Enemies;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Encounter gate I4 between Room E and Room L (x=22, y=24..28).
    /// Counts deaths of two distinct serialized Room E enemies (Zombie E and Archer E) and disables
    /// a single barrier child holding both the iron bars sprite and solid blocker simultaneously.
    /// </summary>
    public sealed class EncounterGate : MonoBehaviour
    {
        [SerializeField] private LevelRunController _run;
        [SerializeField] private EnemyController _firstEnemy;
        [SerializeField] private EnemyController _secondEnemy;
        [SerializeField] private GameObject _gateBarrierRoot;

        private readonly HashSet<EnemyController> _requiredEnemies = new HashSet<EnemyController>();
        private readonly HashSet<EnemyController> _defeatedEnemies = new HashSet<EnemyController>();
        private bool _isOpen;
        private bool _subscribed;

        public bool IsOpen => _isOpen;
        public int DefeatedCount => _defeatedEnemies.Count;
        public int RequiredCount => _requiredEnemies.Count;
        public EnemyController FirstEnemy => _firstEnemy;
        public EnemyController SecondEnemy => _secondEnemy;
        public GameObject GateBarrierRoot => _gateBarrierRoot;

        public event Action Opened;

        public void Configure(
            LevelRunController run,
            EnemyController firstEnemy,
            EnemyController secondEnemy,
            GameObject gateBarrierRoot)
        {
            Unsubscribe();
            _run = run;
            _firstEnemy = firstEnemy;
            _secondEnemy = secondEnemy;
            _gateBarrierRoot = gateBarrierRoot;
            _isOpen = false;
            _defeatedEnemies.Clear();
            RebuildRequiredSet();
            if (_gateBarrierRoot != null)
                _gateBarrierRoot.SetActive(true);
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void Awake()
        {
            RebuildRequiredSet();
            if (_gateBarrierRoot != null && !_isOpen)
                _gateBarrierRoot.SetActive(true);
        }

        private void OnEnable()
        {
            RebuildRequiredSet();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Processes an enemy death event. Counts only distinct assigned Room E instances during Playing.
        /// </summary>
        public bool NotifyEnemyDied(EnemyController enemy)
        {
            if (_isOpen)
                return false;

            if (_run != null && !_run.IsPlaying)
                return false;

            if (enemy == null || !_requiredEnemies.Contains(enemy))
                return false;

            if (!_defeatedEnemies.Add(enemy))
                return false;

            if (_requiredEnemies.Count >= 2 && _defeatedEnemies.Count >= _requiredEnemies.Count)
            {
                OpenGate();
                return true;
            }

            return false;
        }

        private void OpenGate()
        {
            _isOpen = true;
            bool hasBounds = SwitchDoor.TryCaptureBarrierBounds(_gateBarrierRoot, out Bounds barrierBounds);
            if (_gateBarrierRoot != null)
                _gateBarrierRoot.SetActive(false);
            if (hasBounds)
            {
                Physics2D.SyncTransforms();
                DeadCells.HUD.MinimapHud.NotifyWorldGeometryChanged(barrierBounds);
            }
            Opened?.Invoke();
        }

        private void OnEnemyDied(EnemyController enemy) => NotifyEnemyDied(enemy);

        private void RebuildRequiredSet()
        {
            _requiredEnemies.Clear();
            if (_firstEnemy != null)
                _requiredEnemies.Add(_firstEnemy);
            if (_secondEnemy != null)
                _requiredEnemies.Add(_secondEnemy);
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            foreach (var enemy in _requiredEnemies)
            {
                if (enemy != null)
                    enemy.Died += OnEnemyDied;
            }
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            foreach (var enemy in _requiredEnemies)
            {
                if (enemy != null)
                    enemy.Died -= OnEnemyDied;
            }
            _subscribed = false;
        }
    }
}
