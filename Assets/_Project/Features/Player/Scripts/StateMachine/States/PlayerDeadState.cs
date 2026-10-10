namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// No control while dead. The controller decides when to respawn (temporary until the run loop exists).
    /// </summary>
    public class PlayerDeadState : PlayerStateBase
    {
        public PlayerDeadState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.SetHorizontalVelocity(0f);
            Ctx.Animator.SetDead(true);
        }

        public override void Exit() => Ctx.Animator.SetDead(false);

        public override void FixedTick()
        {
            Ctx.SetHorizontalVelocity(0f);
            Ctx.ClampFallSpeed();
        }
    }
}
