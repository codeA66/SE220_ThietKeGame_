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
        private int _menuPauseCount;

        public bool IsMenuPaused => _menuPauseCount > 0;

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
            float start = Mathf.Max(Time.unscaledTime, _resumeAt);
            _slowUntil = Mathf.Max(_slowUntil, start + duration);
            _slowScale = Mathf.Clamp01(scale);
            _isActive = true;
            ApplyTimeScale();
        }

        public void SetMenuPaused(bool paused)
        {
            if (paused)
            {
                if (!isActiveAndEnabled)
                    return;
                _menuPauseCount++;
            }
            else
            {
                _menuPauseCount = Mathf.Max(0, _menuPauseCount - 1);
                if (!isActiveAndEnabled)
                    return;
            }

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
            if (_isActive || _menuPauseCount > 0)
                Time.timeScale = 1f;
            _resumeAt = -1f;
            _slowUntil = -1f;
            _isActive = false;
            _menuPauseCount = 0;
        }

        private void ApplyTimeScale()
        {
            float now = Time.unscaledTime;
            if (_menuPauseCount > 0 || now < _resumeAt)
                Time.timeScale = 0f;
            else if (now < _slowUntil)
                Time.timeScale = _slowScale;
            else
                Time.timeScale = 1f;
        }
    }
}
