using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Spawn intro: the body lies headless and ignores input and damage until PlayerSpawnHead reports
    /// that the head reached it, then plays the rebirth animation and hands control to Idle.
    /// </summary>
    public class PlayerSpawnState : PlayerStateBase
    {
        private bool _rising;
        private float _timer;

        public PlayerSpawnState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _rising = false;
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.SetHorizontalVelocity(0f);
            Ctx.IsInvulnerable = true;
            Ctx.Animator.SetSpawnPhase(1);
        }

        public override void Exit()
        {
            Ctx.Animator.SetSpawnPhase(0);
            Ctx.IsInvulnerable = false;
            Ctx.SpawnHeadArrived = false;
        }

        public override void Tick()
        {
            if (!_rising)
            {
                if (!Ctx.SpawnHeadArrived)
                    return;
                _rising = true;
                _timer = Ctx.Animator.RebirthLength;
                Ctx.Animator.SetSpawnPhase(2);
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                Machine.ChangeState(Machine.Idle);
        }

        public override void FixedTick()
        {
            Ctx.SetHorizontalVelocity(0f);
            Ctx.ClampFallSpeed();
        }
    }
}
