using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Currency
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GoldPickup : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _magnetDistance = 5.25f;
        [SerializeField, Min(0.1f)] private float _magnetSpeed = 15f;
        [SerializeField] private LayerMask _sightBlockers;
        // Authored scene coins (treasure caches) stay put until the magnet picks them up instead of falling.
        [SerializeField] private bool _staticPlacement;
        [SerializeField] private GoldWallet _wallet;
        [SerializeField] private Health _player;
        [SerializeField] private int _value;

        private Rigidbody2D _body;
        private float _attractAt;
        private bool _attracting;

        public bool IsAttracting => _attracting;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (_staticPlacement && _body != null)
            {
                _body.bodyType = RigidbodyType2D.Kinematic;
                _body.linearVelocity = Vector2.zero;
            }

            EnsureVfxSortingLayer();
        }

        public void EnsureVfxSortingLayer()
        {
            int vfxLayerId = SortingLayer.NameToID("VFX");
            if (vfxLayerId == 0)
                return;

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr != null && sr.sortingLayerID == 0)
                {
                    sr.sortingLayerID = vfxLayerId;
                    if (sr.sortingOrder < 5)
                        sr.sortingOrder = 5;
                }
            }
        }

        public void Launch(GoldWallet wallet, Health player, int value)
        {
            _wallet = wallet;
            _player = player;
            _value = value;
            _attractAt = Time.time + 0.24f;
            _body.linearVelocity = new Vector2(Random.Range(-1.4f, 1.4f), 3.6f);
        }

        private void Update() => Tick(Time.deltaTime, Time.time);

        public void Tick(float deltaTime, float currentTime)
        {
            if (_player == null || !_player.IsAlive)
            {
                if (_attracting)
                {
                    _attracting = false;
                    if (_body != null && !_staticPlacement)
                    {
                        _body.simulated = true;
                        _body.linearVelocity = Vector2.zero;
                    }
                }
                return;
            }

            if (_value <= 0 || _wallet == null || deltaTime <= 0f || currentTime < _attractAt)
                return;

            Vector2 target = (Vector2)_player.transform.position + Vector2.up * 0.75f;
            Vector2 position = transform.position;
            if (!_attracting)
            {
                if ((target - position).sqrMagnitude > _magnetDistance * _magnetDistance ||
                    Physics2D.Linecast(position, target, _sightBlockers))
                    return;

                _attracting = true;
                _body.simulated = false;
            }

            Vector2 next = Vector2.MoveTowards(position, target, _magnetSpeed * deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            if ((target - next).sqrMagnitude > 0.0625f)
                return;

            int value = _value;
            _value = 0;
            _wallet.Credit(value);
            Destroy(gameObject);
        }
    }
}
