namespace DeadCells.Player.StateMachine
{
    public interface IPlayerState
    {
        void Enter();
        void Exit();

        /// <summary>
        /// Called from Update: read input and decide transitions.
        /// </summary>
        void Tick();

        /// <summary>
        /// Called from FixedUpdate: apply physics.
        /// </summary>
        void FixedTick();
    }
}
