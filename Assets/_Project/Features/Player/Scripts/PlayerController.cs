using DeadCells.Player.StateMachine;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Owns the player FSM and drives it from Update/FixedUpdate. Movement logic lives in the states.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig _config;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerAnimatorBridge _animator;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private LayerMask _groundMask;

        private Rigidbody2D _body;
        private PlayerContext _context;
        private PlayerStateMachine _machine;

        public PlayerContext Context => _context;
        public IPlayerState CurrentState => _machine?.Current;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            var collider = GetComponent<BoxCollider2D>();
            _context = new PlayerContext(_body, collider, _sprite, _groundMask, _config, _input, _animator);
            _machine = new PlayerStateMachine(_context);
        }

        private void Start() => _machine.ChangeState(_machine.Idle);

        private void Update()
        {
            _input.Poll();
            _context.RefreshFrame();
            _machine.Tick();
            _animator.SetLocomotion(Mathf.Abs(_body.linearVelocity.x), _context.IsGrounded, _body.linearVelocity.y);
        }

        private void FixedUpdate() => _machine.FixedTick();
    }
}
