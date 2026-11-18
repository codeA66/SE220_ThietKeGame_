using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Pursue the player without walking off ledges. Each frame, in priority order: back off when the player
    /// is too close (teleporting behind them instead when cornered), then use the first usable skill, then move.
    /// Turning around takes Data.TurnDelay. Gives up after losing the player for a while.
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
            _moveDir = 0f;
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

            int toPlayer = Ctx.DirectionToPlayer;
            bool facingPlayer = Ctx.TurnToward(toPlayer);
            float dist = Ctx.DistanceToPlayer;

            if (visible && Ctx.IsGrounded && TryKeepDistance(dist, toPlayer))
                return;

            if (visible && Ctx.IsGrounded && facingPlayer)
            {
                int skill = Ctx.PickSkill(dist);
                if (skill >= 0)
                {
                    Ctx.CurrentSkill = skill;
                    Machine.ChangeState(Machine.Skills[skill]);
                    return;
                }
            }

            // Walk up to the closest skill's reach, or stand still while turning / out of usable skills.
            _moveDir = facingPlayer && dist > NearestSkillRange() * 0.8f && Ctx.CanWalk(toPlayer) ? toPlayer : 0f;
            Ctx.Animator.Play(_moveDir == 0f ? EnemyAnimator.Idle
                : Ctx.Animator.HasState(EnemyAnimator.Run) ? EnemyAnimator.Run : EnemyAnimator.Walk, 1f, restart: false);
        }

        public override void FixedTick() => Ctx.SetHorizontalVelocity(_moveDir * Ctx.Data.ChaseSpeed);

        /// <summary>Backstep away, or teleport behind the player when there is no room behind.</summary>
        private bool TryKeepDistance(float dist, int toPlayer)
        {
            var d = Ctx.Data;
            if (d.BackstepTrigger <= 0f || dist >= d.BackstepTrigger)
                return false;

            bool cornered = !Ctx.CanWalk(-toPlayer);
            if (cornered)
                return d.CanTeleport && Time.time >= Ctx.NextTeleportTime && Machine.ChangeState(Machine.Teleport);
            return Time.time >= Ctx.NextBackstepTime && Machine.ChangeState(Machine.Backstep);
        }

        private float NearestSkillRange()
        {
            float best = float.PositiveInfinity;
            foreach (var s in Ctx.Data.Skills)
                best = Mathf.Min(best, s.MaxRange);
            return float.IsPositiveInfinity(best) ? 1f : best;
        }
    }
}
