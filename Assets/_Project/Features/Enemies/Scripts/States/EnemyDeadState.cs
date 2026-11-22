using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Death: physics off, white flash, fade out, then the object is disabled. Dead Cells enemies have no
    /// death animation in the extracted data, so this stands in until a gib effect exists.
    /// </summary>
    public class EnemyDeadState : EnemyStateBase
    {
        private const float FlashTime = 0.07f;
        private const float FadeTime = 0.18f;

        private float _timer;
        private Vector3 _startPos;
        private int _pushDir;

        public EnemyDeadState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _timer = 0f;
            _startPos = Ctx.Owner.transform.position;
            _pushDir = Ctx.LastDamage.Direction != 0 ? Ctx.LastDamage.Direction : -Ctx.Facing;
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
            float t = Mathf.Clamp01((_timer - FlashTime) / FadeTime);
            float ease = t * (2f - t);
            Ctx.Owner.transform.position = _startPos + new Vector3(_pushDir * 0.35f * ease, 0f, 0f);
            Ctx.Owner.transform.localScale = new Vector3(1f + 0.15f * t, 1f - 0.25f * t, 1f);
            Ctx.Animator.SetAlpha(1f - t);
            if (t >= 1f)
            {
                Ctx.Owner.transform.localScale = Vector3.one;
                Ctx.Owner.SetActive(false);
            }
        }
    }
}
