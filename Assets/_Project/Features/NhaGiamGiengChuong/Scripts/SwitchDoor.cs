using System;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
using DeadCells.Player;
using DeadCells.VFX;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Interactive lever/switch door that opens a linked barrier atomically once when a living Player
    /// interacts inside the lever trigger during the Playing phase.
    /// </summary>
    public sealed class SwitchDoor : MonoBehaviour
    {
        [SerializeField] private string _doorId;
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private BoxCollider2D _switchTrigger;
        [SerializeField] private GameObject _doorBarrierRoot;
        [SerializeField] private SpriteRenderer _switchRenderer;
        [SerializeField] private Sprite _switchOffSprite;
        [SerializeField] private Sprite _switchOnSprite;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _promptIndicator;
        [SerializeField] private CameraFollow2D _camera;
        [SerializeField] private VfxSpawner _vfx;
        [SerializeField] private VfxDefinition _openVfx;

        private InputAction _interactAction;
        private bool _isOpen;
        private bool _promptVisible;

        public string DoorId => _doorId;
        public bool IsOpen => _isOpen;
        public bool PromptVisible => _promptVisible;
        public GameObject DoorBarrierRoot => _doorBarrierRoot;
        public BoxCollider2D SwitchTrigger => _switchTrigger;
        public GameObject PromptIndicator => _promptIndicator;
        public InputActionAsset InputActions => _inputActions;

        public event Action<SwitchDoor> Opened;

        public void Configure(
            string doorId,
            LevelRunController run,
            PlayerController player,
            BoxCollider2D switchTrigger,
            GameObject doorBarrierRoot,
            SpriteRenderer switchRenderer = null,
            Sprite switchOffSprite = null,
            Sprite switchOnSprite = null,
            InputActionAsset inputActions = null,
            GameObject promptIndicator = null,
            Health playerHealth = null,
            BoxCollider2D playerCollider = null)
        {
            UnsubscribeInput();
            _doorId = doorId;
            _run = run;
            _player = player;
            _switchTrigger = switchTrigger;
            _doorBarrierRoot = doorBarrierRoot;
            _switchRenderer = switchRenderer;
            _switchOffSprite = switchOffSprite;
            _switchOnSprite = switchOnSprite;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _isOpen = false;

            if (_doorBarrierRoot != null)
                _doorBarrierRoot.SetActive(true);
            if (_switchRenderer != null && _switchOffSprite != null)
                _switchRenderer.sprite = _switchOffSprite;

            if (isActiveAndEnabled)
                SubscribeInput();
            RefreshVisualState();
        }

        private void Awake()
        {
            if (_switchTrigger == null)
                _switchTrigger = GetComponent<BoxCollider2D>();

            if (_doorBarrierRoot != null)
                _doorBarrierRoot.SetActive(!_isOpen);

            ApplySwitchSprite();
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
            bool inTrigger = IsColliderInTrigger(col);

            _promptVisible = !_isOpen
                && _run != null && _run.IsPlaying
                && !IsInputBlocked()
                && health != null && health.IsAlive
                && inTrigger;

            if (_promptIndicator != null)
                _promptIndicator.SetActive(_promptVisible);
        }

        public bool TryInteract()
        {
            if (_isOpen)
                return false;

            if (_run == null || !_run.IsPlaying || IsInputBlocked())
                return false;

            var health = ResolvePlayerHealth();
            if (health == null || !health.IsAlive)
                return false;

            var col = ResolvePlayerCollider();
            if (!IsColliderInTrigger(col))
                return false;

            _isOpen = true;
            if (_doorBarrierRoot != null)
                _doorBarrierRoot.SetActive(false);

            ApplySwitchSprite();
            RefreshVisualState();
            if (_camera != null)
                _camera.Shake(0.10f, 0.14f);
            if (_vfx != null && _openVfx != null && _doorBarrierRoot != null)
                _vfx.Play(_openVfx, _doorBarrierRoot.transform.position);
            CombatAudioHub.Play(SfxId.SwitchDoorOpen);
            Opened?.Invoke(this);
            return true;
        }

        public bool IsColliderInTrigger(Collider2D candidate)
        {
            if (candidate == null || _switchTrigger == null)
                return false;

            Bounds triggerBounds = GetWorldBounds(_switchTrigger);
            Bounds actorBounds = GetWorldBounds(candidate);

            return actorBounds.max.x >= triggerBounds.min.x && actorBounds.min.x <= triggerBounds.max.x
                && actorBounds.max.y >= triggerBounds.min.y && actorBounds.min.y <= triggerBounds.max.y;
        }

        private void ApplySwitchSprite()
        {
            if (_switchRenderer == null)
                return;

            Sprite target = _isOpen ? _switchOnSprite : _switchOffSprite;
            if (target != null)
                _switchRenderer.sprite = target;
        }

        private static Bounds GetWorldBounds(Collider2D collider)
        {
            if (collider is BoxCollider2D box)
            {
                Vector3 center = box.transform.TransformPoint(box.offset);
                Vector3 scale = box.transform.lossyScale;
                Vector3 size = new Vector3(
                    Mathf.Abs(box.size.x * scale.x),
                    Mathf.Abs(box.size.y * scale.y),
                    1f);
                return new Bounds(center, size);
            }

            return collider.bounds;
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
