using DeadCells.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.UI
{
    /// <summary>
    /// Top-right HUD with two counters: gold (Wallet) and enemies killed (KillCounter). Creates itself
    /// when the first scene loads and survives scene reloads. Each number pops briefly when it changes.
    /// Sprites are generated pixel art and the canvas is built in code, so no scene or prefab wiring.
    /// </summary>
    public class RunHud : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float Margin = 32f;
        private const float PanelWidth = 250f;
        private const float RowHeight = 48f;
        private const float RowGap = 8f;
        private const float PanelPadding = 12f;
        private const float IconSize = 32f;
        private const float TextWidth = 150f;
        private const int FontSize = 32;
        private const float PunchTime = 0.18f;
        private const float PunchScale = 0.3f;
        private const int CanvasSortingOrder = 100;

        // 12x12 bitmaps, '#' = lit pixel. Drawn at 1 texel per pixel with point filtering.
        private static readonly string[] SkullRows =
        {
            "...######...",
            "..########..",
            ".##########.",
            ".##########.",
            ".##..##..##.",
            ".##..##..##.",
            ".##########.",
            "..####.###..",
            "...########.",
            "...#.##.#.#.",
            "...######...",
            "............",
        };

        private static readonly Color PanelColor = new Color(0.04f, 0.03f, 0.05f, 0.6f);
        private static readonly Color TextColor = new Color(1f, 0.95f, 0.85f, 1f);
        private static readonly Color GoldColor = new Color(0.98f, 0.78f, 0.22f, 1f);
        private static readonly Color GoldShade = new Color(0.72f, 0.5f, 0.1f, 1f);
        private static readonly Color SkullColor = new Color(0.9f, 0.88f, 0.82f, 1f);

        private Text _goldText;
        private Text _killText;
        private float _goldPunchStart = float.NegativeInfinity;
        private float _killPunchStart = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<RunHud>() != null)
                return;
            var go = new GameObject("RunHud");
            DontDestroyOnLoad(go);
            go.AddComponent<RunHud>();
        }

        private void Awake()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform panel = BuildCanvas();

            _goldText = BuildRow(panel, 0, CreateCoinSprite(), font);
            _killText = BuildRow(panel, 1, CreateSkullSprite(), font);

            RefreshGold(Wallet.Gold);
            RefreshKills(KillCounter.Kills);

            Wallet.Changed += OnWalletChanged;
            KillCounter.Changed += OnKillsChanged;
            enabled = false;
        }

        private void OnDestroy()
        {
            Wallet.Changed -= OnWalletChanged;
            KillCounter.Changed -= OnKillsChanged;
        }

        // Update only runs while a number is popping; it switches itself off afterwards.
        private void Update()
        {
            bool goldBusy = AnimatePunch(_goldText, _goldPunchStart);
            bool killBusy = AnimatePunch(_killText, _killPunchStart);
            if (!goldBusy && !killBusy)
                enabled = false;
        }

        private void OnWalletChanged(int gold, int gems)
        {
            RefreshGold(gold);
            _goldPunchStart = Time.unscaledTime;
            enabled = true;
        }

        private void OnKillsChanged(int kills)
        {
            RefreshKills(kills);
            _killPunchStart = Time.unscaledTime;
            enabled = true;
        }

        private void RefreshGold(int gold) => _goldText.text = gold.ToString();

        private void RefreshKills(int kills) => _killText.text = kills.ToString();

        private static bool AnimatePunch(Text text, float startTime)
        {
            float t = (Time.unscaledTime - startTime) / PunchTime;
            if (t >= 1f)
            {
                text.rectTransform.localScale = Vector3.one;
                return false;
            }

            // Quick rise, slower settle.
            float bump = Mathf.Sin(t * Mathf.PI);
            text.rectTransform.localScale = Vector3.one * (1f + PunchScale * bump);
            return true;
        }

        private RectTransform BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var panelGo = new GameObject("CounterPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(transform, false);
            var rect = (RectTransform)panelGo.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-Margin, -Margin);
            rect.sizeDelta = new Vector2(PanelWidth, PanelPadding * 2f + RowHeight * 2f + RowGap);

            var background = panelGo.GetComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;
            return rect;
        }

        private static Text BuildRow(RectTransform panel, int index, Sprite icon, Font font)
        {
            float top = -PanelPadding - index * (RowHeight + RowGap);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(panel, false);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(PanelPadding, top - (RowHeight - IconSize) * 0.5f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            var image = iconGo.GetComponent<Image>();
            image.sprite = icon;
            image.raycastTarget = false;

            var textGo = new GameObject("Value", typeof(RectTransform), typeof(Text), typeof(Shadow));
            textGo.transform.SetParent(panel, false);
            var textRect = (RectTransform)textGo.transform;
            // Right-aligned against a fixed edge so changing digits never shift the layout.
            textRect.anchorMin = textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(1f, 0.5f); // pops around the right edge, vertically centred
            textRect.anchoredPosition = new Vector2(-PanelPadding, top - RowHeight * 0.5f);
            textRect.sizeDelta = new Vector2(TextWidth, RowHeight);

            var text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = FontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleRight;
            text.color = TextColor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var shadow = textGo.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private static Sprite CreateCoinSprite()
        {
            const int size = 12;
            const float radius = 5.5f;
            var texture = NewTexture(size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - (size - 1) * 0.5f;
                    float dy = y - (size - 1) * 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = Color.clear;
                    if (distance <= radius)
                        c = distance > radius - 1.4f ? GoldShade : GoldColor;
                    // Vertical slot in the middle, like an embossed coin.
                    if (distance <= radius - 2.5f && Mathf.Abs(dx) < 0.8f && Mathf.Abs(dy) < 2.6f)
                        c = GoldShade;
                    texture.SetPixel(x, y, c);
                }
            }
            return FinishSprite(texture);
        }

        private static Sprite CreateSkullSprite()
        {
            int size = SkullRows.Length;
            var texture = NewTexture(size);
            for (int row = 0; row < size; row++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool lit = SkullRows[row][x] == '#';
                    // Row 0 is the top of the sprite; textures count from the bottom.
                    texture.SetPixel(x, size - 1 - row, lit ? SkullColor : Color.clear);
                }
            }
            return FinishSprite(texture);
        }

        private static Texture2D NewTexture(int size) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

        private static Sprite FinishSprite(Texture2D texture)
        {
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        }
    }
}
