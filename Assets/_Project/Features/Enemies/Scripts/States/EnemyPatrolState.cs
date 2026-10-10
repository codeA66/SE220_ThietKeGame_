using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>Walk one way for a random time, turning at walls and ledges, then idle.</summary>
    public class EnemyPatrolState : EnemyStateBase
    {
        private float _timer;

        public EnemyPatrolState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            var range = Ctx.Data.PatrolTimeRange;
            _timer = Random.Range(range.x, range.y);
            int dir = Random.value < 0.5f ? -1 : 1;
            if (!Ctx.CanWalk(dir))
                dir = -dir;
            Ctx.Face(dir);
            Ctx.Animator.Play(EnemyAnimator.Walk);
        }

        public override void Tick()
        {
            if (TryDetectPlayer())
                return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f || (!Ctx.CanWalk(Ctx.Facing) && !Ctx.CanWalk(-Ctx.Facing)))
            {
                Machine.ChangeState(Machine.Idle);
                return;
            }
            if (Ctx.IsGrounded && !Ctx.CanWalk(Ctx.Facing))
                Ctx.Face(-Ctx.Facing);
        }

        public override void FixedTick() => Ctx.SetHorizontalVelocity(Ctx.Facing * Ctx.Data.PatrolSpeed);
    }
}
