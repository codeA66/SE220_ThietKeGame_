using System;

namespace DeadCells.Player
{
    public enum PlayerWeaponSlot { Sword, Bow, None }

    /// <summary>Owns the two starter slots and their current selection.</summary>
    public sealed class PlayerWeaponLoadout
    {
        private bool _hasSword;
        private bool _hasBow;

        public PlayerWeaponSlot Equipped { get; private set; }
        public event Action<PlayerWeaponSlot> Acquired;
        public event Action<PlayerWeaponSlot> Changed;

        public PlayerWeaponLoadout(bool startWithWeapons)
        {
            _hasSword = _hasBow = startWithWeapons;
            Equipped = startWithWeapons ? PlayerWeaponSlot.Sword : PlayerWeaponSlot.None;
        }

        public bool Owns(PlayerWeaponSlot slot) => slot switch
        {
            PlayerWeaponSlot.Sword => _hasSword,
            PlayerWeaponSlot.Bow => _hasBow,
            _ => false
        };

        public bool TryAcquire(PlayerWeaponSlot slot)
        {
            if (Owns(slot))
                return false;
            switch (slot)
            {
                case PlayerWeaponSlot.Sword: _hasSword = true; break;
                case PlayerWeaponSlot.Bow: _hasBow = true; break;
                default: return false;
            }

            if (Equipped == PlayerWeaponSlot.None)
                Equip(slot);
            Acquired?.Invoke(slot);
            return true;
        }

        public bool Equip(PlayerWeaponSlot slot)
        {
            if (Equipped == slot || !Owns(slot))
                return false;
            Equipped = slot;
            Changed?.Invoke(slot);
            return true;
        }
    }
}
