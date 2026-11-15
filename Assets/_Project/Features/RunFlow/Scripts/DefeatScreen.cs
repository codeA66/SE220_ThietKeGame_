using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    public sealed class DefeatScreen : MonoBehaviour
    {
        [SerializeField] private RunSession _run;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _retry;
        [SerializeField, Min(0f)] private float _revealDelay = 0.4f;

        public event Action RetryRequested;

        private void OnEnable()
        {
            _run.Defeated += ShowAfterDeath;
            _retry.onClick.AddListener(RequestRetry);
            if (_run.IsDefeated)
                ShowAfterDeath();
        }

        private void OnDisable()
        {
            if (_run != null)
                _run.Defeated -= ShowAfterDeath;
            if (_retry != null)
                _retry.onClick.RemoveListener(RequestRetry);
            StopAllCoroutines();
        }

        public void SetRetryAvailable(bool available)
        {
            if (_retry == null)
                return;

            _retry.interactable = available;
            if (available && _panel.activeInHierarchy)
                _retry.Select();
        }

        private void ShowAfterDeath() => StartCoroutine(Reveal());

        private IEnumerator Reveal()
        {
            yield return new WaitForSeconds(_revealDelay);
            _panel.SetActive(true);
            if (_retry.interactable)
                _retry.Select();
        }

        private void RequestRetry() => RetryRequested?.Invoke();
    }
}
