using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Climbing a chain or vine (Dead Cells wiki: "Up or Down: While on a chain or vine, climb up or down").
    /// Gravity is off and the player is centred on the climbable. Jump leaves the chain; reaching
    /// either end lets go.
    /// </summary>
    public class PlayerClimbState : PlayerStateBase
    {
        public PlayerClimbState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            Ctx.BeginClimb();
            Ctx.Animator.SetClimbingChain(true);
        }

        public override void Exit()
        {
            Ctx.EndClimb();
            Ctx.Animator.SetClimbingChain(false);
            Ctx.Animator.SetClimbAnimSpeed(1f);
        }

        public override void Tick()
        {
            var target = Ctx.ClimbTarget;
            if (target == null || !target.enabled)
            {
                ReturnToDefault();
                return;
            }

            if (Ctx.Input.JumpPressed)
            {
                Machine.ChangeState(Machine.Jump);
                return;
            }

            float y = Ctx.Position.y;
            var bounds = target.bounds;
            bool movingDown = Ctx.Input.MoveY < 0f;

            // Bottom: stepping off onto the ground. Top / bottom of a hanging chain: let go.
            if ((movingDown && Ctx.IsGrounded) || y > bounds.max.y || y < bounds.min.y - 0.01f)
            {
                ReturnToDefault();
                return;
            }

            Ctx.Animator.SetClimbAnimSpeed(Mathf.Abs(Ctx.Input.MoveY) > 0.01f ? 1f : 0f);
        }

        public override void FixedTick()
        {
            Ctx.SetHorizontalVelocity(0f);
            Ctx.SetVerticalVelocity(Ctx.Input.MoveY * Ctx.Config.ClimbSpeed);
        }
    }
}
