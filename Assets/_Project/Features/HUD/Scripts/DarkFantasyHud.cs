using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    // Dresses the existing HUD in a dark fantasy look without touching the scene: a blood-red health bar with a
    // dark ember trail that lags behind damage and a pulse when health is low, blackened slots trimmed in tarnished
    // gold, bone-coloured serif text with a hard shadow, and gold diamond ornaments. It finds the views at start and
    // restyles them, so the scene and prefab stay as they are.
    [DisallowMultipleComponent]
    public sealed class DarkFantasyHud : MonoBehaviour
    {
        private static readonly Color Obsidian = new Color32(13, 10, 14, 247);
        private static readonly Color Iron = new Color32(46, 40, 48, 255);
        private static readonly Color Gold = new Color32(176, 138, 64, 255);
        private static readonly Color BrightGold = new Color32(236, 190, 86, 255);
        private static readonly Color Bone = new Color32(226, 212, 184, 255);
        private static readonly Color Crimson = new Color32(150, 16, 26, 255);
        private static readonly Color CrimsonBright = new Color32(222, 40, 44, 255);
        private static readonly Color Ember = new Color32(196, 88, 30, 255);
        private static readonly Color Blood = new Color32(200, 70, 64, 255);

        private const float LowHealth = 0.3f;

        private static bool s_sceneHookRegistered;
        private static Font s_font;

        private HealthBarView _health;
        private RectTransform _fill;
        private Image _fillImage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_font = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            if (s_sceneHookRegistered)
                return;
            s_sceneHookRegistered = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CreateInScene();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInScene()
        {
            if (FindFirstObjectByType<DarkFantasyHud>() != null || FindFirstObjectByType<HealthBarView>() == null)
                return;
            new GameObject("DarkFantasyHud").AddComponent<DarkFantasyHud>();
        }

        // Georgia or a similar serif if the machine has one, otherwise the built-in font.
        private static Font Serif
        {
            get
            {
                if (s_font == null)
                {
#if !UNITY_WEBGL
                    s_font = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Palatino Linotype", "Cambria", "Times New Roman" }, 16);
#endif
                    if (s_font == null)
                        s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return s_font;
            }
        }

        private void Start()
        {
            _health = FindFirstObjectByType<HealthBarView>();
            if (_health != null)
                StyleHealth();
            foreach (WeaponSlotView slot in FindObjectsByType<WeaponSlotView>(FindObjectsSortMode.None))
                StyleSlot(slot);
            foreach (BowAmmoView ammo in FindObjectsByType<BowAmmoView>(FindObjectsSortMode.None))
                ammo.ApplyPalette(Blood, Bone);
            var gold = FindFirstObjectByType<GoldHud>();
            if (gold != null && gold.Amount != null)
                StyleGold(gold.Amount);
        }

        private void Update()
        {
            if (_fill == null || _fillImage == null)
                return;

            // Under a third of health the bar pulses, slowly at first and quicker the lower it gets.
            float fraction = _fill.anchorMax.x;
            float danger = fraction < LowHealth ? 1f - fraction / LowHealth : 0f;
            float pulse = danger > 0f ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (4f + danger * 6f)) : 0f;
            _fillImage.color = Color.Lerp(Crimson, CrimsonBright, pulse * danger);
        }

        // ---------- Health ----------

        private void StyleHealth()
        {
            Transform root = FindRoot(_health.transform, "Track");
            if (root == null)
                return;

            Tint(root, "Frame", Gold);
            Tint(root, "Track", Obsidian);
            Tint(root, "Highlight", new Color(1f, 0.45f, 0.4f, 0.28f));
            _fill = _health.Fill;
            if (_fill != null && _fill.TryGetComponent(out _fillImage))
                _fillImage.color = Crimson;

            if (_health.TrailFill != null && _health.TrailFill.TryGetComponent(out Image trailImage))
                trailImage.color = Ember;

            StyleText(_health.Value, Bone);
            RectTransform bar = (RectTransform)root;
            AddDiamond(bar, new Vector2(0f, 0.5f), 14f);
            AddDiamond(bar, new Vector2(1f, 0.5f), 14f);
        }

        // ---------- Weapon slots ----------

        private static void StyleSlot(WeaponSlotView slot)
        {
            slot.ApplyPalette(new Color32(40, 34, 40, 255), new Color32(120, 100, 62, 255), BrightGold);
            Transform root = FindRoot(slot.transform, "Background");
            if (root != null)
                Tint(root, "Background", Obsidian);
            foreach (Text label in slot.GetComponentsInChildren<Text>(true))
                StyleText(label, BrightGold);
            if (slot.transform is RectTransform rect)
            {
                AddDiamond(rect, new Vector2(0f, 1f), 11f);
                AddDiamond(rect, new Vector2(1f, 0f), 11f);
            }
        }

        // ---------- Gold ----------

        private static void StyleGold(Text amount)
        {
            StyleText(amount, BrightGold);
            if (amount.transform is RectTransform rect)
                AddDiamond(rect, new Vector2(0f, 0.5f), 14f, new Vector2(-12f, 0f));
        }

        // ---------- Helpers ----------

        // The nearest ancestor (starting at start) that contains a descendant with the given name.
        private static Transform FindRoot(Transform start, string childName)
        {
            for (Transform current = start; current != null; current = current.parent)
            {
                if (FindDeep(current, childName) != null)
                    return current;
            }
            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name)
                    return child;
                Transform deeper = FindDeep(child, name);
                if (deeper != null)
                    return deeper;
            }
            return null;
        }

        private static void Tint(Transform root, string childName, Color color)
        {
            Transform target = FindDeep(root, childName);
            if (target != null && target.TryGetComponent(out Graphic graphic))
                graphic.color = color;
        }

        private static void StyleText(Text text, Color color)
        {
            if (text == null)
                return;
            text.font = Serif;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            if (!text.TryGetComponent(out Shadow _))
            {
                var shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        // A small gold diamond with a black centre, a corner ornament for plates and frames.
        private static void AddDiamond(RectTransform parent, Vector2 anchor, float size, Vector2 offset = default)
        {
            RectTransform outer = NewImage("Ornament", parent, Gold, size);
            outer.anchorMin = outer.anchorMax = anchor;
            outer.anchoredPosition = offset;
            outer.localRotation = Quaternion.Euler(0f, 0f, 45f);
            RectTransform inner = NewImage("OrnamentCore", outer, Obsidian, size * 0.45f);
            inner.anchorMin = inner.anchorMax = new Vector2(0.5f, 0.5f);
            inner.anchoredPosition = Vector2.zero;
        }

        private static RectTransform NewImage(string name, Transform parent, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
