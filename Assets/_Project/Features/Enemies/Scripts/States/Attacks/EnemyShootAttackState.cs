using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Undead Archer shot (Wiki: "very slow startup", the arrow "travels slowly" and "can be avoided by
    /// crouching"). The arrow flies horizontally at Skill.MuzzleHeight, so a crouching player ducks under it.
    /// </summary>
    public class EnemyShootAttackState : EnemyAttackStateBase
    {
        private const float ReleaseHold = 0.25f;

        public EnemyShootAttackState(EnemyContext context, EnemyStateMachine machine, int skillIndex)
            : base(context, machine, skillIndex) { }

        protected override void OnChargeStart() => Ctx.Face(Ctx.DirectionToPlayer);

        protected override void OnExecuteStart()
        {
            if (Ctx.Projectiles == null || Skill.ProjectilePrefab == null)
                return;
            var from = new Vector2(Ctx.Position.x + Ctx.Facing * 0.5f, Ctx.Position.y + Skill.MuzzleHeight);
            var velocity = new Vector2(Ctx.Facing * Skill.ProjectileSpeed, 0f);
            for (int i = 0; i < Skill.ProjectileCount; i++)
            {
                var p = Ctx.Projectiles.Get(Skill.ProjectilePrefab);
                p.LaunchStraight(from, velocity, Skill.Damage, Skill.Knockback, Ctx.Owner);
            }
        }

        protected override bool ExecuteTick() => ExecuteTime >= ReleaseHold;
    }
}
