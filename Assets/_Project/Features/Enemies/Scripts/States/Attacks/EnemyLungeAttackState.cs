using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Leap forward and damage on contact (Wiki: Zombie "leans back then leaps forward, dealing damage on
    /// collision"; Rancid Rat "starts glowing, then attacks with a short forward leap"). The leap covers
    /// Skill.TravelDistance (shortened to reach the player) in Skill.TravelDuration; one hit per leap.
    /// </summary>
    public class EnemyLungeAttackState : EnemyAttackStateBase
    {
        private float _speedX;

        public EnemyLungeAttackState(EnemyContext context, EnemyStateMachine machine, int skillIndex)
            : base(context, machine, skillIndex) { }

        protected override void OnExecuteStart()
        {
            float dist = Mathf.Min(Ctx.DistanceToPlayer + 0.5f, Skill.TravelDistance);
            _speedX = Ctx.Facing * dist / Mathf.Max(Skill.TravelDuration, 0.05f);
            Ctx.SetVelocity(new Vector2(_speedX, Skill.LungeUpSpeed));
        }

        protected override bool ExecuteTick()
        {
            // Contact damage for the whole flight; the hit query lets it land only once.
            if (ExecuteTime < Skill.TravelDuration)
                Ctx.Strike(Skill);
            return ExecuteTime >= Skill.TravelDuration && Ctx.IsGrounded;
        }

        protected override void ExecuteFixedTick()
        {
            if (ExecuteTime < Skill.TravelDuration && !Ctx.IsWallAhead(Ctx.Facing))
                Ctx.SetHorizontalVelocity(_speedX);
            else
                Ctx.SetHorizontalVelocity(0f);
        }
    }
}
