namespace DeadCells.Core.StateMachine
{
    /// <summary>
    /// Synchronous single-state machine. Accepts one transition per frame; ForceChangeState is for
    /// interrupts that must not be lost (damage, death).
    /// </summary>
    public class StateMachine
    {
        private int _lastTransitionFrame = -1;

        public IState Current { get; private set; }

        public bool ChangeState(IState next)
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame == _lastTransitionFrame)
                return false;
            _lastTransitionFrame = frame;
            Switch(next);
            return true;
        }

        public void ForceChangeState(IState next)
        {
            _lastTransitionFrame = UnityEngine.Time.frameCount;
            Switch(next);
        }

        public void Tick() => Current?.Tick();

        public void FixedTick() => Current?.FixedTick();

        private void Switch(IState next)
        {
            Current?.Exit();
            Current = next;
            Current.Enter();
        }
    }
}
