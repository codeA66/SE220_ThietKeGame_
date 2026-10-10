using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Dodge roll on the ground or in the air (DC: DodgeDuration, DodgeCD, mid-air roll).
    /// Speed eases out from RollStartSpeed to RollEndSpeed. The player is invulnerable and passes
    /// through enemies. Jump or attack cancel the roll after RollCancelNormalizedTime.
    /// </summary>
    public class PlayerRollState : PlayerStateBase
    {
        private const string ClipName = "Player_Roll";

        private float _clipLength = -1f;
        private float _timer;
        private int _direction;
        private bool _airborne;

        public PlayerRollState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            if (_clipLength < 0f)
                _clipLength = Ctx.Animator.GetClipLength(ClipName);

            _timer = 0f;
            _airborne = !Ctx.IsGrounded;
            Ctx.FaceInput();
            _direction = Ctx.FacingDirection;
            Ctx.BeginRoll();

            if (_airborne && !Ctx.Config.AirRollKeepsGravity)
            {
                Ctx.SetGravityScale(0f);
                Ctx.SetVerticalVelocity(0f);
            }

            // Play the whole clip within the roll duration, whatever the duration is tuned to.
            Ctx.Animator.SetRollAnimSpeed(_clipLength > 0f ? _clipLength / Ctx.Config.RollDuration : 1f);
            Ctx.Animator.SetRolling(true);
        }

        public override void Exit()
        {
            Ctx.EndRoll();
            Ctx.OpenComboCarryWindow();
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.Animator.SetRolling(false);
        }

        public override void Tick()
        {
            _timer += Time.deltaTime;
            float normalized = _timer / Ctx.Config.RollDuration;

            if (normalized >= Ctx.Config.RollCancelNormalizedTime && TryCancel())
                return;

            if (normalized >= 1f)
                ReturnToDefault();
        }

        public override void FixedTick()
        {
            float t = Mathf.Clamp01(_timer / Ctx.Config.RollDuration);
            float easeOut = 1f - (1f - t) * (1f - t);
            Ctx.SetHorizontalVelocity(_direction * Mathf.Lerp(Ctx.Config.RollStartSpeed, Ctx.Config.RollEndSpeed, easeOut));

            if (_airborne && !Ctx.Config.AirRollKeepsGravity)
                Ctx.SetVerticalVelocity(0f);
            else
                Ctx.ClampFallSpeed();
        }

        private bool TryCancel()
        {
            if (TryAttack())
                return true;

            if (!Ctx.Input.JumpPressed)
                return false;

            if (Ctx.IsGrounded || Ctx.WithinCoyoteTime)
                return Machine.ChangeState(Machine.Jump);

            return TryAirJump();
        }
    }
}
