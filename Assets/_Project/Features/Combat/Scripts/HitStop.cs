using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Freezes time briefly on impact, optionally followed by a short slow-motion (DC: critical hits
    /// freeze one frame, then slow down for a few tenths of a second). Overlapping requests extend the
    /// effect instead of stacking. A menu can also pause gameplay through it, which wins over both.
    /// One per scene.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        private float _resumeAt = -1f;
        private float _slowUntil = -1f;
        private float _slowScale = 1f;
        private bool _isActive;
        private bool _menuPaused;

        public void Trigger(float duration)
        {
            if (duration <= 0f)
                return;
            _resumeAt = Mathf.Max(_resumeAt, Time.unscaledTime + duration);
            _isActive = true;
            Time.timeScale = 0f;
        }

        /// <summary>Runs time at scale for duration seconds (real time), after any freeze ends.</summary>
        public void Slow(float duration, float scale)
        {
            if (duration <= 0f)
                return;
            _slowUntil = Mathf.Max(_slowUntil, Time.unscaledTime + duration);
            _slowScale = Mathf.Clamp01(scale);
            _isActive = true;
            ApplyTimeScale();
        }

        public void SetMenuPaused(bool paused)
        {
            if (!isActiveAndEnabled)
                return;

            _menuPaused = paused;
            ApplyTimeScale();
        }

        // Only touches timeScale while an effect or a menu pause runs, so other systems can own it otherwise.
        private void Update()
        {
            if (!_isActive)
                return;
            float now = Time.unscaledTime;
            if (now >= _resumeAt && now >= _slowUntil)
            {
                _resumeAt = -1f;
                _slowUntil = -1f;
                _isActive = false;
            }
            ApplyTimeScale();
        }

        private void OnDisable()
        {
            if (_isActive || _menuPaused)
                Time.timeScale = 1f;
            _resumeAt = -1f;
            _slowUntil = -1f;
            _isActive = false;
            _menuPaused = false;
        }

        private void ApplyTimeScale()
        {
            float now = Time.unscaledTime;
            if (_menuPaused || now < _resumeAt)
                Time.timeScale = 0f;
            else if (now < _slowUntil)
                Time.timeScale = _slowScale;
            else
                Time.timeScale = 1f;
        }
    }
}
