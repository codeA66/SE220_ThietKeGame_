namespace DeadCells.Core.StateMachine
{
    public interface IState
    {
        void Enter();
        void Exit();

        /// <summary>Called from Update: read input / sensors and decide transitions.</summary>
        void Tick();

        /// <summary>Called from FixedUpdate: apply physics.</summary>
        void FixedTick();
    }
}
