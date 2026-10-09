namespace DeadCells.Player.StateMachine.States
{
    public class PlayerJumpState : PlayerStateBase
    {
        private bool _jumpCut;

        public PlayerJumpState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _jumpCut = false;
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            ApplyJumpImpulse();
        }

        /// <summary>
        /// The upward impulse that starts this jump. Overridden by the air jump.
        /// </summary>
        protected virtual void ApplyJumpImpulse() => Ctx.StartJump();

        public override void Tick()
        {
            if (TryAirAttack() || TryLedgeClimb() || TryAirJump())
                return;

            // Variable jump height: releasing early cuts the remaining upward speed once.
            if (!_jumpCut && !Ctx.Input.JumpHeld && Ctx.Velocity.y > 0f)
            {
                _jumpCut = true;
                Ctx.SetVerticalVelocity(Ctx.Velocity.y * Ctx.Config.JumpCutMultiplier);
            }

            if (Ctx.Velocity.y <= 0f)
                Machine.ChangeState(Machine.Fall);
        }

        public override void FixedTick() => ApplyAirControl();
    }
}
