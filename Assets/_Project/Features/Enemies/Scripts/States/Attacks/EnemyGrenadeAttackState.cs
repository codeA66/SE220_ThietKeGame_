using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Grenadier bomb (Wiki: "fires a bomb at the player's location. They detonate after a delay upon landing";
    /// no contact damage). Throws Skill.ProjectileCount bombs (DC: count 3) landing around the player's
    /// position at throw time; each fuse (DC: triggerDelay 1.5) starts when it lands.
    /// </summary>
    public class EnemyGrenadeAttackState : EnemyAttackStateBase
    {
        private const float ThrowHeight = 1.4f;
        private const float FlightTime = 0.8f;
        private const float AfterThrow = 0.3f;

        private bool _thrown;

        public EnemyGrenadeAttackState(EnemyContext context, EnemyStateMachine machine, int skillIndex)
            : base(context, machine, skillIndex) { }

        protected override void OnChargeStart() => Ctx.Face(Ctx.DirectionToPlayer);

        protected override void OnExecuteStart() => _thrown = false;

        protected override bool ExecuteTick()
        {
            if (!_thrown && ExecuteTime >= HitTime)
            {
                _thrown = true;
                Throw();
            }
            return _thrown && ExecuteTime >= HitTime + AfterThrow;
        }

        private void Throw()
        {
            if (Ctx.Projectiles == null || Skill.ProjectilePrefab == null || Ctx.Player == null)
                return;
            var from = new Vector2(Ctx.Position.x + Ctx.Facing * 0.3f, Ctx.Position.y + ThrowHeight);
            Vector2 target = Ctx.Player.position;
            float g = Mathf.Abs(Physics2D.gravity.y);
            int n = Skill.ProjectileCount;
            for (int i = 0; i < n; i++)
            {
                float offset = n > 1 ? Mathf.Lerp(-Skill.LandingSpread, Skill.LandingSpread, i / (float)(n - 1)) : 0f;
                // Ballistic velocity that reaches (target.x + offset, target.y) after FlightTime.
                float vx = (target.x + offset - from.x) / FlightTime;
                float vy = (target.y - from.y) / FlightTime + 0.5f * g * FlightTime;
                var p = Ctx.Projectiles.Get(Skill.ProjectilePrefab);
                p.LaunchLob(from, new Vector2(vx, vy), Skill.Damage, Skill.FuseTime, Skill.BlastRadius, Skill.BlastVfx, Ctx.Owner);
            }
        }
    }
}
