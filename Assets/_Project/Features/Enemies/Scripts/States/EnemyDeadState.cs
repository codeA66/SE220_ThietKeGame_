using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Death: physics off, white flash, fade out, then the object is disabled. Dead Cells enemies have no
    /// death animation in the extracted data, so this stands in until a gib effect exists.
    /// </summary>
    public class EnemyDeadState : EnemyStateBase
    {
        private const float FlashTime = 0.1f;
        private const float FadeTime = 0.4f;

        private float _timer;

        public EnemyDeadState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _timer = 0f;
            Ctx.SetPhysicsActive(false);
            Ctx.Animator.Play(EnemyAnimator.Hit);
            Ctx.Animator.SetFlash(1f);
        }

        public override void Tick()
        {
            _timer += Time.deltaTime;
            if (_timer < FlashTime)
                return;
            Ctx.Animator.SetFlash(0f);
            float t = (_timer - FlashTime) / FadeTime;
            Ctx.Animator.SetAlpha(1f - t);
            if (t >= 1f)
                Ctx.Owner.SetActive(false);
        }
    }
}
