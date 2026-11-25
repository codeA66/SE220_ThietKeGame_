namespace DeadCells.Core.StateMachine
{
    /// <summary>
    /// Synchronous single-state machine. Accepts one transition per Tick; ForceChangeState is for
    /// interrupts that must not be lost (damage, death).
    /// </summary>
    public class StateMachine
    {
        private bool _inTick;
        private bool _transitionedInTick;

        public IState Current { get; private set; }

        public bool ChangeState(IState next)
        {
            if (_inTick && _transitionedInTick)
                return false;
            if (_inTick)
                _transitionedInTick = true;
            Switch(next);
            return true;
        }

        public void ForceChangeState(IState next)
        {
            if (_inTick)
                _transitionedInTick = true;
            Switch(next);
        }

        public void Tick()
        {
            _inTick = true;
            _transitionedInTick = false;
            try
            {
                Current?.Tick();
            }
            finally
            {
                _inTick = false;
                _transitionedInTick = false;
            }
        }

        public void FixedTick() => Current?.FixedTick();

        private void Switch(IState next)
        {
            Current?.Exit();
            Current = next;
            Current.Enter();
        }
    }
}
