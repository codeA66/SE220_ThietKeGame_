using UnityEngine;

namespace DeadCells.CameraSystem
{
    /// <summary>
    /// Updates background/foreground parallax layers strictly after <see cref="CameraFollow2D"/>
    /// with optional horizontal wrapping and pixel-grid snapping.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ParallaxLayer2D : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;
        [Tooltip("0 = locked to camera, 1 = world static, 0.15 = far background depth.")]
        [SerializeField] private Vector2 _parallaxFactor = new Vector2(0.15f, 0f);
        [Tooltip("Horizontal wrap span in world units. Values <= 0.01 disable wrapping.")]
        [SerializeField] private float _wrapWidthUnits;
        [SerializeField] private bool _pixelSnap = true;
        [SerializeField] private float _pixelsPerUnit = 24f;
        [SerializeField] private bool _lockYToCameraStartOffset;
        [SerializeField] private float _referenceCameraY;
        [SerializeField, HideInInspector] private Vector3 _originPosition;
        [SerializeField, HideInInspector] private bool _hasOrigin;

        private float _wrapAnchorX;

        private void Awake()
        {
            if (!_hasOrigin)
            {
                _originPosition = transform.position;
                _hasOrigin = true;
            }

            _wrapAnchorX = 0f;
        }

        private void Start()
        {
            ResolveCameraIfNeeded();
            if (_cameraTransform != null)
                UpdateParallax(_cameraTransform.position);
        }

        private void LateUpdate()
        {
            ResolveCameraIfNeeded();
            if (_cameraTransform != null)
                UpdateParallax(_cameraTransform.position);
        }

        public void Configure(
            Transform cameraTransform,
            Vector2 parallaxFactor,
            float wrapWidthUnits,
            bool pixelSnap = true,
            float pixelsPerUnit = 24f)
        {
            _cameraTransform = cameraTransform;
            _parallaxFactor = parallaxFactor;
            _wrapWidthUnits = wrapWidthUnits;
            _pixelSnap = pixelSnap;
            _pixelsPerUnit = pixelsPerUnit;
            _originPosition = transform.position;
            _hasOrigin = true;
            _wrapAnchorX = 0f;
        }

        public void ConfigureVerticalCameraLock(bool lockToCameraTop, float referenceCameraY)
        {
            _lockYToCameraStartOffset = lockToCameraTop;
            _referenceCameraY = referenceCameraY;
        }

        public void UpdateParallax(Vector3 cameraPosition)
        {
            if (!_hasOrigin)
            {
                _originPosition = transform.position;
                _hasOrigin = true;
            }

            float offsetX = (cameraPosition.x - _originPosition.x) * (1f - _parallaxFactor.x);
            float offsetY = _lockYToCameraStartOffset
                ? (cameraPosition.y - _referenceCameraY)
                : (cameraPosition.y - _originPosition.y) * (1f - _parallaxFactor.y);

            if (_wrapWidthUnits > 0.01f)
            {
                float rawX = _originPosition.x + _wrapAnchorX + offsetX;
                float deltaX = cameraPosition.x - rawX;
                if (Mathf.Abs(deltaX) > _wrapWidthUnits * 0.5f)
                {
                    float wrapSteps = Mathf.Round(deltaX / _wrapWidthUnits);
                    _wrapAnchorX += wrapSteps * _wrapWidthUnits;
                }
            }

            float targetX = _originPosition.x + _wrapAnchorX + offsetX;
            float targetY = _originPosition.y + offsetY;

            if (_pixelSnap && _pixelsPerUnit > 0f)
            {
                targetX = Mathf.Round(targetX * _pixelsPerUnit) / _pixelsPerUnit;
                targetY = Mathf.Round(targetY * _pixelsPerUnit) / _pixelsPerUnit;
            }

            transform.position = new Vector3(targetX, targetY, transform.position.z);
        }

        private void ResolveCameraIfNeeded()
        {
            if (_cameraTransform != null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                _cameraTransform = mainCamera.transform;
        }
    }
}
