using System.Collections;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Full-screen black transition fader and subtle top/bottom letterbox framing bars.
    /// Uses unscaled delta time so transitions stay deterministic across pause/hit-stop boundaries.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFadeOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fadeGroup;
        [SerializeField] private RectTransform _topBar;
        [SerializeField] private RectTransform _bottomBar;
        [SerializeField] private float _barMaxHeight = 96f;

        public bool IsTransitioning { get; private set; }
        public float CurrentAlpha { get; private set; }
        public float LetterboxAmount { get; private set; }

        public void Configure(CanvasGroup fadeGroup, RectTransform topBar, RectTransform bottomBar, float barMaxHeight = 96f)
        {
            _fadeGroup = fadeGroup;
            _topBar = topBar;
            _bottomBar = bottomBar;
            _barMaxHeight = Mathf.Max(0f, barMaxHeight);
            ResetImmediate();
        }

        private void Awake() => ResetImmediate();

        private void OnDisable() => ResetImmediate();

        public void SetTransitioning(bool transitioning) => IsTransitioning = transitioning;

        public void SetAlphaImmediate(float alpha)
        {
            CurrentAlpha = Mathf.Clamp01(alpha);
            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = CurrentAlpha;
                _fadeGroup.blocksRaycasts = CurrentAlpha > 0.01f;
            }
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float clampedTarget = Mathf.Clamp01(targetAlpha);
            if (duration <= 0f)
            {
                SetAlphaImmediate(clampedTarget);
                yield break;
            }

            float start = CurrentAlpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlphaImmediate(Mathf.Lerp(start, clampedTarget, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            SetAlphaImmediate(clampedTarget);
        }

        public void SetLetterbox(float normalized)
        {
            LetterboxAmount = Mathf.Clamp01(normalized);
            float height = _barMaxHeight * LetterboxAmount;
            ApplyBarHeight(_topBar, height);
            ApplyBarHeight(_bottomBar, height);
        }

        public IEnumerator SlideLetterbox(float targetNormalized, float duration)
        {
            float target = Mathf.Clamp01(targetNormalized);
            if (duration <= 0f)
            {
                SetLetterbox(target);
                yield break;
            }

            float start = LetterboxAmount;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetLetterbox(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            SetLetterbox(target);
        }

        public void ResetImmediate()
        {
            IsTransitioning = false;
            SetAlphaImmediate(0f);
            SetLetterbox(0f);
        }

        private static void ApplyBarHeight(RectTransform bar, float height)
        {
            if (bar == null)
                return;
            Vector2 size = bar.sizeDelta;
            size.y = height;
            bar.sizeDelta = size;
            bar.gameObject.SetActive(height > 0.5f);
        }
    }
}
