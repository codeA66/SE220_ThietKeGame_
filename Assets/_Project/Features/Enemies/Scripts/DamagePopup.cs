using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Floating damage number: pops up, arcs away from the hit, then fades. Pooled; never parented to the
    /// enemy so it ignores flips and survives the enemy dying. Runs on unscaled time so it keeps moving
    /// through hit-stop, and fades through a property block so the text mesh is only built once per spawn.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class DamagePopup : MonoBehaviour
    {
        private const float Lifetime = 0.75f;
        private const float Gravity = 9f;
        private const float PunchTime = 0.1f;
        private const int FontSize = 48;
        private const int SortingOrder = 200;
        private const int PrewarmCount = 8;
        private const string Glyphs = "0123456789!";

        private static readonly Stack<DamagePopup> s_pool = new Stack<DamagePopup>();
        private static readonly int s_colorId = Shader.PropertyToID("_Color");
        private static readonly Color NormalColor = new Color(1f, 0.95f, 0.85f, 1f);
        private static readonly Color CriticalColor = new Color(1f, 0.55f, 0.1f, 1f);

        private static Font s_font;
        private static MaterialPropertyBlock s_block;

        private TextMesh _text;
        private MeshRenderer _renderer;
        private Transform _cachedTransform;
        private Vector2 _velocity;
        private float _age;
        private float _baseScale;

        public static void Spawn(Vector3 position, int amount, int direction, bool critical)
        {
            Prewarm();

            DamagePopup popup = null;
            while (popup == null && s_pool.Count > 0)
                popup = s_pool.Pop(); // entries die with their scene
            if (popup == null)
                popup = Create();
            popup.Play(position, amount, direction, critical);
        }

        /// <summary>Pays the font load and glyph rasterisation up front instead of on the first hit.</summary>
        public static void Prewarm()
        {
            if (s_font != null)
                return;
            s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            s_font.RequestCharactersInTexture(Glyphs, FontSize, FontStyle.Bold);
            s_block = new MaterialPropertyBlock();
            s_pool.Clear();
            for (int i = 0; i < PrewarmCount; i++)
            {
                DamagePopup popup = Create();
                popup.gameObject.SetActive(false);
                s_pool.Push(popup);
            }
        }

        /// <summary>Destroys every pooled damage popup and clears the pool for test teardown isolation.</summary>
        public static void ClearPoolForTests()
        {
            while (s_pool.Count > 0)
            {
                DamagePopup popup = s_pool.Pop();
                if (popup == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(popup.gameObject);
                else
                    DestroyImmediate(popup.gameObject);
            }
            s_pool.Clear();
        }

        private static DamagePopup Create()
        {
            var go = new GameObject("DamagePopup");
            var text = go.AddComponent<TextMesh>();
            text.font = s_font;
            text.fontSize = FontSize;
            text.characterSize = 0.08f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = s_font.material;
            int vfxLayer = SortingLayer.NameToID("VFX");
            if (vfxLayer != 0)
                renderer.sortingLayerID = vfxLayer;
            renderer.sortingOrder = SortingOrder;

            DamagePopup popup = go.AddComponent<DamagePopup>();
            popup._text = text;
            popup._renderer = renderer;
            popup._cachedTransform = go.transform;
            return popup;
        }

        private void Play(Vector3 position, int amount, int direction, bool critical)
        {
            _cachedTransform.position = position;
            _text.text = critical ? amount + "!" : amount.ToString();
            _text.color = critical ? CriticalColor : NormalColor;
            _baseScale = critical ? 1.4f : 1f;
            _velocity = new Vector2(direction * Random.Range(0.8f, 1.8f), Random.Range(3.5f, 4.5f));
            _age = 0f;
            SetAlpha(1f);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            if (_age >= Lifetime)
            {
                gameObject.SetActive(false);
                s_pool.Push(this);
                return;
            }

            _velocity.y -= Gravity * dt;
            _cachedTransform.position += (Vector3)(_velocity * dt);

            // Scale punch on spawn, then settle; fade over the second half of the lifetime.
            float punch = _age < PunchTime ? Mathf.Lerp(1.6f, 1f, _age / PunchTime) : 1f;
            _cachedTransform.localScale = Vector3.one * (_baseScale * punch);
            if (_age > Lifetime * 0.5f)
                SetAlpha((Lifetime - _age) / (Lifetime * 0.5f));
        }

        private void SetAlpha(float alpha)
        {
            s_block.SetColor(s_colorId, new Color(1f, 1f, 1f, alpha));
            _renderer.SetPropertyBlock(s_block);
        }
    }
}
