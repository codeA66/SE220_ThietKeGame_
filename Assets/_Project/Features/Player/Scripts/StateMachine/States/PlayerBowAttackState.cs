using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// One press draws for the shot's ChargeTime and fires automatically. Jumping (and air jumps) keep the
    /// draw going; a press during recovery queues the next shot. Rolling cancels and spends no ammo.
    /// </summary>
    public class PlayerBowAttackState : PlayerStateBase
    {
        private BowWeaponData.Shot _shot;
        private float _timer;
        private float _recoveryDuration;
        private bool _released;
        private bool _crouching;
        private bool _queued;
        private bool _wasGrounded;

        public PlayerBowAttackState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _queued = false;
            _wasGrounded = Ctx.IsGrounded;
            BeginDraw();
        }

        private void BeginDraw()
        {
            _shot = Ctx.Bow.GetNextShot();
            _timer = 0f;
            _released = false;
            Ctx.FaceInput();
            UpdateCrouching();
            Ctx.Animator.DrawBow(_crouching, 0f);
        }

        public override void Exit()
        {
            Ctx.Animator.HideBow();
            Ctx.Animator.SetCrouching(false);
            Ctx.SetCrouchCollider(false);
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
        }

        public override void Tick()
        {
            if (Ctx.Input.RollPressed && Ctx.CanRoll)
            {
                Machine.ChangeState(Machine.Roll);
                return;
            }

            TickJumpAndLanding();

            _timer += Time.deltaTime;
            if (_released)
            {
                if (Ctx.Input.AttackPressed)
                    _queued = true;
                if (_timer < _recoveryDuration)
                    return;
                if (_queued && Ctx.Bow.CanShoot)
                {
                    _queued = false;
                    BeginDraw();
                    return;
                }
                ReturnToDefault();
                return;
            }

            Ctx.FaceInput();
            UpdateCrouching();
            Ctx.Animator.DrawBow(_crouching, Mathf.Clamp01(_timer / _shot.ChargeTime));
            if (_timer < _shot.ChargeTime)
                return;

            if (!Ctx.Bow.TryFire(Ctx.Position, Ctx.FacingDirection, _crouching, _shot))
            {
                ReturnToDefault();
                return;
            }

            _released = true;
            _timer = 0f;
            float releaseDuration = Ctx.Animator.ReleaseBow(_crouching, _shot.AnimationSpeed);
            _recoveryDuration = Mathf.Max(_shot.RecoveryTime, releaseDuration);
        }

        public override void FixedTick()
        {
            if (!_released)
                Ctx.FaceInput();
            float speed = Ctx.IsGrounded ? Ctx.Config.RunSpeed : Ctx.Config.AirSpeed;
            Ctx.SetHorizontalVelocity(_crouching ? 0f : Ctx.Input.MoveX * speed * Ctx.Config.AttackMoveSpeedMultiplier);
            bool falling = Ctx.Velocity.y < 0f && !Ctx.IsGrounded;
            Ctx.SetGravityScale(Ctx.Config.GravityScale * (falling ? Ctx.Config.FallGravityMultiplier : 1f));
            Ctx.ClampFallSpeed();
        }

        /// <summary>Ground jump (with coyote time) or air jump without leaving the bow state.</summary>
        private void TickJumpAndLanding()
        {
            if (Ctx.IsGrounded && !_wasGrounded)
                Ctx.NotifyLanded();
            _wasGrounded = Ctx.IsGrounded;

            if (Ctx.JumpBuffered && Ctx.WithinCoyoteTime && !Ctx.IsDownHeld)
                Ctx.StartJump();
            else if (Ctx.CanAirJump)
                Ctx.StartAirJump();
        }

        private void UpdateCrouching()
        {
            _crouching = Ctx.IsGrounded && Ctx.IsDownHeld;
            Ctx.SetCrouchCollider(_crouching);
            Ctx.Animator.SetCrouching(_crouching);
        }
    }
}
