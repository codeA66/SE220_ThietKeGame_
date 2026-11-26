using System;
using DeadCells.Combat;
using DeadCells.Core;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Interactive lore tablet that toggles an in-world/UI lore banner when a living Player
    /// interacts inside its trigger during Playing, and auto-closes when walking away or when the run ends.
    /// </summary>
    public sealed class LoreTablet : MonoBehaviour
    {
        [SerializeField] private string _tabletId;
        [SerializeField] private string _loreTitle;
        [SerializeField, TextArea] private string _loreBody;
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private BoxCollider2D _triggerZone;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _promptIndicator;
        [SerializeField] private GameObject _loreBannerRoot;
        [SerializeField] private Text _loreBannerText;

        private InputAction _interactAction;
        private bool _isReading;
        private int _readCount;
        private bool _promptVisible;

        public string TabletId => _tabletId;
        public string LoreTitle => _loreTitle;
        public string LoreBody => _loreBody;
        public bool IsReading => _isReading;
        public int ReadCount => _readCount;
        public bool PromptVisible => _promptVisible;
        public BoxCollider2D TriggerZone => _triggerZone;
        public GameObject PromptIndicator => _promptIndicator;
        public GameObject LoreBannerRoot => _loreBannerRoot;
        public InputActionAsset InputActions => _inputActions;

        public event Action<LoreTablet, bool> ReadingStateChanged;

        public void Configure(
            string tabletId,
            string loreTitle,
            string loreBody,
            LevelRunController run,
            PlayerController player,
            BoxCollider2D triggerZone,
            InputActionAsset inputActions = null,
            GameObject promptIndicator = null,
            GameObject loreBannerRoot = null,
            Text loreBannerText = null,
            Health playerHealth = null,
            BoxCollider2D playerCollider = null)
        {
            UnsubscribeInput();
            _tabletId = tabletId;
            _loreTitle = loreTitle;
            _loreBody = loreBody;
            _run = run;
            _player = player;
            _triggerZone = triggerZone;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            _loreBannerRoot = loreBannerRoot;
            _loreBannerText = loreBannerText;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _isReading = false;
            _readCount = 0;

            if (isActiveAndEnabled)
                SubscribeInput();
            RefreshVisualState();
        }

        private void Awake()
        {
            if (_triggerZone == null)
                _triggerZone = GetComponent<BoxCollider2D>();
            RefreshVisualState();
        }

        private void OnEnable()
        {
            SubscribeInput();
            RefreshVisualState();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
        }

        private void Update()
        {
            RefreshVisualState();
        }

        public void RefreshVisualState()
        {
            var health = ResolvePlayerHealth();
            var col = ResolvePlayerCollider();
            bool canInteract = _run != null && _run.IsPlaying
                && !IsInputBlocked()
                && health != null && health.IsAlive
                && IsColliderInTrigger(col);

            bool autoClosed = false;
            if (_isReading && !canInteract)
            {
                _isReading = false;
                autoClosed = true;
            }

            _promptVisible = !_isReading && canInteract;
            if (_promptIndicator != null)
                _promptIndicator.SetActive(_promptVisible);
            if (_loreBannerRoot != null)
                _loreBannerRoot.SetActive(_isReading);

            if (autoClosed)
                ReadingStateChanged?.Invoke(this, false);
        }

        public bool TryInteract()
        {
            if (_run == null || !_run.IsPlaying || IsInputBlocked())
                return false;

            var health = ResolvePlayerHealth();
            if (health == null || !health.IsAlive)
                return false;

            var col = ResolvePlayerCollider();
            if (!IsColliderInTrigger(col))
                return false;

            _isReading = !_isReading;
            if (_isReading)
            {
                _readCount++;
                if (_loreBannerText != null)
                    _loreBannerText.text = $"{_loreTitle}\n{_loreBody}";
            }

            RefreshVisualState();
            ReadingStateChanged?.Invoke(this, _isReading);
            return true;
        }

        public bool IsColliderInTrigger(Collider2D candidate)
        {
            if (candidate == null || _triggerZone == null)
                return false;

            Bounds triggerBounds = ColliderBoundsUtility.GetWorldBounds(_triggerZone);
            Bounds actorBounds = ColliderBoundsUtility.GetWorldBounds(candidate);

            return actorBounds.max.x >= triggerBounds.min.x && actorBounds.min.x <= triggerBounds.max.x
                && actorBounds.max.y >= triggerBounds.min.y && actorBounds.min.y <= triggerBounds.max.y;
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            TryInteract();
        }

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

        private PlayerController ResolvePlayer() =>
            _player != null ? _player : (_run != null ? _run.Player : null);

        private bool IsInputBlocked()
        {
            var p = ResolvePlayer();
            return p != null && p.RunStats != null && p.RunStats.BlocksGameplayInput;
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
