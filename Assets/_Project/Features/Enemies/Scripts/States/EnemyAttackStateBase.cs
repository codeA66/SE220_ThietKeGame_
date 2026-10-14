using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Shared skill flow: Charge (telegraph: wind-up animation + pulsing glow in Data.GlowColor, DC: skill.charge)
    /// -> Execute (subclass) -> Recover (DC: lockAiAfter). The skill's cooldown (DC: coolDown) starts when it ends.
    /// </summary>
    public abstract class EnemyAttackStateBase : EnemyStateBase
    {
        private const float TelegraphGlow = 0.6f;
        private const float TelegraphPulseHz = 8f;
        protected const float ClipFps = 30f;

        private enum Phase { Charge, Execute, Recover }

        private readonly int _skillIndex;
        private readonly int _chargeAnim;
        private readonly int _executeAnim;
        private Phase _phase;
        private float _phaseTimer;

        protected EnemyAttackStateBase(EnemyContext context, EnemyStateMachine machine, int skillIndex) : base(context, machine)
        {
            _skillIndex = skillIndex;
            _chargeAnim = Animator.StringToHash(Skill.ChargeAnim);
            _executeAnim = Animator.StringToHash(Skill.ExecuteAnim);
        }

        protected EnemyData.Skill Skill => Ctx.Data.Skills[_skillIndex];

        /// <summary>Seconds since Execute started.</summary>
        protected float ExecuteTime => _phase == Phase.Execute ? _phaseTimer : 0f;

        protected float HitTime => Skill.HitFrame / ClipFps;

        public override void Enter()
        {
            _phase = Phase.Charge;
            _phaseTimer = 0f;
            Ctx.SetHorizontalVelocity(0f);
            Ctx.Animator.Play(_chargeAnim);
            OnChargeStart();
        }

        public override void Exit()
        {
            Ctx.Animator.SetFlash(0f);
            Ctx.StartSkillCooldown(_skillIndex);
        }

        public override void Tick()
        {
            _phaseTimer += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Charge:
                    bool on = Mathf.Repeat(_phaseTimer * TelegraphPulseHz, 1f) < 0.5f;
                    Ctx.Animator.SetFlash(on ? TelegraphGlow : 0f, Ctx.Data.GlowColor);
                    if (_phaseTimer >= Skill.Charge)
                    {
                        Ctx.Animator.SetFlash(0f);
                        _phase = Phase.Execute;
                        _phaseTimer = 0f;
                        Ctx.BeginSwing();
                        Ctx.Animator.Play(_executeAnim);
                        OnExecuteStart();
                    }
                    break;
                case Phase.Execute:
                    if (ExecuteTick())
                    {
                        _phase = Phase.Recover;
                        _phaseTimer = 0f;
                        OnExecuteEnd();
                    }
                    break;
                case Phase.Recover:
                    if (_phaseTimer >= Skill.Recover)
                        Machine.ChangeState(Machine.Chase);
                    break;
            }
        }

        public override void FixedTick()
        {
            if (_phase == Phase.Execute)
                ExecuteFixedTick();
            else
                Ctx.SetHorizontalVelocity(0f);
        }

        protected virtual void OnChargeStart() { }

        protected abstract void OnExecuteStart();

        /// <summary>Return true when the execution is over.</summary>
        protected abstract bool ExecuteTick();

        protected virtual void ExecuteFixedTick() => Ctx.SetHorizontalVelocity(0f);

        protected virtual void OnExecuteEnd() { }
    }
}
