using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Freezes time briefly on impact. Overlapping requests extend the freeze instead of stacking.
    /// One per scene.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        private float _resumeAt = -1f;
        private bool _menuPaused;

        public void Trigger(float duration)
        {
            if (duration <= 0f)
                return;
            _resumeAt = Mathf.Max(_resumeAt, Time.unscaledTime + duration);
            Time.timeScale = 0f;
        }

        public void SetMenuPaused(bool paused)
        {
            if (!isActiveAndEnabled)
                return;

            _menuPaused = paused;
            Time.timeScale = _menuPaused || _resumeAt >= 0f ? 0f : 1f;
        }

        private void Update()
        {
            if (_resumeAt >= 0f && Time.unscaledTime >= _resumeAt)
            {
                _resumeAt = -1f;
                Time.timeScale = _menuPaused ? 0f : 1f;
            }
        }

        private void OnDisable()
        {
            if (_resumeAt >= 0f || _menuPaused)
                Time.timeScale = 1f;
            _resumeAt = -1f;
            _menuPaused = false;
        }
    }
}
