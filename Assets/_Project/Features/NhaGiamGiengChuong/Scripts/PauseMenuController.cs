using System;
using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Standalone uGUI pause overlay controller triggered by Esc / Gamepad Start during Playing.
    /// Rejects toggling during Intro, Ended, Reloading, or active ScrollUpgradeMenu selection,
    /// freezes HitStop/timeScale while disabling PlayerInputReader, and auto-closes if the run ends.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerInputReader _playerInput;
        [SerializeField] private HitStop _hitStop;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;

        private bool _isPaused;
        private bool _subscribed;

        public bool IsPaused => _isPaused;
        public bool CanPause =>
            _run != null &&
            _run.IsPlaying &&
            (_player == null || ((_player.RunStats == null || !_player.RunStats.IsChoosing) && !_player.IsInScriptedSequence));

        public Action QuitHandler { get; set; }

        public void Configure(
            LevelRunController run,
            PlayerController player,
            PlayerInputReader playerInput,
            HitStop hitStop,
            GameObject panelRoot,
            Button resumeButton,
            Button restartButton,
            Button quitButton)
        {
            Unsubscribe();
            _run = run;
            _player = player;
            _playerInput = playerInput;
            _hitStop = hitStop;
            _panelRoot = panelRoot;
            _resumeButton = resumeButton;
            _restartButton = restartButton;
            _quitButton = quitButton;
            _isPaused = false;

            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            if (isActiveAndEnabled)
                Subscribe();
        }

        private void Awake()
        {
            if (!_isPaused && _panelRoot != null)
                _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_isPaused)
            {
                _isPaused = false;
                if (_hitStop != null)
                    _hitStop.SetMenuPaused(false);
                else
                    Time.timeScale = 1f;
                if (_playerInput != null &&
                    (_hitStop == null || !_hitStop.IsMenuPaused) &&
                    (_run == null || (_run.IsPlaying && !_run.GameplayLocked)))
                    _playerInput.enabled = true;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool startPressed = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
            if (escPressed || startPressed)
                HandlePauseInput(escPressed);
        }

        public bool HandlePauseInput(bool escPressed)
        {
            if (escPressed && DeadCells.HUD.MinimapHud.IsAnyFullMapOpen)
            {
                DeadCells.HUD.MinimapHud.Active.SetFullMapOpen(false);
                return false;
            }
            if (escPressed && DeadCells.HUD.MinimapHud.LastEscapeCloseFrame == Time.frameCount)
                return false;
            return TogglePause();
        }

        public bool TogglePause() => _isPaused ? Resume() : OpenPause();

        public bool OpenPause()
        {
            if (_isPaused || !CanPause)
                return false;

            _isPaused = true;
            if (_panelRoot != null)
                _panelRoot.SetActive(true);
            if (_playerInput != null)
                _playerInput.enabled = false;
            if (_hitStop != null)
                _hitStop.SetMenuPaused(true);
            else
                Time.timeScale = 0f;

            if (_resumeButton != null)
            {
                _resumeButton.interactable = true;
                _resumeButton.Select();
            }

            return true;
        }

        public bool Resume()
        {
            if (!_isPaused)
                return false;

            _isPaused = false;
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
            if (_hitStop != null)
                _hitStop.SetMenuPaused(false);
            else
                Time.timeScale = 1f;

            if (_playerInput != null &&
                (_hitStop == null || !_hitStop.IsMenuPaused) &&
                (_run == null || (_run.IsPlaying && !_run.GameplayLocked)))
                _playerInput.enabled = true;

            return true;
        }

        public bool RestartRun()
        {
            if (_run == null || !_run.IsPlaying)
                return false;

            Resume();
            return _run.TryRestartFromPause();
        }

        public void QuitGame()
        {
            Resume();
            if (QuitHandler != null)
            {
                QuitHandler.Invoke();
                return;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;
            _subscribed = true;

            if (_run != null)
                _run.Finished += OnRunFinished;
            if (_resumeButton != null)
                _resumeButton.onClick.AddListener(OnResumeClicked);
            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestartClicked);
            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            _subscribed = false;

            if (_run != null)
                _run.Finished -= OnRunFinished;
            if (_resumeButton != null)
                _resumeButton.onClick.RemoveListener(OnResumeClicked);
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            if (_quitButton != null)
                _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnRunFinished(LevelOutcome _) => Resume();

        private void OnResumeClicked() => Resume();

        private void OnRestartClicked() => RestartRun();

        private void OnQuitClicked() => QuitGame();
    }
}
