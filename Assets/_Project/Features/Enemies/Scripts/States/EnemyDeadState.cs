using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Death: physics off, the sprite holds in a light grey flash for a moment, then vanishes in a poof
    /// (EnemyVfx plays the puff when the context raises Vanished). Dead Cells enemies have no death
    /// animation in the extracted data, so the hit frame stands in.
    /// </summary>
    public class EnemyDeadState : EnemyStateBase
    {
        private const float HoldTime = 0.3f;
        private const float GhostFlash = 0.55f;

        // Tint and flash together read as a light grey silhouette that still shows the sprite's detail.
        private static readonly Color GhostTint = new Color(0.72f, 0.72f, 0.72f, 1f);

        private float _timer;

        public EnemyDeadState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _timer = 0f;
            Ctx.SetPhysicsActive(false);
            Ctx.Animator.Play(EnemyAnimator.Hit);
            Ctx.Animator.Renderer.color = GhostTint;
            Ctx.Animator.SetFlash(GhostFlash);
        }

        public override void Tick()
        {
            _timer += Time.deltaTime;
            if (_timer >= HoldTime)
                Ctx.Vanish();
        }
    }
}
