using UnityEngine;

namespace DeadCells.Player
{
    [DisallowMultipleComponent]
    public sealed class PowerScrollPickup : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _range = 1.35f;
        [SerializeField] private GameObject _prompt;
        [SerializeField] private Transform _icon;

        private Vector3 _iconOrigin;

        public bool IsAvailable => isActiveAndEnabled;

        private void Awake() => _iconOrigin = _icon.localPosition;

        private void Update() =>
            _icon.localPosition = _iconOrigin + Vector3.up * (0.10f * Mathf.Sin(3f * Time.time));

        public bool TryGetDistance(Vector2 position, out float distanceSquared)
        {
            distanceSquared = ((Vector2)transform.position - position).sqrMagnitude;
            return IsAvailable && distanceSquared <= _range * _range;
        }

        public void SetFocused(bool focused) => _prompt.SetActive(focused);

        internal void Consume()
        {
            SetFocused(false);
            gameObject.SetActive(false);
        }
    }
}
