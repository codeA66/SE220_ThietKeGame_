using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Archer "shoot": after the charge, fire one straight projectile at the player.
    /// DC hitFrame (26) exceeds the extracted shoot clip (22 frames), so the arrow leaves at Execute start.
    /// </summary>
    public class EnemyShootAttackState : EnemyAttackStateBase
    {
        private const float ReleaseHold = 0.25f;
        private const float MuzzleHeight = 1.0f;

        public EnemyShootAttackState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        protected override void OnExecuteStart()
        {
            var d = Ctx.Data;
            if (Ctx.Projectiles == null || d.ProjectilePrefab == null)
                return;
            var from = new Vector2(Ctx.Position.x + Ctx.Facing * 0.5f, Ctx.Position.y + MuzzleHeight);
            Vector2 to = Ctx.Player != null ? (Vector2)Ctx.Player.position + Vector2.up * 0.8f : from + Vector2.right * Ctx.Facing;
            Vector2 dir = (to - from).normalized;
            if (Mathf.Sign(dir.x) != Ctx.Facing)
                dir = new Vector2(Ctx.Facing, 0f);
            for (int i = 0; i < d.ProjectileCount; i++)
            {
                var p = Ctx.Projectiles.Get(d.ProjectilePrefab);
                p.LaunchStraight(from, dir * d.ProjectileSpeed, d.Damage, d.Knockback, Ctx.Owner);
            }
        }

        protected override bool ExecuteTick() => ExecuteTime >= ReleaseHold;
    }
}
