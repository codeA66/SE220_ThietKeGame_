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

        [Tooltip("Dust puffed out on a ledge climb. Left empty, a generated pixel-art puff is used.")]
        [SerializeField] private VfxDefinition _runDustVfx;
        [Tooltip("Dust burst when a roll starts and when skidding. Left empty, a generated burst is used.")]
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
        [Tooltip("Ground speed needed before turning around kicks up a skid burst (units/s).")]
        [SerializeField] private float _minSkidSpeed = 4f;

        private PlayerContext _context;
        private float _lastVelocityX;

        private void Start()
        {
            if (_runDustVfx == null)
                _runDustVfx = GeneratedVfx.RunDust;
            if (_rollDustVfx == null)
                _rollDustVfx = GeneratedVfx.RollDust;
            if (_stompVfx == null)
                _stompVfx = GeneratedVfx.StompWave;

            _context = _controller.Context;
            _context.RollStarted += OnRollStarted;
            _context.Jumped += OnJumped;
            _context.AirJumped += OnAirJumped;
            _context.Landed += OnLanded;
            _context.Stomped += OnStomped;
            _context.LedgeClimbStarted += OnLedgeClimbStarted;
            _context.Combat.StrikePerformed += OnStrikePerformed;

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
            _context.LedgeClimbStarted -= OnLedgeClimbStarted;
            _context.Combat.StrikePerformed -= OnStrikePerformed;
        }

        private void Update()
        {
            if (_context == null)
                return;

            float velocityX = _context.Velocity.x;
            float lastVelocityX = _lastVelocityX;
            _lastVelocityX = velocityX;

            // Turning around at speed: dust is thrown the way the player was going.
            if (_context.IsGrounded && Mathf.Abs(lastVelocityX) >= _minSkidSpeed && velocityX * lastVelocityX < 0f)
                _spawner.Play(_rollDustVfx, _context.Position, lastVelocityX > 0f);
        }

        // Dust trails behind the roll, so it flips with the facing.
        private void OnRollStarted(Vector2 feet) => _spawner.Play(_rollDustVfx, feet, _context.FacingDirection < 0);

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

        // A puff each way on the ledge top as the player hauls up onto it.
        private void OnLedgeClimbStarted(Vector2 standPoint)
        {
            _spawner.Play(_runDustVfx, standPoint, false);
            _spawner.Play(_runDustVfx, standPoint, true);
        }
    }
}
