namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Shield "dash": charge forward TravelDistance in TravelDuration (DC: distance 8, duration 0.6),
    /// hitting the player once and stunning them (DC: stunTime 1). Stops at walls and ledges.
    /// </summary>
    public class EnemyDashAttackState : EnemyAttackStateBase
    {
        private float _speed;
        private bool _stopped;

        public EnemyDashAttackState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        protected override void OnExecuteStart()
        {
            var d = Ctx.Data;
            _speed = d.TravelDistance / UnityEngine.Mathf.Max(d.TravelDuration, 0.05f);
            _stopped = false;
        }

        protected override bool ExecuteTick()
        {
            var d = Ctx.Data;
            if (!_stopped)
                Ctx.Strike(d.HitReach, d.HitHeight, d.Damage, d.Knockback, d.StunDealt);
            return ExecuteTime >= d.TravelDuration;
        }

        protected override void ExecuteFixedTick()
        {
            if (!_stopped && !Ctx.CanWalk(Ctx.Facing))
                _stopped = true;
            Ctx.SetHorizontalVelocity(_stopped ? 0f : Ctx.Facing * _speed);
        }
    }
}
