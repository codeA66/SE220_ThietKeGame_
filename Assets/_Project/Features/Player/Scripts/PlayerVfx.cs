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
        [SerializeField] private VfxDefinition _dodgeGlintVfx;

        [Header("Rules")]
        [Tooltip("Also play the air-jump effect on ground jumps.")]
        [SerializeField] private bool _jumpVfxOnGroundJump;
        [Tooltip("Minimum downward speed at impact for the landing dust (units/s).")]
        [SerializeField] private float _minLandSpeed = 8f;

        private PlayerContext _context;

        private void Start()
        {
            _context = _controller.Context;
            _context.Jumped += OnJumped;
            _context.AirJumped += OnAirJumped;
            _context.Landed += OnLanded;
            _context.RollStarted += OnRollStarted;
            _context.PerfectDodged += OnPerfectDodged;
            _context.StompLanded += OnStompLanded;
            _context.Combat.StrikePerformed += OnStrikePerformed;
        }

        private void OnDestroy()
        {
            if (_context == null)
                return;
            _context.Jumped -= OnJumped;
            _context.AirJumped -= OnAirJumped;
            _context.Landed -= OnLanded;
            _context.RollStarted -= OnRollStarted;
            _context.PerfectDodged -= OnPerfectDodged;
            _context.StompLanded -= OnStompLanded;
            if (_context.Combat != null)
                _context.Combat.StrikePerformed -= OnStrikePerformed;
        }

        // Swing frames are drawn facing right with the hero's feet at the canvas centre, like the hero sprites.
        private void OnStrikePerformed(WeaponData.Strike strike, Vector2 feet, int facing)
        {
            if (_spawner != null)
                _spawner.Play(strike.SwingVfx, feet, facing < 0);
        }

        private void OnJumped(Vector2 feet)
        {
            if (_spawner != null && _jumpVfxOnGroundJump)
                _spawner.Play(_airJumpVfx, feet);
        }

        private void OnAirJumped(Vector2 feet)
        {
            if (_spawner != null)
                _spawner.Play(_airJumpVfx, feet);
        }

        private void OnLanded(Vector2 feet, float impactSpeed)
        {
            if (_spawner != null && impactSpeed >= _minLandSpeed)
                _spawner.Play(_landVfx, feet);
        }

        private void OnRollStarted(Vector2 feet)
        {
            if (_spawner != null && _landVfx != null)
                _spawner.Play(_landVfx, feet, _context.FacingDirection < 0);
        }

        private void OnPerfectDodged()
        {
            if (_spawner == null)
                return;
            var vfx = _dodgeGlintVfx != null ? _dodgeGlintVfx : _airJumpVfx;
            if (vfx != null)
                _spawner.Play(vfx, _context.PerfectDodgeGhostPosition + Vector2.up * 0.8f);
        }

        private void OnStompLanded(Vector2 feet)
        {
            if (_spawner == null)
                return;
            if (_landVfx != null)
            {
                _spawner.Play(_landVfx, feet + new Vector2(-0.4f, 0f));
                _spawner.Play(_landVfx, feet + new Vector2(0.4f, 0f));
            }
            if (_dodgeGlintVfx != null)
                _spawner.Play(_dodgeGlintVfx, feet + new Vector2(0f, 0.3f));
        }
    }
}
