using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadCells.HUD
{
    /// <summary>
    /// Builds and manages the Dead Cells-inspired uGUI minimap hierarchy:
    /// a translucent bottom-right corner frame (anchored above Run stats when present)
    /// and a toggleable fullscreen map overlay with zoom, pan, biome header, and icon legend.
    /// </summary>
    public sealed class MinimapOverlayView
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float MinZoom = 1.0f;
        private const float MaxZoom = 5.0f;

        private static readonly Color FrameOuter = new Color32(12, 17, 28, 238);
        private static readonly Color FrameInnerBorder = new Color32(62, 152, 196, 220);
        private static readonly Color MapBackdrop = new Color32(8, 12, 22, 208);
        private static readonly Color OverlayScrim = new Color32(5, 8, 16, 226);
        private static readonly Color AccentGold = new Color32(198, 158, 76, 255);
        private static readonly Color TextPrimary = new Color32(226, 236, 244, 255);
        private static readonly Color TextMuted = new Color32(142, 184, 208, 230);

        private static Font s_font;

        private readonly Canvas _canvas;
        private readonly GameObject _cornerRoot;
        private readonly UnityEngine.UI.RawImage _cornerImage;
        private readonly UnityEngine.UI.Text _cornerStatusText;
        private readonly GameObject _fullRoot;
        private readonly RectTransform _fullViewport;
        private readonly RectTransform _fullMapRect;
        private readonly UnityEngine.UI.RawImage _fullImage;
        private readonly UnityEngine.UI.Text _fullHeaderMeta;

        private float _zoom = 2.5f;
        private Vector2 _panOffset;
        private Vector2Int _fullTextureSize;

        public bool IsFullMapOpen => _fullRoot != null && _fullRoot.activeSelf;
        public float Zoom => _zoom;

        public MinimapOverlayView(Transform owner, Texture2D cornerTexture, Vector2 cornerMapSize)
        {
            var canvasGo = new GameObject("MinimapCanvas");
            canvasGo.transform.SetParent(owner, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 15;

            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            BuildCornerPanel(canvasGo.transform, cornerTexture, cornerMapSize,
                out _cornerRoot, out _cornerImage, out _cornerStatusText);
            BuildFullOverlay(canvasGo.transform,
                out _fullRoot, out _fullViewport, out _fullMapRect, out _fullImage, out _fullHeaderMeta);

            _fullRoot.SetActive(false);
        }

        public void SetCornerVisible(bool visible)
        {
            if (_cornerRoot != null && _cornerRoot.activeSelf != visible)
                _cornerRoot.SetActive(visible);
        }

        public void SetFullMapOpen(bool open)
        {
            if (_fullRoot == null)
                return;
            _fullRoot.SetActive(open);
            _canvas.sortingOrder = open ? 50 : 15;
            if (open)
                ResetCameraToCenter();
        }

        public void UpdateCornerTexture(Texture2D texture)
        {
            if (_cornerImage != null && _cornerImage.texture != texture)
                _cornerImage.texture = texture;
        }

        public void UpdateFullTexture(Texture2D texture, int cellsWide, int cellsHigh)
        {
            if (_fullImage == null || _fullMapRect == null)
                return;

            _fullImage.texture = texture;
            _fullTextureSize = new Vector2Int(Mathf.Max(1, cellsWide), Mathf.Max(1, cellsHigh));
            // 4 reference pixels per cell at 1x zoom keeps silhouettes crisp at every zoom stop.
            _fullMapRect.sizeDelta = new Vector2(_fullTextureSize.x * 4f, _fullTextureSize.y * 4f);
            ApplyPanAndZoom();
        }

        public void UpdateStatusText(int exploredCells)
        {
            string meta = $"{exploredCells} CELLS";
            if (_cornerStatusText != null)
                _cornerStatusText.text = meta;
            if (_fullHeaderMeta != null)
                _fullHeaderMeta.text = $"{FormatBiomeTitle()}   •   {meta}   •   ZOOM {_zoom:0.0}x";
        }

        public void AdjustZoom(float delta)
        {
            float next = Mathf.Clamp(_zoom + delta, MinZoom, MaxZoom);
            if (Mathf.Approximately(next, _zoom))
                return;
            _zoom = next;
            ApplyPanAndZoom();
        }

        public void Pan(Vector2 deltaReferencePixels)
        {
            _panOffset += deltaReferencePixels;
            ApplyPanAndZoom();
        }

        public void CenterOnNormalized(Vector2 normalizedInFullMap)
        {
            Vector2 scaledSize = _fullMapRect.sizeDelta * _zoom;
            Vector2 localFromCenter = new Vector2(
                (normalizedInFullMap.x - 0.5f) * scaledSize.x,
                (normalizedInFullMap.y - 0.5f) * scaledSize.y);
            _panOffset = -localFromCenter;
            ApplyPanAndZoom();
        }

        public void ResetCameraToCenter()
        {
            _panOffset = Vector2.zero;
            ApplyPanAndZoom();
        }

        private void ApplyPanAndZoom()
        {
            if (_fullMapRect == null || _fullViewport == null)
                return;

            _fullMapRect.localScale = new Vector3(_zoom, _zoom, 1f);
            Vector2 scaledHalf = _fullMapRect.sizeDelta * (_zoom * 0.5f);
            Vector2 viewHalf = _fullViewport.rect.size * 0.5f;
            float maxX = Mathf.Max(0f, scaledHalf.x - viewHalf.x + 80f);
            float maxY = Mathf.Max(0f, scaledHalf.y - viewHalf.y + 80f);
            _panOffset.x = Mathf.Clamp(_panOffset.x, -maxX, maxX);
            _panOffset.y = Mathf.Clamp(_panOffset.y, -maxY, maxY);
            _fullMapRect.anchoredPosition = _panOffset;
        }

        private static void BuildCornerPanel(
            Transform parent,
            Texture2D cornerTexture,
            Vector2 cornerMapSize,
            out GameObject cornerRoot,
            out UnityEngine.UI.RawImage mapImage,
            out UnityEngine.UI.Text statusText)
        {
            const float pad = 6f;
            const float footerHeight = 20f;
            Vector2 totalSize = new Vector2(cornerMapSize.x + pad * 2f, cornerMapSize.y + pad * 2f + footerHeight);

            // Place bottom-right just like Dead Cells, neatly above the Run stats strip when present.
            float bottomOffset = HasBottomRightStatsStrip() ? 114f : 28f;

            RectTransform outer = CreateRect("CornerMinimap", parent);
            outer.anchorMin = new Vector2(1f, 0f);
            outer.anchorMax = new Vector2(1f, 0f);
            outer.pivot = new Vector2(1f, 0f);
            outer.anchoredPosition = new Vector2(-28f, bottomOffset);
            outer.sizeDelta = totalSize;
            AddImage(outer.gameObject, FrameOuter);
            cornerRoot = outer.gameObject;

            RectTransform rim = CreateRect("CyanRim", outer);
            Stretch(rim, 2f, 2f, 2f, 2f);
            AddImage(rim.gameObject, FrameInnerBorder);

            RectTransform inner = CreateRect("Backdrop", rim);
            Stretch(inner, 2f, 2f, 2f, 2f);
            AddImage(inner.gameObject, MapBackdrop);

            RectTransform mapRect = CreateRect("MapTexture", inner);
            mapRect.anchorMin = new Vector2(0.5f, 1f);
            mapRect.anchorMax = new Vector2(0.5f, 1f);
            mapRect.pivot = new Vector2(0.5f, 1f);
            mapRect.anchoredPosition = new Vector2(0f, -2f);
            mapRect.sizeDelta = cornerMapSize;
            mapImage = mapRect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            mapImage.texture = cornerTexture;
            mapImage.raycastTarget = false;

            // Subtle divider line above footer strip.
            RectTransform divider = CreateRect("FooterDivider", inner);
            divider.anchorMin = new Vector2(0f, 0f);
            divider.anchorMax = new Vector2(1f, 0f);
            divider.pivot = new Vector2(0.5f, 0f);
            divider.anchoredPosition = new Vector2(0f, footerHeight);
            divider.sizeDelta = new Vector2(0f, 1f);
            AddImage(divider.gameObject, new Color(0.24f, 0.58f, 0.76f, 0.45f));

            // Compact footer: [TAB / M] MAP on left, explored counter on right.
            RectTransform hintRect = CreateRect("HintLabel", inner);
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(0.55f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.anchoredPosition = new Vector2(6f, 1f);
            hintRect.sizeDelta = new Vector2(0f, footerHeight - 2f);
            AddText(hintRect.gameObject, "[TAB / M] MAP", 11, TextMuted, TextAnchor.MiddleLeft);

            RectTransform statusRect = CreateRect("StatusLabel", inner);
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(1f, 0f);
            statusRect.anchoredPosition = new Vector2(-6f, 1f);
            statusRect.sizeDelta = new Vector2(0f, footerHeight - 2f);
            statusText = AddText(statusRect.gameObject, string.Empty, 11, AccentGold, TextAnchor.MiddleRight);

            // Four subtle gold diamond corner ornaments matching DarkFantasyHud.
            AddCornerDiamond(outer, new Vector2(0f, 1f), 9f);
            AddCornerDiamond(outer, new Vector2(1f, 1f), 9f);
            AddCornerDiamond(outer, new Vector2(0f, 0f), 9f);
            AddCornerDiamond(outer, new Vector2(1f, 0f), 9f);
        }

        private static void BuildFullOverlay(
            Transform parent,
            out GameObject fullRoot,
            out RectTransform viewportRect,
            out RectTransform mapRect,
            out UnityEngine.UI.RawImage mapImage,
            out UnityEngine.UI.Text headerMeta)
        {
            RectTransform scrim = CreateRect("FullMapOverlay", parent);
            Stretch(scrim, 0f, 0f, 0f, 0f);
            AddImage(scrim.gameObject, OverlayScrim);
            fullRoot = scrim.gameObject;

            // Main gothic map window centered on screen.
            RectTransform window = CreateRect("Window", scrim);
            window.anchorMin = new Vector2(0.08f, 0.08f);
            window.anchorMax = new Vector2(0.92f, 0.92f);
            window.offsetMin = Vector2.zero;
            window.offsetMax = Vector2.zero;
            AddImage(window.gameObject, FrameOuter);

            RectTransform rim = CreateRect("WindowRim", window);
            Stretch(rim, 3f, 3f, 3f, 3f);
            AddImage(rim.gameObject, FrameInnerBorder);

            RectTransform body = CreateRect("WindowBody", rim);
            Stretch(body, 2f, 2f, 2f, 2f);
            AddImage(body.gameObject, MapBackdrop);

            // Header bar.
            RectTransform header = CreateRect("Header", body);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 48f);
            AddImage(header.gameObject, new Color32(14, 22, 36, 240));

            RectTransform titleRect = CreateRect("Title", header);
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0.55f, 1f);
            titleRect.offsetMin = new Vector2(18f, 0f);
            titleRect.offsetMax = Vector2.zero;
            headerMeta = AddText(titleRect.gameObject, FormatBiomeTitle(), 18, TextPrimary, TextAnchor.MiddleLeft);

            RectTransform controlsRect = CreateRect("Controls", header);
            controlsRect.anchorMin = new Vector2(0.45f, 0f);
            controlsRect.anchorMax = new Vector2(1f, 1f);
            controlsRect.offsetMin = Vector2.zero;
            controlsRect.offsetMax = new Vector2(-18f, 0f);
            AddText(controlsRect.gameObject, "[TAB / M / ESC] Close    [Scroll / +-] Zoom    [Drag / WASD] Pan    [R] Center",
                13, TextMuted, TextAnchor.MiddleRight);

            // Clipped map viewport between header and legend footer.
            viewportRect = CreateRect("Viewport", body);
            Stretch(viewportRect, 12f, 52f, 12f, 56f);
            viewportRect.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            AddImage(viewportRect.gameObject, new Color32(6, 10, 18, 220));

            mapRect = CreateRect("ExploredMap", viewportRect);
            mapRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.anchoredPosition = Vector2.zero;
            mapRect.sizeDelta = new Vector2(256f, 160f);
            mapImage = mapRect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            mapImage.raycastTarget = false;

            // Legend bar at bottom.
            RectTransform legend = CreateRect("Legend", body);
            legend.anchorMin = new Vector2(0f, 0f);
            legend.anchorMax = new Vector2(1f, 0f);
            legend.pivot = new Vector2(0.5f, 0f);
            legend.anchoredPosition = Vector2.zero;
            legend.sizeDelta = new Vector2(0f, 44f);
            AddImage(legend.gameObject, new Color32(12, 18, 30, 240));

            var layout = legend.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 8, 8);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddLegendItem(legend, MinimapMapData.ColorPlayerCore, "You");
            AddLegendItem(legend, MinimapMapData.ColorEnemy, "Enemy");
            AddLegendItem(legend, MinimapMapData.ColorScroll, "Scroll");
            AddLegendItem(legend, MinimapMapData.ColorClimbable, "Chain");
            AddLegendItem(legend, MinimapMapData.ColorOneWay, "Platform");
            AddLegendItem(legend, MinimapMapData.ColorExit, "Exit");
            AddLegendItem(legend, MinimapMapData.ColorTeleport, "Portal");

            AddCornerDiamond(window, new Vector2(0f, 1f), 14f);
            AddCornerDiamond(window, new Vector2(1f, 1f), 14f);
            AddCornerDiamond(window, new Vector2(0f, 0f), 14f);
            AddCornerDiamond(window, new Vector2(1f, 0f), 14f);
        }

        private static void AddLegendItem(Transform parent, Color32 swatchColor, string label)
        {
            RectTransform item = CreateRect($"Legend_{label}", parent);
            item.sizeDelta = new Vector2(116f, 24f);
            var hlg = item.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            RectTransform swatch = CreateRect("Swatch", item);
            swatch.sizeDelta = new Vector2(14f, 14f);
            AddImage(swatch.gameObject, swatchColor);

            RectTransform textRect = CreateRect("Label", item);
            textRect.sizeDelta = new Vector2(92f, 22f);
            AddText(textRect.gameObject, label, 14, TextPrimary, TextAnchor.MiddleLeft);
        }

        private static bool HasBottomRightStatsStrip()
        {
            GameObject stats = GameObject.Find("Run stats");
            return stats != null && stats.activeInHierarchy;
        }

        private static string FormatBiomeTitle()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName) || sceneName == "Main_Level")
                return "PRISONERS' QUARTERS";
            return sceneName.Replace('_', ' ').ToUpperInvariant();
        }

        private static void AddCornerDiamond(RectTransform parent, Vector2 anchor, float size)
        {
            RectTransform outer = CreateRect("CornerDiamond", parent);
            outer.anchorMin = anchor;
            outer.anchorMax = anchor;
            outer.pivot = new Vector2(0.5f, 0.5f);
            outer.anchoredPosition = Vector2.zero;
            outer.sizeDelta = new Vector2(size, size);
            outer.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddImage(outer.gameObject, AccentGold);

            RectTransform core = CreateRect("Core", outer);
            core.anchorMin = new Vector2(0.5f, 0.5f);
            core.anchorMax = new Vector2(0.5f, 0.5f);
            core.pivot = new Vector2(0.5f, 0.5f);
            core.anchoredPosition = Vector2.zero;
            core.sizeDelta = new Vector2(size * 0.45f, size * 0.45f);
            AddImage(core.gameObject, FrameOuter);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static UnityEngine.UI.Image AddImage(GameObject go, Color color)
        {
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static UnityEngine.UI.Text AddText(GameObject go, string content, int size, Color color, TextAnchor alignment)
        {
            var text = go.AddComponent<UnityEngine.UI.Text>();
            text.font = ResolveFont();
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content;

            var shadow = go.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);
            return text;
        }

        private static Font ResolveFont()
        {
            if (s_font != null)
                return s_font;

            try
            {
                s_font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Segoe UI", "Georgia", "Arial", "Liberation Sans" }, 16);
            }
            catch
            {
                s_font = null;
            }

            if (s_font == null)
                s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return s_font;
        }
    }
}
