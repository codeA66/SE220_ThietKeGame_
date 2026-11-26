using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Crouch while holding down on the ground. Stationary: the game data has crouch / crouchIdle
    /// animations but no crouch-walk. Roll, attack and drop-through still work, and holding down on
    /// a one-way deck slot above a chain descends that chain — but only after
    /// <see cref="DescendHoldTime"/>, so a quick Down + Jump still reads as a drop-through
    /// (PilotPlayTests.P01 presses S then Space on the same breath and must fall through).
    /// </summary>
    public class PlayerCrouchState : PlayerStateBase
    {
        /// <summary>Down-hold time before a crouch turns into a chain descent (seconds).</summary>
        private const float DescendHoldTime = 0.18f;

        private float _holdTimer;

        public PlayerCrouchState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _holdTimer = 0f;
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.SetCrouchCollider(true);
            Ctx.Animator.SetCrouching(true);
        }

        public override void Exit()
        {
            Ctx.TryStandUpCollider();
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.Animator.SetCrouching(false);
        }

        public override void Tick() => Tick(Time.deltaTime);

        /// <summary>
        /// Frame-driven tick, also callable with an explicit delta so the hold window is deterministic
        /// in EditMode tests.
        /// </summary>
        public void Tick(float deltaTime)
        {
            bool canStand = Ctx.CanStandUp();
            if (canStand)
            {
                if (TryGroundedActions())
                    return;
            }
            else
            {
                if (TryRoll() || (Ctx.Weapons != null && Ctx.Weapons.Equipped == PlayerWeaponSlot.Bow && TryAttack()))
                    return;

                if (Ctx.TryDropThrough())
                {
                    Machine.ChangeState(Machine.Fall);
                    return;
                }

                if (!Ctx.IsGrounded)
                {
                    Machine.ChangeState(Machine.Fall);
                    return;
                }

                // Grounded under a low ceiling: discard blocked jump buffer so it cannot fire once clear.
                Ctx.ClearJumpBuffer();
            }

            _holdTimer += deltaTime;
            if (_holdTimer >= DescendHoldTime && Ctx.IsDownHeld && !Ctx.Input.JumpHeld
                && Ctx.TryFindClimbableBelowDeck(out var chain))
            {
                Ctx.BeginDescendClimb(chain);
                Machine.ChangeState(Machine.Climb);
                return;
            }

            if (canStand && !Ctx.IsCrouchHeld)
                ReturnToDefault();
        }

        public override void FixedTick()
        {
            Ctx.FaceInput();
            Ctx.ApplyGroundedMove(0f);
        }
    }
}
