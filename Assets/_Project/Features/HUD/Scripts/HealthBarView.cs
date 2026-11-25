using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    [DisallowMultipleComponent]
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [SerializeField] private RectTransform _trailFill;
        [SerializeField] private Text _value;
        [SerializeField, Min(0f)] private float _trailHoldDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float _trailShrinkSpeed = 1.5f;

        private float _currentFraction = 1f;
        private float _trailFraction = 1f;
        private float _holdTimer;
        private bool _initialized;

        public float CurrentFillFraction => _currentFraction;
        public float TrailFillFraction => _trailFraction;

        public RectTransform Fill => _fill;
        public RectTransform TrailFill => _trailFill;
        public Text Value => _value;

        public void Render(int current, int max)
        {
            float fraction = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (!_initialized)
            {
                _initialized = true;
                _currentFraction = fraction;
                _trailFraction = fraction;
                _holdTimer = 0f;
            }
            else if (fraction < _currentFraction)
            {
                _currentFraction = fraction;
                _holdTimer = _trailHoldDuration;
            }
            else
            {
                _currentFraction = fraction;
                if (_currentFraction >= _trailFraction)
                {
                    _trailFraction = _currentFraction;
                    _holdTimer = 0f;
                }
            }

            ApplyAnchor(_fill, _currentFraction);
            ApplyAnchor(_trailFill, _trailFraction);
            if (_value != null)
                _value.text = $"{current} / {max}";
        }

        private void Update() => TickTrail(Time.unscaledDeltaTime);

        public void TickTrail(float unscaledDeltaTime)
        {
            if (!_initialized || _trailFraction <= _currentFraction)
                return;

            float dt = Mathf.Max(0f, unscaledDeltaTime);
            if (_holdTimer > 0f)
            {
                float consumed = Mathf.Min(_holdTimer, dt);
                _holdTimer -= consumed;
                dt -= consumed;
            }

            if (dt > 0f)
            {
                _trailFraction = Mathf.MoveTowards(_trailFraction, _currentFraction, _trailShrinkSpeed * dt);
                ApplyAnchor(_trailFill, _trailFraction);
            }
        }

        private static void ApplyAnchor(RectTransform target, float fraction)
        {
            if (target == null)
                return;
            Vector2 end = target.anchorMax;
            end.x = fraction;
            target.anchorMax = end;
        }
    }
}
