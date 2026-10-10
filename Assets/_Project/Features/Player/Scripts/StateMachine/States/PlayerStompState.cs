using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Air stomp (Dead Cells wiki: "Down smash in air", Down + Jump while airborne).
    /// Dives straight down at StompSpeed, then a short landing lock.
    /// Uses the Fall/Land animations: no dedicated stomp clip was found in the game data.
    /// </summary>
    public class PlayerStompState : PlayerStateBase
    {
        private bool _landed;
        private float _landTimer;

        public PlayerStompState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            _landed = false;
            _landTimer = 0f;
            Ctx.SetGravityScale(0f);
            Ctx.SetHorizontalVelocity(0f);
            Ctx.SetVerticalVelocity(-Ctx.Config.StompSpeed);
        }

        public override void Exit() => Ctx.SetGravityScale(Ctx.Config.GravityScale);

        public override void Tick()
        {
            if (!_landed)
            {
                if (Ctx.IsGrounded)
                {
                    _landed = true;
                    Ctx.NotifyLanded();
                    Ctx.Combat.PerformStomp(Ctx.Config.StompDamage, Ctx.Config.StompStun, Ctx.Config.StompArea);
                }
                return;
            }

            _landTimer += Time.deltaTime;
            if (_landTimer >= Ctx.Config.StompLandDuration)
                ReturnToDefault();
        }

        public override void FixedTick()
        {
            Ctx.SetHorizontalVelocity(0f);
            if (!_landed)
                Ctx.SetVerticalVelocity(-Ctx.Config.StompSpeed);
        }
    }
}
