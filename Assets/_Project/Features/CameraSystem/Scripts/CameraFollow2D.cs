using UnityEngine;

namespace DeadCells.CameraSystem
{
    /// <summary>
    /// Smooth 2D follow camera with a deadzone, horizontal look-ahead and optional level bounds.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        [Header("Framing")]
        [Tooltip("Offset from the target (units). Y lifts the view above the feet pivot.")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 1.5f);
        [Tooltip("Half-size of the box the target can move in without moving the camera (units).")]
        [SerializeField] private Vector2 _deadzone = new Vector2(1f, 0.5f);
        [Tooltip("How far ahead of the movement direction the camera looks (units).")]
        [SerializeField] private float _lookAhead = 1.5f;
        [Tooltip("Target speed (units/s) above which look-ahead switches direction.")]
        [SerializeField] private float _lookAheadSpeedThreshold = 0.5f;

        [Header("Smoothing")]
        [SerializeField] private float _smoothTime = 0.15f;
        [SerializeField] private float _lookAheadSmoothTime = 0.4f;

        [Header("Bounds (optional)")]
        [SerializeField] private bool _useBounds;
        [SerializeField] private Rect _bounds = new Rect(-20f, -5f, 40f, 30f);

        private Camera _camera;
        private Vector2 _focus;
        private Vector2 _lastTargetPosition;
        private float _lookAheadCurrent;
        private float _lookAheadVelocity;
        private Vector3 _velocity;
        private Vector3 _basePosition;
        private float _shakeAmplitude;
        private float _shakeDuration;
        private float _shakeUntil = -1f;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _basePosition = transform.position;
        }

        private void Start()
        {
            if (_target == null)
                return;
            _focus = (Vector2)_target.position + _offset;
            _lastTargetPosition = _target.position;
            transform.position = new Vector3(_focus.x, _focus.y, transform.position.z);
            _basePosition = transform.position;
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Vector2 targetPos = (Vector2)_target.position + _offset;

            // Deadzone: move the focus point only by how far the target leaves the box.
            Vector2 delta = targetPos - _focus;
            if (Mathf.Abs(delta.x) > _deadzone.x)
                _focus.x += delta.x - Mathf.Sign(delta.x) * _deadzone.x;
            if (Mathf.Abs(delta.y) > _deadzone.y)
                _focus.y += delta.y - Mathf.Sign(delta.y) * _deadzone.y;

            float speedX = (_target.position.x - _lastTargetPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);
            _lastTargetPosition = _target.position;
            float lookAheadGoal = Mathf.Abs(speedX) > _lookAheadSpeedThreshold ? Mathf.Sign(speedX) * _lookAhead : _lookAheadCurrent;
            _lookAheadCurrent = Mathf.SmoothDamp(_lookAheadCurrent, lookAheadGoal, ref _lookAheadVelocity, _lookAheadSmoothTime);

            var desired = new Vector3(_focus.x + _lookAheadCurrent, _focus.y, transform.position.z);
            if (_useBounds)
                desired = ClampToBounds(desired);

            _basePosition = Vector3.SmoothDamp(_basePosition, desired, ref _velocity, _smoothTime);
            transform.position = _basePosition + ShakeOffset();
        }

        /// <summary>
        /// Random jitter that decays linearly over the duration. Uses unscaled time so it still plays
        /// during hit-stop. A stronger request replaces a weaker one in progress.
        /// </summary>
        public void Shake(float amplitude, float duration)
        {
            float remaining = _shakeUntil - Time.unscaledTime;
            float currentAmp = remaining > 0f && _shakeDuration > 0f ? _shakeAmplitude * remaining / _shakeDuration : 0f;
            if (amplitude < currentAmp || duration <= 0f)
                return;
            _shakeAmplitude = amplitude;
            _shakeDuration = duration;
            _shakeUntil = Time.unscaledTime + duration;
        }

        private Vector3 ShakeOffset()
        {
            float remaining = _shakeUntil - Time.unscaledTime;
            if (remaining <= 0f)
                return Vector3.zero;
            float amp = _shakeAmplitude * remaining / _shakeDuration;
            return new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp), 0f);
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            position.x = Mathf.Clamp(position.x, _bounds.xMin + halfWidth, Mathf.Max(_bounds.xMin + halfWidth, _bounds.xMax - halfWidth));
            position.y = Mathf.Clamp(position.y, _bounds.yMin + halfHeight, Mathf.Max(_bounds.yMin + halfHeight, _bounds.yMax - halfHeight));
            return position;
        }

        private void OnDrawGizmosSelected()
        {
            if (_target == null)
                return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube((Vector2)_target.position + _offset, _deadzone * 2f);
            if (_useBounds)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(_bounds.center, _bounds.size);
            }
        }
    }
}
