using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Archer escape (Wiki: "if it's cornered at a wall, it can teleport behind you instead"; DC: swap coolDown 3.5).
    /// Fades out, reappears Data.TeleportBehind past the player, plays the rise animation, then resumes chasing.
    /// Falls back to Chase if there is no valid landing spot.
    /// </summary>
    public class EnemyTeleportState : EnemyStateBase
    {
        private static readonly int RiseAnim = Animator.StringToHash("Rise");
        private const float FadeOut = 0.2f;
        private const float FadeIn = 0.3f;

        private float _timer;
        private bool _moved;
        private Vector2 _destination;

        public EnemyTeleportState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _timer = 0f;
            _moved = false;
            Ctx.NextTeleportTime = Time.time + Ctx.Data.TeleportCooldown;
            if (!TryFindDestination(out _destination))
            {
                Machine.ForceChangeState(Machine.Chase);
                return;
            }
            Ctx.SetHorizontalVelocity(0f);
        }

        public override void Exit() => Ctx.Animator.SetAlpha(1f);

        public override void Tick()
        {
            _timer += Time.deltaTime;
            if (!_moved)
            {
                Ctx.Animator.SetAlpha(1f - Mathf.Clamp01(_timer / FadeOut));
                if (_timer < FadeOut)
                    return;
                Ctx.Teleport(_destination);
                Ctx.Face(Ctx.DirectionToPlayer);
                Ctx.Animator.Play(Ctx.Animator.HasState(RiseAnim) ? RiseAnim : EnemyAnimator.Idle);
                _moved = true;
                _timer = 0f;
                return;
            }

            Ctx.Animator.SetAlpha(Mathf.Clamp01(_timer / FadeIn));
            if (_timer >= FadeIn)
                Machine.ChangeState(Machine.Chase);
        }

        public override void FixedTick() => Ctx.SetHorizontalVelocity(0f);

        private bool TryFindDestination(out Vector2 feet)
        {
            feet = default;
            if (Ctx.Player == null)
                return false;
            int side = Ctx.DirectionToPlayer;
            var playerFeet = (Vector2)Ctx.Player.position;
            feet = playerFeet + new Vector2(side * Ctx.Data.TeleportBehind, 0.05f);
            return Ctx.CanStandAt(feet);
        }
    }
}
