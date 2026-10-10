namespace DeadCells.Player.StateMachine.States
{
    public class PlayerFallState : PlayerStateBase
    {
        public PlayerFallState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter() =>
            Ctx.SetGravityScale(Ctx.Config.GravityScale * Ctx.Config.FallGravityMultiplier);

        public override void Exit() => Ctx.SetGravityScale(Ctx.Config.GravityScale);

        public override void Tick()
        {
            // Coyote jump: walked off a ledge but jump pressed shortly after.
            if (Ctx.JumpBuffered && Ctx.WithinCoyoteTime && !Ctx.IsDownHeld)
            {
                Machine.ChangeState(Machine.Jump);
                return;
            }

            if (TryAirActions())
                return;

            if (Ctx.IsGrounded && Ctx.Velocity.y <= 0f)
                Machine.ChangeState(Machine.Land);
        }

        public override void FixedTick()
        {
            ApplyAirControl();
            Ctx.ClampFallSpeed();
        }
    }
}
