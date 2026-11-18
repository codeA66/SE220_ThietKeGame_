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

        [Tooltip("Dust kicked up while running. Left empty, a generated pixel-art puff is used.")]
        [SerializeField] private VfxDefinition _runDustVfx;
        [Tooltip("Dust burst when a roll starts. Left empty, a generated pixel-art burst is used.")]
        [SerializeField] private VfxDefinition _rollDustVfx;

        [Header("Rules")]
        [Tooltip("Also play the air-jump effect on ground jumps.")]
        [SerializeField] private bool _jumpVfxOnGroundJump;
        [Tooltip("Minimum downward speed at impact for the landing dust (units/s).")]
        [SerializeField] private float _minLandSpeed = 8f;
        [Tooltip("Ground distance covered between two run-dust puffs (units).")]
        [Min(0.1f)]
        [SerializeField] private float _runDustStride = 2.2f;
        [Tooltip("Ground speed needed before running kicks up dust (units/s).")]
        [SerializeField] private float _minRunDustSpeed = 3f;

        private PlayerContext _context;
        private float _strideDistance;

        private void Start()
        {
            if (_runDustVfx == null)
                _runDustVfx = GeneratedVfx.RunDust;
            if (_rollDustVfx == null)
                _rollDustVfx = GeneratedVfx.RollDust;

            _context = _controller.Context;
            _context.RollStarted += OnRollStarted;
            _context.Jumped += OnJumped;
            _context.AirJumped += OnAirJumped;
            _context.Landed += OnLanded;
            _context.Combat.StrikePerformed += OnStrikePerformed;
        }

        private void OnDestroy()
        {
            if (_context == null)
                return;
            _context.RollStarted -= OnRollStarted;
            _context.Jumped -= OnJumped;
            _context.AirJumped -= OnAirJumped;
            _context.Landed -= OnLanded;
            _context.Combat.StrikePerformed -= OnStrikePerformed;
        }

        private void Update()
        {
            if (_context == null)
                return;

            float velocityX = _context.Velocity.x;
            float speed = Mathf.Abs(velocityX);
            if (!_context.IsGrounded || speed < _minRunDustSpeed)
            {
                _strideDistance = 0f;
                return;
            }

            _strideDistance += speed * Time.deltaTime;
            if (_strideDistance < _runDustStride)
                return;

            _strideDistance = 0f;
            _spawner.Play(_runDustVfx, _context.Position, velocityX < 0f);
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
    }
}
