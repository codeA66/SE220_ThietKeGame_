using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Interactive wall inscription inside VaultA: stays dormant until unlocked after the framing
    /// sequence, displays an [E] prompt while the player stands beneath the tapestry, toggles the
    /// quote banner on Interact, and auto-closes when walking away or when the run ends.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WallInscription : MonoBehaviour
    {
        [SerializeField] private string _title;
        [SerializeField, TextArea(2, 6)] private string _body;
        [SerializeField] private bool _startsUnlocked;
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private BoxCollider2D _triggerZone;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _promptIndicator;
        [SerializeField] private GameObject _bannerRoot;
        [SerializeField] private Text _bannerText;

        private InputAction _interactAction;
        private bool _isUnlocked;
        private bool _isReading;
        private bool _promptVisible;

        public bool IsUnlocked => _isUnlocked;
        public bool IsReading => _isReading;
        public bool PromptVisible => _promptVisible;
        public string Title => _title;
        public string Body => _body;
        public BoxCollider2D TriggerZone => _triggerZone;
        public GameObject PromptIndicator => _promptIndicator;
        public GameObject BannerRoot => _bannerRoot;

        public void Configure(
            string title,
            string body,
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCollider,
            BoxCollider2D triggerZone,
            InputActionAsset inputActions,
            GameObject promptIndicator,
            GameObject bannerRoot,
            Text bannerText,
            bool startsUnlocked = false)
        {
            UnsubscribeInput();
            _title = title;
            _body = body;
            _run = run;
            _player = player;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _triggerZone = triggerZone;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            _bannerRoot = bannerRoot;
            _bannerText = bannerText;
            _startsUnlocked = startsUnlocked;
            _isUnlocked = startsUnlocked;
            _isReading = false;
            _promptVisible = false;
            ApplyBannerText();
            if (isActiveAndEnabled)
                SubscribeInput();
            RefreshState();
        }

        private void Awake()
        {
            _isUnlocked = _startsUnlocked;
            ApplyBannerText();
            if (_promptIndicator != null)
                _promptIndicator.SetActive(false);
            if (_bannerRoot != null)
                _bannerRoot.SetActive(false);
        }

        private void OnEnable()
        {
            SubscribeInput();
            RefreshState();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            CloseBanner();
        }

        private void Update() => RefreshState();

        public void Unlock()
        {
            _isUnlocked = true;
            RefreshState();
        }

        public bool TryToggleReading()
        {
            if (!CanInteractNow())
                return false;

            _isReading = !_isReading;
            ApplyBannerText();
            RefreshState();
            return true;
        }

        public void RefreshState()
        {
            bool canInteract = CanInteractNow();
            if (!canInteract && _isReading)
                _isReading = false;

            _promptVisible = canInteract && !_isReading;
            if (_promptIndicator != null && _promptIndicator.activeSelf != _promptVisible)
                _promptIndicator.SetActive(_promptVisible);
            if (_bannerRoot != null && _bannerRoot.activeSelf != _isReading)
                _bannerRoot.SetActive(_isReading);
        }

        private bool CanInteractNow()
        {
            if (!_isUnlocked || _run == null || !_run.IsPlaying)
                return false;
            if (_player == null || _player.IsInScriptedSequence)
                return false;
            if (_playerHealth == null || !_playerHealth.IsAlive)
                return false;
            if (_player.RunStats != null && _player.RunStats.IsChoosing)
                return false;
            return IsPlayerInsideTrigger();
        }

        private bool IsPlayerInsideTrigger()
        {
            if (_triggerZone == null || _playerCollider == null)
                return false;
            return _triggerZone.bounds.Intersects(_playerCollider.bounds);
        }

        private void ApplyBannerText()
        {
            if (_bannerText == null)
                return;
            _bannerText.text = string.IsNullOrWhiteSpace(_title) ? _body : $"{_title}\n{_body}";
        }

        private void CloseBanner()
        {
            _isReading = false;
            _promptVisible = false;
            if (_promptIndicator != null)
                _promptIndicator.SetActive(false);
            if (_bannerRoot != null)
                _bannerRoot.SetActive(false);
        }

        private void SubscribeInput()
        {
            if (_interactAction != null || _inputActions == null)
                return;
            var map = _inputActions.FindActionMap(_actionMapName, throwIfNotFound: false);
            _interactAction = map?.FindAction(_interactActionName, throwIfNotFound: false);
            if (_interactAction != null)
                _interactAction.started += OnInteractStarted;
        }

        private void UnsubscribeInput()
        {
            if (_interactAction != null)
                _interactAction.started -= OnInteractStarted;
            _interactAction = null;
        }

        private void OnInteractStarted(InputAction.CallbackContext ctx) => TryToggleReading();
    }
}
