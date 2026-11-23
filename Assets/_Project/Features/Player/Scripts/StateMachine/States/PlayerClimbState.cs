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
        /// <summary>How far below the chain top the feet are probed for the deck (units).</summary>
        private const float TopDismountInset = 0.06f;
        /// <summary>Climbing within this distance of the chain top steps onto the deck, if Up is held.</summary>
        private const float TopDismountTrigger = 0.1f;
        /// <summary>
        /// Clearance left between the feet and the deck found under them on a top dismount (units).
        /// The dismount is a short drop from just above the surface, not a placement onto it, so the
        /// fall path resolves a real contact and lands instead of leaving the feet probe as the only
        /// thing holding the body up.
        /// </summary>
        private const float TopDismountReleaseGap = 0.02f;

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
            // overhanging trigger. The feet go to the surface the probe finds, never to a fixed offset
            // below the trigger top: a dismount must not put the body down where it is not touching
            // anything (a trigger that overhangs its deck by more than the grab margin would otherwise
            // leave it hanging in mid air).
            if (Ctx.IsUpHeld && y >= bounds.max.y - TopDismountTrigger)
            {
                var dismountFeet = new Vector2(Ctx.Position.x, bounds.max.y - TopDismountInset);
                // The chain is latched off in both cases: the next Up grab must not take it straight
                // back and restart the climb every frame.
                Ctx.LatchDismountedClimbable(target);

                if (Ctx.TryGetFootingAt(dismountFeet, out float surfaceY))
                {
                    // Land on the deck that was actually found, a release gap above it, and hand the
                    // player to Fall: the drop resolves a real contact and PlayerFallState enters Land,
                    // which reports a genuine landing. Handing a velocity-zeroed body straight to Idle
                    // made the feet probe the only thing claiming the player was on the deck, and a
                    // body that is never really supported slides through a one-way collider.
                    Ctx.SnapPosition(new Vector2(dismountFeet.x, surfaceY + TopDismountReleaseGap));
                    Machine.ChangeState(Machine.Fall);
                }
                else
                {
                    // No floor under the trigger top: drop the player off in mid air, exactly as before.
                    Ctx.SnapPosition(dismountFeet);
                    Machine.ChangeState(Ctx.HasMoveInput ? Machine.Run : Machine.Idle);
                }

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
