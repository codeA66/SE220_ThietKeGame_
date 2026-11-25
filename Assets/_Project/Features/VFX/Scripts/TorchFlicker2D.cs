using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DeadCells.VFX
{
    /// <summary>
    /// Smoothly modulates a 2D torch <see cref="Light2D"/> using two incommensurate sine waves
    /// for organic breathing without harsh strobing.
    /// </summary>
    public class TorchFlicker2D : MonoBehaviour
    {
        [SerializeField] private Light2D _targetLight;
        [SerializeField] private float _baseIntensity = 1f;
        [SerializeField] private float _baseOuterRadius = 4f;
        [SerializeField, Range(0f, 0.5f)] private float _amplitude = 0.08f;
        [SerializeField] private float _speed = 3.2f;
        [SerializeField] private float _phaseOffset;

        private void Awake()
        {
            if (_targetLight == null)
                _targetLight = GetComponent<Light2D>();
        }

        private void Reset()
        {
            _targetLight = GetComponent<Light2D>();
            if (_targetLight != null)
            {
                _baseIntensity = _targetLight.intensity;
                _baseOuterRadius = _targetLight.pointLightOuterRadius;
            }
        }

        private void Update()
        {
            ApplyAtTime(Time.time);
        }

        public void Configure(
            Light2D targetLight,
            float baseIntensity,
            float baseOuterRadius,
            float amplitude = 0.08f,
            float speed = 3.2f,
            float phaseOffset = 0f)
        {
            _targetLight = targetLight;
            _baseIntensity = baseIntensity;
            _baseOuterRadius = baseOuterRadius;
            _amplitude = Mathf.Max(0f, amplitude);
            _speed = speed;
            _phaseOffset = phaseOffset;
        }

        public float EvaluateIntensity(float time)
        {
            float factor = 1f + EvaluateNormalizedWave(time) * Mathf.Max(0f, _amplitude);
            return _baseIntensity * factor;
        }

        public float EvaluateOuterRadius(float time)
        {
            float factor = 1f + EvaluateNormalizedWave(time) * Mathf.Max(0f, _amplitude);
            return _baseOuterRadius * factor;
        }

        public void ApplyAtTime(float time)
        {
            if (_targetLight == null)
                return;

            _targetLight.intensity = EvaluateIntensity(time);
            _targetLight.pointLightOuterRadius = EvaluateOuterRadius(time);
        }

        private float EvaluateNormalizedWave(float time)
        {
            float w = time * _speed + _phaseOffset;
            float combined = 0.65f * Mathf.Sin(w) + 0.35f * Mathf.Sin(w * 2.37f + 1.1f);
            return Mathf.Clamp(combined, -1f, 1f);
        }
    }
}
