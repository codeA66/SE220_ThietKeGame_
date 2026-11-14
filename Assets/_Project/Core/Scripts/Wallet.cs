using System;

namespace DeadCells.Core
{
    /// <summary>
    /// The player's currencies for the current run. Static so pickups, HUD and shops can share it
    /// without scene wiring; call Reset when a new run starts.
    /// </summary>
    public static class Wallet
    {
        public static int Gold { get; private set; }
        public static int Gems { get; private set; }

        /// <summary>Arguments: gold, gems.</summary>
        public static event Action<int, int> Changed;

        public static void AddGold(int amount)
        {
            if (amount <= 0)
                return;
            Gold += amount;
            Changed?.Invoke(Gold, Gems);
        }

        public static void AddGems(int amount)
        {
            if (amount <= 0)
                return;
            Gems += amount;
            Changed?.Invoke(Gold, Gems);
        }

        public static void Reset()
        {
            Gold = 0;
            Gems = 0;
            Changed?.Invoke(Gold, Gems);
        }
    }
}
