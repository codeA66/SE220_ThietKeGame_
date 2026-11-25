using System.Collections.Generic;
using DeadCells.Core;
using UnityEngine;

namespace DeadCells.Enemies
{
    public enum LootKind { Gold, Gem }

    /// <summary>Gem rarity, rarest last. Each tier has its own cut, colour, glow and gem value.</summary>
    public enum GemTier { Sapphire, Emerald, Amethyst, Ruby }

    /// <summary>
    /// A coin or gem thrown out of a dead enemy. Bounces on the ground with a few raycasts (no Rigidbody),
    /// then gets pulled into the player once they come close. Gems glow with a pulsing halo and twinkle.
    /// Pooled; sprites are generated pixel art.
    /// </summary>
    public class LootPickup : MonoBehaviour
    {
        private const float Px = 1f / 32f;
        private const float Gravity = 22f;
        private const float Radius = 3 * Px;
        private const float Bounce = 0.35f;
        private const float GroundFriction = 0.6f;
        private const float RestSpeed = 1.2f;
        private const float PickupDelay = 0.45f;
        private const float MagnetRange = 2.5f;
        private const float MagnetAcceleration = 60f;
        private const float CollectRange = 0.35f;
        private const float SparkleTime = 0.18f;
        private const int SortingOrder = 50;
        // The player's pivot is at the feet; its collider is centred this far up.
        private static readonly Vector3 PlayerCenterOffset = new Vector3(0f, 0.8f, 0f);

        private static readonly Stack<LootPickup> s_pool = new Stack<LootPickup>();
        private static Sprite s_coinSmall;
        private static Sprite s_coinLarge;
        private static Sprite s_halo;
        private static Sprite s_sparkle;
        private static GemStyle[] s_gems;

        private SpriteRenderer _renderer;
        private SpriteRenderer _halo;
        private SpriteRenderer _sparkle;
        private Transform _cachedTransform;
        private Transform _haloTransform;
        private Transform _sparkleTransform;
        private LootKind _kind;
        private int _value;
        private GemStyle _gem;
        private Transform _player;
        private LayerMask _groundMask;
        private Vector2 _velocity;
        private float _age;
        private float _phase;
        private float _nextSparkle;
        private float _sparkleStartAge;
        private bool _isResting;
        private bool _isMagnetised;

        public static void SpawnGold(int value, Vector3 position, Vector2 velocity, Transform player, LayerMask groundMask) =>
            Take().Play(LootKind.Gold, value, null, position, velocity, player, groundMask);

        public static void SpawnGem(GemTier tier, Vector3 position, Vector2 velocity, Transform player, LayerMask groundMask)
        {
            EnsureSprites();
            GemStyle style = s_gems[(int)tier];
            Take().Play(LootKind.Gem, style.Value, style, position, velocity, player, groundMask);
        }

        /// <summary>Destroys every pooled pickup and clears the pool for test teardown isolation.</summary>
        public static void ClearPoolForTests()
        {
            while (s_pool.Count > 0)
            {
                LootPickup pickup = s_pool.Pop();
                if (pickup == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(pickup.gameObject);
                else
                    DestroyImmediate(pickup.gameObject);
            }
            s_pool.Clear();
        }

        private static LootPickup Take()
        {
            EnsureSprites();
            LootPickup pickup = null;
            while (pickup == null && s_pool.Count > 0)
                pickup = s_pool.Pop(); // entries die with their scene
            return pickup != null ? pickup : Create();
        }

        private static LootPickup Create()
        {
            var go = new GameObject("LootPickup");
            LootPickup pickup = go.AddComponent<LootPickup>();
            pickup._cachedTransform = go.transform;
            int vfxLayer = SortingLayer.NameToID("VFX");
            pickup._renderer = go.AddComponent<SpriteRenderer>();
            if (vfxLayer != 0)
                pickup._renderer.sortingLayerID = vfxLayer;
            pickup._renderer.sortingOrder = SortingOrder;
            pickup._halo = CreateChild(go.transform, "Halo", s_halo, vfxLayer, SortingOrder - 1);
            pickup._haloTransform = pickup._halo.transform;
            pickup._sparkle = CreateChild(go.transform, "Sparkle", s_sparkle, vfxLayer, SortingOrder + 1);
            pickup._sparkleTransform = pickup._sparkle.transform;
            return pickup;
        }

        private static SpriteRenderer CreateChild(Transform parent, string childName, Sprite sprite, int sortingLayerId, int order)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (sortingLayerId != 0)
                sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = order;
            return sr;
        }

        private void Play(LootKind kind, int value, GemStyle gem, Vector3 position, Vector2 velocity, Transform player, LayerMask groundMask)
        {
            _kind = kind;
            _value = value;
            _gem = gem;
            _player = player;
            _groundMask = groundMask;
            _velocity = velocity;
            _age = 0f;
            _phase = Random.value * Mathf.PI * 2f;
            _nextSparkle = Random.Range(0.2f, 0.8f);
            _isResting = false;
            _isMagnetised = false;

            bool isGem = kind == LootKind.Gem;
            _renderer.sprite = isGem ? gem.Sprite : value >= 3 ? s_coinLarge : s_coinSmall;
            _halo.enabled = isGem;
            _sparkle.enabled = false;
            if (isGem)
                _sparkle.color = Color.Lerp(Color.white, gem.Glow, 0.25f);

            _cachedTransform.position = position;
            _cachedTransform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_kind == LootKind.Gem)
                AnimateGlow();

            if (_age >= PickupDelay && _player != null)
            {
                Vector3 target = _player.position + PlayerCenterOffset;
                Vector3 toPlayer = target - _cachedTransform.position;
                float sqrDistance = toPlayer.sqrMagnitude;
                if (sqrDistance <= CollectRange * CollectRange)
                {
                    Collect();
                    return;
                }
                if (!_isMagnetised && sqrDistance <= MagnetRange * MagnetRange)
                {
                    _isMagnetised = true;
                    _isResting = false;
                    _velocity = Vector2.zero;
                }
                if (_isMagnetised)
                {
                    // Steer straight at the player, ignoring terrain, accelerating so it always catches up.
                    float speed = _velocity.magnitude + MagnetAcceleration * dt;
                    _velocity = (Vector2)toPlayer.normalized * speed;
                    _cachedTransform.position += (Vector3)(_velocity * dt);
                    return;
                }
            }

            if (!_isResting)
                Simulate(dt);
        }

        private void AnimateGlow()
        {
            float wave = Mathf.Sin(_age * 4f + _phase);
            float haloScale = _gem.HaloScale * (1f + wave * 0.12f);
            _haloTransform.localScale = new Vector3(haloScale, haloScale, 1f);
            Color glow = _gem.Glow;
            glow.a = _gem.HaloAlpha * (0.75f + wave * 0.25f);
            _halo.color = glow;

            // Resting gems breathe a little; all gems twinkle at random spots on their surface.
            if (_isResting)
                _cachedTransform.localScale = Vector3.one * (1f + wave * 0.06f);

            // Rarer gems twinkle more often.
            if (_age >= _nextSparkle)
            {
                _sparkle.enabled = true;
                _sparkleStartAge = _age;
                _sparkleTransform.localPosition = new Vector3(Random.Range(-3, 4) * Px, Random.Range(-2, 4) * Px, 0f);
                _nextSparkle = _age + Random.Range(0.6f, 1.4f) / (1f + (int)_gem.Tier * 0.4f);
            }
            if (_sparkle.enabled)
            {
                float remaining = 1f - (_age - _sparkleStartAge) / SparkleTime;
                if (remaining <= 0f)
                    _sparkle.enabled = false;
                else
                    _sparkleTransform.localScale = new Vector3(remaining, remaining, 1f);
            }
        }

        private void Simulate(float dt)
        {
            _velocity.y -= Gravity * dt;
            Vector2 position = _cachedTransform.position;

            if (!Mathf.Approximately(_velocity.x, 0f))
            {
                float stepX = _velocity.x * dt;
                RaycastHit2D wall = Physics2D.Raycast(position, new Vector2(Mathf.Sign(stepX), 0f), Mathf.Abs(stepX) + Radius, _groundMask);
                if (wall.collider != null)
                    _velocity.x = -_velocity.x * Bounce;
                else
                    position.x += stepX;
            }

            float stepY = _velocity.y * dt;
            if (stepY < 0f)
            {
                RaycastHit2D ground = Physics2D.Raycast(position, Vector2.down, -stepY + Radius, _groundMask);
                if (ground.collider != null)
                {
                    position.y = ground.point.y + Radius;
                    _velocity.y = -_velocity.y * Bounce;
                    _velocity.x *= GroundFriction;
                    if (_velocity.y < RestSpeed)
                    {
                        _velocity = Vector2.zero;
                        _isResting = true;
                    }
                }
                else
                {
                    position.y += stepY;
                }
            }
            else
            {
                position.y += stepY;
            }

            _cachedTransform.position = position;
        }

        private void Collect()
        {
            if (_kind == LootKind.Gem)
                Wallet.AddGems(_value);
            else
                Wallet.AddGold(_value);
            gameObject.SetActive(false);
            s_pool.Push(this);
        }

        // ---------- Generated pixel art (rows top to bottom) ----------

        private sealed class GemStyle
        {
            public GemTier Tier;
            public int Value;
            public Sprite Sprite;
            public Color Glow;
            public float HaloScale;
            public float HaloAlpha;
        }

        private static void EnsureSprites()
        {
            if (s_gems != null)
                return;

            var gold = Palette(new Color(0.30f, 0.17f, 0.04f), new Color(0.75f, 0.48f, 0.08f),
                new Color(0.96f, 0.74f, 0.16f), new Color(1f, 0.95f, 0.62f));
            s_coinSmall = BuildSprite(gold,
                "..dd..",
                ".dhgd.",
                "dhggGd",
                "dgggGd",
                ".dgGd.",
                "..dd..");
            s_coinLarge = BuildSprite(gold,
                "..dddd..",
                ".dhhggd.",
                "dhggggGd",
                "dhggggGd",
                "dgggggGd",
                "dggggGGd",
                ".dGGGGd.",
                "..dddd..");

            s_halo = BuildHalo(16);
            s_sparkle = BuildSprite(Palette(Color.white, Color.white, Color.white, Color.white),
                ".h.",
                "hhh",
                ".h.");

            s_gems = new[]
            {
                // Small blue shard.
                Gem(GemTier.Sapphire, 1, new Color(0.30f, 0.70f, 1f), 0.9f, 0.45f,
                    Palette(new Color(0.04f, 0.12f, 0.28f), new Color(0.10f, 0.38f, 0.80f),
                        new Color(0.22f, 0.76f, 1f), new Color(0.85f, 1f, 1f)),
                    "..d..",
                    ".dhd.",
                    "dhgGd",
                    "dggGd",
                    "dgGGd",
                    ".dGd.",
                    "..d.."),
                // Green emerald cut.
                Gem(GemTier.Emerald, 2, new Color(0.30f, 1f, 0.50f), 1.1f, 0.5f,
                    Palette(new Color(0.02f, 0.20f, 0.08f), new Color(0.05f, 0.50f, 0.20f),
                        new Color(0.20f, 0.90f, 0.40f), new Color(0.80f, 1f, 0.85f)),
                    ".dddd.",
                    "dhhggd",
                    "dhgggd",
                    "dhggGd",
                    "dgggGd",
                    "dggGGd",
                    "dgGGGd",
                    ".dddd."),
                // Violet diamond.
                Gem(GemTier.Amethyst, 4, new Color(0.75f, 0.45f, 1f), 1.3f, 0.55f,
                    Palette(new Color(0.18f, 0.04f, 0.28f), new Color(0.45f, 0.15f, 0.75f),
                        new Color(0.72f, 0.40f, 1f), new Color(0.95f, 0.85f, 1f)),
                    "...d...",
                    "..dhd..",
                    ".dhggd.",
                    "dhgggGd",
                    "dggggGd",
                    ".dggGd.",
                    "..dGd..",
                    "...d..."),
                // Large red brilliant.
                Gem(GemTier.Ruby, 8, new Color(1f, 0.30f, 0.30f), 1.7f, 0.65f,
                    Palette(new Color(0.28f, 0.02f, 0.05f), new Color(0.70f, 0.05f, 0.12f),
                        new Color(1f, 0.25f, 0.30f), new Color(1f, 0.85f, 0.85f)),
                    "..ddddd..",
                    ".dhhhggd.",
                    "dhhgggggd",
                    "dGGGGGGGd",
                    ".dhgggGd.",
                    "..dggGd..",
                    "...dGd...",
                    "....d...."),
            };
        }

        private static GemStyle Gem(GemTier tier, int value, Color glow, float haloScale, float haloAlpha,
            Dictionary<char, Color> palette, params string[] rows) =>
            new GemStyle
            {
                Tier = tier,
                Value = value,
                Sprite = BuildSprite(palette, rows),
                Glow = glow,
                HaloScale = haloScale,
                HaloAlpha = haloAlpha
            };

        // d = outline, G = shade, g = base, h = highlight.
        private static Dictionary<char, Color> Palette(Color outline, Color shade, Color fill, Color highlight) =>
            new Dictionary<char, Color> { { 'd', outline }, { 'G', shade }, { 'g', fill }, { 'h', highlight } };

        private static Sprite BuildSprite(Dictionary<char, Color> palette, params string[] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            var pixels = new Color[width * height];
            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Texture rows start at the bottom.
                    pixels[(height - 1 - row) * width + x] =
                        palette.TryGetValue(rows[row][x], out Color color) ? color : Color.clear;
                }
            }
            return ToSprite(pixels, width, height, FilterMode.Point);
        }

        // Soft radial falloff, filtered so it reads as light rather than pixels.
        private static Sprite BuildHalo(int size)
        {
            var pixels = new Color[size * size];
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Clamp01(Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) / centre);
                    float a = (1f - d) * (1f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            return ToSprite(pixels, size, size, FilterMode.Bilinear);
        }

        private static Sprite ToSprite(Color[] pixels, int width, int height, FilterMode filter)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
