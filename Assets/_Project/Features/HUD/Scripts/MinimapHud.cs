using DeadCells.Combat;
using DeadCells.Core;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    // A corner map of the level around the player, drawn as a mosaic: one chunky block per world cell, not a shrunken
    // copy of the screen. Each cell is checked against the level's colliders, so ground is a solid block with a
    // bright rim where it meets open air, one-way platforms get their own colour, enemies are red blocks and the
    // player a bright gold one. It scrolls a whole cell at a time. Sits in the top-right corner under the gold
    // counter and builds itself, so it needs no scene wiring.
    [DisallowMultipleComponent]
    public sealed class MinimapHud : MonoBehaviour
    {
        private static readonly Color32 Empty = new Color32(14, 11, 16, 255);
        private static readonly Color32 EmptyAlt = new Color32(18, 14, 20, 255);
        private static readonly Color32 Ground = new Color32(86, 70, 52, 255);
        private static readonly Color32 GroundRim = new Color32(176, 138, 64, 255);
        private static readonly Color32 OneWay = new Color32(132, 122, 168, 255);
        private static readonly Color32 EnemyColor = new Color32(206, 44, 52, 255);
        private static readonly Color32 PlayerColor = new Color32(255, 226, 120, 255);

        [Tooltip("How many cells the map shows, across and down. One cell is one world unit.")]
        [SerializeField] private Vector2Int _cells = new Vector2Int(36, 20);
        [SerializeField, Min(0.02f)] private float _refreshInterval = 0.1f;
        [SerializeField] private Vector2 _panelSize = new Vector2(288f, 160f);
        [Tooltip("Distance from the top-right corner, in reference pixels, when there is no gold counter to hang under.")]
        [SerializeField] private Vector2 _cornerOffset = new Vector2(-32f, -84f);
        [SerializeField] private Color _border = new Color(0.69f, 0.54f, 0.25f, 1f);

        private Transform _target;
        private PlayerController _player;
        private Texture2D _texture;
        private Color32[] _pixels;
        private bool[] _solid;
        private bool[] _oneWay;
        private Collider2D[] _hits;
        private Health[] _healths = new Health[0];
        private float _nextRefresh;
        private float _nextHealthScan;
        private LayerMask _groundLayers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInScene()
        {
            if (FindFirstObjectByType<MinimapHud>() != null || FindFirstObjectByType<PlayerController>() == null)
                return;
            new GameObject("MinimapHud").AddComponent<MinimapHud>();
        }

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null)
            {
                GameDebug.LogWarning("No player in the scene, so the minimap has nothing to follow.", LogTopic.UI);
                enabled = false;
                return;
            }

            _target = _player.transform;
            _groundLayers = LayerMask.GetMask("Ground", "OneWayPlatform");
            _texture = new Texture2D(_cells.x, _cells.y, TextureFormat.RGBA32, false)
            {
                name = "MinimapMosaic",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _pixels = new Color32[_cells.x * _cells.y];
            _solid = new bool[_pixels.Length];
            _oneWay = new bool[_pixels.Length];
            _hits = new Collider2D[4];
            BuildPanel();
            Refresh();
        }

        private void Update()
        {
            if (_target == null || Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + _refreshInterval;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }

        // Redraws the whole mosaic around the player's current cell.
        private void Refresh()
        {
            LayerMask ground = _groundLayers;
            Vector2 position = _target.position;
            // Whole-cell origin so the map steps from cell to cell instead of shimmering.
            int originX = Mathf.FloorToInt(position.x) - _cells.x / 2;
            int originY = Mathf.FloorToInt(position.y) - _cells.y / 2;

            // First pass: which cells hold ground, and whether each is a one-way platform.
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = ground, useTriggers = false };
            for (int y = 0; y < _cells.y; y++)
            {
                for (int x = 0; x < _cells.x; x++)
                {
                    var centre = new Vector2(originX + x + 0.5f, originY + y + 0.5f);
                    int count = Physics2D.OverlapPoint(centre, filter, _hits);
                    int index = y * _cells.x + x;
                    _solid[index] = count > 0;
                    _oneWay[index] = count > 0 && _hits[0].usedByEffector;
                }
            }

            // Second pass: colour them, with a bright rim on ground that touches open air above or beside it.
            for (int y = 0; y < _cells.y; y++)
            {
                for (int x = 0; x < _cells.x; x++)
                {
                    int index = y * _cells.x + x;
                    if (!_solid[index])
                        _pixels[index] = (x + y) % 2 == 0 ? Empty : EmptyAlt;
                    else if (_oneWay[index])
                        _pixels[index] = OneWay;
                    else
                        _pixels[index] = !CellSolid(x, y + 1) || !CellSolid(x - 1, y) || !CellSolid(x + 1, y) ? GroundRim : Ground;
                }
            }

            // Enemies and other living things as red blocks, the player as a gold one.
            if (Time.unscaledTime >= _nextHealthScan)
            {
                _nextHealthScan = Time.unscaledTime + 0.5f;
                _healths = FindObjectsByType<Health>(FindObjectsSortMode.None);
            }
            foreach (Health health in _healths)
            {
                if (health == null || !health.IsAlive || health == _player.Health)
                    continue;
                Plot(health.transform.position, originX, originY, EnemyColor);
            }
            Plot(position + Vector2.up * 0.5f, originX, originY, PlayerColor);

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private bool CellSolid(int x, int y) => x >= 0 && y >= 0 && x < _cells.x && y < _cells.y && _solid[y * _cells.x + x];

        private void Plot(Vector2 world, int originX, int originY, Color32 color)
        {
            int x = Mathf.FloorToInt(world.x) - originX;
            int y = Mathf.FloorToInt(world.y) - originY;
            if (x < 0 || y < 0 || x >= _cells.x || y >= _cells.y)
                return;
            _pixels[y * _cells.x + x] = color;
        }

        private void BuildPanel()
        {
            // Sit in the gold counter's own canvas, so both use the same scaling, and hang just under the counter.
            var gold = FindFirstObjectByType<GoldHud>();
            Canvas host = gold != null && gold.Amount != null ? gold.Amount.canvas : null;
            Transform parent;
            bool underGold = host != null;
            if (underGold)
            {
                parent = host.rootCanvas.transform;
            }
            else
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
                parent = canvasObject.transform;
            }

            // The border is a slightly larger solid panel behind the map.
            RectTransform frame = NewRect("Frame", parent, _panelSize + new Vector2(8f, 8f));
            frame.gameObject.AddComponent<Image>().color = _border;
            Anchor(frame);
            if (underGold)
                frame.anchoredPosition = OffsetUnder((RectTransform)gold.Amount.transform, (RectTransform)parent);

            RectTransform map = NewRect("Map", frame, _panelSize);
            var image = map.gameObject.AddComponent<RawImage>();
            image.texture = _texture;
            map.anchorMin = map.anchorMax = map.pivot = new Vector2(0.5f, 0.5f);
            map.anchoredPosition = Vector2.zero;
        }

        // Where the top-right anchored frame must sit so its top edge is a little below the bottom of the counter and
        // its right edge lines up with the counter's, in the canvas's own units.
        private static Vector2 OffsetUnder(RectTransform counter, RectTransform canvas)
        {
            var corners = new Vector3[4];
            counter.GetWorldCorners(corners);
            Vector3 bottomRight = canvas.InverseTransformPoint(corners[3]);
            Rect rect = canvas.rect;
            return new Vector2(bottomRight.x - rect.xMax, bottomRight.y - rect.yMax - 14f);
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
