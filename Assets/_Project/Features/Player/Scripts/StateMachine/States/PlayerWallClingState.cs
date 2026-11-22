using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Wall-cling and wall-slide on solid walls, with wall-jump kick-off and ledge-mantle handoff.
    /// </summary>
    public class PlayerWallClingState : PlayerStateBase
    {
        public PlayerWallClingState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            Ctx.Face(Ctx.WallDirection);
            Ctx.SetHorizontalVelocity(0f);
            if (Ctx.Velocity.y < -Ctx.Config.WallSlideMaxSpeed)
                Ctx.SetVerticalVelocity(-Ctx.Config.WallSlideMaxSpeed);
            Ctx.Animator.SetWallClinging(true);
        }

        public override void Exit()
        {
            Ctx.Animator.SetWallClinging(false);
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
        }

        public override void Tick()
        {
            if (Ctx.IsGrounded)
            {
                ReturnToDefault();
                return;
            }

            if (Ctx.Input.JumpPressed && !Ctx.IsDownHeld && Ctx.WallJumpsRemaining > 0)
            {
                Ctx.StartWallJump(-Ctx.WallDirection);
                Machine.ChangeState(Machine.Jump);
                return;
            }

            if (TryRoll() || TryLedgeClimb())
                return;

            if (Ctx.IsDownHeld || !Ctx.TryFindWall(out int dir) || dir != Ctx.WallDirection)
            {
                Machine.ChangeState(Machine.Fall);
                return;
            }
        }

        public override void FixedTick()
        {
            Ctx.SetHorizontalVelocity(0f);
            if (Ctx.Velocity.y < -Ctx.Config.WallSlideMaxSpeed)
                Ctx.SetVerticalVelocity(-Ctx.Config.WallSlideMaxSpeed);
        }
    }
}
