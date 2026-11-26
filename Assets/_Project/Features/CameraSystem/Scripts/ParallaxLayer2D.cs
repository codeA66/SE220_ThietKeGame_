using System;
using System.Collections.Generic;
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
        [SerializeField] private bool _pinColumnsToCameraFrustumEdges;
        [SerializeField] private float _frustumEdgeVisibleInsetUnits = 0.46f;
        [SerializeField] private float _frustumOuterGuardOffsetUnits = 26f;
        [SerializeField, HideInInspector] private Vector3 _originPosition;
        [SerializeField, HideInInspector] private bool _hasOrigin;

        private float _wrapAnchorX;
        private Transform[] _pinnedColumns;
        private Vector3[] _pinnedColumnRestLocalPositions;
        private Camera _cachedCamera;
        private Transform _cachedCameraOwner;
        private float _leftInnerHalfW;
        private float _rightInnerHalfW;
        private bool _columnWidthsCached;

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

        public void ConfigureFrustumEdgePinning(
            bool enabled,
            float edgeVisibleInsetUnits = 0.46f,
            float outerGuardOffsetUnits = 26f)
        {
            _pinColumnsToCameraFrustumEdges = enabled;
            _frustumEdgeVisibleInsetUnits = edgeVisibleInsetUnits;
            _frustumOuterGuardOffsetUnits = outerGuardOffsetUnits;
            if (enabled)
                EnsureColumnRestStateCached();
        }

        public void ResetFrustumPinnedColumnsToRest()
        {
            EnsureColumnRestStateCached();
            if (_pinnedColumns == null)
                return;

            for (int i = 0; i < _pinnedColumns.Length; i++)
            {
                if (_pinnedColumns[i] != null)
                    _pinnedColumns[i].localPosition = _pinnedColumnRestLocalPositions[i];
            }
        }

        public void UpdateParallax(Vector3 cameraPosition)
        {
            if (!_hasOrigin)
            {
                _originPosition = transform.position;
                _hasOrigin = true;
            }

            float offsetY = _lockYToCameraStartOffset
                ? (cameraPosition.y - _referenceCameraY)
                : (cameraPosition.y - _originPosition.y) * (1f - _parallaxFactor.y);

            float targetX;
            if (_pinColumnsToCameraFrustumEdges)
            {
                targetX = cameraPosition.x;
            }
            else
            {
                float offsetX = (cameraPosition.x - _originPosition.x) * (1f - _parallaxFactor.x);
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

                targetX = _originPosition.x + _wrapAnchorX + offsetX;
            }

            float targetY = _originPosition.y + offsetY;

            if (_pixelSnap && _pixelsPerUnit > 0f)
            {
                targetX = Mathf.Round(targetX * _pixelsPerUnit) / _pixelsPerUnit;
                targetY = Mathf.Round(targetY * _pixelsPerUnit) / _pixelsPerUnit;
            }

            transform.position = new Vector3(targetX, targetY, transform.position.z);

            if (_pinColumnsToCameraFrustumEdges)
                PinColumnsToFrustumEdges();
        }

        private void PinColumnsToFrustumEdges()
        {
            EnsureColumnRestStateCached();
            if (_pinnedColumns == null || _pinnedColumns.Length < 4)
                return;

            if (_cameraTransform != null)
            {
                if (_cameraTransform != _cachedCameraOwner || _cachedCamera == null)
                {
                    _cachedCameraOwner = _cameraTransform;
                    _cachedCamera = _cameraTransform.GetComponent<Camera>();
                }
            }
            else
            {
                _cachedCameraOwner = null;
                _cachedCamera = null;
            }

            Camera cam = _cachedCamera != null ? _cachedCamera : Camera.main;

            float aspect = (cam != null && cam.aspect > 0.01f) ? cam.aspect : (16f / 9f);
            float ortho = (cam != null && cam.orthographic && cam.orthographicSize > 0.01f) ? cam.orthographicSize : 6.75f;
            float halfW = ortho * aspect;

            float leftInnerX = SnapUnits(halfW + _leftInnerHalfW - _frustumEdgeVisibleInsetUnits);
            float rightInnerX = SnapUnits(halfW + _rightInnerHalfW - _frustumEdgeVisibleInsetUnits);
            float outerX = SnapUnits(Mathf.Max(38f, halfW + _frustumOuterGuardOffsetUnits));

            SetColumnLocalX(0, -outerX);
            SetColumnLocalX(1, -leftInnerX);
            SetColumnLocalX(2, rightInnerX);
            SetColumnLocalX(3, outerX);
        }

        private void EnsureColumnRestStateCached()
        {
            if (_pinnedColumns == null || _pinnedColumns.Length == 0)
            {
                var cols = new List<Transform>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name.StartsWith("FgColumn_", StringComparison.Ordinal))
                        cols.Add(child);
                }

                if (cols.Count == 0)
                    return;

                cols.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));
                _pinnedColumns = cols.ToArray();
                _pinnedColumnRestLocalPositions = new Vector3[_pinnedColumns.Length];
                for (int i = 0; i < _pinnedColumns.Length; i++)
                    _pinnedColumnRestLocalPositions[i] = _pinnedColumns[i].localPosition;
                _columnWidthsCached = false;
            }

            if (!_columnWidthsCached && _pinnedColumns.Length >= 4)
            {
                _leftInnerHalfW = MeasureColumnHalfWidth(_pinnedColumns[1]);
                _rightInnerHalfW = MeasureColumnHalfWidth(_pinnedColumns[2]);
                _columnWidthsCached = true;
            }
        }

        private static float MeasureColumnHalfWidth(Transform column)
        {
            if (column != null)
            {
                var sr = column.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    return sr.sprite.bounds.extents.x * Mathf.Abs(sr.transform.localScale.x);
            }

            return 1.3125f;
        }

        private float SnapUnits(float value)
        {
            if (_pixelSnap && _pixelsPerUnit > 0f)
                return Mathf.Round(value * _pixelsPerUnit) / _pixelsPerUnit;
            return value;
        }

        private void SetColumnLocalX(int index, float localX)
        {
            Transform col = _pinnedColumns[index];
            if (col == null)
                return;
            Vector3 rest = _pinnedColumnRestLocalPositions[index];
            col.localPosition = new Vector3(localX, rest.y, rest.z);
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
