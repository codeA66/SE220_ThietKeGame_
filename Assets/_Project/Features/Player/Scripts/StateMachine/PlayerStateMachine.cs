using DeadCells.Core;
using DeadCells.Player.StateMachine.States;

namespace DeadCells.Player.StateMachine
{
    /// <summary>
    /// Owns every player state (created once) and the single active state.
    /// All transitions go through ChangeState.
    /// </summary>
    public class PlayerStateMachine
    {
        private int _lastTransitionFrame = -1;

        public PlayerStateMachine(PlayerContext context)
        {
            Idle = new PlayerIdleState(context, this);
            Run = new PlayerRunState(context, this);
            Jump = new PlayerJumpState(context, this);
            Fall = new PlayerFallState(context, this);
            Land = new PlayerLandState(context, this);
            Roll = new PlayerRollState(context, this);
            Attack = new PlayerAttackState(context, this);
            LedgeClimb = new PlayerLedgeClimbState(context, this);
            AirJump = new PlayerAirJumpState(context, this);
            Stomp = new PlayerStompState(context, this);
            Climb = new PlayerClimbState(context, this);
            Crouch = new PlayerCrouchState(context, this);
        }

        public IPlayerState Current { get; private set; }

        public IPlayerState Idle { get; }
        public IPlayerState Run { get; }
        public IPlayerState Jump { get; }
        public IPlayerState Fall { get; }
        public IPlayerState Land { get; }
        public IPlayerState Roll { get; }
        public IPlayerState Attack { get; }
        public IPlayerState LedgeClimb { get; }
        public IPlayerState AirJump { get; }
        public IPlayerState Stomp { get; }
        public IPlayerState Climb { get; }
        public IPlayerState Crouch { get; }

        /// <summary>
        /// Switches state. Only one transition is accepted per frame.
        /// Returns false if a transition already happened this frame.
        /// </summary>
        public bool ChangeState(IPlayerState next)
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame == _lastTransitionFrame)
                return false;
            _lastTransitionFrame = frame;

            Current?.Exit();
            GameDebug.Log($"{Current?.GetType().Name ?? "None"} -> {next.GetType().Name}", LogTopic.Gameplay);
            Current = next;
            Current.Enter();
            return true;
        }

        public void Tick() => Current?.Tick();

        public void FixedTick() => Current?.FixedTick();
    }
}
