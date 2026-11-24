using System;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// uGUI result overlay controller. Stays active on its root GameObject to receive LevelRunController.Finished,
    /// toggles only its visual PanelRoot, displays "Thắng" or "Thua", and wires the single "Chơi lại" button to TryReplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultPanel : MonoBehaviour
    {
        public const string WinTitle = "Thắng";
        public const string LossTitle = "Thua";
        private static readonly Color WinTitleColor = new Color(1f, 0.86f, 0.35f, 1f);
        private static readonly Color LossTitleColor = new Color(0.95f, 0.28f, 0.28f, 1f);

        [SerializeField] private LevelRunController _run;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private UnityEngine.UI.Text _titleText;
        [SerializeField] private UnityEngine.UI.Text _subtitleText;
        [SerializeField] private UnityEngine.UI.Button _replayButton;

        private int _showCount;
        private LevelOutcome? _displayedOutcome;

        public LevelRunController Run => _run;
        public GameObject PanelRoot => _panelRoot;
        public UnityEngine.UI.Text TitleText => _titleText;
        public UnityEngine.UI.Text SubtitleText => _subtitleText;
        public UnityEngine.UI.Button ReplayButton => _replayButton;
        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;
        public int ShowCount => _showCount;
        public LevelOutcome? DisplayedOutcome => _displayedOutcome;

        public event Action<LevelOutcome> PanelShown;

        public void Configure(
            LevelRunController run,
            GameObject panelRoot,
            UnityEngine.UI.Text titleText,
            UnityEngine.UI.Button replayButton,
            UnityEngine.UI.Text subtitleText = null)
        {
            Unsubscribe();
            _run = run;
            _panelRoot = panelRoot;
            _titleText = titleText;
            _subtitleText = subtitleText;
            _replayButton = replayButton;
            _showCount = 0;
            _displayedOutcome = null;
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void Awake()
        {
            if (_panelRoot != null && (_run == null || !_run.HasOutcome))
                _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            Subscribe();
            if (_run != null && _run.HasOutcome && _showCount == 0)
                OnRunFinished(_run.Outcome);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnRunFinished(LevelOutcome outcome)
        {
            _showCount++;
            _displayedOutcome = outcome;

            if (_titleText != null)
            {
                _titleText.text = outcome == LevelOutcome.Win ? WinTitle : LossTitle;
                _titleText.color = outcome == LevelOutcome.Win ? WinTitleColor : LossTitleColor;
            }

            if (_subtitleText != null)
            {
                int totalSeconds = Mathf.FloorToInt(Mathf.Max(0f, _run != null ? _run.ElapsedRunTime : 0f));
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                _subtitleText.text = $"{minutes:00}:{seconds:00} — Nhấn [R] hoặc Enter để chơi lại";
            }

            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            if (_replayButton != null)
            {
                _replayButton.interactable = true;
                _replayButton.Select();
            }

            PanelShown?.Invoke(outcome);
        }

        private void OnReplayClicked()
        {
            if (_run != null && _run.TryReplay() && _replayButton != null)
                _replayButton.interactable = false;
        }

        private void Subscribe()
        {
            if (_run != null)
                _run.Finished += OnRunFinished;
            if (_replayButton != null)
                _replayButton.onClick.AddListener(OnReplayClicked);
        }

        private void Unsubscribe()
        {
            if (_run != null)
                _run.Finished -= OnRunFinished;
            if (_replayButton != null)
                _replayButton.onClick.RemoveListener(OnReplayClicked);
        }
    }
}
