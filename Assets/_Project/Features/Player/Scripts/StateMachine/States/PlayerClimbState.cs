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
        /// <summary>How far below the chain top the feet are placed on a top dismount (units).</summary>
        private const float TopDismountInset = 0.06f;
        /// <summary>Climbing within this distance of the chain top steps onto the deck, if Up is held.</summary>
        private const float TopDismountTrigger = 0.1f;

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

            // Top of the chain: step off onto the deck the chain serves instead of hanging on the
            // overhanging trigger (the trigger tops out above the plank surface). Snapping lands the
            // feet inside GroundCheckDistance of the deck, so the next frame is already grounded.
            if (Ctx.IsUpHeld && y >= bounds.max.y - TopDismountTrigger)
            {
                Ctx.SnapPosition(new Vector2(Ctx.Position.x, bounds.max.y - TopDismountInset));
                Machine.ChangeState(Ctx.HasMoveInput ? Machine.Run : Machine.Idle);
                return;
            }

            // Bottom: stepping off onto the ground. Top / bottom of a hanging chain: let go.
            if ((movingDown && Ctx.IsGrounded) || y > bounds.max.y || y < bounds.min.y - 0.01f)
            {
                ReturnToDefault();
                return;
            }

            if (Ctx.HasMoveInput)
            {
                int inputDir = Ctx.Input.MoveX > 0f ? 1 : -1;
                if (inputDir != Ctx.FacingDirection)
                {
                    Ctx.Face(inputDir);
                    Ctx.AlignToClimbTarget();
                }
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
