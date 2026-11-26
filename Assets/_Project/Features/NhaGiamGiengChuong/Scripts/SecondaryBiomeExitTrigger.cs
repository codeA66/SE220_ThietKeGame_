using System;
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Core;
using DeadCells.Enemies;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Secondary biome exit trigger (e.g. Toxic Sewers I15, Promenade I18). Unlocks only when the linked SwitchDoor
    /// is open (if assigned) and both assigned guardians are defeated, displays an [E] prompt, and submits a win
    /// candidate only when the living Player presses Interact inside the trigger during Playing.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SecondaryBiomeExitTrigger : MonoBehaviour
    {
        [SerializeField] private string _exitId;
        [SerializeField] private string _biomeName = "Toxic Sewers";
        [SerializeField] private LevelRunController _run;
        [SerializeField] private SwitchDoor _switchDoor;
        [SerializeField] private EnemyController _firstGuardian;
        [SerializeField] private EnemyController _secondGuardian;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _lockedPromptIndicator;
        [SerializeField] private GameObject _promptIndicator;

        private readonly HashSet<EnemyController> _defeatedGuardians = new HashSet<EnemyController>();
        private BoxCollider2D _triggerZone;
        private InputAction _interactAction;
        private bool _subscribed;
        private bool _lockedPromptVisible;
        private bool _promptVisible;

        public string ExitId => _exitId;
        public string BiomeName => _biomeName;
        public LevelRunController Run => _run;
        public SwitchDoor SwitchDoor => _switchDoor;
        public EnemyController FirstGuardian => _firstGuardian;
        public EnemyController SecondGuardian => _secondGuardian;
        public bool AreGuardiansDefeated =>
            IsGuardianDefeated(_firstGuardian) && IsGuardianDefeated(_secondGuardian);
        public bool IsUnlocked =>
            (_switchDoor == null || _switchDoor.IsOpen) && AreGuardiansDefeated;
        public bool LockedPromptVisible => _lockedPromptVisible;
        public bool PromptVisible => _promptVisible;
        public GameObject LockedPromptIndicator => _lockedPromptIndicator;
        public GameObject PromptIndicator => _promptIndicator;

        public event Action<int> WinCandidateSubmitted;

        public void Configure(
            string exitId,
            string biomeName,
            LevelRunController run,
            SwitchDoor switchDoor,
            EnemyController firstGuardian,
            EnemyController secondGuardian,
            PlayerController player,
            Health playerHealth = null,
            BoxCollider2D playerCollider = null,
            InputActionAsset inputActions = null,
            GameObject promptIndicator = null)
        {
            UnsubscribeGuardians();
            UnsubscribeInput();
            _exitId = exitId;
            _biomeName = biomeName;
            _run = run;
            _switchDoor = switchDoor;
            _firstGuardian = firstGuardian;
            _secondGuardian = secondGuardian;
            _player = player;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            _defeatedGuardians.Clear();

            if (isActiveAndEnabled)
            {
                SubscribeGuardians();
                SubscribeInput();
            }
            RefreshVisualState();
        }

        private void Awake()
        {
            _triggerZone = GetComponent<BoxCollider2D>();
            RefreshVisualState();
        }

        private void OnEnable()
        {
            SubscribeGuardians();
            SubscribeInput();
            RefreshVisualState();
        }

        private void OnDisable()
        {
            UnsubscribeGuardians();
            UnsubscribeInput();
        }

        private void OnDestroy()
        {
            UnsubscribeGuardians();
            UnsubscribeInput();
        }

        private void Update()
        {
            RefreshVisualState();
        }

        public void RefreshVisualState()
        {
            bool inTrigger = IsPlayerInTrigger();
            var health = ResolvePlayerHealth();
            bool alive = health != null && health.IsAlive;
            bool playing = _run != null && _run.IsPlaying;
            bool unlocked = IsUnlocked;

            _lockedPromptVisible = playing && alive && !unlocked && inTrigger;
            _promptVisible = playing && alive && unlocked && !IsInputBlocked() && inTrigger;

            if (_lockedPromptIndicator != null)
                _lockedPromptIndicator.SetActive(_lockedPromptVisible);
            if (_promptIndicator != null)
                _promptIndicator.SetActive(_promptVisible);
        }

        public bool NotifyGuardianDied(EnemyController enemy)
        {
            if (enemy == null)
                return false;

            if (_run != null && !_run.IsPlaying)
                return false;

            if (enemy != _firstGuardian && enemy != _secondGuardian)
                return false;

            bool added = _defeatedGuardians.Add(enemy);
            if (added)
                RefreshVisualState();
            return added;
        }

        public bool TryInteract()
        {
            if (IsInputBlocked() || !IsPlayerInTrigger())
                return false;
            return TrySubmitForCollider(ResolvePlayerCollider(), Time.frameCount);
        }

        public bool TrySubmitForCollider(Collider2D candidateCollider, int frame)
        {
            if (candidateCollider == null)
                return false;

            if (_run == null || !_run.IsPlaying)
                return false;

            if (!IsUnlocked)
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

        private void OnInteractStarted(InputAction.CallbackContext _) => TryInteract();

        private void SubscribeInput()
        {
            if (_inputActions == null || _interactAction != null)
                return;
            var map = _inputActions.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (map == null)
                return;
            _interactAction = map.FindAction(_interactActionName, throwIfNotFound: false);
            if (_interactAction != null)
                _interactAction.started += OnInteractStarted;
        }

        private void UnsubscribeInput()
        {
            if (_interactAction == null)
                return;
            _interactAction.started -= OnInteractStarted;
            _interactAction = null;
        }

        private bool IsInputBlocked()
        {
            var p = ResolvePlayer();
            return p != null && ((p.RunStats != null && p.RunStats.BlocksGameplayInput) || p.IsInScriptedSequence);
        }

        private bool IsGuardianDefeated(EnemyController enemy)
        {
            if (enemy == null)
                return true;

            return _defeatedGuardians.Contains(enemy)
                || (Application.isPlaying && enemy.IsDead);
        }

        private void OnGuardianDied(EnemyController enemy) => NotifyGuardianDied(enemy);

        private void SubscribeGuardians()
        {
            if (_subscribed)
                return;

            if (_firstGuardian != null)
                _firstGuardian.Died += OnGuardianDied;
            if (_secondGuardian != null && _secondGuardian != _firstGuardian)
                _secondGuardian.Died += OnGuardianDied;

            _subscribed = true;
        }

        private void UnsubscribeGuardians()
        {
            if (!_subscribed)
                return;

            if (_firstGuardian != null)
                _firstGuardian.Died -= OnGuardianDied;
            if (_secondGuardian != null && _secondGuardian != _firstGuardian)
                _secondGuardian.Died -= OnGuardianDied;

            _subscribed = false;
        }

        private PlayerController ResolvePlayer() =>
            _player != null ? _player : (_run != null ? _run.Player : null);

        private bool IsPlayerInTrigger()
        {
            if (_triggerZone == null)
                _triggerZone = GetComponent<BoxCollider2D>();

            var col = ResolvePlayerCollider();
            if (col == null || _triggerZone == null)
                return false;

            Bounds triggerBounds = ColliderBoundsUtility.GetWorldBounds(_triggerZone);
            Bounds actorBounds = ColliderBoundsUtility.GetWorldBounds(col);

            return actorBounds.max.x >= triggerBounds.min.x && actorBounds.min.x <= triggerBounds.max.x
                && actorBounds.max.y >= triggerBounds.min.y && actorBounds.min.y <= triggerBounds.max.y;
        }

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
