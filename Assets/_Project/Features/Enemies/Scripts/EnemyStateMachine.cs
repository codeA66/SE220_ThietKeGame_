using DeadCells.Core.StateMachine;
using DeadCells.Enemies.States;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Owns one instance of every enemy state. The attack state depends on EnemyData.AttackKind.
    /// </summary>
    public class EnemyStateMachine : StateMachine
    {
        public EnemyStateMachine(EnemyContext ctx)
        {
            Idle = new EnemyIdleState(ctx, this);
            Patrol = new EnemyPatrolState(ctx, this);
            Chase = new EnemyChaseState(ctx, this);
            Stun = new EnemyStunState(ctx, this);
            Dead = new EnemyDeadState(ctx, this);
            Attack = ctx.Data.AttackKind switch
            {
                EnemyAttackKind.Lunge => new EnemyLungeAttackState(ctx, this),
                EnemyAttackKind.Dash => new EnemyDashAttackState(ctx, this),
                EnemyAttackKind.Shoot => new EnemyShootAttackState(ctx, this),
                EnemyAttackKind.Grenade => new EnemyGrenadeAttackState(ctx, this),
                _ => new EnemyMeleeAttackState(ctx, this),
            };
        }

        public IState Idle { get; }
        public IState Patrol { get; }
        public IState Chase { get; }
        public IState Attack { get; }
        public IState Stun { get; }
        public IState Dead { get; }
    }
}
