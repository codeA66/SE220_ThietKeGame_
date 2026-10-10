using System;
using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Owns an enemy's FSM and routes Health events into Stun / Dead. Behaviour lives in the states.
    /// Scene references (player, pools) come from Initialize, or from the serialized fallbacks.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyData _data;
        [SerializeField] private EnemyAnimator _animator;
        [SerializeField] private Health _health;
        [SerializeField] private LayerMask _groundMask;
        [Tooltip("Layers this enemy's attacks can hit (the player).")]
        [SerializeField] private LayerMask _attackTargets;

        [Header("Scene references (optional if a spawner calls Initialize)")]
        [SerializeField] private Transform _player;
        [SerializeField] private ProjectileSpawner _projectiles;
        [SerializeField] private VfxSpawner _vfx;

        private EnemyContext _context;
        private EnemyStateMachine _machine;

        public event Action<EnemyController> Died;

        public EnemyData Data => _data;
        public bool IsDead => _machine != null && _machine.Current == _machine.Dead;
        public string CurrentStateName => _machine?.Current?.GetType().Name;

        private void Awake()
        {
            _health.Initialize(_data.MaxHealth, 0f);
            _context = new EnemyContext(GetComponent<Rigidbody2D>(), GetComponent<BoxCollider2D>(), _animator, _health,
                _data, _groundMask, _attackTargets);
            _machine = new EnemyStateMachine(_context);
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            Initialize(_player, _projectiles, _vfx);
        }

        private void OnDestroy()
        {
            if (_health == null)
                return;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        public void Initialize(Transform player, ProjectileSpawner projectiles, VfxSpawner vfx)
        {
            _context.Player = player;
            _context.Projectiles = projectiles;
            _context.Vfx = vfx;
        }

        private void Start() => _machine.ChangeState(_machine.Idle);

        private void Update()
        {
            _context.RefreshFrame();
            _machine.Tick();
        }

        private void FixedUpdate() => _machine.FixedTick();

        private void OnDamaged(DamageInfo info)
        {
            _context.LastDamage = info;
            _machine.ForceChangeState(_machine.Stun);
        }

        private void OnDied(DamageInfo info)
        {
            _context.LastDamage = info;
            _machine.ForceChangeState(_machine.Dead);
            Died?.Invoke(this);
        }
    }
}
