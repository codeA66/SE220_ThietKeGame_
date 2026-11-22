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
    public class PlayerController : MonoBehaviour, IStaggerable
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
        [SerializeField] private PlayerBow _bow;
        [SerializeField] private StarterWeaponPickup[] _starterWeaponPickups = System.Array.Empty<StarterWeaponPickup>();
        [Tooltip("Layers the player's attacks can hit.")]
        [SerializeField] private LayerMask _attackTargets;
        [Tooltip("Respawn at the start position without resetting the scene.")]
        [SerializeField] private bool _autoRespawn = true;
        [Tooltip("Seconds before an automatic respawn.")]
        [SerializeField] private float _respawnDelay = 1.5f;
        [SerializeField] private PowerScrollInteraction _powerScrolls;

        [Header("Spawn Intro")]
        [Tooltip("Head that drops and crawls to the body on scene start. Leave empty to skip the intro.")]
        [SerializeField] private PlayerSpawnHead _spawnHead;

        private Rigidbody2D _body;
        private PlayerContext _context;
        private PlayerStateMachine _machine;
        private Vector2 _spawnPoint;
        private float _respawnAt = -1f;
        private StarterWeaponPickup _focusedPickup;

        public PlayerContext Context => _context;
        public IPlayerState CurrentState => _machine?.Current;
        public Health Health => _health;
        public PlayerWeaponLoadout Weapons => _context?.Weapons;
        public PlayerRunStats RunStats { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            var collider = GetComponent<BoxCollider2D>();
            _context = new PlayerContext(_body, collider, _sprite, _groundMask, _rollPassThroughMask, _climbableMask, _config, _input, _animator);
            RunStats = new PlayerRunStats(_health);
            _context.Combat = new PlayerCombat(transform, collider, _weapon, _attackTargets, RunStats);
            if (_bow == null)
                throw new System.InvalidOperationException("PlayerController requires its PlayerBow reference.");
            _bow.Initialize(_attackTargets, _groundMask, RunStats);
            _context.Bow = _bow;
            _context.Weapons = new PlayerWeaponLoadout(_starterWeaponPickups.Length == 0);
            _machine = new PlayerStateMachine(_context);
            _spawnPoint = _body.position;

            _context.Combat.TargetHit += OnMeleeTargetHit;
            _health.DamageFilter = FilterDamage;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_context?.Combat != null)
                _context.Combat.TargetHit -= OnMeleeTargetHit;
            if (_health == null)
                return;
            _health.DamageFilter = null;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void OnMeleeTargetHit(Health victim, DamageInfo _)
        {
            if (victim != null)
                _bow.ExtractArrowsFrom(victim);
        }

        private void Start()
        {
            if (_config.PlaySpawnIntro && _spawnHead != null)
            {
                _machine.ChangeState(_machine.Spawn);
                _spawnHead.Play(_context);
                return;
            }
            _machine.ChangeState(_machine.Idle);
        }

        private void Update()
        {
            if (_respawnAt >= 0f && Time.time >= _respawnAt)
                Respawn();

            _input.Poll();
            if (RunStats.BlocksGameplayInput)
                return;

            _context.RefreshFrame();
            if (_powerScrolls != null && _powerScrolls.Tick(_body.position, CanReadScroll(), _input.InteractPressed, RunStats))
                return;

            UpdateStarterPickup();
            UpdateWeaponSelection();
            _machine.Tick();
            _animator.SetLocomotion(Mathf.Abs(_body.linearVelocity.x), _context.IsGrounded, _body.linearVelocity.y);
        }

        private void FixedUpdate() => _machine.FixedTick();

        private bool CanReadScroll() => _health.IsAlive && _context.IsGrounded &&
            (_machine.Current == _machine.Idle || _machine.Current == _machine.Run || _machine.Current == _machine.Crouch);

        private void UpdateStarterPickup()
        {
            StarterWeaponPickup nearest = null;
            float bestDistance = float.PositiveInfinity;
            if (_health.IsAlive && _machine.Current != _machine.Spawn && _machine.Current != _machine.Hurt)
            {
                Vector2 position = _body.position;
                foreach (var pickup in _starterWeaponPickups)
                {
                    if (pickup == null || !pickup.TryGetDistance(position, out float distance) || distance >= bestDistance)
                        continue;
                    nearest = pickup;
                    bestDistance = distance;
                }
            }

            if (nearest != _focusedPickup)
            {
                if (_focusedPickup != null)
                    _focusedPickup.SetFocused(false);
                _focusedPickup = nearest;
                if (_focusedPickup != null)
                    _focusedPickup.SetFocused(true);
            }

            if (_input.InteractPressed && _focusedPickup != null && _focusedPickup.TryCollect(Weapons))
                _focusedPickup = null;
        }

        private void UpdateWeaponSelection()
        {
            if (!_health.IsAlive)
                return;
            if (_machine.Current is StateMachine.States.PlayerAttackState attackState && !attackState.CanWeaponSwapCancel)
                return;
            PlayerWeaponSlot slot;
            if (_input.EquipSwordPressed)
                slot = PlayerWeaponSlot.Sword;
            else if (_input.EquipBowPressed)
                slot = PlayerWeaponSlot.Bow;
            else
                return;

            if (!_context.Weapons.Equip(slot))
                return;
            _context.ClearComboCarry();
            if (_machine.Current != _machine.Attack && _machine.Current != _machine.BowAttack)
                return;

            IPlayerState next = !_context.IsGrounded ? _machine.Fall
                : _context.IsCrouchHeld ? _machine.Crouch
                : _context.HasMoveInput ? _machine.Run : _machine.Idle;
            _machine.ForceChangeState(next);
        }

        /// <summary>
        /// Rolling ignores hits; a hit during the perfect-dodge window counts as a perfect dodge.
        /// </summary>
        private bool FilterDamage(DamageInfo info)
        {
            if (RunStats.IsChoosing)
                return false;

            if (_context.IsPerfectDodgeWindowOpen && _context.TryRegisterPerfectDodge())
                return false;
            return !_context.IsInvulnerable;
        }

        private void OnDamaged(DamageInfo info)
        {
            _context.Combat.LastDamageTaken = info;
            _machine.ForceChangeState(_machine.Hurt);
        }

        /// <summary>
        /// Lose control briefly without damage (e.g. a melee hit on a Shieldbearer's shield).
        /// Ignored while dead or invulnerable (rolling).
        /// </summary>
        public void Stagger(float duration, int direction)
        {
            if (!_health.IsAlive || _context.IsInvulnerable)
                return;
            _context.Combat.LastDamageTaken = new DamageInfo(0, direction, _config.HurtMinKnockback, duration, false, null);
            _machine.ForceChangeState(_machine.Hurt);
        }

        private void OnDied(DamageInfo info)
        {
            _context.Combat.LastDamageTaken = info;
            _machine.ForceChangeState(_machine.Dead);
            if (_autoRespawn)
                _respawnAt = Time.time + _respawnDelay;
        }

        private void Respawn()
        {
            _respawnAt = -1f;
            _body.position = _spawnPoint;
            _body.linearVelocity = Vector2.zero;
            _health.RestoreFull();
            _bow.ResetShots();
            _context.ClearComboCarry();
            _machine.ForceChangeState(_machine.Idle);
        }
    }
}
