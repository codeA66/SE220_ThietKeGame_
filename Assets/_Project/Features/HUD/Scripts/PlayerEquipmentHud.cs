using DeadCells.Core;
using DeadCells.Player;
using UnityEngine;

namespace DeadCells.HUD
{
    [DisallowMultipleComponent]
    public sealed class PlayerEquipmentHud : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerBow _bow;
        [SerializeField] private WeaponSlotView _swordSlot;
        [SerializeField] private WeaponSlotView _bowSlot;
        [SerializeField] private BowAmmoView _ammo;

        private PlayerWeaponLoadout _weapons;

        // Start binds after PlayerController.Awake creates the loadout and initializes the bow.
        private void Start() => Bind();

        private void OnEnable()
        {
            if (_player != null && _player.Weapons != null)
                Bind();
        }

        private void OnDisable()
        {
            if (_weapons == null)
                return;

            _weapons.Acquired -= OnSlotUpdated;
            _weapons.Changed -= OnSlotUpdated;
            _bow.AmmoChanged -= RenderAmmo;
            _weapons = null;
        }

        private void Bind()
        {
            if (_weapons != null)
                return;
            if (_player == null || _player.Weapons == null || _bow == null ||
                _swordSlot == null || _bowSlot == null || _ammo == null)
            {
                GameDebug.LogError("Assign the player, bow and equipment HUD views.", LogTopic.UI);
                return;
            }

            _weapons = _player.Weapons;
            _weapons.Acquired += OnSlotUpdated;
            _weapons.Changed += OnSlotUpdated;
            _bow.AmmoChanged += RenderAmmo;
            OnSlotUpdated(_weapons.Equipped);
        }

        private void OnSlotUpdated(PlayerWeaponSlot slot)
        {
            _swordSlot.Render(_weapons.Owns(PlayerWeaponSlot.Sword), _weapons.Equipped == PlayerWeaponSlot.Sword);
            _bowSlot.Render(_weapons.Owns(PlayerWeaponSlot.Bow), _weapons.Equipped == PlayerWeaponSlot.Bow);
            RenderAmmo(_bow.Ammo, _bow.Data.AmmoCapacity);
        }

        private void RenderAmmo(int current, int max) =>
            _ammo.Render(_weapons.Owns(PlayerWeaponSlot.Bow), current, max);
    }
}
