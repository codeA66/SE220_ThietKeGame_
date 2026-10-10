using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Grenader "grenade": on HitFrame, lob ProjectileCount fused grenades (DC: count 3, triggerDelay 1.5)
    /// toward the player, spread horizontally. Each explodes in BlastRadius (DC: radius 2.9 cells).
    /// </summary>
    public class EnemyGrenadeAttackState : EnemyAttackStateBase
    {
        private const float ThrowHeight = 1.4f;
        private const float AfterThrow = 0.3f;

        private bool _thrown;

        public EnemyGrenadeAttackState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

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
            var d = Ctx.Data;
            if (Ctx.Projectiles == null || d.ProjectilePrefab == null)
                return;
            var from = new Vector2(Ctx.Position.x + Ctx.Facing * 0.3f, Ctx.Position.y + ThrowHeight);
            float dx = Ctx.Player != null ? Ctx.Player.position.x - from.x : Ctx.Facing * 3f;
            float g = Mathf.Abs(Physics2D.gravity.y);
            const float flightTime = 0.9f;
            // Velocity that lands at the player's x after flightTime (same height assumed).
            float vx = dx / flightTime;
            float vy = 0.5f * g * flightTime;
            int n = d.ProjectileCount;
            for (int i = 0; i < n; i++)
            {
                float offset = (i - (n - 1) * 0.5f) * d.ThrowSpread;
                var p = Ctx.Projectiles.Get(d.ProjectilePrefab);
                p.LaunchLob(from, new Vector2(vx + offset, vy), d.Damage, d.FuseTime, d.BlastRadius, d.BlastVfx, Ctx.Owner);
            }
        }
    }
}
