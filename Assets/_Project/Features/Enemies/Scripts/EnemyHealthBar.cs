using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// World-space health bar built at runtime above the enemy's collider. Hidden until the first hit,
    /// hidden again on death. Lost health lingers as a pale chip that drains after a short delay, and
    /// the fill flashes white on each hit. Sizes are whole pixels at 32 PPU to sit with the sprites.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        private const float Px = 1f / 32f;
        private const float Width = 28 * Px;
        private const float Height = 3 * Px;
        private const float Trim = 1 * Px;
        private const float Frame = 1 * Px;
        private const float StudWidth = 2 * Px;
        private const float StudHeight = 7 * Px;
        private const float Padding = 6 * Px;
        private const float ChipDelay = 0.35f;
        private const float ChipDrainSpeed = 1.2f;
        private const float FlashTime = 0.08f;
        private const int SortingOrder = 100;

        private static readonly Color FrameColor = new Color(0.04f, 0.03f, 0.05f, 1f);
        private static readonly Color TrimColor = new Color(0.50f, 0.36f, 0.16f, 1f);
        private static readonly Color StudColor = new Color(0.72f, 0.55f, 0.26f, 1f);
        private static readonly Color InsetColor = new Color(0.14f, 0.03f, 0.05f, 1f);
        private static readonly Color ChipColor = new Color(0.95f, 0.85f, 0.70f, 1f);
        private static readonly Color FillColor = new Color(0.72f, 0.09f, 0.12f, 1f);
        private static readonly Color FlashColor = Color.white;
        private static readonly Color HighlightColor = new Color(0.95f, 0.30f, 0.28f, 0.6f);

        private static Sprite s_barSprite;

        private Health _health;
        private Transform _root;
        private Transform _fill;
        private Transform _chip;
        private SpriteRenderer _fillRenderer;
        private float _fillRatio = 1f;
        private float _chipRatio = 1f;
        private float _chipHoldUntil;
        private float _flashUntil;

        public void Setup(Health health, BoxCollider2D collider)
        {
            _health = health;

            var rootGo = new GameObject("HealthBar");
            _root = rootGo.transform;
            _root.SetParent(transform, false);
            _root.localPosition = collider.offset + new Vector2(0f, collider.size.y * 0.5f + Padding);

            float trimW = Width + Trim * 2f;
            float frameW = trimW + Frame * 2f;
            CreateBar("Frame", _root, FrameColor, SortingOrder, -frameW * 0.5f, 0f, frameW, Height + (Trim + Frame) * 2f);
            CreateBar("Trim", _root, TrimColor, SortingOrder + 1, -trimW * 0.5f, 0f, trimW, Height + Trim * 2f);
            CreateBar("Inset", _root, InsetColor, SortingOrder + 2, -Width * 0.5f, 0f, Width, Height);
            _chip = CreateBar("Chip", _root, ChipColor, SortingOrder + 3, -Width * 0.5f, 0f, Width, Height);
            _fill = CreateBar("Fill", _root, FillColor, SortingOrder + 4, -Width * 0.5f, 0f, Width, Height);
            _fillRenderer = _fill.GetComponent<SpriteRenderer>();
            // Child of the fill, so it shrinks with it: a one-pixel shine along the top row.
            CreateBar("Highlight", _fill, HighlightColor, SortingOrder + 5, 0f, 1f / 3f, 1f, 1f / 3f);

            // Bronze end caps with a dark outline, like rivets on an iron bar.
            float studX = frameW * 0.5f;
            CreateBar("StudLOutline", _root, FrameColor, SortingOrder + 6, -studX - StudWidth - Px, 0f, StudWidth + Px * 2f, StudHeight + Px * 2f);
            CreateBar("StudL", _root, StudColor, SortingOrder + 7, -studX - StudWidth, 0f, StudWidth, StudHeight);
            CreateBar("StudROutline", _root, FrameColor, SortingOrder + 6, studX - Px, 0f, StudWidth + Px * 2f, StudHeight + Px * 2f);
            CreateBar("StudR", _root, StudColor, SortingOrder + 7, studX, 0f, StudWidth, StudHeight);

            SetVisible(false);
            _health.Changed += OnChanged;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Changed -= OnChanged;
        }

        private void LateUpdate()
        {
            // Cancel the parent's scale (including a flip) so the bar never mirrors or stretches.
            Vector3 parentScale = transform.lossyScale;
            _root.localScale = new Vector3(Safe(parentScale.x), Safe(parentScale.y), 1f);
            _root.rotation = Quaternion.identity;

            if (_chipRatio > _fillRatio && Time.time >= _chipHoldUntil)
            {
                _chipRatio = Mathf.Max(_fillRatio, _chipRatio - ChipDrainSpeed * Time.deltaTime);
                _chip.localScale = new Vector3(Width * _chipRatio, Height, 1f);
            }

            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                _fillRenderer.color = FillColor;
            }
        }

        private void OnChanged(int current, int max)
        {
            bool visible = current > 0 && current < max;
            SetVisible(visible);
            if (!visible)
            {
                _fillRatio = _chipRatio = 1f;
                return;
            }

            float ratio = (float)current / max;
            if (ratio < _fillRatio)
            {
                _chipHoldUntil = Time.time + ChipDelay;
                _flashUntil = Time.time + FlashTime;
                _fillRenderer.color = FlashColor;
            }
            else
            {
                _chipRatio = ratio; // healing: no chip
            }
            _fillRatio = ratio;
            _fill.localScale = new Vector3(Width * _fillRatio, Height, 1f);
            _chip.localScale = new Vector3(Width * _chipRatio, Height, 1f);
        }

        private void SetVisible(bool visible)
        {
            _root.gameObject.SetActive(visible);
            enabled = visible;
        }

        // x is the left edge, y the vertical centre (the sprite is left-pivoted).
        private static Transform CreateBar(string barName, Transform parent, Color color, int order, float x, float y, float width, float height)
        {
            var go = new GameObject(barName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(width, height, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetBarSprite();
            sr.color = color;
            sr.sortingOrder = order;
            return go.transform;
        }

        // Left-pivoted 1x1 unit white sprite, shared by every bar.
        private static Sprite GetBarSprite()
        {
            if (s_barSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                s_barSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0.5f), tex.width);
            }
            return s_barSprite;
        }

        private static float Safe(float scale) => Mathf.Approximately(scale, 0f) ? 1f : 1f / scale;
    }
}
