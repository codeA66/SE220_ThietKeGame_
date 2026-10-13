using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>Hold to draw, release a fully drawn shot; early release and interrupts spend no ammo.</summary>
    public class PlayerBowAttackState : PlayerStateBase
    {
        private BowWeaponData.Shot _shot;
        private float _timer;
        private float _recoveryDuration;
        private bool _released;
        private bool _crouching;

        public PlayerBowAttackState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
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

            _timer += Time.deltaTime;
            if (_released)
            {
                if (_timer >= _recoveryDuration)
                    ReturnToDefault();
                return;
            }

            Ctx.FaceInput();
            UpdateCrouching();
            Ctx.Animator.DrawBow(_crouching, Mathf.Clamp01(_timer / _shot.ChargeTime));
            if (!Ctx.Input.AttackReleased && Ctx.Input.AttackHeld)
                return;

            if (_timer < _shot.ChargeTime || !Ctx.Bow.TryFire(Ctx.Position, Ctx.FacingDirection, _crouching, _shot))
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

        private void UpdateCrouching()
        {
            _crouching = Ctx.IsGrounded && Ctx.IsDownHeld;
            Ctx.SetCrouchCollider(_crouching);
            Ctx.Animator.SetCrouching(_crouching);
        }
    }
}
