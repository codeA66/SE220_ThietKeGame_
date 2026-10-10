using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Shared attack flow: Charge (telegraph: wind-up animation + pulsing flash, DC: skill.charge) ->
    /// Execute (subclass) -> Recover (DC: lockAiAfter). The cooldown (DC: coolDown) starts when the attack ends.
    /// </summary>
    public abstract class EnemyAttackStateBase : EnemyStateBase
    {
        private const float TelegraphFlash = 0.35f;
        private const float TelegraphPulseHz = 8f;
        protected const float ClipFps = 30f;

        private enum Phase { Charge, Execute, Recover }

        private Phase _phase;
        private float _phaseTimer;

        protected EnemyAttackStateBase(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        /// <summary>Seconds since Execute started.</summary>
        protected float ExecuteTime => _phase == Phase.Execute ? _phaseTimer : 0f;

        public override void Enter()
        {
            _phase = Phase.Charge;
            _phaseTimer = 0f;
            Ctx.FacePlayer();
            Ctx.SetHorizontalVelocity(0f);
            Ctx.Animator.Play(EnemyAnimator.AtkLoad);
        }

        public override void Exit()
        {
            Ctx.Animator.SetFlash(0f);
            Ctx.NextAttackTime = Time.time + Ctx.Data.Cooldown;
        }

        public override void Tick()
        {
            _phaseTimer += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Charge:
                    bool on = Mathf.Repeat(_phaseTimer * TelegraphPulseHz, 1f) < 0.5f;
                    Ctx.Animator.SetFlash(on ? TelegraphFlash : 0f);
                    if (_phaseTimer >= Ctx.Data.Charge)
                    {
                        Ctx.Animator.SetFlash(0f);
                        _phase = Phase.Execute;
                        _phaseTimer = 0f;
                        Ctx.BeginSwing();
                        Ctx.Animator.Play(EnemyAnimator.Atk);
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
                    if (_phaseTimer >= Ctx.Data.Recover)
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

        protected abstract void OnExecuteStart();

        /// <summary>Return true when the execution is over.</summary>
        protected abstract bool ExecuteTick();

        protected virtual void ExecuteFixedTick() => Ctx.SetHorizontalVelocity(0f);

        protected virtual void OnExecuteEnd() { }

        protected float HitTime => Ctx.Data.HitFrame / ClipFps;
    }
}
