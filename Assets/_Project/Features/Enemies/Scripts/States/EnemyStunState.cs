using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Hit reaction: knocked back (reduced by knockback resistance), white flash, then back to chasing.
    /// Stun lasts the longer of the hit's own stun and EnemyData.HitStun.
    /// </summary>
    public class EnemyStunState : EnemyStateBase
    {
        private const float FlashTime = 0.08f;

        private float _timer;
        private float _duration;

        public EnemyStunState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            var hit = Ctx.LastDamage;
            _timer = 0f;
            _duration = Mathf.Max(Ctx.Data.HitStun, hit.Stun);
            float push = hit.Knockback * (1f - Ctx.Data.KnockbackResistance);
            if (hit.Direction != 0)
            {
                Ctx.Face(-hit.Direction);
                Ctx.SetHorizontalVelocity(hit.Direction * push);
            }
            Ctx.Animator.SetFlash(1f);
            Ctx.Animator.Play(hit.Stun > Ctx.Data.HitStun && Ctx.Animator.HasState(EnemyAnimator.Stun)
                ? EnemyAnimator.Stun : EnemyAnimator.Hit);
        }

        public override void Exit() => Ctx.Animator.SetFlash(0f);

        public override void Tick()
        {
            _timer += Time.deltaTime;
            if (_timer >= FlashTime)
                Ctx.Animator.SetFlash(0f);
            if (_timer >= _duration)
                Machine.ChangeState(Machine.Chase);
        }

        public override void FixedTick()
        {
            // Bleed off the knockback.
            Ctx.SetHorizontalVelocity(Mathf.MoveTowards(Ctx.Velocity.x, 0f, 30f * Time.fixedDeltaTime));
        }
    }
}
