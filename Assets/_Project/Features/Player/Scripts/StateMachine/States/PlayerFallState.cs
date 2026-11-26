namespace DeadCells.Player.StateMachine.States
{
    public class PlayerFallState : PlayerStateBase
    {
        /// <summary>How far below the chain top a falling player may still catch it with Down (units).</summary>
        private const float DescendGrabMargin = 0.25f;

        public PlayerFallState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter() =>
            Ctx.SetGravityScale(Ctx.Config.GravityScale * Ctx.Config.FallGravityMultiplier);

        public override void Exit() => Ctx.SetGravityScale(Ctx.Config.GravityScale);

        public override void Tick()
        {
            Ctx.TryStandUpCollider();

            // Coyote jump: walked off a ledge but jump pressed shortly after.
            if (Ctx.JumpBuffered && Ctx.WithinCoyoteTime && !Ctx.IsDownHeld)
            {
                Machine.ChangeState(Machine.Jump);
                return;
            }

            if (TryAirActions())
                return;

            // Holding Down onto a chain below: grip it instead of sliding past. Never during a
            // deliberate drop-through — that one must stay a drop.
            if (Ctx.IsDownHeld && !Ctx.IsDroppingThrough && Ctx.TryFindClimbable()
                && Ctx.Position.y < Ctx.ClimbTarget.bounds.max.y - DescendGrabMargin)
            {
                Machine.ChangeState(Machine.Climb);
                return;
            }

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
