using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Brief loss of control after taking a hit: knocked back away from the attacker, then back to
    /// Idle/Run/Fall. Invulnerability is handled by Health (DC: OnHitGlobalShield 0.5 s).
    /// </summary>
    public class PlayerHurtState : PlayerStateBase
    {
        private float _timer;

        public PlayerHurtState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _timer = 0f;
            var hit = Ctx.Combat.LastDamageTaken;
            int dir = hit.Direction != 0 ? hit.Direction : -Ctx.FacingDirection;
            Ctx.Face(-dir);
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
            Ctx.SetHorizontalVelocity(dir * Mathf.Max(hit.Knockback, Ctx.Config.HurtMinKnockback));
            Ctx.SetVerticalVelocity(Ctx.Config.HurtKnockUp);
            Ctx.Animator.SetHurt(true);
        }

        public override void Exit() => Ctx.Animator.SetHurt(false);

        public override void Tick()
        {
            _timer += Time.deltaTime;
            float lockTime = Mathf.Max(Ctx.Config.HurtLockDuration, Ctx.Combat.LastDamageTaken.Stun);
            if (_timer >= lockTime)
                ReturnToDefault();
        }

        public override void FixedTick() => Ctx.ClampFallSpeed();
    }
}
