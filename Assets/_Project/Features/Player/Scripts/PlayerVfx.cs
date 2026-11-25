using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
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
        [SerializeField] private VfxDefinition _dodgeGlintVfx;

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
        private PlayerSpawnHead _spawnHead;
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

        // Where the stun stars circle (units above the feet) and how fast they orbit (radians per second).
        private const float StunHeadHeight = 1.75f;
        private const float StunOrbitSpeed = 7f;

        // On death the scarf lifts off for this long, floats for this long, then is carried off by the beam over this long (seconds).
        private const float ScarfLiftTime = 0.35f;
        private const float ScarfFloatTime = 0.7f;
        private const float ScarfLeaveTime = 0.85f;
        // How long the scarf convulses under the beam before it bursts, and how hard the shards fall (units per second squared).
        private const float ScarfConvulseTime = 0.18f;
        private const float ShardGravity = 14f;

        // The camera shakes for as long as the beam lasts.
        private const float AwakenShakeAmplitude = 0.15f;

        // The awakening tears the body into this many vertical strips, each sliding up to this far up or down (units).
        private const int TearStrips = 9;
        private const float TearMaxShift = 0.14f;
        private readonly System.Collections.Generic.Dictionary<(Sprite, int), Sprite> _tearSprites =
            new System.Collections.Generic.Dictionary<(Sprite, int), Sprite>();
        private SpriteRenderer _tearBody;
        private SpriteRenderer[] _activeTearStrips;

        // The landing cloud is played at the feet, nudged down this many units (3 px) so it sits on the floor.
        private const float LandDownOffset = 3f / 32f;

        private void Start()
        {

            _context = _controller.Context;
            _camera = FindFirstObjectByType<CameraFollow2D>();
            _spawnHead = GetComponentInParent<PlayerController>() != null ? GetComponentInParent<PlayerController>().GetComponentInChildren<PlayerSpawnHead>(true) : null;
            _context.RollStarted += OnRollStarted;
            _context.Jumped += OnJumped;
            _context.AirJumped += OnAirJumped;
            _context.WallJumped += OnWallJumped;
            _context.Landed += OnLanded;
            _context.PerfectDodged += OnPerfectDodged;
            _context.StompStarted += OnStompStarted;
            _context.Stomped += OnStomped;
            _context.StompEnded += OnStompEnded;
            _context.LedgeClimbFinished += OnLedgeClimbFinished;
            _context.Awakened += OnAwakened;
            _context.Stunned += OnStunned;
            _controller.Health.Damaged += OnHurt;
            _controller.Health.Died += OnKilled;
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

        private void OnDisable() => RestoreTearBody();

        private void OnDestroy()
        {
            RestoreTearBody();
            foreach (Sprite piece in _tearSprites.Values)
                DestroyRuntimeObject(piece);
            _tearSprites.Clear();

            if (_context == null)
                return;
            _context.RollStarted -= OnRollStarted;
            _context.Jumped -= OnJumped;
            _context.AirJumped -= OnAirJumped;
            _context.WallJumped -= OnWallJumped;
            _context.Landed -= OnLanded;
            _context.PerfectDodged -= OnPerfectDodged;
            _context.StompStarted -= OnStompStarted;
            _context.Stomped -= OnStomped;
            _context.StompEnded -= OnStompEnded;
            _context.LedgeClimbFinished -= OnLedgeClimbFinished;
            _context.Awakened -= OnAwakened;
            _context.Stunned -= OnStunned;
            if (_controller != null && _controller.Health != null)
            {
                _controller.Health.Damaged -= OnHurt;
                _controller.Health.Died -= OnKilled;
            }
            if (_context.Combat != null)
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
            if (_spawner != null && Time.time < _rollTrailUntil && _context.IsGrounded && Time.time >= _nextRollTrailAt)
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
            if (_spawner != null)
                _spawner.Play(RollDust, feet, _context.FacingDirection < 0, 0f, Random.Range(0.85f, 1.25f));
            _rollTrailUntil = Time.time + _context.Config.RollDuration;
            _nextRollTrailAt = Time.time + _rollTrailInterval;
            CombatAudioHub.Play(SfxId.Roll, 0.72f);
        }

        // Swing frames are drawn facing right with the hero's feet at the canvas centre, like the hero sprites.
        private void OnStrikePerformed(WeaponData.Strike strike, Vector2 feet, int facing)
        {
            if (_spawner != null)
                _spawner.Play(strike.SwingVfx, feet, facing < 0);
            SfxId slashSfx = strike.CanCrit || strike.AnimState == "AttackC"
                ? SfxId.SwordSlash3
                : strike.AnimState == "AttackB" ? SfxId.SwordSlash2 : SfxId.SwordSlash1;
            CombatAudioHub.Play(slashSfx, 0.78f);
        }

        private void OnJumped(Vector2 feet)
        {
            if (_spawner != null && _jumpVfxOnGroundJump)
                _spawner.Play(_airJumpVfx, feet);
            CombatAudioHub.Play(SfxId.Jump, 0.70f);
        }

        private void OnAirJumped(Vector2 feet)
        {
            if (_spawner != null)
                _spawner.Play(_airJumpVfx, feet);
            CombatAudioHub.Play(SfxId.AirJump, 0.75f);
        }

        private void OnWallJumped(Vector2 feet, int awayDir) =>
            CombatAudioHub.Play(SfxId.WallJump, 0.75f);

        private void OnLanded(Vector2 feet, float impactSpeed)
        {
            // A stomp lands with a bigger version of this same cloud, so the ordinary one is skipped.
            if (_isStomping || impactSpeed < _minLandSpeed)
                return;
            if (_spawner != null)
                _spawner.Play(_landVfx, feet + Vector2.down * LandDownOffset);
            CombatAudioHub.Play(SfxId.Land, 0.65f);
        }

        private void OnPerfectDodged()
        {
            if (_spawner != null)
            {
                var vfx = _dodgeGlintVfx != null ? _dodgeGlintVfx : _airJumpVfx;
                if (vfx != null)
                    _spawner.Play(vfx, _context.PerfectDodgeGhostPosition + Vector2.up * 0.8f);
            }
            CombatAudioHub.Play(SfxId.PerfectDodge, 0.85f);
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
            if (_spawner != null)
            {
                _spawner.Play(_landVfx, feet + Vector2.down * LandDownOffset, false, 0f, _stompLandScale);
                if (_dodgeGlintVfx != null)
                    _spawner.Play(_dodgeGlintVfx, feet + new Vector2(0f, 0.3f));
            }
            float duration = _landVfx != null ? _landVfx.Frames.Length / _landVfx.Fps : 0.25f;
            Debris.Shockwave(feet, _context.GroundMask, duration);
            CombatAudioHub.Play(SfxId.StompImpact, 0.90f);
        }

        // Release flash at the bow; the combo finisher gets the bigger gold one.
        private void OnBowFired(Vector2 muzzle, int facing, bool heavy)
        {
            if (_spawner != null)
                _spawner.Play(heavy ? GeneratedVfx.MuzzleFlashHeavy : GeneratedVfx.MuzzleFlash, muzzle, facing < 0);
        }

        // Debris bursts back off the surface, against the arrow's flight.
        private void OnArrowWorldHit(Vector2 point, int direction, bool heavy)
        {
            if (_spawner != null)
                _spawner.Play(heavy ? GeneratedVfx.ArrowImpactHeavy : GeneratedVfx.ArrowImpact, point, direction > 0);
        }

        // The player has just stood up: a solid white beam drops from the sky onto them. While it lasts the body is
        // a black silhouette torn into vertical strips that slide up and down, and white motes float up off it.
        private void OnAwakened()
        {
            Vector2 feet = _context.Position;
            VfxDefinition beam = GeneratedVfx.AscendBeam;
            if (_spawner != null)
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
            if (body == null)
                yield break;
            RestoreTearBody();

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

            _activeTearStrips = strips;
            _tearBody = body;
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

            RestoreTearBody();
        }

        private void RestoreTearBody()
        {
            if (_tearBody != null)
            {
                _tearBody.enabled = true;
                _tearBody = null;
            }

            if (_activeTearStrips == null)
                return;
            foreach (SpriteRenderer strip in _activeTearStrips)
            {
                if (strip != null)
                    DestroyRuntimeObject(strip.gameObject);
            }
            _activeTearStrips = null;
        }

        private static void DestroyRuntimeObject(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
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

        // Taking a hit: a spark on the side the blow came from, a light spray of blood thrown the way the player is
        // pushed, and a quick white flash on the body.
        private void OnHurt(DamageInfo info)
        {
            Vector3 chest = ChestPoint();
            int push = info.Direction == 0 ? -_context.FacingDirection : info.Direction;
            if (_spawner != null)
                _spawner.Play(GeneratedVfx.PlayerHitSpark, chest + new Vector3(-push * 0.25f, 0f, 0f), push < 0, Random.Range(-25f, 25f));
            BloodDrops.Emit(chest, push, BloodBurst.Light, _context.GroundMask);
            StartCoroutine(FlashBody(0.12f));
        }

        // Dying: a heavy burst of blood and a mist cloud, thrown the way the killing blow pushed.
        private void OnKilled(DamageInfo info)
        {
            Vector3 chest = ChestPoint();
            int push = info.Direction == 0 ? -_context.FacingDirection : info.Direction;
            BloodDrops.Emit(chest, push, BloodBurst.Gore, _context.GroundMask, Random.Range(-10f, 10f));
            BloodDrops.Emit(chest, push, BloodBurst.Heavy, _context.GroundMask, Random.Range(-25f, 25f));
            if (_spawner != null)
                _spawner.Play(GeneratedVfx.PlayerBloodMist, chest, push < 0, Random.Range(-10f, 10f));
            StartCoroutine(FlashBody(0.2f));
            StartCoroutine(ScarfLeaves());
        }

        // The scarf (the spawn head sprite) leaves the dead player: a copy of it rises off the neck and floats in the
        // air, swaying and turning. After a few seconds a white beam drops from the sky onto it and carries it away
        // upward. The body stays where it fell.
        private System.Collections.IEnumerator ScarfLeaves()
        {
            if (_spawnHead == null || _spawnHead.Renderer == null || _spawnHead.Renderer.sprite == null)
                yield break;
            yield return new WaitForSeconds(0.3f);
            if (_controller.Health.IsAlive)
                yield break;

            SpriteRenderer head = _spawnHead.Renderer;
            Vector2 feet = _context.Position;
            int facing = _context.FacingDirection;
            var go = new GameObject("DriftingScarf");
            go.transform.position = feet + new Vector2(0.24f * facing, 0.15f);
            go.transform.localScale = Vector3.one * _spawnHead.BaseScale;
            var scarf = go.AddComponent<SpriteRenderer>();
            scarf.sprite = head.sprite;
            scarf.flipX = facing < 0;
            scarf.sharedMaterial = head.sharedMaterial;
            scarf.sortingLayerID = head.sortingLayerID;
            scarf.sortingOrder = head.sortingOrder + 1;
            var block = new MaterialPropertyBlock();
            head.GetPropertyBlock(block);
            scarf.SetPropertyBlock(block);

            // Rise off the neck and drift back, easing to a stop.
            Vector2 start = go.transform.position;
            Vector2 hover = start + new Vector2(-facing * 0.5f, 1.5f);
            for (float t = 0f; t < ScarfLiftTime; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / ScarfLiftTime, 3f);
                go.transform.position = Vector2.Lerp(start, hover, k);
                yield return null;
            }

            // Float, swaying, bobbing and turning slightly.
            for (float t = 0f; t < ScarfFloatTime; t += Time.deltaTime)
            {
                go.transform.position = hover + new Vector2(Mathf.Sin(t * 3.2f) * 0.18f, Mathf.Sin(t * 2.1f + 1f) * 0.1f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.6f) * 10f);
                yield return null;
            }

            // The beam comes down through it and the scarf is lifted away along it.
            Vector2 held = go.transform.position;
            // The strike: a white-hot flash on the scarf and a hit spark at the point of impact, then it goes black.
            scarf.color = Color.white;
            if (_spawner != null)
                _spawner.Play(GeneratedVfx.PlayerHitSpark, held, false, Random.Range(-20f, 20f), 1.6f);
            VfxDefinition beam = GeneratedVfx.AscendBeam;
            if (_spawner != null)
                _spawner.Play(beam, new Vector2(held.x, feet.y));
            if (_camera != null)
                _camera.Shake(AwakenShakeAmplitude * 1.6f, beam.Frames.Length / beam.Fps);
            yield return new WaitForSeconds(0.04f);
            scarf.color = Color.black;
            yield return TearApart(scarf, block);
            Destroy(go);
        }

        // The scarf is smitten by the beam: it convulses, then bursts into a grid of black shards thrown out at their own speeds
        // after a tiny stagger, spinning and falling as they shrink away, so it looks destroyed, not carried off.
        private System.Collections.IEnumerator TearApart(SpriteRenderer scarf, MaterialPropertyBlock block)
        {
            Sprite source = scarf.sprite;
            Rect rect = source.rect;
            const int columns = 6;
            const int rows = 3;
            int count = columns * rows;
            var shards = new SpriteRenderer[count];
            var pieces = new Sprite[count];
            var startPositions = new Vector3[count];
            var delays = new float[count];
            var speeds = new float[count];
            var angles = new float[count];
            var spins = new float[count];

            float cellWidth = rect.width / columns;
            float cellHeight = rect.height / rows;
            
            for (int i = 0; i < count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                var cell = new Rect(rect.x + column * cellWidth, rect.y + row * cellHeight, cellWidth, cellHeight);
                var pivot = new Vector2((source.pivot.x - column * cellWidth) / cellWidth, (source.pivot.y - row * cellHeight) / cellHeight);
                pieces[i] = Sprite.Create(source.texture, cell, pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);

                var go = new GameObject("ScarfShard");
                go.transform.SetPositionAndRotation(scarf.transform.position, scarf.transform.rotation);
                go.transform.localScale = scarf.transform.lossyScale;
                var shard = go.AddComponent<SpriteRenderer>();
                shard.sprite = pieces[i];
                shard.flipX = scarf.flipX;
                shard.sharedMaterial = scarf.sharedMaterial;
                shard.sortingLayerID = scarf.sortingLayerID;
                shard.sortingOrder = scarf.sortingOrder;
                shard.color = Color.black;
                shard.SetPropertyBlock(block);
                shards[i] = shard;
                startPositions[i] = go.transform.position;
                delays[i] = Random.Range(0f, 0.08f);
                speeds[i] = Random.Range(6f, 14f);
                angles[i] = Random.Range(15f, 165f) * Mathf.Deg2Rad;
                spins[i] = Random.Range(-540f, 540f);
            }
            scarf.enabled = false;

            for (float t = 0f; t < ScarfLeaveTime; t += Time.deltaTime)
            {
                for (int i = 0; i < count; i++)
                {
                    // Struck: the whole scarf convulses in place under the light, then it blows apart, the shards
                    // thrown out from its centre and falling as they spin and shrink to nothing.
                    float local = t - ScarfConvulseTime - delays[i];
                    Transform shardTransform = shards[i].transform;
                    if (local < 0f)
                    {
                        float shake = t < ScarfConvulseTime ? 0.06f : 0f;
                        shardTransform.position = startPositions[i] + new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0f);
                        continue;
                    }
                    Vector3 velocity = new Vector3(Mathf.Cos(angles[i]), Mathf.Sin(angles[i]), 0f) * speeds[i];
                    shardTransform.position = startPositions[i] + velocity * local + new Vector3(0f, -0.5f * ShardGravity * local * local, 0f);
                    shardTransform.rotation = Quaternion.Euler(0f, 0f, spins[i] * local);
                    float life = ScarfLeaveTime - ScarfConvulseTime - delays[i];
                    shardTransform.localScale = scarf.transform.lossyScale * Mathf.Clamp01(1f - local / life);
                }
                yield return null;
            }

            foreach (SpriteRenderer shard in shards)
                Destroy(shard.gameObject);
            foreach (Sprite piece in pieces)
                Destroy(piece);
        }

        private Vector3 ChestPoint() => (Vector3)_context.Position + Vector3.up * 0.9f;

        private System.Collections.IEnumerator FlashBody(float duration)
        {
            var block = new MaterialPropertyBlock();
            int flashId = Shader.PropertyToID("_Flash");
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                _context.Sprite.GetPropertyBlock(block);
                block.SetFloat(flashId, 1f - t / duration);
                _context.Sprite.SetPropertyBlock(block);
                yield return null;
            }
            _context.Sprite.GetPropertyBlock(block);
            block.SetFloat(flashId, 0f);
            _context.Sprite.SetPropertyBlock(block);
        }

        // A stun: a ring of gold stars circles the head for as long as the player is stunned, with a small spark where it
        // lands. The stars orbit in an ellipse, pass in front of the head and behind it, spin, and shrink away at the end.
        private void OnStunned(float duration)
        {
            if (_spawner != null)
                _spawner.Play(GeneratedVfx.PlayerHitSpark, (Vector3)_context.Position + Vector3.up * StunHeadHeight, false, Random.Range(-20f, 20f), 0.8f);
            StartCoroutine(StunStars(duration));
        }

        private System.Collections.IEnumerator StunStars(float duration)
        {
            const int count = 3;
            SpriteRenderer body = _context.Sprite;
            var stars = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("StunStar");
                stars[i] = go.AddComponent<SpriteRenderer>();
                stars[i].sprite = GeneratedVfx.StunStar;
                stars[i].sharedMaterial = RuntimeSprites.UnlitMaterial;
                stars[i].sortingLayerID = body.sortingLayerID;
            }

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                Vector3 centre = (Vector3)_context.Position + Vector3.up * StunHeadHeight;
                float shrink = Mathf.Clamp01((duration - t) / 0.2f);
                for (int i = 0; i < count; i++)
                {
                    float angle = t * StunOrbitSpeed + i * Mathf.PI * 2f / count;
                    stars[i].transform.position = centre + new Vector3(Mathf.Cos(angle) * 0.42f, Mathf.Sin(angle) * 0.13f, 0f);
                    stars[i].transform.rotation = Quaternion.Euler(0f, 0f, t * 240f);
                    stars[i].transform.localScale = Vector3.one * shrink;
                    // The star on the near side of the orbit passes in front of the head, the far side behind it.
                    stars[i].sortingOrder = body.sortingOrder + (Mathf.Sin(angle) < 0f ? 2 : -1);
                }
                yield return null;
            }

            foreach (SpriteRenderer star in stars)
                Destroy(star.gameObject);
        }

        // One small puff at the feet as the player gets up onto the ledge, trailing behind.
        private void OnLedgeClimbFinished(Vector2 feet)
        {
            if (_spawner != null)
                _spawner.Play(GeneratedVfx.LedgeDust, feet, _context.FacingDirection < 0, 0f, Random.Range(0.8f, 1.2f));
        }
    }
}
