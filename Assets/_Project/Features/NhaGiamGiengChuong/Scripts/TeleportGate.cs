using System;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core;
using DeadCells.Core.Audio;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Paired fast-travel stone. Auto-discovers on proximity during Playing, requires both gates
    /// discovered before teleporting, warps Player feet to the paired gate's arrival point, and
    /// enforces a shared cooldown across both gates to prevent bounce-back loops.
    /// </summary>
    public sealed class TeleportGate : MonoBehaviour
    {
        public const float TeleportCooldownSeconds = 0.25f;

        [SerializeField] private string _gateId;
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private BoxCollider2D _triggerZone;
        [SerializeField] private TeleportGate _pairedGate;
        [SerializeField] private bool _startsDiscovered;
        [SerializeField] private Vector2 _arrivalOffset;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _promptIndicator;
        [SerializeField] private GameObject _lockedHintIndicator;
        [SerializeField] private CameraFollow2D _camera;
        [SerializeField] private PowerScrollInteraction _powerScrolls;
        [SerializeField] private SpriteRenderer _stoneRenderer;
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private Sprite _activeSprite;

        private InputAction _interactAction;
        private bool _isDiscovered;
        private bool _promptVisible;
        private bool _lockedHintVisible;
        private int _teleportCount;
        private float _cooldownUntilTime = -10f;

        public string GateId => _gateId;
        public TeleportGate PairedGate => _pairedGate;
        public bool IsDiscovered => _isDiscovered;
        public bool CanTeleport => _isDiscovered && _pairedGate != null && _pairedGate != this && _pairedGate._isDiscovered;
        public Vector2 ArrivalFeetPosition => (Vector2)transform.position + _arrivalOffset;
        public int TeleportCount => _teleportCount;
        public bool PromptVisible => _promptVisible;
        public bool LockedHintVisible => _lockedHintVisible;
        public BoxCollider2D TriggerZone => _triggerZone;
        public GameObject PromptIndicator => _promptIndicator;
        public GameObject LockedHintIndicator => _lockedHintIndicator;
        public InputActionAsset InputActions => _inputActions;

        public event Action<TeleportGate> Discovered;
        public event Action<TeleportGate, TeleportGate> Teleported;

        public void Configure(
            string gateId,
            LevelRunController run,
            PlayerController player,
            BoxCollider2D triggerZone,
            TeleportGate pairedGate,
            bool startsDiscovered,
            Vector2 arrivalOffset,
            InputActionAsset inputActions = null,
            GameObject promptIndicator = null,
            SpriteRenderer stoneRenderer = null,
            Sprite inactiveSprite = null,
            Sprite activeSprite = null,
            Health playerHealth = null,
            BoxCollider2D playerCollider = null)
        {
            UnsubscribeInput();
            _gateId = gateId;
            _run = run;
            _player = player;
            _triggerZone = triggerZone;
            _pairedGate = pairedGate;
            _startsDiscovered = startsDiscovered;
            _arrivalOffset = arrivalOffset;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            _stoneRenderer = stoneRenderer;
            _inactiveSprite = inactiveSprite;
            _activeSprite = activeSprite;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;

            _isDiscovered = startsDiscovered;
            _teleportCount = 0;
            _cooldownUntilTime = -10f;

            ApplyStoneSprite();
            if (isActiveAndEnabled)
                SubscribeInput();
            RefreshVisualState();
        }

        private void Awake()
        {
            if (_triggerZone == null)
                _triggerZone = GetComponent<BoxCollider2D>();

            if (_startsDiscovered)
                _isDiscovered = true;

            ApplyStoneSprite();
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

        public void Discover()
        {
            if (_isDiscovered)
                return;

            _isDiscovered = true;
            ApplyStoneSprite();
            Discovered?.Invoke(this);
        }

        public void RefreshVisualState()
        {
            var health = ResolvePlayerHealth();
            var col = ResolvePlayerCollider();
            bool inTrigger = IsColliderInTrigger(col);
            bool playingAndAlive = _run != null && _run.IsPlaying
                && !IsInputBlocked()
                && health != null && health.IsAlive;

            if (!_isDiscovered && playingAndAlive && inTrigger)
                Discover();

            ApplyStoneSprite();

            _promptVisible = playingAndAlive
                && CanTeleport
                && inTrigger
                && Time.unscaledTime >= _cooldownUntilTime;

            if (_promptIndicator != null)
                _promptIndicator.SetActive(_promptVisible);

            bool showLockedHint = playingAndAlive
                && inTrigger
                && _isDiscovered
                && !CanTeleport;
            _lockedHintVisible = showLockedHint;
            if (_lockedHintIndicator != null)
                _lockedHintIndicator.SetActive(showLockedHint);
        }

        public bool TryTeleport() => TryTeleport(Time.unscaledTime);

        public bool TryTeleport(float currentTime)
        {
            if (_run == null || !_run.IsPlaying || IsInputBlocked())
                return false;

            var health = ResolvePlayerHealth();
            if (health == null || !health.IsAlive)
                return false;

            var col = ResolvePlayerCollider();
            if (!IsColliderInTrigger(col))
                return false;

            if (!_isDiscovered)
                Discover();

            if (!CanTeleport || currentTime < _cooldownUntilTime)
                return false;

            var player = ResolvePlayer();
            Transform actorTransform = player != null ? player.transform : col.transform;

            Vector2 currentFeet = EncounterZone.GetColliderFeet(col);
            Vector2 delta = _pairedGate.ArrivalFeetPosition - currentFeet;
            actorTransform.position += (Vector3)delta;

            if (actorTransform.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.position = (Vector2)actorTransform.position;
                rb.linearVelocity = Vector2.zero;
            }

            Physics2D.SyncTransforms();

            if (_camera == null)
                _camera = UnityEngine.Object.FindAnyObjectByType<CameraFollow2D>();
            if (_camera != null)
                _camera.SnapToTarget();
            CombatAudioHub.Play(SfxId.TeleportWarp);

            float cooldownUntil = currentTime + TeleportCooldownSeconds;
            _cooldownUntilTime = cooldownUntil;
            _pairedGate._cooldownUntilTime = cooldownUntil;
            _teleportCount++;

            RefreshVisualState();
            _pairedGate.RefreshVisualState();
            Teleported?.Invoke(this, _pairedGate);
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

        private void ApplyStoneSprite()
        {
            if (_stoneRenderer == null)
                return;

            Sprite target = _isDiscovered ? _activeSprite : _inactiveSprite;
            if (target != null)
                _stoneRenderer.sprite = target;
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            TryTeleport();
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
            if (p != null && p.RunStats != null && p.RunStats.BlocksGameplayInput)
                return true;

            if (_powerScrolls == null)
            {
                if (p != null)
                    _powerScrolls = p.GetComponentInChildren<PowerScrollInteraction>();
                if (_powerScrolls == null)
                    _powerScrolls = UnityEngine.Object.FindAnyObjectByType<PowerScrollInteraction>();
            }

            if (_powerScrolls != null)
                return _powerScrolls.HasFocusedScroll;

            return false;
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
