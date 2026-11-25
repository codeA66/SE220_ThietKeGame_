using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Core;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DeadCells.HUD
{
    /// <summary>
    /// Dead Cells-style minimap and expandable fullscreen map overlay.
    /// Features persistent flood-filled fog-of-war exploration (blocked by solid walls),
    /// crisp slate-blue rooms with cyan wall/floor silhouettes, one-way platforms, climbable chains,
    /// live enemy/scroll/exit/portal markers, and Tab/M fullscreen map with pan &amp; zoom.
    /// Auto-bootstraps in any gameplay scene containing a PlayerController (including after Retry reloads).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapHud : MonoBehaviour
    {
        private const int FullMapPaddingCells = 10;
        private const int MinFullMapWidth = 96;
        private const int MinFullMapHeight = 60;
        private const int MaxFullMapDimension = 512;
        private const float PanKeyboardSpeed = 620f;
        private const float ScrollZoomStep = 0.35f;
        private const float KeyZoomStep = 0.5f;

        private static bool s_sceneHookRegistered;
        private static MinimapHud s_active;

        [Header("Grid & Exploration")]
        [Tooltip("World units per minimap cell (0.5 = 2 cells per world unit for crisp Dead Cells silhouettes).")]
        [SerializeField, Min(0.25f)] private float _cellSize = 0.5f;
        [Tooltip("Visible cells shown in the corner minimap window.")]
        [SerializeField] private Vector2Int _cornerCells = new Vector2Int(72, 44);
        [Tooltip("Corner minimap viewport size in 1920x1080 reference pixels (exact 4x of 72x44).")]
        [SerializeField] private Vector2 _cornerPanelSize = new Vector2(288f, 176f);
        [Tooltip("Fog-of-war reveal ellipse radii around the player in world units.")]
        [SerializeField] private Vector2 _revealRadius = new Vector2(12f, 7.5f);
        [SerializeField, Min(0.03f)] private float _refreshInterval = 0.1f;

        private PlayerController _player;
        private PlayerInputReader _playerInput;
        private Transform _target;
        private HitStop _hitStop;
        private Canvas _gameplayHudCanvas;

        private MinimapMapData _mapData;
        private MinimapWorldScanner _scanner;
        private MinimapOverlayView _view;

        private Texture2D _cornerTexture;
        private Color32[] _cornerPixels;

        private Texture2D _fullTexture;
        private Color32[] _fullPixels;
        private int _fullOriginX;
        private int _fullOriginY;
        private int _fullWidth;
        private int _fullHeight;

        private readonly List<MinimapMarker> _markers = new(64);
        private float _nextRefresh;
        private bool _ownsPause;
        private bool _disabledPlayerInput;

        public static MinimapHud Active => s_active;
        public static bool IsAnyFullMapOpen => s_active != null && s_active.IsFullMapOpen;
        public static int LastEscapeCloseFrame { get; private set; } = -1;

        public MinimapMapData MapData => _mapData;
        public bool IsFullMapOpen => _view != null && _view.IsFullMapOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_active = null;
            LastEscapeCloseFrame = -1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            if (s_sceneHookRegistered)
                return;
            s_sceneHookRegistered = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInActiveScene();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInActiveScene()
        {
            if (FindAnyObjectByType<MinimapHud>() != null || FindAnyObjectByType<PlayerController>() == null)
                return;
            new GameObject("MinimapHud").AddComponent<MinimapHud>();
        }

        private void OnEnable()
        {
            s_active = this;
        }

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerController>();
            if (_player == null)
            {
                if (s_active == this)
                    s_active = null;
                GameDebug.LogWarning("No player in the scene, so the minimap has nothing to follow.", LogTopic.UI);
                enabled = false;
                return;
            }

            s_active = this;
            _playerInput = _player.GetComponent<PlayerInputReader>();
            _target = _player.transform;
            _hitStop = FindAnyObjectByType<HitStop>();

            var healthHud = FindAnyObjectByType<PlayerHealthHud>();
            if (healthHud != null)
                _gameplayHudCanvas = healthHud.GetComponentInParent<Canvas>();

            _mapData = new MinimapMapData(_cellSize);
            _scanner = new MinimapWorldScanner(_cellSize);
            _scanner.ScanStaticLandmarks();

            _cornerTexture = CreateMapTexture("DeadCellsMinimapCorner", _cornerCells.x, _cornerCells.y);
            _cornerPixels = new Color32[_cornerCells.x * _cornerCells.y];
            _view = new MinimapOverlayView(transform, _cornerTexture, _cornerPanelSize);

            RevealAndRender(Time.unscaledTime);
        }

        private void Update()
        {
            if (_target == null || _view == null)
                return;

            bool blocked = IsBlockedByOtherUi();
            if (blocked)
            {
                if (_view.IsFullMapOpen)
                    SetFullMapOpen(false);
                _view.SetCornerVisible(false);
                return;
            }

            HandleMapInput();
            _view.SetCornerVisible(!_view.IsFullMapOpen);

            float now = Time.unscaledTime;
            if (now >= _nextRefresh)
            {
                _nextRefresh = now + _refreshInterval;
                RevealAndRender(now);
            }
        }

        private void OnDisable()
        {
            if (s_active == this)
                s_active = null;
            ReleaseMenuPause();
        }

        private void OnDestroy()
        {
            if (s_active == this)
                s_active = null;
            ReleaseMenuPause();
            DestroyTexture(_cornerTexture);
            DestroyTexture(_fullTexture);
        }

        public void ToggleFullMap()
        {
            if (_view == null || IsBlockedByOtherUi())
                return;
            SetFullMapOpen(!_view.IsFullMapOpen);
        }

        public void SetFullMapOpen(bool open)
        {
            if (_view == null || _view.IsFullMapOpen == open)
                return;

            _view.SetFullMapOpen(open);
            if (open)
            {
                AcquireMenuPause();
                RevealAndRender(Time.unscaledTime);
                CenterFullMapOnPlayer();
            }
            else
            {
                ReleaseMenuPause();
            }
        }

        private void HandleMapInput()
        {
            Keyboard kb = Keyboard.current;
            Gamepad gp = Gamepad.current;
            if ((kb != null && (kb.tabKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame)) ||
                (gp != null && gp.selectButton.wasPressedThisFrame))
            {
                SetFullMapOpen(!_view.IsFullMapOpen);
                return;
            }

            if (!_view.IsFullMapOpen)
                return;

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                LastEscapeCloseFrame = Time.frameCount;
                SetFullMapOpen(false);
                return;
            }

            if (gp != null && gp.buttonEast.wasPressedThisFrame)
            {
                SetFullMapOpen(false);
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (kb != null)
            {
                Vector2 panDir = Vector2.zero;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                    panDir.x += 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                    panDir.x -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                    panDir.y -= 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                    panDir.y += 1f;
                if (panDir.sqrMagnitude > 0f)
                    _view.Pan(panDir.normalized * (PanKeyboardSpeed * dt));

                if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
                {
                    _view.AdjustZoom(KeyZoomStep);
                    _view.UpdateStatusText(_mapData.ExploredOpenCellCount);
                }
                else if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
                {
                    _view.AdjustZoom(-KeyZoomStep);
                    _view.UpdateStatusText(_mapData.ExploredOpenCellCount);
                }

                if (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                    CenterFullMapOnPlayer();
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    _view.AdjustZoom(Mathf.Sign(scrollY) * ScrollZoomStep);
                    _view.UpdateStatusText(_mapData.ExploredOpenCellCount);
                }

                if (mouse.leftButton.isPressed)
                {
                    Vector2 drag = mouse.delta.ReadValue();
                    if (drag.sqrMagnitude > 0f)
                    {
                        float scale = 1920f / Mathf.Max(1f, Screen.width);
                        _view.Pan(drag * scale);
                    }
                }
            }
        }

        private void RevealAndRender(float now)
        {
            Vector2 playerCenter = (Vector2)_target.position + Vector2.up * 0.75f;
            _mapData.RevealAround(playerCenter, _revealRadius.x, _revealRadius.y, _scanner.SampleCell);
            _scanner.CollectMarkers(_player, _markers, now);

            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 5.5f);
            Vector2Int playerCell = _mapData.WorldToCell(playerCenter);

            if (!_view.IsFullMapOpen)
            {
                int originX = playerCell.x - _cornerCells.x / 2;
                int originY = playerCell.y - _cornerCells.y / 2;
                _mapData.RenderWindow(_cornerPixels, _cornerCells.x, _cornerCells.y, originX, originY, _markers, pulse);
                _cornerTexture.SetPixels32(_cornerPixels);
                _cornerTexture.Apply(false);
                _view.UpdateCornerTexture(_cornerTexture);
            }
            else
            {
                RenderFullMapTexture(playerCell, pulse);
            }

            _view.UpdateStatusText(_mapData.ExploredOpenCellCount);
        }

        private void RenderFullMapTexture(Vector2Int playerCell, float pulse)
        {
            Vector2Int min = _mapData.HasExploredBounds ? _mapData.ExploredMin : playerCell;
            Vector2Int max = _mapData.HasExploredBounds ? _mapData.ExploredMax : playerCell;

            int desiredMinX = min.x - FullMapPaddingCells;
            int desiredMinY = min.y - FullMapPaddingCells;
            int desiredWidth = Mathf.Clamp((max.x - min.x + 1) + FullMapPaddingCells * 2, MinFullMapWidth, MaxFullMapDimension);
            int desiredHeight = Mathf.Clamp((max.y - min.y + 1) + FullMapPaddingCells * 2, MinFullMapHeight, MaxFullMapDimension);

            // Center small explored areas inside the minimum full-map canvas, or follow player if clipped at max size.
            int exploredWidth = max.x - min.x + 1;
            int exploredHeight = max.y - min.y + 1;
            if (exploredWidth + FullMapPaddingCells * 2 < desiredWidth)
                _fullOriginX = min.x - (desiredWidth - exploredWidth) / 2;
            else if (exploredWidth + FullMapPaddingCells * 2 > MaxFullMapDimension)
                _fullOriginX = Mathf.Clamp(playerCell.x - desiredWidth / 2, desiredMinX, max.x + FullMapPaddingCells - desiredWidth + 1);
            else
                _fullOriginX = desiredMinX;

            if (exploredHeight + FullMapPaddingCells * 2 < desiredHeight)
                _fullOriginY = min.y - (desiredHeight - exploredHeight) / 2;
            else if (exploredHeight + FullMapPaddingCells * 2 > MaxFullMapDimension)
                _fullOriginY = Mathf.Clamp(playerCell.y - desiredHeight / 2, desiredMinY, max.y + FullMapPaddingCells - desiredHeight + 1);
            else
                _fullOriginY = desiredMinY;

            if (_fullTexture == null || _fullWidth != desiredWidth || _fullHeight != desiredHeight)
            {
                DestroyTexture(_fullTexture);
                _fullWidth = desiredWidth;
                _fullHeight = desiredHeight;
                _fullTexture = CreateMapTexture("DeadCellsFullMap", _fullWidth, _fullHeight);
                _fullPixels = new Color32[_fullWidth * _fullHeight];
            }

            _mapData.RenderWindow(_fullPixels, _fullWidth, _fullHeight, _fullOriginX, _fullOriginY, _markers, pulse);
            _fullTexture.SetPixels32(_fullPixels);
            _fullTexture.Apply(false);
            _view.UpdateFullTexture(_fullTexture, _fullWidth, _fullHeight);
        }

        private void CenterFullMapOnPlayer()
        {
            if (_target == null || _fullWidth <= 0 || _fullHeight <= 0)
                return;

            Vector2 playerCenter = (Vector2)_target.position + Vector2.up * 0.75f;
            Vector2Int playerCell = _mapData.WorldToCell(playerCenter);
            var normalized = new Vector2(
                Mathf.Clamp01((playerCell.x - _fullOriginX + 0.5f) / _fullWidth),
                Mathf.Clamp01((playerCell.y - _fullOriginY + 0.5f) / _fullHeight));
            _view.CenterOnNormalized(normalized);
        }

        private bool IsBlockedByOtherUi()
        {
            if (_player == null || !_player.enabled)
                return true;
            if (_player.Health != null && !_player.Health.IsAlive)
                return true;
            if (_player.IsInScriptedSequence)
                return true;
            if (_player.RunStats != null && _player.RunStats.IsChoosing)
                return true;
            if (_gameplayHudCanvas != null && !_gameplayHudCanvas.enabled)
                return true;
            if (_hitStop != null && _hitStop.IsMenuPaused && !_ownsPause)
                return true;
            return false;
        }

        private void AcquireMenuPause()
        {
            if (!_ownsPause)
            {
                if (_hitStop != null)
                    _hitStop.SetMenuPaused(true);
                else
                    Time.timeScale = 0f;
                _ownsPause = true;
            }

            if (!_disabledPlayerInput && _playerInput != null && _playerInput.enabled)
            {
                _playerInput.enabled = false;
                _disabledPlayerInput = true;
            }
        }

        private void ReleaseMenuPause()
        {
            if (_ownsPause)
            {
                _ownsPause = false;
                if (_hitStop != null)
                    _hitStop.SetMenuPaused(false);
                else
                    Time.timeScale = 1f;
            }

            if (_disabledPlayerInput)
            {
                _disabledPlayerInput = false;
                if (_playerInput != null &&
                    _player != null &&
                    _player.enabled &&
                    (_player.RunStats == null || !_player.RunStats.BlocksGameplayInput) &&
                    (_hitStop == null || !_hitStop.IsMenuPaused))
                {
                    _playerInput.enabled = true;
                }
            }
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
                return;
            if (Application.isPlaying)
                Destroy(texture);
            else
                DestroyImmediate(texture);
        }

        private static Texture2D CreateMapTexture(string textureName, int width, int height) =>
            new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
    }
}
