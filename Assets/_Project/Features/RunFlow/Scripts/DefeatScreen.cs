using System;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    public sealed class DefeatScreen : MonoBehaviour
    {
        public const string DefeatHeadingText = "Lượt chơi kết thúc";
        public const string DefeatDescriptionText = "Quay lại điểm xuất phát để bắt đầu lượt mới.";
        public const string VictoryHeadingText = "Hoàn thành màn!";
        public const string VictoryDescriptionText = "Bạn đã vượt qua khu giam giữ. Chọn Chơi lại để bắt đầu lượt mới.";

        [SerializeField] private RunSession _run;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _retry;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _description;
        [SerializeField, Min(0f)] private float _revealDelay = 0.4f;

        private bool _pendingReveal;
        private float _revealElapsedUnscaled;

        public float RevealDelay => _revealDelay;
        public event Action RetryRequested;

        private void OnEnable()
        {
            if (_run != null)
            {
                _run.Defeated += ShowAfterDeath;
                _run.Completed += ShowAfterVictory;
            }
            if (_retry != null)
                _retry.onClick.AddListener(RequestRetry);

            if (_run != null && _run.IsCompleted)
                ShowAfterVictory();
            else if (_run != null && _run.IsDefeated)
                ShowAfterDeath();
        }

        private void OnDisable()
        {
            if (_run != null)
            {
                _run.Defeated -= ShowAfterDeath;
                _run.Completed -= ShowAfterVictory;
            }
            if (_retry != null)
                _retry.onClick.RemoveListener(RequestRetry);
            _pendingReveal = false;
            _revealElapsedUnscaled = 0f;
        }

        private void Update()
        {
            if (_pendingReveal)
                TickUnscaled(Time.unscaledDeltaTime);
        }

        public void TickUnscaled(float unscaledDeltaTime)
        {
            if (!_pendingReveal)
                return;

            _revealElapsedUnscaled += Mathf.Max(0f, unscaledDeltaTime);
            if (_revealElapsedUnscaled < _revealDelay)
                return;

            _pendingReveal = false;
            RevealPanel(DefeatHeadingText, DefeatDescriptionText);
        }

        public void SetRetryAvailable(bool available)
        {
            if (_retry == null)
                return;

            _retry.interactable = available;
            if (available && _panel != null && _panel.activeInHierarchy)
                _retry.Select();
        }

        private void ShowAfterDeath()
        {
            if (_pendingReveal || (_panel != null && _panel.activeSelf))
                return;

            _pendingReveal = true;
            _revealElapsedUnscaled = 0f;
            if (_revealDelay <= 0f)
                TickUnscaled(0f);
        }

        private void ShowAfterVictory()
        {
            _pendingReveal = false;
            _revealElapsedUnscaled = 0f;
            RevealPanel(VictoryHeadingText, VictoryDescriptionText);
        }

        private void RevealPanel(string headingText, string descriptionText)
        {
            if (_heading != null)
                _heading.text = headingText;
            if (_description != null)
                _description.text = descriptionText;
            if (_panel != null)
                _panel.SetActive(true);
            if (_retry != null && _retry.interactable)
                _retry.Select();
        }

        private void RequestRetry() => RetryRequested?.Invoke();
    }
}
