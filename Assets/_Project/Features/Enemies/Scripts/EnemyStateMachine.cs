using DeadCells.Core.StateMachine;
using DeadCells.Enemies.States;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Owns one instance of every enemy state, plus one attack state per entry in EnemyData.Skills.
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
            Backstep = new EnemyBackstepState(ctx, this);
            Teleport = new EnemyTeleportState(ctx, this);

            var skills = ctx.Data.Skills;
            Skills = new IState[skills.Length];
            for (int i = 0; i < skills.Length; i++)
            {
                Skills[i] = skills[i].Kind switch
                {
                    EnemyAttackKind.Lunge => new EnemyLungeAttackState(ctx, this, i),
                    EnemyAttackKind.Dash => new EnemyDashAttackState(ctx, this, i),
                    EnemyAttackKind.Shoot => new EnemyShootAttackState(ctx, this, i),
                    EnemyAttackKind.Grenade => new EnemyGrenadeAttackState(ctx, this, i),
                    _ => new EnemyMeleeAttackState(ctx, this, i),
                };
            }
        }

        public IState Idle { get; }
        public IState Patrol { get; }
        public IState Chase { get; }
        public IState Stun { get; }
        public IState Dead { get; }
        public IState Backstep { get; }
        public IState Teleport { get; }
        public IState[] Skills { get; }
    }
}
