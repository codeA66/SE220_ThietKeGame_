using DeadCells.Combat;
using DeadCells.Player.StateMachine;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Owns the player FSM and drives it from Update/FixedUpdate. Movement logic lives in the states.
    /// Routes Health events into the Hurt / Dead states.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig _config;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerAnimatorBridge _animator;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private LayerMask _groundMask;
        [Tooltip("Layers the player passes through while rolling (enemies).")]
        [SerializeField] private LayerMask _rollPassThroughMask;
        [Tooltip("Layers of chain / vine triggers the player can climb.")]
        [SerializeField] private LayerMask _climbableMask;

        [Header("Combat")]
        [SerializeField] private Health _health;
        [SerializeField] private WeaponData _weapon;
        [Tooltip("Layers the player's attacks can hit.")]
        [SerializeField] private LayerMask _attackTargets;
        [Tooltip("Seconds before respawning after death (temporary until the run loop exists).")]
        [SerializeField] private float _respawnDelay = 1.5f;

        private Rigidbody2D _body;
        private PlayerContext _context;
        private PlayerStateMachine _machine;
        private Vector2 _spawnPoint;
        private float _respawnAt = -1f;

        public PlayerContext Context => _context;
        public IPlayerState CurrentState => _machine?.Current;
        public Health Health => _health;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            var collider = GetComponent<BoxCollider2D>();
            _context = new PlayerContext(_body, collider, _sprite, _groundMask, _rollPassThroughMask, _climbableMask, _config, _input, _animator);
            _context.Combat = new PlayerCombat(transform, collider, _weapon, _attackTargets);
            _machine = new PlayerStateMachine(_context);
            _spawnPoint = _body.position;

            _health.DamageFilter = FilterDamage;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health == null)
                return;
            _health.DamageFilter = null;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void Start() => _machine.ChangeState(_machine.Idle);

        private void Update()
        {
            if (_respawnAt >= 0f && Time.time >= _respawnAt)
                Respawn();

            _input.Poll();
            _context.RefreshFrame();
            _machine.Tick();
            _animator.SetLocomotion(Mathf.Abs(_body.linearVelocity.x), _context.IsGrounded, _body.linearVelocity.y);
        }

        private void FixedUpdate() => _machine.FixedTick();

        /// <summary>
        /// Rolling ignores hits; a hit during the perfect-dodge window counts as a perfect dodge.
        /// </summary>
        private bool FilterDamage(DamageInfo info)
        {
            if (_context.IsPerfectDodgeWindowOpen && _context.TryRegisterPerfectDodge())
                return false;
            return !_context.IsInvulnerable;
        }

        private void OnDamaged(DamageInfo info)
        {
            _context.Combat.LastDamageTaken = info;
            _machine.ForceChangeState(_machine.Hurt);
        }

        private void OnDied(DamageInfo info)
        {
            _context.Combat.LastDamageTaken = info;
            _machine.ForceChangeState(_machine.Dead);
            _respawnAt = Time.time + _respawnDelay;
        }

        private void Respawn()
        {
            _respawnAt = -1f;
            _body.position = _spawnPoint;
            _body.linearVelocity = Vector2.zero;
            _health.RestoreFull();
            _machine.ForceChangeState(_machine.Idle);
        }
    }
}
