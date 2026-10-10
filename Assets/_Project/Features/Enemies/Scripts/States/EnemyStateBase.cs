using DeadCells.Core.StateMachine;

namespace DeadCells.Enemies.States
{
    public abstract class EnemyStateBase : IState
    {
        protected readonly EnemyContext Ctx;
        protected readonly EnemyStateMachine Machine;

        protected EnemyStateBase(EnemyContext context, EnemyStateMachine machine)
        {
            Ctx = context;
            Machine = machine;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }

        /// <summary>Switch to Chase when the player is detected. Returns true if a transition started.</summary>
        protected bool TryDetectPlayer() =>
            Ctx.CanSeePlayer(Ctx.Data.DetectRange) && Machine.ChangeState(Machine.Chase);
    }
}
