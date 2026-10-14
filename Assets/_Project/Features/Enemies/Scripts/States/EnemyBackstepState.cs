using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Quick hop away from a player who got too close (Wiki: Shieldbearer "will briefly back step",
    /// Archer "will do a backstep before firing", Grenadier "can backstep to avoid the player when close").
    /// Keeps facing the player and stops at ledges and walls.
    /// </summary>
    public class EnemyBackstepState : EnemyStateBase
    {
        private static readonly int BackstepAnim = Animator.StringToHash("Backstep");
        private const float HopSpeed = 3f;

        private float _timer;
        private int _awayDir;

        public EnemyBackstepState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _timer = 0f;
            Ctx.Face(Ctx.DirectionToPlayer);
            _awayDir = -Ctx.Facing;
            Ctx.SetVelocity(new Vector2(_awayDir * Speed, HopSpeed));
            Ctx.Animator.Play(Ctx.Animator.HasState(BackstepAnim) ? BackstepAnim : EnemyAnimator.Walk);
        }

        public override void Exit() => Ctx.NextBackstepTime = Time.time + Ctx.Data.BackstepCooldown;

        public override void Tick()
        {
            _timer += Time.deltaTime;
            if (_timer >= Ctx.Data.BackstepDuration && Ctx.IsGrounded)
                Machine.ChangeState(Machine.Chase);
        }

        public override void FixedTick()
        {
            bool moving = _timer < Ctx.Data.BackstepDuration && Ctx.CanWalk(_awayDir);
            Ctx.SetHorizontalVelocity(moving ? _awayDir * Speed : 0f);
        }

        private float Speed => Ctx.Data.BackstepDistance / Mathf.Max(Ctx.Data.BackstepDuration, 0.05f);
    }
}
