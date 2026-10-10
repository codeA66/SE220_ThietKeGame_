using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Ground jump, modelled on Dead Cells: a base impulse plus an extra boost while jump is held
    /// during the first JumpHoldTime seconds (DC: HeroJump + HeroJumpExtra).
    /// </summary>
    public class PlayerJumpState : PlayerStateBase
    {
        private float _holdTimer;
        private bool _holding;

        public PlayerJumpState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _holdTimer = 0f;
            _holding = UsesHoldBoost;
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            ApplyJumpImpulse();
        }

        /// <summary>
        /// The upward impulse that starts this jump. Overridden by the air jump.
        /// </summary>
        protected virtual void ApplyJumpImpulse() => Ctx.StartJump();

        /// <summary>
        /// Whether holding jump adds the extra boost. The air jump is a single fixed impulse.
        /// </summary>
        protected virtual bool UsesHoldBoost => true;

        public override void Tick()
        {
            if (TryAirActions())
                return;

            if (_holding && !Ctx.Input.JumpHeld)
                _holding = false;

            if (Ctx.Velocity.y <= 0f)
                Machine.ChangeState(Machine.Fall);
        }

        public override void FixedTick()
        {
            if (_holding && _holdTimer < Ctx.Config.JumpHoldTime)
            {
                float step = Mathf.Min(Time.fixedDeltaTime, Ctx.Config.JumpHoldTime - _holdTimer);
                Ctx.AddVerticalVelocity(Ctx.Config.JumpHoldBoost * step / Ctx.Config.JumpHoldTime);
                _holdTimer += step;
            }

            ApplyAirControl();
        }
    }
}
