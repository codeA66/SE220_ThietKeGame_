namespace DeadCells.Enemies.States
{
    /// <summary>Zombie scratch (Wiki: "a melee range scratch with a long startup"): one hit on HitFrame.</summary>
    public class EnemyMeleeAttackState : EnemyAttackStateBase
    {
        private const float SwingLength = 0.3f;

        private bool _hitDone;

        public EnemyMeleeAttackState(EnemyContext context, EnemyStateMachine machine, int skillIndex)
            : base(context, machine, skillIndex) { }

        protected override void OnExecuteStart() => _hitDone = false;

        protected override bool ExecuteTick()
        {
            if (!_hitDone && ExecuteTime >= HitTime)
            {
                _hitDone = true;
                Ctx.Strike(Skill);
            }
            return ExecuteTime >= HitTime + SwingLength;
        }
    }
}
