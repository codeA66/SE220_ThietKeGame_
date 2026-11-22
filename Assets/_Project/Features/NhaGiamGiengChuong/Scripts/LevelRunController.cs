using System;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using DeadCells.Player.StateMachine.States;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace DeadCells.NhaGiamGiengChuong
{
    public enum LevelOutcome
    {
        Win,
        Loss
    }

    public enum LevelPhase
    {
        Intro,
        Playing,
        Ended,
        Reloading
    }

    /// <summary>
    /// Owns the run lifecycle for Nha Giam Gieng Chuong: Intro -> Playing -> Ended -> Reloading.
    /// Arbitrates same-frame Win/Loss candidates (Loss always wins on tie) and settles frame N at N+1.
    /// Locks gameplay components independently of Time.timeScale so HitStop or auto-respawn cannot leak.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelRunController : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private PlayerInputReader _playerInput;
        [SerializeField] private ProjectileSpawner _projectileSpawner;
        [SerializeField] private HitStop _hitStop;

        private LevelPhase _phase = LevelPhase.Intro;
        private bool _hasOutcome;
        private LevelOutcome _outcome;
        private bool _clockStarted;
        private float _runStartTime = -1f;
        private float _endTime = -1f;
        private bool _gameplayLocked;

        private bool _hasPendingCandidate;
        private int _pendingFrame;
        private bool _pendingHasLoss;

        private Health _subscribedHealth;
        private bool _initialized;

        public PlayerController Player => _player;
        public Health PlayerHealth => ResolvePlayerHealth();
        public LevelPhase Phase => _phase;
        public bool IsPlaying => _phase == LevelPhase.Playing;
        public bool IsEnded => _phase == LevelPhase.Ended || _phase == LevelPhase.Reloading;
        public bool HasOutcome => _hasOutcome;
        public LevelOutcome Outcome => _outcome;
        public bool ClockStarted => _clockStarted;
        public float RunStartTime => _runStartTime;
        public float ElapsedRunTime => _clockStarted ? Mathf.Max(0f, (_endTime >= 0f ? _endTime : Time.time) - _runStartTime) : 0f;
        public bool GameplayLocked => _gameplayLocked;

        /// <summary>Optional frame provider override for deterministic EditMode unit tests.</summary>
        public Func<int> FrameProvider { get; set; }

        /// <summary>Optional scene loader override for deterministic EditMode unit tests (U15).</summary>
        public Func<bool> ReplayLoader { get; set; }

        public event Action RunStarted;
        public event Action<LevelOutcome> Finished;
        public event Action ReplayRequested;

        public void Configure(
            PlayerController player,
            Health playerHealth = null,
            PlayerInputReader playerInput = null,
            ProjectileSpawner projectileSpawner = null,
            HitStop hitStop = null)
        {
            UnsubscribeHealth();
            _player = player;
            _playerHealth = playerHealth != null ? playerHealth : (player != null ? player.Health : null);
            _playerInput = playerInput != null ? playerInput : (player != null ? player.GetComponent<PlayerInputReader>() : null);
            _projectileSpawner = projectileSpawner;
            _hitStop = hitStop;
            SubscribeHealth();
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            _initialized = true;
            if (_player != null)
            {
                if (_playerHealth == null)
                    _playerHealth = _player.Health != null ? _player.Health : _player.GetComponent<Health>();
                if (_playerInput == null)
                    _playerInput = _player.GetComponent<PlayerInputReader>();
            }
            SubscribeHealth();
        }

        private void OnEnable()
        {
            if (_initialized)
                SubscribeHealth();
        }

        private void OnDisable()
        {
            UnsubscribeHealth();
        }

        private void OnDestroy()
        {
            UnsubscribeHealth();
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (_phase == LevelPhase.Intro)
            {
                TickIntro(_player != null ? _player.CurrentState : null, Time.time);
            }

            if (_phase == LevelPhase.Playing)
            {
                ResolvePendingOutcome(GetCurrentFrame());
            }

            if (_phase == LevelPhase.Ended && Application.isPlaying && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                TryReplay();
            }
        }

        /// <summary>
        /// Advances Intro -> Playing once Player leaves PlayerSpawnState and holds a valid gameplay state.
        /// </summary>
        public bool TickIntro(IPlayerState currentState, float currentTime)
        {
            if (_phase != LevelPhase.Intro)
                return false;

            if (currentState == null || currentState is PlayerSpawnState)
                return false;

            _phase = LevelPhase.Playing;
            if (!_clockStarted)
            {
                _clockStarted = true;
                _runStartTime = currentTime;
                RunStarted?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Registers a Win candidate for the given frame. Ignored outside Playing or if an earlier frame is already pending.
        /// </summary>
        public bool RequestWin(int frame)
        {
            if (_phase != LevelPhase.Playing)
                return false;

            if (!_hasPendingCandidate)
            {
                _hasPendingCandidate = true;
                _pendingFrame = frame;
                _pendingHasLoss = false;
                return true;
            }

            if (frame < _pendingFrame)
            {
                _pendingFrame = frame;
                _pendingHasLoss = false;
                return true;
            }

            if (frame == _pendingFrame)
            {
                return true;
            }

            // Later frame cannot overwrite an already pending earlier frame N.
            return false;
        }

        /// <summary>
        /// Registers a Loss candidate for the given frame. On same-frame tie with Win, Loss always wins.
        /// </summary>
        public bool RequestLoss(int frame)
        {
            if (_phase != LevelPhase.Playing)
                return false;

            if (!_hasPendingCandidate)
            {
                _hasPendingCandidate = true;
                _pendingFrame = frame;
                _pendingHasLoss = true;
                return true;
            }

            if (frame < _pendingFrame)
            {
                _pendingFrame = frame;
                _pendingHasLoss = true;
                return true;
            }

            if (frame == _pendingFrame)
            {
                _pendingHasLoss = true;
                return true;
            }

            // Later frame cannot overwrite an already pending earlier frame N.
            return false;
        }

        /// <summary>
        /// Settles pending frame N once currentFrame > N. Loss overrides Win when both occurred in frame N.
        /// </summary>
        public bool ResolvePendingOutcome(int currentFrame)
        {
            if (_phase != LevelPhase.Playing || !_hasPendingCandidate)
                return false;

            if (currentFrame <= _pendingFrame)
                return false;

            LevelOutcome finalOutcome = _pendingHasLoss ? LevelOutcome.Loss : LevelOutcome.Win;
            _hasPendingCandidate = false;
            _pendingHasLoss = false;

            _phase = LevelPhase.Ended;
            _endTime = Time.time;
            _hasOutcome = true;
            _outcome = finalOutcome;
            LockGameplay();
            Finished?.Invoke(finalOutcome);
            return true;
        }

        /// <summary>
        /// Requests a single-scene reload. Only accepted once after the run has ended.
        /// </summary>
        public bool TryReplay()
        {
            if (_phase != LevelPhase.Ended)
                return false;

            return ExecuteSceneReload();
        }

        /// <summary>
        /// Requests a scene reload from an active Playing pause menu. Rejected outside Playing.
        /// </summary>
        public bool TryRestartFromPause()
        {
            if (_phase != LevelPhase.Playing)
                return false;

            return ExecuteSceneReload();
        }

        private bool ExecuteSceneReload()
        {
            _phase = LevelPhase.Reloading;
            if (_hitStop != null)
                _hitStop.SetMenuPaused(false);
            Time.timeScale = 1f;
            ReplayRequested?.Invoke();

            if (ReplayLoader != null)
                return ReplayLoader();

            if (!Application.isPlaying)
                return true;

            Scene activeScene = gameObject.scene;
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadSceneAsync(activeScene.buildIndex, LoadSceneMode.Single);
                return true;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.LoadSceneAsyncInPlayMode(activeScene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return true;
            }
#endif
            if (!string.IsNullOrEmpty(activeScene.name))
            {
                SceneManager.LoadSceneAsync(activeScene.name, LoadSceneMode.Single);
                return true;
            }

            return true;
        }

        /// <summary>
        /// Handles R-key shortcut decision explicitly (callable from unit tests without live keyboard).
        /// </summary>
        public bool HandleRestartKeyPressed()
        {
            if (_phase != LevelPhase.Ended)
                return false;
            return TryReplay();
        }

        private void LockGameplay()
        {
            _gameplayLocked = true;

            if (_playerInput != null)
                _playerInput.enabled = false;

            if (_player != null)
            {
                _player.enabled = false;
                if (_player.TryGetComponent<Rigidbody2D>(out var playerBody))
                {
                    playerBody.linearVelocity = Vector2.zero;
                    playerBody.angularVelocity = 0f;
                    playerBody.simulated = false;
                }
            }

            var health = ResolvePlayerHealth();
            if (health != null)
                health.DamageFilter = static _ => false;

            if (Application.isPlaying)
            {
                var enemies = FindObjectsByType<EnemyController>();
                for (int i = 0; i < enemies.Length; i++)
                {
                    var enemy = enemies[i];
                    if (enemy == null)
                        continue;
                    enemy.enabled = false;
                    if (enemy.TryGetComponent<Rigidbody2D>(out var enemyBody))
                    {
                        enemyBody.linearVelocity = Vector2.zero;
                        enemyBody.simulated = false;
                    }
                    if (enemy.TryGetComponent<Health>(out var enemyHealth))
                        enemyHealth.DamageFilter = static _ => false;
                }

                var projectiles = FindObjectsByType<Projectile>();
                for (int i = 0; i < projectiles.Length; i++)
                {
                    var proj = projectiles[i];
                    if (proj != null && proj.gameObject.activeSelf)
                        proj.gameObject.SetActive(false);
                }
            }
        }

        private void OnPlayerHealthDied(DamageInfo info)
        {
            RequestLoss(GetCurrentFrame());
        }

        private int GetCurrentFrame() => FrameProvider != null ? FrameProvider() : Time.frameCount;

        private Health ResolvePlayerHealth()
        {
            if (_playerHealth != null)
                return _playerHealth;
            if (_player != null)
                return _player.Health != null ? _player.Health : _player.GetComponent<Health>();
            return null;
        }

        private void SubscribeHealth()
        {
            var health = ResolvePlayerHealth();
            if (health == null || health == _subscribedHealth)
                return;

            UnsubscribeHealth();
            _subscribedHealth = health;
            _subscribedHealth.Died += OnPlayerHealthDied;
        }

        private void UnsubscribeHealth()
        {
            if (_subscribedHealth == null)
                return;

            _subscribedHealth.Died -= OnPlayerHealthDied;
            _subscribedHealth = null;
        }
    }
}
