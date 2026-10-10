namespace DeadCells.Player.StateMachine.States
{
    public class PlayerRunState : PlayerStateBase
    {
        public PlayerRunState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter() => Ctx.SetGravityScale(Ctx.Config.GravityScale);

        public override void Tick()
        {
            if (TryGroundedActions() || TryCrouch())
                return;
            if (!Ctx.HasMoveInput)
                Machine.ChangeState(Machine.Idle);
        }

        public override void FixedTick()
        {
            Ctx.FaceInput();
            Ctx.SetHorizontalVelocity(Ctx.Input.MoveX * Ctx.Config.RunSpeed);
        }
    }
}
