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
        private Vector2 _punchDir;
        private float _punchAmplitude;
        private float _punchDuration;
        private float _punchUntil = -1f;
        private float _defaultOrthoSize;
        private float _targetOrthoSize;
        private float _orthoVelocity;
        private bool _focusOverrideActive;
        private Vector2 _focusOverrideTarget;
        private float _overrideSmoothTime = 1.2f;
        private bool _bypassBoundsDuringOverride;
        private bool _isReturningFromOverride;
        private float _returnSmoothTime = 0.6f;

        public bool IsFocusOverrideActive => _focusOverrideActive;
        public bool UseBounds => _useBounds;
        public Rect CurrentBounds => _bounds;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _basePosition = transform.position;
            _defaultOrthoSize = _camera != null ? _camera.orthographicSize : 7f;
            _targetOrthoSize = _defaultOrthoSize;
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

        private void LateUpdate() => StepCamera(Time.deltaTime);

        public void SetBounds(bool useBounds, Rect bounds)
        {
            _useBounds = useBounds;
            _bounds = bounds;
        }

        public void BeginFocusOverride(Vector2 worldTarget, float smoothTime, float? targetOrthoSize = null, bool bypassBounds = false)
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_defaultOrthoSize <= 0f && _camera != null)
                _defaultOrthoSize = _camera.orthographicSize;

            _focusOverrideActive = true;
            _isReturningFromOverride = false;
            _focusOverrideTarget = worldTarget;
            _overrideSmoothTime = Mathf.Max(0.01f, smoothTime);
            _targetOrthoSize = targetOrthoSize ?? _defaultOrthoSize;
            _bypassBoundsDuringOverride = bypassBounds;
        }

        public void EndFocusOverride(float returnSmoothTime = -1f)
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            _focusOverrideActive = false;
            _bypassBoundsDuringOverride = false;
            _targetOrthoSize = _defaultOrthoSize;
            _isReturningFromOverride = returnSmoothTime > 0f;
            _returnSmoothTime = returnSmoothTime > 0f ? returnSmoothTime : _smoothTime;
            if (returnSmoothTime == 0f && _camera != null && _camera.orthographic && _defaultOrthoSize > 0f)
            {
                _camera.orthographicSize = _defaultOrthoSize;
                _orthoVelocity = 0f;
            }
            if (_target != null)
            {
                _focus = (Vector2)_target.position + _offset;
                _lastTargetPosition = _target.position;
                _lookAheadCurrent = 0f;
                _lookAheadVelocity = 0f;
            }
        }

        public void StepCamera(float deltaTime)
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (deltaTime <= 0f)
            {
                // Still apply unscaled Shake and Punch offsets while HitStop freezes scaled deltaTime.
                transform.position = _basePosition + ShakeOffset() + PunchOffset();
                return;
            }
            if (!_focusOverrideActive && _target == null)
                return;

            float activeSmooth;
            Vector3 desired;
            if (_focusOverrideActive)
            {
                activeSmooth = _overrideSmoothTime;
                DampOrthoSize(_targetOrthoSize, activeSmooth, deltaTime);
                desired = new Vector3(_focusOverrideTarget.x, _focusOverrideTarget.y, transform.position.z);
                if (_useBounds && !_bypassBoundsDuringOverride)
                    desired = ClampToBounds(desired);
            }
            else
            {
                Vector2 targetPos = (Vector2)_target.position + _offset;

                // Deadzone: move the focus point only by how far the target leaves the box.
                Vector2 delta = targetPos - _focus;
                if (Mathf.Abs(delta.x) > _deadzone.x)
                    _focus.x += delta.x - Mathf.Sign(delta.x) * _deadzone.x;
                if (Mathf.Abs(delta.y) > _deadzone.y)
                    _focus.y += delta.y - Mathf.Sign(delta.y) * _deadzone.y;

                float speedX = (_target.position.x - _lastTargetPosition.x) / Mathf.Max(deltaTime, 0.0001f);
                _lastTargetPosition = _target.position;
                float lookAheadGoal = Mathf.Abs(speedX) > _lookAheadSpeedThreshold ? Mathf.Sign(speedX) * _lookAhead : _lookAheadCurrent;
                _lookAheadCurrent = Mathf.SmoothDamp(_lookAheadCurrent, lookAheadGoal, ref _lookAheadVelocity, _lookAheadSmoothTime, Mathf.Infinity, deltaTime);

                activeSmooth = _isReturningFromOverride ? _returnSmoothTime : _smoothTime;
                DampOrthoSize(_defaultOrthoSize, activeSmooth, deltaTime);

                desired = new Vector3(_focus.x + _lookAheadCurrent, _focus.y, transform.position.z);
                if (_useBounds)
                    desired = ClampToBounds(desired);
            }

            _basePosition = Vector3.SmoothDamp(_basePosition, desired, ref _velocity, activeSmooth, Mathf.Infinity, deltaTime);
            transform.position = _basePosition + ShakeOffset() + PunchOffset();

            if (_isReturningFromOverride &&
                (_basePosition - desired).sqrMagnitude < 0.0025f &&
                (_camera == null || Mathf.Abs(_camera.orthographicSize - _defaultOrthoSize) < 0.01f))
            {
                _isReturningFromOverride = false;
            }
        }

        private void DampOrthoSize(float targetSize, float smoothTime, float deltaTime)
        {
            if (_camera == null || !_camera.orthographic || targetSize <= 0f)
                return;
            if (Mathf.Abs(_camera.orthographicSize - targetSize) <= 0.001f)
            {
                _camera.orthographicSize = targetSize;
                _orthoVelocity = 0f;
                return;
            }
            _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, targetSize, ref _orthoVelocity, smoothTime, Mathf.Infinity, deltaTime);
        }

        /// <summary>
        /// Immediately warps the camera to the target focus + offset (clamped to bounds if enabled)
        /// and resets SmoothDamp/look-ahead state so long-distance teleports have zero lag.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null)
                return;
            if (_camera == null)
                _camera = GetComponent<Camera>();
            _focus = (Vector2)_target.position + _offset;
            _lastTargetPosition = _target.position;
            _lookAheadCurrent = 0f;
            _lookAheadVelocity = 0f;
            _velocity = Vector3.zero;
            var desired = new Vector3(_focus.x, _focus.y, transform.position.z);
            if (_useBounds && _camera != null)
                desired = ClampToBounds(desired);
            _basePosition = desired;
            transform.position = _basePosition + ShakeOffset() + PunchOffset();
        }

        public Vector3 CurrentPunchOffset => PunchOffset();

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

        /// <summary>
        /// Directional kick decaying quadratically over unscaled time so impacts have directional punch.
        /// </summary>
        public void Punch(Vector2 direction, float amplitude, float duration)
        {
            if (duration <= 0f || amplitude <= 0f || direction.sqrMagnitude < 0.0001f)
                return;
            float remaining = _punchUntil - Time.unscaledTime;
            float currentAmp = remaining > 0f && _punchDuration > 0f
                ? _punchAmplitude * Mathf.Pow(Mathf.Clamp01(remaining / _punchDuration), 2f)
                : 0f;
            if (amplitude < currentAmp)
                return;
            _punchDir = direction.normalized;
            _punchAmplitude = amplitude;
            _punchDuration = duration;
            _punchUntil = Time.unscaledTime + duration;
        }

        private Vector3 PunchOffset()
        {
            float remaining = _punchUntil - Time.unscaledTime;
            if (remaining <= 0f || _punchDuration <= 0f)
                return Vector3.zero;
            float t = 1f - Mathf.Clamp01(remaining / _punchDuration);
            float falloff = (1f - t) * (1f - t);
            return new Vector3(_punchDir.x, _punchDir.y, 0f) * (_punchAmplitude * falloff);
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
