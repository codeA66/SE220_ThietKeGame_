using UnityEngine;

namespace DeadCells.Enemies.States
{
    /// <summary>
    /// Rat "nibble": leap toward the player and bite HitCount times (DC: count 2) while travelling.
    /// Ends after TravelDuration once back on the ground.
    /// </summary>
    public class EnemyLungeAttackState : EnemyAttackStateBase
    {
        private int _bites;
        private float _nextBiteTime;
        private float _speedX;

        public EnemyLungeAttackState(EnemyContext context, EnemyStateMachine machine) : base(context, machine) { }

        protected override void OnExecuteStart()
        {
            var d = Ctx.Data;
            _bites = 0;
            _nextBiteTime = HitTime;
            float dist = Mathf.Min(Ctx.DistanceToPlayer, d.TravelDistance);
            _speedX = Ctx.Facing * dist / Mathf.Max(d.TravelDuration, 0.05f);
            Ctx.SetVelocity(new Vector2(_speedX, d.LungeUpSpeed));
        }

        protected override bool ExecuteTick()
        {
            var d = Ctx.Data;
            if (_bites < d.HitCount && ExecuteTime >= _nextBiteTime)
            {
                Ctx.BeginSwing();
                Ctx.Strike(d.HitReach, d.HitHeight, d.Damage, d.Knockback, d.StunDealt);
                _bites++;
                _nextBiteTime += d.TravelDuration / d.HitCount;
            }
            return ExecuteTime >= d.TravelDuration && Ctx.IsGrounded;
        }

        protected override void ExecuteFixedTick()
        {
            if (ExecuteTime < Ctx.Data.TravelDuration && !Ctx.IsWallAhead(Ctx.Facing))
                Ctx.SetHorizontalVelocity(_speedX);
            else
                Ctx.SetHorizontalVelocity(0f);
        }
    }
}
