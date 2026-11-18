using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Plays movement effects from PlayerContext events. Keeps the FSM free of any VFX dependency.
    /// </summary>
    public class PlayerVfx : MonoBehaviour
    {
        [SerializeField] private PlayerController _controller;
        [SerializeField] private VfxSpawner _spawner;

        [Header("Effects")]
        [SerializeField] private VfxDefinition _airJumpVfx;
        [SerializeField] private VfxDefinition _landVfx;

        [Tooltip("Small puffs laid along the floor during a roll. Left empty, generated puffs are used, picked at random.")]
        [SerializeField] private VfxDefinition _runDustVfx;
        [Tooltip("Dust burst when a roll starts. Left empty, generated bursts are used, picked at random so no two rolls look alike.")]
        [SerializeField] private VfxDefinition _rollDustVfx;
        [Tooltip("How much bigger the landing cloud is drawn when an air stomp lands, so the ground pound reuses it.")]
        [Min(1f)]
        [SerializeField] private float _stompLandScale = 1.8f;
        [Tooltip("Draw speed streaks and scarf-coloured streaks behind the player during a stomp dive.")]
        [SerializeField] private bool _stompStreaks = true;
        [Tooltip("Leave a fading trail of afterimages behind every roll and stomp dive.")]
        [SerializeField] private bool _rollAfterimages = true;

        [Header("Rules")]
        [Tooltip("Also play the air-jump effect on ground jumps.")]
        [SerializeField] private bool _jumpVfxOnGroundJump;
        [Tooltip("Minimum downward speed at impact for the landing dust (units/s).")]
        [SerializeField] private float _minLandSpeed = 8f;
        [Tooltip("Seconds between the small dust puffs laid down along the floor while rolling.")]
        [Min(0.02f)]
        [SerializeField] private float _rollTrailInterval = 0.06f;

        private PlayerContext _context;
        private PlayerBow _bow;
        private CameraFollow2D _camera;
        private float _rollTrailUntil = -1f;
        private float _nextRollTrailAt;
        private bool _isStomping;
        private TrailRibbon[] _stompTrails;
        private Transform[] _scarfEmitters;
        private (float baseX, float size, float phase)[] _scarfFlutter;

        // The streaks start this far above the feet, about the middle of the body (units).
        private const float StompStreakHeight = 0.8f;
        // The scarf streaks start at the neck, and wave this fast (radians per second).
        private const float ScarfStreakHeight = 1.25f;
        private const float ScarfFlutterSpeed = 38f;

        // The camera shakes for as long as the beam lasts.
        private const float AwakenShakeAmplitude = 0.15f;

        // The awakening tears the body into this many vertical strips, each sliding up to this far up or down (units).
        private const int TearStrips = 9;
        private const float TearMaxShift = 0.14f;
        private readonly System.Collections.Generic.Dictionary<(Sprite, int), Sprite> _tearSprites =
            new System.Collections.Generic.Dictionary<(Sprite, int), Sprite>();

        // The landing cloud is played at the feet, nudged down this many units (3 px) so it sits on the floor.
        private const float LandDownOffset = 3f / 32f;

        private void Start()
        {

            _context = _controller.Context;
            _camera = FindFirstObjectByType<CameraFollow2D>();
            _context.RollStarted += OnRollStarted;
            _context.Jumped += OnJumped;
            _context.AirJumped += OnAirJumped;
            _context.Landed += OnLanded;
            _context.StompStarted += OnStompStarted;
            _context.Stomped += OnStomped;
            _context.StompEnded += OnStompEnded;
            _context.LedgeClimbFinished += OnLedgeClimbFinished;
            _context.Awakened += OnAwakened;
            _context.Combat.StrikePerformed += OnStrikePerformed;

            _bow = _context.Bow;
            if (_bow != null)
            {
                _bow.Fired += OnBowFired;
                _bow.ArrowWorldHit += OnArrowWorldHit;
            }

            if (_rollAfterimages)
                gameObject.AddComponent<PlayerAfterimages>().Setup(_context);
            if (_stompStreaks)
                BuildStompStreaks();
        }

        // Three parallel speed streaks riding the body, white at the head fading to the dust's sky blue. They only
        // record the player's path while a stomp dive lasts, then shrink away behind the player on landing.
        private void BuildStompStreaks()
        {
            var head = new Color32(255, 255, 255, 255);
            var tail = new Color32(108, 211, 255, 255);
            // sideways offset, head width (units), how long the tail lags (seconds)
            (float offset, float width, float lag)[] streaks = { (-0.3f, 0.1f, 0.12f), (0f, 0.2f, 0.16f), (0.3f, 0.1f, 0.12f) };
            // The scarf is what sells the character, so two streaks in its colours (sampled from the sprite: a
            // muted mauve fading to a deep purple) come off the neck, longer than the speed lines. They flutter
            // from side to side, so they read as the scarf whipping behind the dive.
            var scarfHead = new Color32(86, 74, 104, 255);
            var scarfTail = new Color32(54, 42, 66, 255);
            // sideways offset, head width, tail lag, flutter size (units), flutter phase
            (float offset, float width, float lag, float flutter, float phase)[] scarf =
            {
                (-0.1f, 0.15f, 0.26f, 0.1f, 0f),
                (0.1f, 0.1f, 0.2f, 0.13f, 2.2f),
            };

            _stompTrails = new TrailRibbon[streaks.Length + scarf.Length];
            for (int i = 0; i < streaks.Length; i++)
            {
                var go = new GameObject("StompStreak");
                go.transform.SetParent(transform, false);
                // Centred on the body: the player's pivot is at the feet.
                go.transform.localPosition = new Vector3(streaks[i].offset, StompStreakHeight, 0f);
                var trail = go.AddComponent<TrailRibbon>();
                trail.Setup(head, tail, streaks[i].width, streaks[i].lag, _context.Sprite, unlit: true);
                _stompTrails[i] = trail;
            }

            _scarfEmitters = new Transform[scarf.Length];
            _scarfFlutter = new (float, float, float)[scarf.Length];
            for (int i = 0; i < scarf.Length; i++)
            {
                var go = new GameObject("ScarfStreak");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(scarf[i].offset, ScarfStreakHeight, 0f);
                var trail = go.AddComponent<TrailRibbon>();
                trail.Setup(scarfHead, scarfTail, scarf[i].width, scarf[i].lag, _context.Sprite, unlit: true);
                _stompTrails[streaks.Length + i] = trail;
                _scarfEmitters[i] = go.transform;
                _scarfFlutter[i] = (scarf[i].offset, scarf[i].flutter, scarf[i].phase);
            }
        }

        private void OnDestroy()
        {
            if (_context == null)
                return;
            _context.RollStarted -= OnRollStarted;
            _context.Jumped -= OnJumped;
            _context.AirJumped -= OnAirJumped;
            _context.Landed -= OnLanded;
            _context.StompStarted -= OnStompStarted;
            _context.Stomped -= OnStomped;
            _context.StompEnded -= OnStompEnded;
            _context.LedgeClimbFinished -= OnLedgeClimbFinished;
            _context.Awakened -= OnAwakened;
            _context.Combat.StrikePerformed -= OnStrikePerformed;
            if (_bow != null)
            {
                _bow.Fired -= OnBowFired;
                _bow.ArrowWorldHit -= OnArrowWorldHit;
            }
        }

        private void Update()
        {
            if (_context == null)
                return;

            // The scarf streaks wave from side to side while the dive lasts.
            if (_isStomping && _scarfEmitters != null)
            {
                for (int i = 0; i < _scarfEmitters.Length; i++)
                {
                    (float baseX, float size, float phase) = _scarfFlutter[i];
                    Vector3 local = _scarfEmitters[i].localPosition;
                    local.x = baseX + Mathf.Sin(Time.time * ScarfFlutterSpeed + phase) * size;
                    _scarfEmitters[i].localPosition = local;
                }
            }

            // Rolling on the ground: lay small puffs along the floor behind the player.
            if (Time.time < _rollTrailUntil && _context.IsGrounded && Time.time >= _nextRollTrailAt)
            {
                _nextRollTrailAt = Time.time + _rollTrailInterval;
                _spawner.Play(RunDust, _context.Position, _context.FacingDirection < 0, 0f, Random.Range(0.7f, 1.2f));
            }
        }

        // A serialized asset wins; otherwise a randomly chosen generated variant, so no two puffs look alike.
        private VfxDefinition RunDust => _runDustVfx != null ? _runDustVfx : GeneratedVfx.RunDust;

        private VfxDefinition RollDust => _rollDustVfx != null ? _rollDustVfx : GeneratedVfx.RollDust;

        // The opening burst, then small puffs along the floor for as long as the roll lasts.
        private void OnRollStarted(Vector2 feet)
        {
            _spawner.Play(RollDust, feet, _context.FacingDirection < 0, 0f, Random.Range(0.85f, 1.25f));
            _rollTrailUntil = Time.time + _context.Config.RollDuration;
            _nextRollTrailAt = Time.time + _rollTrailInterval;
        }

        // Swing frames are drawn facing right with the hero's feet at the canvas centre, like the hero sprites.
        private void OnStrikePerformed(WeaponData.Strike strike, Vector2 feet, int facing) =>
            _spawner.Play(strike.SwingVfx, feet, facing < 0);

        private void OnJumped(Vector2 feet)
        {
            if (_jumpVfxOnGroundJump)
                _spawner.Play(_airJumpVfx, feet);
        }

        private void OnAirJumped(Vector2 feet) => _spawner.Play(_airJumpVfx, feet);

        private void OnLanded(Vector2 feet, float impactSpeed)
        {
            // A stomp lands with a bigger version of this same cloud, so the ordinary one is skipped.
            if (_isStomping)
                return;
            if (impactSpeed >= _minLandSpeed)
                _spawner.Play(_landVfx, feet + Vector2.down * LandDownOffset);
        }

        private void OnStompStarted()
        {
            _isStomping = true;
            SetStreaks(true);
        }

        // The stomp state ended, whether it landed or was cut short. A stomp that never lands (a hit, death, a
        // state change) would otherwise leave the streaks recording and the landing cloud suppressed.
        private void OnStompEnded()
        {
            _isStomping = false;
            SetStreaks(false);
        }

        private void SetStreaks(bool emitting)
        {
            if (_stompTrails == null)
                return;
            foreach (TrailRibbon trail in _stompTrails)
            {
                if (emitting)
                    trail.Clear();
                trail.Emitting = emitting;
            }
        }

        // The ground pound is the landing cloud drawn bigger, with physical rubble thrown up along the floor.
        private void OnStomped(Vector2 feet)
        {
            _isStomping = false;
            SetStreaks(false);
            _spawner.Play(_landVfx, feet + Vector2.down * LandDownOffset, false, 0f, _stompLandScale);
            float duration = _landVfx != null ? _landVfx.Frames.Length / _landVfx.Fps : 0.25f;
            Debris.Shockwave(feet, _context.GroundMask, duration);
        }

        // Release flash at the bow; the combo finisher gets the bigger gold one.
        private void OnBowFired(Vector2 muzzle, int facing, bool heavy) =>
            _spawner.Play(heavy ? GeneratedVfx.MuzzleFlashHeavy : GeneratedVfx.MuzzleFlash, muzzle, facing < 0);

        // Debris bursts back off the surface, against the arrow's flight.
        private void OnArrowWorldHit(Vector2 point, int direction, bool heavy) =>
            _spawner.Play(heavy ? GeneratedVfx.ArrowImpactHeavy : GeneratedVfx.ArrowImpact, point, direction > 0);

        // The player has just stood up: a solid white beam drops from the sky onto them. While it lasts the body is
        // a black silhouette torn into vertical strips that slide up and down, and white motes float up off it.
        private void OnAwakened()
        {
            Vector2 feet = _context.Position;
            VfxDefinition beam = GeneratedVfx.AscendBeam;
            _spawner.Play(beam, feet);

            float duration = beam.Frames.Length / beam.Fps;
            if (_camera != null)
                _camera.Shake(AwakenShakeAmplitude, duration);
            StartCoroutine(TearBody(duration));
            StartCoroutine(LightRays(duration));
        }

        // Glowing white motes shoot straight up the beam, each with a trail behind it, a few every moment.
        private System.Collections.IEnumerator LightRays(float duration)
        {
            LayerMask ground = _context.GroundMask;
            for (float t = 0f; t < duration * 0.8f; t += 0.04f)
            {
                Vector2 feet = _context.Position;
                for (int i = 0; i < 2; i++)
                {
                    Vector2 origin = new Vector2(feet.x + Random.Range(-0.45f, 0.45f), feet.y + Random.Range(0f, 0.6f));
                    Sparks.Emit(origin, 1, SparkBurst.Ascend, ground);
                }
                yield return new WaitForSeconds(0.04f);
            }
        }

        // Hides the real sprite and draws it as black strips instead, each slid up or down by a random amount that
        // is re-rolled a few times a second, fading back to a whole body at the end.
        private System.Collections.IEnumerator TearBody(float duration)
        {
            SpriteRenderer body = _context.Sprite;
            var strips = new SpriteRenderer[TearStrips];
            for (int i = 0; i < TearStrips; i++)
            {
                var go = new GameObject("TearStrip");
                go.transform.SetParent(body.transform, false);
                strips[i] = go.AddComponent<SpriteRenderer>();
                strips[i].sharedMaterial = body.sharedMaterial;
                strips[i].sortingLayerID = body.sortingLayerID;
                strips[i].sortingOrder = body.sortingOrder + 1;
                strips[i].color = Color.black;
            }

            body.enabled = false;
            float nextShift = 0f;
            var shifts = new float[TearStrips];
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float fade = Mathf.Clamp01((duration - t) / (duration * 0.35f));
                if (t >= nextShift)
                {
                    nextShift = t + 0.06f;
                    for (int i = 0; i < TearStrips; i++)
                        shifts[i] = Random.value < 0.35f ? 0f : Random.Range(-1f, 1f) * TearMaxShift;
                }
                SliceInto(strips, body, shifts, fade);
                yield return null;
            }

            body.enabled = true;
            foreach (SpriteRenderer strip in strips)
                Destroy(strip.gameObject);
        }

        private void SliceInto(SpriteRenderer[] strips, SpriteRenderer body, float[] shifts, float fade)
        {
            Sprite source = body.sprite;
            if (source == null)
                return;
            Rect rect = source.rect;
            float bandWidth = rect.width / strips.Length;
            for (int i = 0; i < strips.Length; i++)
            {
                float left = Mathf.Floor(bandWidth * i);
                float right = i == strips.Length - 1 ? rect.width : Mathf.Floor(bandWidth * (i + 1));
                var band = new Rect(rect.x + left, rect.y, right - left, rect.height);
                if (!_tearSprites.TryGetValue((source, i), out Sprite piece))
                {
                    Vector2 pivot = new Vector2((source.pivot.x - left) / band.width, source.pivot.y / band.height);
                    piece = Sprite.Create(source.texture, band, pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    _tearSprites[(source, i)] = piece;
                }
                strips[i].sprite = piece;
                strips[i].flipX = body.flipX;
                strips[i].transform.localPosition = new Vector3(0f, shifts[i] * fade, 0f);
            }
        }

        // One small puff at the feet as the player gets up onto the ledge, trailing behind.
        private void OnLedgeClimbFinished(Vector2 feet) =>
            _spawner.Play(GeneratedVfx.LedgeDust, feet, _context.FacingDirection < 0, 0f, Random.Range(0.8f, 1.2f));
    }
}
