using System;

namespace DeadCells.Player
{
    public enum PlayerWeaponSlot { Sword, Bow }

    /// <summary>Owns slot selection; attack states and animation do not read keyboard keys.</summary>
    public sealed class PlayerWeaponLoadout
    {
        public PlayerWeaponSlot Equipped { get; private set; } = PlayerWeaponSlot.Sword;
        public event Action<PlayerWeaponSlot> Changed;

        public bool Equip(PlayerWeaponSlot slot)
        {
            if (Equipped == slot)
                return false;
            Equipped = slot;
            Changed?.Invoke(slot);
            return true;
        }
    }
}
