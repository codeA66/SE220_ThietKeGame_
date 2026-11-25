using UnityEngine;

namespace DeadCells.VFX
{
    // A ribbon that climbs: it rises from a point, slowing as it goes and swaying side to side, leaving a tapered
    // streak behind it, then lets go and shrinks away. Used for the scarf-coloured streaks that lift off the body
    // when the player wakes up. Cleans itself up.
    public class RisingStreak : MonoBehaviour
    {
        private const float SwaySpeed = 13f;

        private TrailRibbon _trail;
        private float _speed;
        private float _life;
        private float _sway;
        private float _phase;
        private float _lag;
        private float _baseX;
        private float _age;
        private bool _released;

        /// <summary>
        /// Starts a streak at origin. Colours in sRGB; width is the head width in units; lag is how long the tail
        /// follows (seconds); speed is the climb speed (units/s); life is how long it climbs; sway is the side
        /// to side travel in units.
        /// </summary>
        public static void Spawn(Vector3 origin, SpriteRenderer sortingReference, Color32 head, Color32 tail,
            float width, float lag, float speed, float life, float sway, float phase)
        {
            var go = new GameObject("RisingStreak");
            go.transform.position = origin;
            var streak = go.AddComponent<RisingStreak>();
            streak._baseX = origin.x;
            streak._speed = speed;
            streak._life = life;
            streak._sway = sway;
            streak._phase = phase;
            streak._lag = lag;
            streak._trail = go.AddComponent<TrailRibbon>();
            streak._trail.Setup(head, tail, width, lag, sortingReference, unlit: true);
            streak._trail.Emitting = true;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_released)
                return;

            float progress = Mathf.Clamp01(_age / _life);
            // Quick off the mark, slowing as it climbs.
            float climb = _speed * (1f - 0.65f * progress) * Time.deltaTime;
            Vector3 position = transform.position;
            position.y += climb;
            position.x = _baseX + Mathf.Sin(_age * SwaySpeed + _phase) * _sway * Mathf.Sin(progress * Mathf.PI * 0.5f);
            transform.position = position;

            if (_age >= _life)
            {
                _released = true;
                _trail.Emitting = false;
                Destroy(gameObject, _lag + 0.2f);
            }
        }
    }
}
