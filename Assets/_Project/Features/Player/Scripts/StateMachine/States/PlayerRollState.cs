using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    public class PlayerRollState : PlayerStateBase
    {
        private float _timer;
        private int _direction;

        public PlayerRollState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _timer = 0f;
            Ctx.FaceInput();
            _direction = Ctx.FacingDirection;
            Ctx.IsInvulnerable = true;
            Ctx.Animator.SetRolling(true);
        }

        public override void Exit()
        {
            Ctx.IsInvulnerable = false;
            Ctx.Animator.SetRolling(false);
        }

        public override void Tick()
        {
            _timer += Time.deltaTime;
            if (_timer >= Ctx.Config.RollDuration)
                ReturnToDefault();
        }

        public override void FixedTick()
        {
            Ctx.SetHorizontalVelocity(_direction * Ctx.Config.RollSpeed);
            Ctx.ClampFallSpeed();
        }
    }
}
