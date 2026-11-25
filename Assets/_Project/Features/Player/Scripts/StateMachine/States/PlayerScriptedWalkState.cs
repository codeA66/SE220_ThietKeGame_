using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Non-interactive scripted transition/walk state: holds invulnerable lock during fades or walks
    /// smoothly along flat ground toward a target X coordinate once grounded.
    /// </summary>
    public sealed class PlayerScriptedWalkState : PlayerStateBase
    {
        private const float ArrivalTolerance = 0.08f;

        private bool _walkEnabled;
        private float _targetX;
        private float _speedMultiplier = 0.38f;

        public bool HasArrived { get; private set; }

        public PlayerScriptedWalkState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public void ConfigureHold()
        {
            _walkEnabled = false;
            HasArrived = false;
        }

        public void ConfigureWalk(float targetX, float speedMultiplier)
        {
            _walkEnabled = true;
            _targetX = targetX;
            _speedMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1.5f);
            HasArrived = false;
        }

        public override void Enter()
        {
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.SetHorizontalVelocity(0f);
            Ctx.IsInvulnerable = true;
        }

        public override void Exit()
        {
            Ctx.IsInvulnerable = false;
            _walkEnabled = false;
            HasArrived = false;
        }

        public override void Tick() { }

        public override void FixedTick()
        {
            if (!_walkEnabled || HasArrived)
            {
                Ctx.SetHorizontalVelocity(0f);
                Ctx.ClampFallSpeed();
                return;
            }

            if (!Ctx.IsGrounded)
            {
                Ctx.SetHorizontalVelocity(0f);
                Ctx.ClampFallSpeed();
                return;
            }

            float dx = _targetX - Ctx.Position.x;
            if (Mathf.Abs(dx) <= ArrivalTolerance)
            {
                Ctx.SetHorizontalVelocity(0f);
                HasArrived = true;
                return;
            }

            int dir = dx >= 0f ? 1 : -1;
            Ctx.Face(dir);
            Ctx.ApplyGroundedMove(dir * Ctx.Config.RunSpeed * _speedMultiplier);
        }
    }
}
