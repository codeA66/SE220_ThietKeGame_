using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DeadCells.Player
{
    /// <summary>A placed starter item. Ownership lives in the player loadout.</summary>
    public sealed class StarterWeaponPickup : MonoBehaviour
    {
        private const float CollectDuration = 0.32f;

        [SerializeField] private PlayerWeaponSlot _slot;
        [SerializeField, Min(0.1f)] private float _range = 1.25f;
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private Light2D _light;
        [SerializeField] private GameObject _prompt;

        private Vector3 _iconOrigin;
        private Vector3 _iconScale;
        private Color _iconColor;
        private float _lightIntensity;
        private float _collectedAt;
        private bool _collected;

        public event Action<Vector3> Collected;

        private void Awake()
        {
            _iconOrigin = _icon.transform.localPosition;
            _iconScale = _icon.transform.localScale;
            _iconColor = _icon.color;
            _lightIntensity = _light.intensity;
            _prompt.SetActive(false);
        }

        public bool TryGetDistance(Vector2 position, out float distanceSquared)
        {
            distanceSquared = ((Vector2)transform.position - position).sqrMagnitude;
            return !_collected && distanceSquared <= _range * _range;
        }

        public void SetFocused(bool focused) => _prompt.SetActive(focused && !_collected);

        public bool TryCollect(PlayerWeaponLoadout loadout)
        {
            if (_collected || !loadout.TryAcquire(_slot))
                return false;

            _collected = true;
            _collectedAt = Time.time;
            SetFocused(false);
            Collected?.Invoke(_icon.transform.position);
            return true;
        }

        private void Update()
        {
            if (!_collected)
            {
                float wave = Mathf.Sin(Time.time * 3f + (int)_slot * 1.5f);
                _icon.transform.localPosition = _iconOrigin + Vector3.up * (0.12f * wave);
                _light.intensity = _lightIntensity * (1f + 0.18f * wave);
                return;
            }

            float progress = Mathf.Clamp01((Time.time - _collectedAt) / CollectDuration);
            _icon.transform.localPosition = _iconOrigin + Vector3.up * (0.7f * progress);
            _icon.transform.localScale = _iconScale * (1f + 0.35f * progress);
            _icon.color = new Color(_iconColor.r, _iconColor.g, _iconColor.b, 1f - progress);
            _light.intensity = _lightIntensity * (1f - progress) * (2f - progress);
            if (progress >= 1f)
            {
                _icon.enabled = false;
                _light.enabled = false;
                enabled = false;
            }
        }
    }
}
