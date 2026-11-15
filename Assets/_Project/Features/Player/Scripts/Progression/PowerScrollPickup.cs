using UnityEngine;

namespace DeadCells.Player
{
    [DisallowMultipleComponent]
    public sealed class PowerScrollPickup : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _range = 1.35f;
        [SerializeField] private GameObject _prompt;
        [SerializeField] private Transform _icon;
        [SerializeField] private SpriteRenderer _vessel;
        [SerializeField] private Sprite _emptyVessel;
        [SerializeField] private GameObject _glow;

        private Vector3 _iconOrigin;
        private Sprite _sealedVessel;
        private bool _consumed;

        public bool IsAvailable => isActiveAndEnabled && !_consumed;
        public Vector3 IconPosition => _icon.position;

        private void Awake()
        {
            _iconOrigin = _icon.localPosition;
            if (_vessel != null)
                _sealedVessel = _vessel.sprite;
        }

        private void Update() =>
            _icon.localPosition = _iconOrigin + Vector3.up * (0.10f * Mathf.Sin(3f * Time.time));

        public bool TryGetDistance(Vector2 position, out float distanceSquared)
        {
            distanceSquared = ((Vector2)transform.position - position).sqrMagnitude;
            return IsAvailable && distanceSquared <= _range * _range;
        }

        public void SetFocused(bool focused) => _prompt.SetActive(focused);

        public void Reveal(bool revealed)
        {
            SetFocused(false);
            _icon.gameObject.SetActive(!revealed);
            if (_glow != null)
                _glow.SetActive(!revealed);
            if (_vessel != null)
                _vessel.sprite = revealed ? _emptyVessel : _sealedVessel;
        }

        internal void Consume()
        {
            _consumed = true;
            Reveal(true);
            enabled = false;
        }
    }
}
