namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Shared plumbing and the common transition checks used by grounded states.
    /// </summary>
    public abstract class PlayerStateBase : IPlayerState
    {
        protected readonly PlayerContext Ctx;
        protected readonly PlayerStateMachine Machine;

        protected PlayerStateBase(PlayerContext context, PlayerStateMachine machine)
        {
            Ctx = context;
            Machine = machine;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }

        /// <summary>
        /// Roll, attack, jump or fall from a grounded state. Returns true if a transition started.
        /// </summary>
        protected bool TryGroundedActions()
        {
            if (Ctx.Input.RollPressed)
                return Machine.ChangeState(Machine.Roll);

            if (Ctx.Input.AttackPressed)
            {
                Ctx.PendingComboIndex = 1;
                return Machine.ChangeState(Machine.Attack);
            }

            if (Ctx.JumpBuffered && Ctx.WithinCoyoteTime)
                return Machine.ChangeState(Machine.Jump);

            if (!Ctx.IsGrounded)
                return Machine.ChangeState(Machine.Fall);

            return false;
        }

        /// <summary>
        /// Idle or Run depending on input, or Fall when airborne.
        /// </summary>
        protected void ReturnToDefault()
        {
            if (!Ctx.IsGrounded)
                Machine.ChangeState(Machine.Fall);
            else
                Machine.ChangeState(Ctx.HasMoveInput ? Machine.Run : Machine.Idle);
        }

        /// <summary>
        /// Start the first combo hit while airborne, if allowed. Returns true if a transition started.
        /// </summary>
        protected bool TryAirAttack()
        {
            if (!Ctx.Config.AllowAirAttack || !Ctx.Input.AttackPressed)
                return false;
            Ctx.PendingComboIndex = 1;
            return Machine.ChangeState(Machine.Attack);
        }

        /// <summary>
        /// Mantle onto a ledge in front of the player. Returns true if a transition started.
        /// </summary>
        protected bool TryLedgeClimb()
        {
            if (!Ctx.TryFindLedge(out var standPoint))
                return false;
            Ctx.LedgeStandPoint = standPoint;
            return Machine.ChangeState(Machine.LedgeClimb);
        }

        /// <summary>
        /// Jump again while airborne if an air jump is available. Returns true if a transition started.
        /// </summary>
        protected bool TryAirJump() => Ctx.CanAirJump && Machine.ChangeState(Machine.AirJump);

        protected void ApplyAirControl()
        {
            Ctx.FaceInput();
            Ctx.SetHorizontalVelocity(Ctx.Input.MoveX * Ctx.Config.AirSpeed);
        }
    }
}
