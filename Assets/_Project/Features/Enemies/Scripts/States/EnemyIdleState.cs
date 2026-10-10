using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>Stand still for a random time, then patrol. Detecting the player starts a chase.</summary>
    public class EnemyIdleState : EnemyStateBase
    {
        private float _timer;

        public EnemyIdleState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            var range = Ctx.Data.IdleTimeRange;
            _timer = Random.Range(range.x, range.y);
            Ctx.Animator.Play(EnemyAnimator.Idle);
        }

        public override void Tick()
        {
            if (TryDetectPlayer())
                return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                Machine.ChangeState(Machine.Patrol);
        }

        public override void FixedTick() => Ctx.SetHorizontalVelocity(0f);
    }
}
