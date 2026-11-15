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

        private Rigidbody2D _body;
        private GoldWallet _wallet;
        private Health _player;
        private int _value;
        private float _attractAt;
        private bool _attracting;

        private void Awake() => _body = GetComponent<Rigidbody2D>();

        public void Launch(GoldWallet wallet, Health player, int value)
        {
            _wallet = wallet;
            _player = player;
            _value = value;
            _attractAt = Time.time + 0.24f;
            _body.linearVelocity = new Vector2(Random.Range(-1.4f, 1.4f), 3.6f);
        }

        private void Update()
        {
            if (_value <= 0 || _wallet == null || _player == null || !_player.IsAlive ||
                Time.deltaTime <= 0f || Time.time < _attractAt)
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

            Vector2 next = Vector2.MoveTowards(position, target, _magnetSpeed * Time.deltaTime);
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
