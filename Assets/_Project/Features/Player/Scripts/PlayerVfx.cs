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
        }

        private void OnDestroy()
        {
            if (_context == null)
                return;
            _context.Jumped -= OnJumped;
            _context.AirJumped -= OnAirJumped;
            _context.Landed -= OnLanded;
        }

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
