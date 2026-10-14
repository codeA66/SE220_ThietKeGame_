namespace DeadCells.Combat
{
    /// <summary>
    /// Something that can be briefly stunned without taking damage (e.g. hitting a shield).
    /// </summary>
    public interface IStaggerable
    {
        /// <param name="duration">Seconds of lost control.</param>
        /// <param name="direction">Push direction: -1 or 1.</param>
        void Stagger(float duration, int direction);
    }
}
