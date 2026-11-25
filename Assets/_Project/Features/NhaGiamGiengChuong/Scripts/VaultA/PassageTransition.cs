using System.Collections;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Interactive wall-fissure transition between the main prison corridor and Room_VaultA.
    /// Runs a black screen fade, ducks/restores dungeon ambient audio, toggles chamber & parallax
    /// roots, swaps CameraFollow2D bounds, and snaps camera cleanly behind the fade.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PassageTransition : MonoBehaviour
    {
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private BoxCollider2D _triggerZone;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _promptIndicator;
        [SerializeField] private ScreenFadeOverlay _fader;
        [SerializeField] private CameraFollow2D _cameraFollow;
        [SerializeField] private Vector2 _destination;
        [SerializeField] private int _arrivalFacing = 1;
        [SerializeField] private Rect _targetCameraBounds;
        [SerializeField] private bool _isEnteringChamber = true;
        [SerializeField] private GameObject _chamberRoot;
        [SerializeField] private GameObject _dungeonParallaxRoot;
        [SerializeField] private AudioSource _chamberAudio;
        [SerializeField] private float _chamberAudioVolume = 0.55f;
        [SerializeField] private ChamberFramingSequence _framingSequence;
        [SerializeField] private bool _startsUnlocked;
        [SerializeField] private float _fadeDuration = 0.35f;

        private InputAction _interactAction;
        private bool _isUnlocked;
        private bool _promptVisible;
        private bool _isChamberActive;
        private bool _runSubscribed;
        private Coroutine _activeCoroutine;

        public bool IsUnlocked => _isUnlocked;
        public bool PromptVisible => _promptVisible;
        public BoxCollider2D TriggerZone => _triggerZone;
        public GameObject PromptIndicator => _promptIndicator;
        public ScreenFadeOverlay Fader => _fader;
        public CameraFollow2D CameraFollow => _cameraFollow;

        public void Configure(
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCollider,
            BoxCollider2D triggerZone,
            InputActionAsset inputActions,
            GameObject promptIndicator,
            ScreenFadeOverlay fader,
            CameraFollow2D cameraFollow,
            Vector2 destination,
            int arrivalFacing,
            Rect targetCameraBounds,
            bool isEnteringChamber,
            GameObject chamberRoot,
            GameObject dungeonParallaxRoot,
            AudioSource chamberAudio,
            float chamberAudioVolume,
            ChamberFramingSequence framingSequence,
            bool startsUnlocked)
        {
            UnsubscribeInput();
            _run = run;
            _player = player;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _triggerZone = triggerZone;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            _fader = fader;
            _cameraFollow = cameraFollow;
            _destination = destination;
            _arrivalFacing = arrivalFacing == 0 ? 1 : arrivalFacing;
            _targetCameraBounds = targetCameraBounds;
            _isEnteringChamber = isEnteringChamber;
            _chamberRoot = chamberRoot;
            _dungeonParallaxRoot = dungeonParallaxRoot;
            _chamberAudio = chamberAudio;
            _chamberAudioVolume = Mathf.Clamp01(chamberAudioVolume);
            _framingSequence = framingSequence;
            _startsUnlocked = startsUnlocked;
            _isUnlocked = startsUnlocked;
            _isChamberActive = false;
            UnsubscribeRun();
            SubscribeRun();
            if (isActiveAndEnabled)
                SubscribeInput();
            RefreshPrompt();
        }

        private void Awake()
        {
            _isUnlocked = _startsUnlocked;
            SubscribeRun();
            if (_promptIndicator != null)
                _promptIndicator.SetActive(false);
        }

        private void OnEnable()
        {
            SubscribeRun();
            SubscribeInput();
            RefreshPrompt();
        }

        private void OnDisable()
        {
            UnsubscribeRun();
            UnsubscribeInput();
            CleanupIfActiveOrOccupied();
            if (_promptIndicator != null)
                _promptIndicator.SetActive(false);
            _promptVisible = false;
        }

        private void OnDestroy()
        {
            UnsubscribeRun();
            UnsubscribeInput();
            CleanupIfActiveOrOccupied();
        }

        private void SubscribeRun()
        {
            if (_runSubscribed || _run == null)
                return;
            _run.Finished += OnRunFinished;
            _runSubscribed = true;
        }

        private void UnsubscribeRun()
        {
            if (!_runSubscribed || _run == null)
                return;
            _run.Finished -= OnRunFinished;
            _runSubscribed = false;
        }

        private void OnRunFinished(LevelOutcome _) => CleanupIfActiveOrOccupied();

        private void CleanupIfActiveOrOccupied()
        {
            bool wasTransitioning = _activeCoroutine != null;
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
            if (!wasTransitioning && !_isChamberActive)
                return;

            _isChamberActive = false;
            if (_fader != null)
                _fader.ResetImmediate();
            if (_cameraFollow != null && _cameraFollow.IsFocusOverrideActive)
                _cameraFollow.EndFocusOverride(0f);
            if (_player != null && _player.IsInScriptedSequence)
                _player.EndScriptedSequence();
            if (_dungeonParallaxRoot != null)
                _dungeonParallaxRoot.SetActive(true);
            if (_chamberAudio != null && _chamberAudio.isPlaying)
                _chamberAudio.Stop();
            CombatAudioHub.SetAmbientMultiplier(1f);
        }

        private void Update() => RefreshPrompt();

        public void Unlock()
        {
            _isUnlocked = true;
            RefreshPrompt();
        }

        public bool TryBeginTransition()
        {
            if (!CanInteractNow())
                return false;

            _activeCoroutine = StartCoroutine(ExecuteTransitionRoutine());
            return true;
        }

        public IEnumerator ExecuteTransitionRoutine()
        {
            if (_fader != null)
                _fader.SetTransitioning(true);
            if (_promptIndicator != null)
                _promptIndicator.SetActive(false);
            _promptVisible = false;

            if (_player != null)
                _player.EnterTransitionLock();

            if (_isEnteringChamber)
            {
                CombatAudioHub.FadeAmbient(0f, _fadeDuration > 0f ? 0.5f : 0f);
            }
            else if (_chamberAudio != null && _chamberAudio.isPlaying)
            {
                if (_framingSequence != null)
                    _framingSequence.FadeChamberAudio(0f, _fadeDuration > 0f ? 0.5f : 0f, stopAtZero: true);
                else
                    yield return FadeAudio(_chamberAudio, 0f, _fadeDuration > 0f ? 0.5f : 0f, stopAtZero: true);
            }

            if (_fader != null)
                yield return _fader.FadeTo(1f, _fadeDuration);

            _isChamberActive = _isEnteringChamber;
            if (_chamberRoot != null)
                _chamberRoot.SetActive(_isEnteringChamber);
            if (_dungeonParallaxRoot != null)
                _dungeonParallaxRoot.SetActive(!_isEnteringChamber);

            if (_player != null)
                _player.TeleportTo(_destination, _arrivalFacing);

            if (_cameraFollow != null)
            {
                if (_cameraFollow.IsFocusOverrideActive)
                    _cameraFollow.EndFocusOverride(0f);
                _cameraFollow.SetBounds(true, _targetCameraBounds);
                _cameraFollow.SnapToTarget();
            }

            if (!_isEnteringChamber)
            {
                CombatAudioHub.FadeAmbient(1f, _fadeDuration > 0f ? 0.6f : 0f);
            }
            else if (_framingSequence != null && _framingSequence.HasPlayed && _chamberAudio != null && _chamberAudio.clip != null)
            {
                _framingSequence.FadeChamberAudio(_chamberAudioVolume, 0.6f, stopAtZero: false);
            }

            if (_fader != null)
                yield return _fader.FadeTo(0f, _fadeDuration);

            if (_player != null)
                _player.EndScriptedSequence();

            if (_fader != null)
                _fader.SetTransitioning(false);

            _activeCoroutine = null;
            RefreshPrompt();
        }

        private void RefreshPrompt()
        {
            bool visible = CanInteractNow();
            _promptVisible = visible;
            if (_promptIndicator != null && _promptIndicator.activeSelf != visible)
                _promptIndicator.SetActive(visible);
        }

        private bool CanInteractNow()
        {
            if (!_isUnlocked || _activeCoroutine != null || _run == null || !_run.IsPlaying)
                return false;
            if (_fader != null && _fader.IsTransitioning)
                return false;
            if (_player == null || _player.IsInScriptedSequence)
                return false;
            if (_playerHealth == null || !_playerHealth.IsAlive)
                return false;
            if (_player.RunStats != null && _player.RunStats.IsChoosing)
                return false;
            if (_triggerZone == null || _playerCollider == null)
                return false;
            return _triggerZone.bounds.Intersects(_playerCollider.bounds);
        }

        private static IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration, bool stopAtZero)
        {
            if (source == null)
                yield break;
            if (duration <= 0f)
            {
                source.volume = targetVolume;
                if (stopAtZero && targetVolume <= 0.001f)
                    source.Stop();
                yield break;
            }

            float start = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(start, targetVolume, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            source.volume = targetVolume;
            if (stopAtZero && targetVolume <= 0.001f)
                source.Stop();
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

        private void OnInteractStarted(InputAction.CallbackContext ctx) => TryBeginTransition();
    }
}
