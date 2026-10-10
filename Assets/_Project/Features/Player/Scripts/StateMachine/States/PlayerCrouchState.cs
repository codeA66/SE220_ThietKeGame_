namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Crouch while holding down on the ground. Stationary: the game data has crouch / crouchIdle
    /// animations but no crouch-walk. Roll, attack and drop-through still work.
    /// </summary>
    public class PlayerCrouchState : PlayerStateBase
    {
        public PlayerCrouchState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.SetCrouchCollider(true);
            Ctx.Animator.SetCrouching(true);
        }

        public override void Exit()
        {
            Ctx.SetCrouchCollider(false);
            Ctx.Animator.SetCrouching(false);
        }

        public override void Tick()
        {
            if (TryGroundedActions())
                return;
            if (!Ctx.IsDownHeld)
                ReturnToDefault();
        }

        public override void FixedTick()
        {
            Ctx.FaceInput();
            Ctx.SetHorizontalVelocity(0f);
        }
    }
}
