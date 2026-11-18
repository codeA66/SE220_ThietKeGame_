using UnityEngine;

namespace DeadCells.Player
{
    // Dead Cells-style motion trail: while a roll or a stomp dive lasts, copies of the player's current sprite are
    // dropped behind at a fixed interval and fade out. Pooled; ghosts share the player's material so
    // the palette swap still applies, and a purple tint keeps them apart from the real sprite.
    public class PlayerAfterimages : MonoBehaviour
    {
        private const int PoolSize = 10;
        private const float Interval = 0.035f;
        private const float Lifetime = 0.22f;
        private const float MaxStompTrail = 1.5f;

        // A lavender tint, in the family of the scarf's mauve, multiplied over the player's own colours.
        private static readonly Color GhostColor = new Color(0.78f, 0.6f, 1f, 0.55f);

        private PlayerContext _context;
        private SpriteRenderer _source;
        private SpriteRenderer[] _ghosts;
        private float[] _ages;
        private int _next;
        private float _emitUntil = -1f;
        private float _nextEmitAt;
        private int _activeCount;

        public void Setup(PlayerContext context)
        {
            _context = context;
            _source = context.Sprite;
            _ghosts = new SpriteRenderer[PoolSize];
            _ages = new float[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Afterimage");
                var ghost = go.AddComponent<SpriteRenderer>();
                ghost.sharedMaterial = _source.sharedMaterial;
                ghost.sortingLayerID = _source.sortingLayerID;
                ghost.sortingOrder = _source.sortingOrder - 1;
                ghost.enabled = false;
                _ghosts[i] = ghost;
            }
            _context.RollStarted += OnRollStarted;
            _context.StompStarted += OnStompStarted;
            _context.Stomped += OnStomped;
            _context.StompEnded += OnStompEnded;
        }

        private void OnDestroy()
        {
            if (_context != null)
            {
                _context.RollStarted -= OnRollStarted;
                _context.StompStarted -= OnStompStarted;
                _context.Stomped -= OnStomped;
                _context.StompEnded -= OnStompEnded;
            }
            if (_ghosts == null)
                return;
            foreach (SpriteRenderer ghost in _ghosts)
            {
                if (ghost != null)
                    Destroy(ghost.gameObject);
            }
        }

        private void Update()
        {
            if (_ghosts == null)
                return;

            if (Time.time < _emitUntil && Time.time >= _nextEmitAt)
            {
                Emit();
                _nextEmitAt = Time.time + Interval;
            }

            if (_activeCount == 0)
                return;
            for (int i = 0; i < PoolSize; i++)
            {
                SpriteRenderer ghost = _ghosts[i];
                if (!ghost.enabled)
                    continue;
                _ages[i] += Time.deltaTime;
                if (_ages[i] >= Lifetime)
                {
                    ghost.enabled = false;
                    _activeCount--;
                    continue;
                }
                Color color = GhostColor;
                color.a *= 1f - _ages[i] / Lifetime;
                ghost.color = color;
            }
        }

        private void OnRollStarted(Vector2 feet)
        {
            _emitUntil = Time.time + _context.Config.RollDuration;
            _nextEmitAt = Time.time;
        }

        // The dive trails until it lands; the cap only matters if a stomp is cut short without landing.
        private void OnStompStarted()
        {
            _emitUntil = Time.time + MaxStompTrail;
            _nextEmitAt = Time.time;
        }

        private void OnStomped(Vector2 feet) => _emitUntil = -1f;

        // A stomp cut short never lands, so this is what stops the trail.
        private void OnStompEnded() => _emitUntil = -1f;

        private void Emit()
        {
            SpriteRenderer ghost = _ghosts[_next];
            if (!ghost.enabled)
                _activeCount++;
            _ages[_next] = 0f;
            _next = (_next + 1) % PoolSize;

            Transform source = _source.transform;
            ghost.transform.SetPositionAndRotation(source.position, source.rotation);
            ghost.transform.localScale = source.lossyScale;
            ghost.sprite = _source.sprite;
            ghost.flipX = _source.flipX;
            ghost.color = GhostColor;
            ghost.enabled = true;
        }
    }
}
