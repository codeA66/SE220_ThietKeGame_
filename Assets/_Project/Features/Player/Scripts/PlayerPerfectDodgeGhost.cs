using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Visual for the perfect-dodge window: a translucent copy of the player's sprite stays where a roll
    /// started for PerfectDodgeDuration (DC: 0.25 s) and fades out. Hits on it are resolved by
    /// PlayerContext.TryRegisterPerfectDodge, which enemy attacks will call once enemies exist.
    /// </summary>
    public class PlayerPerfectDodgeGhost : MonoBehaviour
    {
        [SerializeField] private PlayerController _controller;
        [Tooltip("Player sprite renderer to copy.")]
        [SerializeField] private SpriteRenderer _source;
        [Tooltip("Ghost renderer (child object, same material as the player).")]
        [SerializeField] private SpriteRenderer _ghost;
        [Range(0f, 1f)]
        [SerializeField] private float _startAlpha = 0.45f;

        private PlayerContext _context;
        private float _timer;
        private float _duration;

        private void Start()
        {
            _context = _controller.Context;
            _context.RollStarted += OnRollStarted;
            _ghost.enabled = false;
            // Detach so the ghost stays in place while the player moves.
            _ghost.transform.SetParent(null, true);
        }

        private void OnDestroy()
        {
            if (_context != null)
                _context.RollStarted -= OnRollStarted;
            if (_ghost != null)
                Destroy(_ghost.gameObject);
        }

        private void OnRollStarted(Vector2 feet)
        {
            _duration = _context.Config.PerfectDodgeDuration;
            _timer = 0f;
            _ghost.transform.position = new Vector3(feet.x, feet.y, _source.transform.position.z);
            _ghost.sprite = _source.sprite;
            _ghost.flipX = _source.flipX;
            _ghost.enabled = true;
            SetAlpha(_startAlpha);
        }

        private void Update()
        {
            if (!_ghost.enabled)
                return;
            _timer += Time.deltaTime;
            if (_timer >= _duration)
            {
                _ghost.enabled = false;
                return;
            }
            SetAlpha(_startAlpha * (1f - _timer / _duration));
        }

        private void SetAlpha(float a)
        {
            var c = _ghost.color;
            c.a = a;
            _ghost.color = c;
        }
    }
}
