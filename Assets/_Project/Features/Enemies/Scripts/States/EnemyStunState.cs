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
        /// <summary>Horizontal recoil bleed (u/s²). 18 keeps the slide readable after the hit-stop; 30 stopped it dead.</summary>
        private const float RecoilDeceleration = 18f;
        /// <summary>Minimum grounded push (u/s) that earns an upward pop — light taps stay flat.</summary>
        private const float PopPushThreshold = 3.5f;
        private const float PopSpeed = 2.2f;
        private const float CritPopSpeed = 3.8f;

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
                if (Ctx.IsGrounded && push >= PopPushThreshold)
                {
                    // The pop is a floor on the vertical arc: a body already rising keeps its momentum.
                    float popY = Mathf.Max(Ctx.Velocity.y, hit.IsCritical ? CritPopSpeed : PopSpeed);
                    Ctx.SetVelocity(new Vector2(hit.Direction * push, popY));
                }
                else
                {
                    Ctx.SetHorizontalVelocity(hit.Direction * push);
                }
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
            // Bleed off the knockback. Slower than a dead stop so the carry reads after the hit-stop.
            Ctx.SetHorizontalVelocity(Mathf.MoveTowards(Ctx.Velocity.x, 0f, RecoilDeceleration * Time.fixedDeltaTime));
        }
    }
}
