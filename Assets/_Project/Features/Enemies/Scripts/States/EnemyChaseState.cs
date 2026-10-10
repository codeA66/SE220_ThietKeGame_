using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Move toward the player (or away, for ranged enemies that keep their distance) without walking off
    /// ledges. Attacks when in range and the cooldown is over; gives up when the player is lost.
    /// </summary>
    public class EnemyChaseState : EnemyStateBase
    {
        private const float LoseSightGrace = 2f;

        private float _lostTimer;
        private float _moveDir;

        public EnemyChaseState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _lostTimer = 0f;
            Ctx.Animator.Play(Ctx.Animator.HasState(EnemyAnimator.Run) ? EnemyAnimator.Run : EnemyAnimator.Walk);
        }

        public override void Tick()
        {
            bool visible = Ctx.CanSeePlayer(Ctx.Data.LoseRange);
            _lostTimer = visible ? 0f : _lostTimer + Time.deltaTime;
            if (_lostTimer >= LoseSightGrace)
            {
                Machine.ChangeState(Machine.Idle);
                return;
            }

            float dist = Ctx.DistanceToPlayer;
            if (visible && Ctx.IsGrounded && Ctx.AttackReady && dist <= Ctx.Data.AttackRange)
            {
                Machine.ChangeState(Machine.Attack);
                return;
            }

            int toPlayer = Ctx.DirectionToPlayer;
            Ctx.Face(toPlayer);

            float keepAway = Ctx.Data.KeepAway;
            if (keepAway > 0f && dist < keepAway)
                _moveDir = -toPlayer;
            else if (dist > Ctx.Data.AttackRange * 0.8f)
                _moveDir = toPlayer;
            else
                _moveDir = 0f;

            if (_moveDir != 0f && !Ctx.CanWalk((int)_moveDir))
                _moveDir = 0f;

            Ctx.Animator.Play(_moveDir == 0f ? EnemyAnimator.Idle
                : Ctx.Animator.HasState(EnemyAnimator.Run) ? EnemyAnimator.Run : EnemyAnimator.Walk, 1f, restart: false);
        }

        public override void FixedTick() => Ctx.SetHorizontalVelocity(_moveDir * Ctx.Data.ChaseSpeed);
    }
}
