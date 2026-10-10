namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Mid-air jump: one fixed impulse (DC: HeroDoubleJump, about a full held jump), spends one air jump
    /// and restarts the jump animation. Air attack, roll and ledge climb still work as in a ground jump.
    /// </summary>
    public class PlayerAirJumpState : PlayerJumpState
    {
        public PlayerAirJumpState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        protected override bool UsesHoldBoost => false;

        protected override void ApplyJumpImpulse()
        {
            Ctx.StartAirJump();
            Ctx.Animator.TriggerAirJump();
        }
    }
}
