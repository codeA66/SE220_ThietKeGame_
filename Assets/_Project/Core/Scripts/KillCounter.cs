using System;

namespace DeadCells.Core
{
    /// <summary>
    /// Enemies the player has killed this run. Static like Wallet so the enemy code can report a kill
    /// and the HUD can show it without scene wiring; call Reset when a new run starts.
    /// </summary>
    public static class KillCounter
    {
        public static int Kills { get; private set; }

        public static event Action<int> Changed;

        public static void AddKill()
        {
            Kills++;
            Changed?.Invoke(Kills);
        }

        public static void Reset()
        {
            Kills = 0;
            Changed?.Invoke(Kills);
        }
    }
}
