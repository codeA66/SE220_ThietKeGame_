using System;
using DeadCells.Combat;
using DeadCells.Core;
using DeadCells.Core.Audio;
using DeadCells.Player;
using DeadCells.VFX;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Single-use full-heal chest (I3) at Room G. Listens to Player/Interact.started on the Player's
    /// InputActionAsset, re-validates all conditions at grant time, reports "Đầy máu" at full HP
    /// without consuming the reward, and guards against synchronous re-entrancy inside Health.Changed.
    /// </summary>
    public sealed class HealingChest : MonoBehaviour
    {
        public const string FullHealthMessage = "Đầy máu";

        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private BoxCollider2D _triggerZone;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _promptIndicator;
        [SerializeField] private GameObject _glowHighlight;
        [SerializeField] private GameObject _fullHealthFeedback;
        [SerializeField] private SpriteRenderer _visualRenderer;
        [SerializeField] private Sprite _openedSprite;
        [SerializeField] private Color _openedTint = new Color(0.55f, 0.58f, 0.62f, 1f);
        [SerializeField] private VfxSpawner _vfx;
        [SerializeField] private VfxDefinition _healVfx;

        private InputAction _interactAction;
        private bool _consumed;
        private bool _isGranting;
        private bool _promptVisible;
        private bool _glowVisible = true;
        private string _lastFeedbackMessage = string.Empty;

        public bool IsConsumed => _consumed;
        public bool HasReward => !_consumed;
        public bool PromptVisible => _promptVisible;
        public bool GlowVisible => _glowVisible;
        public string LastFeedbackMessage => _lastFeedbackMessage;
        public InputActionAsset InputActions => _inputActions;
        public BoxCollider2D TriggerZone => _triggerZone;
        public GameObject PromptIndicator => _promptIndicator;
        public GameObject GlowHighlight => _glowHighlight;
        public GameObject FullHealthFeedback => _fullHealthFeedback;

        public event Action Healed;
        public event Action<string> FeedbackShown;

        public void Configure(
            LevelRunController run,
            PlayerController player,
            BoxCollider2D triggerZone,
            InputActionAsset inputActions = null,
            GameObject promptIndicator = null,
            GameObject glowHighlight = null,
            Health playerHealth = null,
            BoxCollider2D playerCollider = null,
            GameObject fullHealthFeedback = null)
        {
            UnsubscribeInput();
            _run = run;
            _player = player;
            _triggerZone = triggerZone;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            _glowHighlight = glowHighlight;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _fullHealthFeedback = fullHealthFeedback;
            _consumed = false;
            _isGranting = false;
            _lastFeedbackMessage = string.Empty;
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

        /// <summary>
        /// Refreshes prompt and glow visibility from live Player, run, and reward state.
        /// </summary>
        public void RefreshVisualState()
        {
            _glowVisible = !_consumed;

            var health = ResolvePlayerHealth();
            var col = ResolvePlayerCollider();
            bool inTrigger = IsColliderInTrigger(col);
            bool inputBlocked = IsInputBlocked();
            _promptVisible = !_consumed
                && _run != null && _run.IsPlaying
                && !inputBlocked
                && health != null && health.IsAlive
                && health.Current < health.Max
                && inTrigger;

            if (_fullHealthFeedback != null &&
                (!inTrigger || _consumed || _run == null || !_run.IsPlaying || inputBlocked || health == null || !health.IsAlive || health.Current < health.Max))
            {
                _fullHealthFeedback.SetActive(false);
            }

            if (_promptIndicator != null)
                _promptIndicator.SetActive(_promptVisible);
            if (_glowHighlight != null)
                _glowHighlight.SetActive(_glowVisible);

            if (_consumed && _visualRenderer != null)
            {
                if (_openedSprite != null)
                    _visualRenderer.sprite = _openedSprite;
                _visualRenderer.color = _openedTint;
            }
        }

        /// <summary>
        /// Attempts to interact as the assigned Player.
        /// </summary>
        public bool TryInteract()
        {
            var p = ResolvePlayer();
            var actorGo = p != null ? p.gameObject : (ResolvePlayerCollider() != null ? ResolvePlayerCollider().gameObject : null);
            return TryInteract(actorGo, ResolvePlayerHealth(), ResolvePlayerCollider());
        }

        /// <summary>
        /// Creates a deferred grant delegate that re-validates all conditions when invoked (U06).
        /// </summary>
        public Func<bool> CreateDeferredGrant(GameObject actor, Health actorHealth, Collider2D actorCollider) =>
            () => TryInteract(actor, actorHealth, actorCollider);

        /// <summary>
        /// Validates all conditions at grant time, reports "Đầy máu" if full HP, or restores full HP once.
        /// Guards against synchronous re-entrancy triggered from Health.Changed.
        /// </summary>
        public bool TryInteract(GameObject actor, Health actorHealth, Collider2D actorCollider)
        {
            if (_consumed || _isGranting)
                return false;

            if (_run == null || !_run.IsPlaying || IsInputBlocked())
                return false;

            var expectedCollider = ResolvePlayerCollider();
            var expectedPlayer = ResolvePlayer();
            var expectedActor = expectedPlayer != null
                ? expectedPlayer.gameObject
                : (expectedCollider != null ? expectedCollider.gameObject : null);
            if (expectedActor != null && actor != expectedActor)
                return false;
            if (expectedCollider != null && actorCollider != expectedCollider)
                return false;

            if (actorHealth == null || !actorHealth.IsAlive)
                return false;

            if (!IsColliderInTrigger(actorCollider))
                return false;

            if (actorHealth.Current >= actorHealth.Max)
            {
                _lastFeedbackMessage = FullHealthMessage;
                if (_fullHealthFeedback != null)
                    _fullHealthFeedback.SetActive(true);
                FeedbackShown?.Invoke(FullHealthMessage);
                RefreshVisualState();
                return false;
            }

            _isGranting = true;
            try
            {
                actorHealth.RestoreFull();
                _consumed = true;
                _lastFeedbackMessage = string.Empty;
                if (_fullHealthFeedback != null)
                    _fullHealthFeedback.SetActive(false);
                if (_vfx != null && _healVfx != null)
                    _vfx.Play(_healVfx, transform.position + Vector3.up * 0.8f);
                CombatAudioHub.Play(SfxId.ChestHeal);
                RefreshVisualState();
                Healed?.Invoke();
                return true;
            }
            finally
            {
                _isGranting = false;
            }
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
            return p != null && ((p.RunStats != null && p.RunStats.BlocksGameplayInput) || p.IsInScriptedSequence);
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
