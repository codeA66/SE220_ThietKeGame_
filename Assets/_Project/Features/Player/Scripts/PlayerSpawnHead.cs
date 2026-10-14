using DeadCells.CameraSystem;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Spawn intro head: drops spinning from above, squashes on impact, hops toward the lying body's
    /// neck leaving slime, and on the last hop merges with a flash, a particle burst and a camera shake,
    /// then sets PlayerContext.SpawnHeadArrived. Motion is scripted (no Rigidbody).
    /// All tuning values are by feel; no Dead Cells numbers were found for this sequence.
    /// </summary>
    public class PlayerSpawnHead : MonoBehaviour
    {
        private enum Phase { Idle, WaitingForBody, Falling, Settling, WindUp, Hopping, Pausing }

        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private LayerMask _groundMask;

        [Header("Effects")]
        [SerializeField] private VfxSpawner _vfx;
        [SerializeField] private VfxDefinition _slimeVfx;
        [Tooltip("Sparks emitted while the head moves (child particle system, world simulation space).")]
        [SerializeField] private ParticleSystem _trail;
        [Tooltip("Burst played where the head lands and merges.")]
        [SerializeField] private ParticleSystem _burst;
        [SerializeField] private SpriteRenderer _bodySprite;
        [SerializeField] private CameraFollow2D _camera;
        [SerializeField] private int _landBurstCount = 6;
        [SerializeField] private int _mergeBurstCount = 24;
        [SerializeField] private float _mergeFlashDuration = 0.15f;
        [SerializeField] private float _mergeShakeAmplitude = 0.15f;
        [SerializeField] private float _mergeShakeDuration = 0.2f;

        [Header("Path")]
        [Tooltip("Neck of the lying body relative to the player's feet, for a body facing right (units).")]
        [SerializeField] private Vector2 _neckOffset = new Vector2(0.24f, 0.1f);
        [Tooltip("Horizontal distance from the neck where the head lands (units).")]
        [SerializeField] private float _crawlDistance = 2.5f;
        [Tooltip("Height above the floor where the head starts falling (units).")]
        [SerializeField] private float _dropHeight = 5f;
        [Tooltip("Height of the head's centre above the ground while resting (units).")]
        [SerializeField] private float _restHeight = 0.22f;

        [Header("Fall")]
        [SerializeField] private float _gravity = 40f;
        [Tooltip("Degrees per second while falling.")]
        [SerializeField] private float _fallSpin = 540f;
        [Tooltip("Fraction of the impact speed kept on each bounce.")]
        [SerializeField] private float _bounce = 0.3f;
        [SerializeField] private float _minBounceSpeed = 3f;
        [SerializeField] private float _settleTime = 0.35f;

        [Header("Hops")]
        [SerializeField] private float _hopDistance = 0.7f;
        [SerializeField] private float _hopHeight = 0.35f;
        [SerializeField] private float _hopTime = 0.26f;
        [SerializeField] private float _windUpTime = 0.12f;
        [Tooltip("Rest on the ground between hops (seconds).")]
        [SerializeField] private float _hopPause = 0.1f;
        [Tooltip("The last hop onto the neck is this much higher.")]
        [SerializeField] private float _finalHopHeightMultiplier = 2f;
        [SerializeField] private float _hopTilt = 20f;
        [SerializeField] private float _baseScale = 1.3f;

        private PlayerContext _context;
        private MaterialPropertyBlock _block;
        private Phase _phase;
        private Vector2 _position;
        private float _velocityY;
        private float _groundY;
        private float _targetX;
        private int _direction;
        private float _timer;
        private float _angle;
        private Vector2 _hopFrom;
        private Vector2 _hopTo;
        private float _hopPeak;
        private bool _finalHop;
        private float _flashTimer = -1f;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _renderer.enabled = false;
            SetTrail(false);
        }

        /// <summary>Starts the sequence; the head drops once the body has settled on the ground.</summary>
        public void Play(PlayerContext context)
        {
            _context = context;
            _phase = Phase.WaitingForBody;
        }

        private void Begin()
        {
            // The head hops along the body's floor; it drops on the side whose column is clear down to it.
            _groundY = _context.Position.y + _restHeight;
            _targetX = _context.Position.x + _neckOffset.x * _context.FacingDirection;
            _direction = IsColumnClear(-_context.FacingDirection) ? _context.FacingDirection : -_context.FacingDirection;
            _position = new Vector2(_targetX - _direction * _crawlDistance, _groundY + _dropHeight);

            _velocityY = 0f;
            _angle = 0f;
            _renderer.flipX = _direction < 0;
            _renderer.enabled = true;
            SetTrail(true);
            _phase = Phase.Falling;
            Apply(Vector2.one);
        }

        private bool IsColumnClear(int side)
        {
            var top = new Vector2(_targetX + side * _crawlDistance, _groundY + _dropHeight);
            return Physics2D.Raycast(top, Vector2.down, _dropHeight - _restHeight, _groundMask).collider == null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            TickBodyFlash(dt);
            switch (_phase)
            {
                case Phase.WaitingForBody:
                    if (_context.IsGrounded)
                        Begin();
                    break;
                case Phase.Falling: TickFall(dt); break;
                case Phase.Settling: TickSettle(dt); break;
                case Phase.WindUp: TickWindUp(dt); break;
                case Phase.Hopping: TickHop(dt); break;
                case Phase.Pausing: TickPause(dt); break;
            }
        }

        private void TickFall(float dt)
        {
            _velocityY -= _gravity * dt;
            _position.y += _velocityY * dt;
            _angle += _fallSpin * _direction * -1f * dt;
            if (_position.y > _groundY)
            {
                Apply(new Vector2(0.9f, 1.15f));
                return;
            }

            _position.y = _groundY;
            float impact = -_velocityY;
            Land(_landBurstCount);
            if (impact > _minBounceSpeed)
            {
                _velocityY = impact * _bounce;
                _angle *= 0.5f;
                return;
            }

            _velocityY = 0f;
            _timer = 0f;
            _phase = Phase.Settling;
        }

        private void TickSettle(float dt)
        {
            _timer += dt;
            float t = Mathf.Clamp01(_timer / _settleTime);
            _angle = Mathf.LerpAngle(_angle, 0f, t);
            Apply(Squash(1f - t, 0.4f));
            if (t >= 1f)
                StartWindUp();
        }

        private void StartWindUp()
        {
            float remaining = Mathf.Abs(_targetX - _position.x);
            _finalHop = remaining <= _hopDistance * 1.5f;
            _timer = 0f;
            _phase = Phase.WindUp;
        }

        private void TickWindUp(float dt)
        {
            _timer += dt;
            float t = Mathf.Clamp01(_timer / _windUpTime);
            _angle = Mathf.Lerp(0f, _hopTilt * -_direction * 0.5f, t);
            Apply(Squash(t, 0.25f));
            if (t < 1f)
                return;

            _hopFrom = _position;
            float x = _finalHop ? _targetX : Mathf.MoveTowards(_position.x, _targetX, _hopDistance);
            _hopTo = new Vector2(x, _groundY);
            _hopPeak = _hopHeight * (_finalHop ? _finalHopHeightMultiplier : 1f);
            _timer = 0f;
            _phase = Phase.Hopping;
        }

        private void TickHop(float dt)
        {
            _timer += dt;
            float t = Mathf.Clamp01(_timer / _hopTime);
            _position.x = Mathf.Lerp(_hopFrom.x, _hopTo.x, t);
            _position.y = _groundY + 4f * _hopPeak * t * (1f - t);
            _angle = Mathf.Lerp(_hopTilt * -_direction, _hopTilt * _direction, t);
            float stretch = 1f - Mathf.Abs(t - 0.5f) * 2f;
            Apply(new Vector2(1f - 0.15f * stretch, 1f + 0.2f * stretch));
            if (t < 1f)
                return;

            _position = _hopTo;
            if (_finalHop)
            {
                Merge();
                return;
            }

            Land(_landBurstCount / 2);
            _timer = 0f;
            _phase = Phase.Pausing;
        }

        private void TickPause(float dt)
        {
            _timer += dt;
            float t = Mathf.Clamp01(_timer / _hopPause);
            _angle = Mathf.LerpAngle(_angle, 0f, t);
            Apply(Squash(1f - t, 0.35f));
            if (t >= 1f)
                StartWindUp();
        }

        private void Land(int sparks)
        {
            if (_vfx != null)
                _vfx.Play(_slimeVfx, new Vector2(_position.x, _groundY - _restHeight), _direction < 0);
            Burst(sparks);
        }

        private void Merge()
        {
            _phase = Phase.Idle;
            _renderer.enabled = false;
            SetTrail(false);
            Burst(_mergeBurstCount);
            if (_camera != null)
                _camera.Shake(_mergeShakeAmplitude, _mergeShakeDuration);
            _flashTimer = _mergeFlashDuration;
            SetBodyFlash(1f);
            _context.SpawnHeadArrived = true;
        }

        private void TickBodyFlash(float dt)
        {
            if (_flashTimer < 0f)
                return;
            _flashTimer -= dt;
            SetBodyFlash(Mathf.Clamp01(_flashTimer / _mergeFlashDuration));
            if (_flashTimer <= 0f)
            {
                _flashTimer = -1f;
                SetBodyFlash(0f);
            }
        }

        private void SetBodyFlash(float amount)
        {
            if (_bodySprite == null)
                return;
            _bodySprite.GetPropertyBlock(_block);
            _block.SetFloat(FlashId, amount);
            _bodySprite.SetPropertyBlock(_block);
        }

        private void Burst(int count)
        {
            if (_burst == null || count <= 0)
                return;
            _burst.transform.position = new Vector3(_position.x, _position.y, 0f);
            _burst.Emit(count);
        }

        private void SetTrail(bool on)
        {
            if (_trail == null)
                return;
            if (on)
                _trail.Play();
            else
                _trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>Squash amount 0..1 maps to wide-and-flat by the given strength.</summary>
        private static Vector2 Squash(float amount, float strength) =>
            new Vector2(1f + strength * amount, 1f - strength * amount);

        private void Apply(Vector2 scale)
        {
            transform.SetPositionAndRotation(_position, Quaternion.Euler(0f, 0f, _angle));
            transform.localScale = new Vector3(scale.x * _baseScale, scale.y * _baseScale, 1f);
        }
    }
}
