using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Freezes time briefly on impact, optionally followed by a short slow-motion (DC: critical hits
    /// freeze one frame, then slow down for a few tenths of a second). Overlapping requests extend the
    /// effect instead of stacking. One per scene.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        private float _resumeAt = -1f;
        private float _slowUntil = -1f;
        private float _slowScale = 1f;
        private bool _isActive;

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
            if (Time.unscaledTime >= _resumeAt)
                Time.timeScale = _slowScale;
        }

        // Only touches timeScale while an effect runs, so other systems can still own it otherwise.
        private void Update()
        {
            if (!_isActive)
                return;
            float now = Time.unscaledTime;
            if (now < _resumeAt)
            {
                Time.timeScale = 0f;
                return;
            }
            if (now < _slowUntil)
            {
                Time.timeScale = _slowScale;
                return;
            }
            Clear();
        }

        private void OnDisable()
        {
            if (_isActive)
                Clear();
        }

        private void Clear()
        {
            _resumeAt = -1f;
            _slowUntil = -1f;
            _isActive = false;
            Time.timeScale = 1f;
        }
    }
}
