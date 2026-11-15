using DeadCells.Currency;
using DeadCells.Player;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.HUD
{
    public sealed class ItemPickupFeedback : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private GoldWallet _wallet;
        [SerializeField] private StarterWeaponPickup[] _weapons;
        [SerializeField] private VfxSpawner _effects;
        [SerializeField] private VfxDefinition _glint;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _goldSound;
        [SerializeField] private AudioClip _weaponSound;

        private void OnEnable()
        {
            _wallet.Changed += OnGoldCollected;
            foreach (var weapon in _weapons)
                weapon.Collected += OnWeaponCollected;
        }

        private void OnDisable()
        {
            if (_wallet != null)
                _wallet.Changed -= OnGoldCollected;
            foreach (var weapon in _weapons)
                if (weapon != null)
                    weapon.Collected -= OnWeaponCollected;
        }

        private void OnGoldCollected(int balance)
        {
            _effects.Play(_glint, _player.position + Vector3.up * 0.8f);
            _audio.PlayOneShot(_goldSound, 0.68f);
        }

        private void OnWeaponCollected(Vector3 position)
        {
            _effects.Play(_glint, position);
            _audio.PlayOneShot(_weaponSound, 0.78f);
        }
    }
}
