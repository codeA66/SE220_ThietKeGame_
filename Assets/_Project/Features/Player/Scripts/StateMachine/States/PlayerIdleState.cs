namespace DeadCells.Player.StateMachine.States
{
    public class PlayerIdleState : PlayerStateBase
    {
        public PlayerIdleState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter() => Ctx.SetGravityScale(Ctx.Config.GravityScale);

        public override void Tick()
        {
            if (TryGroundedActions() || TryCrouch())
                return;
            if (Ctx.HasMoveInput)
                Machine.ChangeState(Machine.Run);
        }

        public override void FixedTick() => Ctx.SetHorizontalVelocity(0f);
    }
}
