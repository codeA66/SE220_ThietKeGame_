using UnityEngine;

namespace DeadCells.CameraSystem
{
    /// <summary>
    /// Keeps ambient particle emitters centered on the camera after CameraFollow2D updates,
    /// so localized particle boxes maintain crisp visual density across the entire level.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public sealed class CameraFollowEmitter2D : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Vector2 _offset;

        public void Configure(Transform cam, Vector2 offset)
        {
            _cameraTransform = cam;
            _offset = offset;
            SyncPosition();
        }

        private void Start() => SyncPosition();

        private void LateUpdate() => SyncPosition();

        private void SyncPosition()
        {
            if (_cameraTransform == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                    _cameraTransform = mainCam.transform;
            }

            if (_cameraTransform == null)
                return;

            Vector3 camPos = _cameraTransform.position;
            transform.position = new Vector3(camPos.x + _offset.x, camPos.y + _offset.y, transform.position.z);
        }
    }
}
