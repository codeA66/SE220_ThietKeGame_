using System.Collections;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// One-shot unskippable framing sequence inside Room_VaultA: locks player input, waits for
    /// grounded contact if entered mid-air, hides gameplay HUD, slides letterbox bars, fades in
    /// gentle chamber piano audio, walks the player slowly to the viewing point while gliding the
    /// camera up onto WallTapestry_04, holds quietly, returns camera/HUD/control, and unlocks the
    /// WallInscription quote trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChamberFramingSequence : MonoBehaviour
    {
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private BoxCollider2D _triggerZone;
        [SerializeField] private CameraFollow2D _cameraFollow;
        [SerializeField] private ScreenFadeOverlay _fader;
        [SerializeField] private AudioSource _chamberAudio;
        [SerializeField] private float _targetAudioVolume = 0.55f;
        [SerializeField] private Canvas _gameplayHud;
        [SerializeField] private GameObject _statsStrip;
        [SerializeField] private WallInscription _inscription;
        [SerializeField] private float _walkTargetX = 190.0f;
        [SerializeField] private float _walkSpeedMultiplier = 0.38f;
        [SerializeField] private Vector2 _cameraFocusPoint = new Vector2(191.5f, 37.2f);
        [SerializeField] private float _focusSmoothTime = 1.5f;
        [SerializeField] private float _focusOrthoSize = 5.2f;
        [SerializeField] private float _holdDuration = 3.2f;
        [SerializeField] private float _returnSmoothTime = 0.9f;
        [SerializeField] private float _letterboxDuration = 0.4f;
        [SerializeField] private float _audioFadeInDuration = 1.0f;

        private bool _hasPlayed;
        private bool _isPlaying;
        private bool _hudWasVisible;
        private bool _stripWasVisible;
        private bool _runSubscribed;
        private Coroutine _activeCoroutine;
        private Coroutine _audioFadeCoroutine;

        public bool HasPlayed => _hasPlayed;
        public bool IsPlaying => _isPlaying;
        public BoxCollider2D TriggerZone => _triggerZone;
        public AudioSource ChamberAudio => _chamberAudio;
        public Vector2 CameraFocusPoint => _cameraFocusPoint;
        public float FocusOrthoSize => _focusOrthoSize;
        public WallInscription Inscription => _inscription;

        public void Configure(
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCollider,
            BoxCollider2D triggerZone,
            CameraFollow2D cameraFollow,
            ScreenFadeOverlay fader,
            AudioSource chamberAudio,
            float targetAudioVolume,
            Canvas gameplayHud,
            GameObject statsStrip,
            WallInscription inscription,
            float walkTargetX = 190.0f,
            float walkSpeedMultiplier = 0.38f,
            Vector2? cameraFocusPoint = null,
            float focusSmoothTime = 1.5f,
            float focusOrthoSize = 5.2f,
            float holdDuration = 3.2f,
            float returnSmoothTime = 0.9f)
        {
            _run = run;
            _player = player;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _triggerZone = triggerZone;
            _cameraFollow = cameraFollow;
            _fader = fader;
            _chamberAudio = chamberAudio;
            _targetAudioVolume = Mathf.Clamp01(targetAudioVolume);
            _gameplayHud = gameplayHud;
            _statsStrip = statsStrip;
            _inscription = inscription;
            _walkTargetX = walkTargetX;
            _walkSpeedMultiplier = walkSpeedMultiplier;
            _cameraFocusPoint = cameraFocusPoint ?? new Vector2(191.5f, 37.2f);
            _focusSmoothTime = focusSmoothTime;
            _focusOrthoSize = focusOrthoSize;
            _holdDuration = holdDuration;
            _returnSmoothTime = returnSmoothTime;
            _hasPlayed = false;
            _isPlaying = false;
            UnsubscribeRun();
            SubscribeRun();
        }

        private void Awake() => SubscribeRun();

        private void OnEnable() => SubscribeRun();

        private void Update()
        {
            if (_hasPlayed || _isPlaying)
                return;
            if (!CanTriggerNow())
                return;
            TryTrigger();
        }

        private void OnDisable()
        {
            UnsubscribeRun();
            StopAudioFade();
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
            if (_isPlaying)
                RestoreStateAfterInterrupt(restoreHud: _run == null || _run.IsPlaying);
        }

        private void OnDestroy() => UnsubscribeRun();

        public void FadeChamberAudio(float targetVolume, float duration, bool stopAtZero)
        {
            if (_chamberAudio == null)
                return;
            StopAudioFade();
            float clamped = Mathf.Clamp01(targetVolume);
            if (clamped > 0.001f && _chamberAudio.clip != null && !_chamberAudio.isPlaying)
            {
                _chamberAudio.loop = true;
                _chamberAudio.volume = 0f;
                _chamberAudio.Play();
            }

            if (duration <= 0f || !isActiveAndEnabled || !Application.isPlaying)
            {
                _chamberAudio.volume = clamped;
                if (stopAtZero && clamped <= 0.001f)
                    _chamberAudio.Stop();
                return;
            }
            _audioFadeCoroutine = StartCoroutine(FadeAudioRoutine(clamped, duration, stopAtZero));
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

        private void OnRunFinished(LevelOutcome _)
        {
            StopAudioFade();
            if (_chamberAudio != null && _chamberAudio.isPlaying)
                _chamberAudio.Stop();
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
            if (_isPlaying)
                RestoreStateAfterInterrupt(restoreHud: false);
        }

        public bool TryTrigger()
        {
            if (!CanTriggerNow())
                return false;
            _activeCoroutine = StartCoroutine(ExecuteSequenceRoutine());
            return true;
        }

        public IEnumerator ExecuteSequenceRoutine()
        {
            _hasPlayed = true;
            _isPlaying = true;

            CaptureAndHideHud();

            if (_player != null)
                _player.EnterTransitionLock();

            StartChamberAudioFadeIn();

            // Wait for feet to touch solid floor if player entered mid-jump/roll
            float groundWait = 0f;
            while (_player != null && _player.Context != null && !_player.Context.IsGrounded && groundWait < 2.0f)
            {
                groundWait += Time.deltaTime;
                yield return null;
            }

            if (_player != null)
                _player.BeginScriptedWalk(_walkTargetX, _walkSpeedMultiplier);

            if (_cameraFollow != null)
                _cameraFollow.BeginFocusOverride(_cameraFocusPoint, _focusSmoothTime, _focusOrthoSize, bypassBounds: false);

            if (_fader != null)
            {
                float lbDur = _holdDuration > 0f ? _letterboxDuration : 0f;
                if (lbDur > 0f)
                    StartCoroutine(_fader.SlideLetterbox(1f, lbDur));
                else
                    _fader.SetLetterbox(1f);
            }

            // Wait until player reaches viewing point (bounded by safety timeout)
            float walkElapsed = 0f;
            while (_player != null && !_player.HasArrivedAtScriptedTarget && walkElapsed < 4.5f)
            {
                walkElapsed += Time.deltaTime;
                yield return null;
            }

            // Quiet hold on illuminated tapestry + gentle piano (zero text)
            if (_holdDuration > 0f)
            {
                float held = 0f;
                while (held < _holdDuration)
                {
                    held += Time.deltaTime;
                    yield return null;
                }
            }

            if (_cameraFollow != null)
                _cameraFollow.EndFocusOverride(_returnSmoothTime);

            if (_fader != null)
            {
                float lbDur = _returnSmoothTime > 0f ? _letterboxDuration : 0f;
                yield return _fader.SlideLetterbox(0f, lbDur);
            }

            if (_returnSmoothTime > 0f)
            {
                float settle = Mathf.Max(0f, _returnSmoothTime - _letterboxDuration + 0.15f);
                float waited = 0f;
                while (waited < settle)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }

            RestoreHud();
            if (_player != null)
                _player.EndScriptedSequence();
            if (_inscription != null)
                _inscription.Unlock();

            _isPlaying = false;
            _activeCoroutine = null;
        }

        private bool CanTriggerNow()
        {
            if (_hasPlayed || _isPlaying)
                return false;
            if (_run == null || !_run.IsPlaying)
                return false;
            if (_fader != null && _fader.IsTransitioning)
                return false;
            if (_player == null || _player.IsInScriptedSequence)
                return false;
            if (_playerHealth == null || !_playerHealth.IsAlive)
                return false;
            if (_triggerZone == null || _playerCollider == null)
                return false;
            return _triggerZone.bounds.Intersects(_playerCollider.bounds);
        }

        private void CaptureAndHideHud()
        {
            if (_gameplayHud != null)
            {
                _hudWasVisible = _gameplayHud.enabled;
                _gameplayHud.enabled = false;
            }
            if (_statsStrip != null)
            {
                _stripWasVisible = _statsStrip.activeSelf;
                _statsStrip.SetActive(false);
            }
        }

        private void RestoreHud()
        {
            if (_gameplayHud != null)
                _gameplayHud.enabled = _hudWasVisible;
            if (_statsStrip != null)
                _statsStrip.SetActive(_stripWasVisible);
        }

        private void RestoreStateAfterInterrupt(bool restoreHud)
        {
            _isPlaying = false;
            if (restoreHud)
                RestoreHud();
            if (_fader != null)
                _fader.SetLetterbox(0f);
            if (_cameraFollow != null && _cameraFollow.IsFocusOverrideActive)
                _cameraFollow.EndFocusOverride(0f);
            if (_player != null && _player.IsInScriptedSequence)
                _player.EndScriptedSequence();
        }

        private void StartChamberAudioFadeIn() =>
            FadeChamberAudio(_targetAudioVolume, _audioFadeInDuration, stopAtZero: false);

        private void StopAudioFade()
        {
            if (_audioFadeCoroutine != null)
            {
                StopCoroutine(_audioFadeCoroutine);
                _audioFadeCoroutine = null;
            }
        }

        private IEnumerator FadeAudioRoutine(float targetVolume, float duration, bool stopAtZero)
        {
            float start = _chamberAudio.volume;
            float elapsed = 0f;
            while (elapsed < duration && _chamberAudio != null)
            {
                elapsed += Time.unscaledDeltaTime;
                _chamberAudio.volume = Mathf.Lerp(start, targetVolume, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (_chamberAudio != null)
            {
                _chamberAudio.volume = targetVolume;
                if (stopAtZero && targetVolume <= 0.001f)
                    _chamberAudio.Stop();
            }
            _audioFadeCoroutine = null;
        }
    }
}
