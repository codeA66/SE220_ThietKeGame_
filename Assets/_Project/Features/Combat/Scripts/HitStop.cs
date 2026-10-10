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

        public void Trigger(float duration)
        {
            if (duration <= 0f)
                return;
            _resumeAt = Mathf.Max(_resumeAt, Time.unscaledTime + duration);
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (_resumeAt >= 0f && Time.unscaledTime >= _resumeAt)
            {
                _resumeAt = -1f;
                Time.timeScale = 1f;
            }
        }

        private void OnDisable()
        {
            if (_resumeAt >= 0f)
                Time.timeScale = 1f;
            _resumeAt = -1f;
        }
    }
}
