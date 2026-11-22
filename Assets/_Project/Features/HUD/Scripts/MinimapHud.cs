using DeadCells.Core;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    // A corner map of the level around the player. A second orthographic camera follows the player and draws the
    // level, without the UI, into a small render texture that sits in the top-right corner under the gold counter,
    // with a marker for the player at its centre. Builds its own canvas and camera, so it needs no scene wiring.
    [DisallowMultipleComponent]
    public sealed class MinimapHud : MonoBehaviour
    {
        private const int TextureWidth = 320;
        private const int TextureHeight = 180;

        [SerializeField, Min(2f)] private float _viewHeight = 14f;
        [SerializeField] private Vector2 _panelSize = new Vector2(320f, 180f);
        [Tooltip("Distance from the top-right corner, in reference pixels. The gold counter sits at the top.")]
        [SerializeField] private Vector2 _cornerOffset = new Vector2(-32f, -84f);
        [SerializeField] private Color _backdrop = new Color(0.05f, 0.04f, 0.06f, 1f);
        [SerializeField] private Color _border = new Color(0.69f, 0.54f, 0.25f, 1f);

        private Transform _target;
        private Camera _camera;
        private RenderTexture _texture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInScene()
        {
            if (FindFirstObjectByType<MinimapHud>() != null || FindFirstObjectByType<PlayerController>() == null)
                return;
            new GameObject("MinimapHud").AddComponent<MinimapHud>();
        }

        private void Start()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                GameDebug.LogWarning("No player in the scene, so the minimap has nothing to follow.", LogTopic.UI);
                enabled = false;
                return;
            }

            _target = player.transform;
            BuildCamera();
            BuildPanel();
        }

        private void LateUpdate()
        {
            if (_target == null || _camera == null)
                return;
            Vector3 position = _target.position;
            _camera.transform.position = new Vector3(position.x, position.y, _camera.transform.position.z);
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }

        private void BuildCamera()
        {
            _texture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.ARGB32)
            {
                name = "MinimapTexture",
                filterMode = FilterMode.Point,
                antiAliasing = 1,
            };

            var go = new GameObject("MinimapCamera");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(_target.position.x, _target.position.y, -10f);
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = _viewHeight * 0.5f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = _backdrop;
            _camera.targetTexture = _texture;
            // The level and the characters, not the UI.
            int uiLayer = LayerMask.NameToLayer("UI");
            _camera.cullingMask = uiLayer >= 0 ? ~(1 << uiLayer) : ~0;
            // Draw before the main camera so the main view is never affected.
            _camera.depth = -50f;
        }

        private void BuildPanel()
        {
            var canvasObject = new GameObject("MinimapCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // The border is a slightly larger solid panel behind the map.
            RectTransform frame = NewRect("Frame", canvasObject.transform, _panelSize + new Vector2(8f, 8f));
            frame.gameObject.AddComponent<Image>().color = _border;
            Anchor(frame);

            RectTransform map = NewRect("Map", frame, _panelSize);
            var image = map.gameObject.AddComponent<RawImage>();
            image.texture = _texture;
            map.anchorMin = map.anchorMax = map.pivot = new Vector2(0.5f, 0.5f);
            map.anchoredPosition = Vector2.zero;

            // The player is always at the centre of the map.
            RectTransform marker = NewRect("PlayerMarker", map, new Vector2(10f, 10f));
            marker.gameObject.AddComponent<Image>().color = new Color(0.93f, 0.75f, 0.34f, 1f);
            marker.anchorMin = marker.anchorMax = marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = Vector2.zero;
        }

        private void Anchor(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = _cornerOffset;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }
    }
}
