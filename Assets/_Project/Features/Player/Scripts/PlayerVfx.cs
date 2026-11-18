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
        [Tooltip("Shockwave when an air stomp hits the ground. Left empty, a generated shockwave is used.")]
        [SerializeField] private VfxDefinition _stompVfx;
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
        private float _rollTrailUntil = -1f;
        private float _nextRollTrailAt;

        private void Start()
        {
            if (_stompVfx == null)
                _stompVfx = GeneratedVfx.StompWave;

            _context = _controller.Context;
            _context.RollStarted += OnRollStarted;
            _context.Jumped += OnJumped;
            _context.AirJumped += OnAirJumped;
            _context.Landed += OnLanded;
            _context.Stomped += OnStomped;
            _context.LedgeClimbFinished += OnLedgeClimbFinished;
            _context.Combat.StrikePerformed += OnStrikePerformed;

            _bow = _context.Bow;
            if (_bow != null)
            {
                _bow.Fired += OnBowFired;
                _bow.ArrowWorldHit += OnArrowWorldHit;
            }

            if (_rollAfterimages)
                gameObject.AddComponent<PlayerAfterimages>().Setup(_context);
        }

        private void OnDestroy()
        {
            if (_context == null)
                return;
            _context.RollStarted -= OnRollStarted;
            _context.Jumped -= OnJumped;
            _context.AirJumped -= OnAirJumped;
            _context.Landed -= OnLanded;
            _context.Stomped -= OnStomped;
            _context.LedgeClimbFinished -= OnLedgeClimbFinished;
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
            if (impactSpeed >= _minLandSpeed)
                _spawner.Play(_landVfx, feet);
        }

        private void OnStomped(Vector2 feet) => _spawner.Play(_stompVfx, feet);

        // Release flash at the bow; the combo finisher gets the bigger gold one.
        private void OnBowFired(Vector2 muzzle, int facing, bool heavy) =>
            _spawner.Play(heavy ? GeneratedVfx.MuzzleFlashHeavy : GeneratedVfx.MuzzleFlash, muzzle, facing < 0);

        // Debris bursts back off the surface, against the arrow's flight.
        private void OnArrowWorldHit(Vector2 point, int direction, bool heavy) =>
            _spawner.Play(heavy ? GeneratedVfx.ArrowImpactHeavy : GeneratedVfx.ArrowImpact, point, direction > 0);

        // One small puff at the feet as the player gets up onto the ledge, trailing behind.
        private void OnLedgeClimbFinished(Vector2 feet) =>
            _spawner.Play(GeneratedVfx.LedgeDust, feet, _context.FacingDirection < 0, 0f, Random.Range(0.8f, 1.2f));
    }
}
