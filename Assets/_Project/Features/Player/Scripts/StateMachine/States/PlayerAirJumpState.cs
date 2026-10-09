namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Mid-air jump. Same control as a ground jump (variable height, air attack, ledge climb)
    /// but uses AirJumpVelocity, spends one air jump and restarts the jump animation.
    /// </summary>
    public class PlayerAirJumpState : PlayerJumpState
    {
        public PlayerAirJumpState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        protected override void ApplyJumpImpulse()
        {
            Ctx.StartAirJump();
            Ctx.Animator.TriggerAirJump();
        }
    }
}
