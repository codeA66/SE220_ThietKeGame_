using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    public class PlayerLandState : PlayerStateBase
    {
        private const string ClipName = "Player_Land";

        private float _timer;
        private float _duration;

        public PlayerLandState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _timer = 0f;
            _duration = Ctx.Animator.GetClipLength(ClipName);
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.NotifyLanded();
        }

        public override void Tick()
        {
            if (TryGroundedActions() || TryCrouch())
                return;

            _timer += Time.deltaTime;
            if (Ctx.HasMoveInput)
                Machine.ChangeState(Machine.Run);
            else if (_timer >= _duration)
                Machine.ChangeState(Machine.Idle);
        }

        public override void FixedTick() => Ctx.SetHorizontalVelocity(0f);
    }
}
