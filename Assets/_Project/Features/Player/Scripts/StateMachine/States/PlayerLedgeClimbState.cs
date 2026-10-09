using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Mantles onto a ledge found by PlayerContext.TryFindLedge. The body is kinematic for the
    /// duration of the climb clip and moves smoothly from the grab point to PlayerContext.LedgeStandPoint.
    /// </summary>
    public class PlayerLedgeClimbState : PlayerStateBase
    {
        private const string ClipName = "Player_LedgeClimb";

        private float _duration;
        private float _timer;
        private Vector2 _start;
        private Vector2 _end;

        public PlayerLedgeClimbState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            if (_duration <= 0f)
                _duration = Mathf.Max(Ctx.Animator.GetClipLength(ClipName), 0.01f);
            _timer = 0f;
            _start = Ctx.Position;
            _end = Ctx.LedgeStandPoint;
            Ctx.SetKinematic(true);
            Ctx.Animator.SetClimbing(true);
        }

        public override void Exit()
        {
            Ctx.SetKinematic(false);
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.Animator.SetClimbing(false);
            Ctx.RefillAirJumps();
        }

        public override void Tick()
        {
            _timer += Time.deltaTime;
            if (_timer >= _duration)
                Machine.ChangeState(Ctx.HasMoveInput ? Machine.Run : Machine.Idle);
        }

        public override void FixedTick()
        {
            // Rise first, then step over the edge, so the path does not cut through the corner.
            float t = Mathf.Clamp01(_timer / _duration);
            float rise = Mathf.Clamp01(t / RisePortion);
            float over = Mathf.Clamp01((t - RisePortion) / (1f - RisePortion));
            Ctx.MovePosition(new Vector2(Mathf.Lerp(_start.x, _end.x, over), Mathf.Lerp(_start.y, _end.y, rise)));
        }

        private const float RisePortion = 0.6f;
    }
}
