namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Shared plumbing and the common transition checks used by grounded and airborne states.
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
        /// Roll, attack, drop through, jump or fall from a grounded state. Returns true if a transition started.
        /// </summary>
        protected bool TryGroundedActions()
        {
            if (TryRoll() || TryAttack())
                return true;

            if (Ctx.TryDropThrough())
                return Machine.ChangeState(Machine.Fall);

            if (TryClimb())
                return true;

            if (Ctx.JumpBuffered && Ctx.WithinCoyoteTime && !Ctx.IsDownHeld)
                return Machine.ChangeState(Machine.Jump);

            if (!Ctx.IsGrounded)
                return Machine.ChangeState(Machine.Fall);

            return false;
        }

        /// <summary>
        /// Stomp, attack, roll, ledge climb or air jump while airborne. Returns true if a transition started.
        /// </summary>
        protected bool TryAirActions()
        {
            if (Ctx.IsDownHeld && Ctx.Input.JumpPressed)
                return Machine.ChangeState(Machine.Stomp);

            if (Ctx.Config.AllowAirAttack && TryAttack())
                return true;

            return TryRoll() || TryClimb() || TryLedgeClimb() || TryAirJump();
        }

        /// <summary>
        /// Grab a chain or vine the player overlaps while holding up. Returns true if a transition started.
        /// </summary>
        protected bool TryClimb() =>
            Ctx.IsUpHeld && Ctx.TryFindClimbable() && Machine.ChangeState(Machine.Climb);

        /// <summary>
        /// Crouch when holding down on the ground. Returns true if a transition started.
        /// </summary>
        protected bool TryCrouch() => Ctx.IsDownHeld && Ctx.IsGrounded && Machine.ChangeState(Machine.Crouch);

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

        protected bool TryRoll() => Ctx.Input.RollPressed && Ctx.CanRoll && Machine.ChangeState(Machine.Roll);

        /// <summary>
        /// Starts a swing, continuing a combo that a roll interrupted if it is still within the carry window.
        /// </summary>
        protected bool TryAttack()
        {
            if (!Ctx.Input.AttackPressed)
                return false;
            Ctx.PendingComboIndex = Ctx.TakeComboStartIndex();
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
