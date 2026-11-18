using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Shieldbearer ram (Wiki: "charges up, then charges forward and deal damage on contact", stuns the player).
    /// Travels Skill.TravelDistance in Skill.TravelDuration (DC: distance 8, duration 0.6); stops at walls/ledges.
    /// </summary>
    public class EnemyDashAttackState : EnemyAttackStateBase
    {
        private float _speed;
        private bool _stopped;

        public EnemyDashAttackState(EnemyContext context, EnemyStateMachine machine, int skillIndex)
            : base(context, machine, skillIndex) { }

        protected override void OnExecuteStart()
        {
            _speed = Skill.TravelDistance / Mathf.Max(Skill.TravelDuration, 0.05f);
            _stopped = false;
        }

        protected override bool ExecuteTick()
        {
            if (!_stopped)
                Ctx.Strike(Skill);
            return ExecuteTime >= Skill.TravelDuration;
        }

        protected override void ExecuteFixedTick()
        {
            if (!_stopped && !Ctx.CanWalk(Ctx.Facing))
                _stopped = true;
            Ctx.SetHorizontalVelocity(_stopped ? 0f : Ctx.Facing * _speed);
        }
    }
}
