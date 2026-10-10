namespace DeadCells.Enemies.States
{
    /// <summary>Zombie "cc": swing in front, one hit on HitFrame.</summary>
    public class EnemyMeleeAttackState : EnemyAttackStateBase
    {
        private const float SwingLength = 0.3f;

        private bool _hitDone;

        public EnemyMeleeAttackState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        protected override void OnExecuteStart() => _hitDone = false;

        protected override bool ExecuteTick()
        {
            var d = Ctx.Data;
            if (!_hitDone && ExecuteTime >= HitTime)
            {
                _hitDone = true;
                Ctx.Strike(d.HitReach, d.HitHeight, d.Damage, d.Knockback, d.StunDealt);
            }
            return ExecuteTime >= HitTime + SwingLength;
        }
    }
}
