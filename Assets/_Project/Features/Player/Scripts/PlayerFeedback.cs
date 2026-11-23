using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Game feel for combat: hit-stop and a small shake when the player's attacks connect, a white flash
    /// and a bigger shake when the player is hit. Values are tuned by feel (no Dead Cells numbers found).
    /// </summary>
    public class PlayerFeedback : MonoBehaviour
    {
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        [SerializeField] private PlayerController _controller;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private HitStop _hitStop;
        [SerializeField] private CameraFollow2D _camera;

        [Header("On Hit Dealt")]
        [SerializeField] private float _hitStopDuration = 0.05f;
        [SerializeField] private float _hitShakeAmplitude = 0.08f;
        [SerializeField] private float _hitShakeDuration = 0.1f;

        [Header("On Critical Hit (DC: one-frame freeze, then a slowdown of a few tenths of a second)")]
        [SerializeField] private float _critFreeze = 0.035f;
        [SerializeField] private float _critSlowDuration = 0.25f;
        [Range(0.05f, 1f)]
        [SerializeField] private float _critSlowScale = 0.35f;
        [SerializeField] private float _critShakeAmplitude = 0.16f;
        [SerializeField] private float _critShakeDuration = 0.15f;

        [Header("On Bow Finisher Release")]
        [SerializeField] private float _finisherShakeAmplitude = 0.07f;
        [SerializeField] private float _finisherShakeDuration = 0.08f;

        [Header("On Kill")]
        [SerializeField] private float _killHitStop = 0.09f;
        [SerializeField] private float _killShakeAmplitude = 0.14f;
        [SerializeField] private float _killShakeDuration = 0.15f;

        [Header("On Stomp Landing")]
        [SerializeField] private float _stompHitStop = 0.06f;
        [SerializeField] private float _stompShakeAmplitude = 0.35f;
        [SerializeField] private float _stompShakeDuration = 0.3f;

        [Header("On Damage Taken")]
        [SerializeField] private float _hurtShakeAmplitude = 0.2f;
        [SerializeField] private float _hurtShakeDuration = 0.2f;
        [SerializeField] private float _flashDuration = 0.1f;

        private MaterialPropertyBlock _block;
        private float _flashUntil = -1f;
        private PlayerContext _context;
        private PlayerCombat _combat;
        private PlayerBow _bow;
        private Health _health;

        private void Start()
        {
            _block = new MaterialPropertyBlock();
            _context = _controller.Context;
            _context.Stomped += OnStomped;
            KillCounter.Changed += OnKill;
            _combat = _controller.Context.Combat;
            _bow = _controller.Context.Bow;
            _health = _controller.Health;
            _combat.StrikeHit += OnStrikeHit;
            _bow.HitDealt += OnArrowHit;
            _bow.Fired += OnBowFired;
            _health.Damaged += OnDamageTaken;
            _health.Died += OnDamageTaken;
        }

        private void OnDestroy()
        {
            KillCounter.Changed -= OnKill;
            if (_context != null)
                _context.Stomped -= OnStomped;
            if (_combat != null)
                _combat.StrikeHit -= OnStrikeHit;
            if (_bow != null)
            {
                _bow.HitDealt -= OnArrowHit;
                _bow.Fired -= OnBowFired;
            }
            if (_health != null)
            {
                _health.Damaged -= OnDamageTaken;
                _health.Died -= OnDamageTaken;
            }
        }

        private void Update()
        {
            if (_flashUntil >= 0f && Time.unscaledTime >= _flashUntil)
            {
                _flashUntil = -1f;
                SetFlash(0f);
            }
        }

        private void OnStrikeHit(int hits, bool critical)
        {
            if (_hitStop != null)
                _hitStop.Trigger(_hitStopDuration);
            if (_camera != null)
                _camera.Shake(_hitShakeAmplitude, _hitShakeDuration);

            if (!critical)
                return;
            if (_hitStop != null)
            {
                _hitStop.Trigger(_critFreeze);
                _hitStop.Slow(Mathf.Max(_critFreeze, _hitStopDuration) + _critSlowDuration, _critSlowScale);
            }
            if (_camera != null)
                _camera.Shake(_critShakeAmplitude, _critShakeDuration);
        }

        private void OnKill(int kills)
        {
            if (_hitStop != null)
                _hitStop.Trigger(_killHitStop);
            if (_camera != null)
                _camera.Shake(_killShakeAmplitude, _killShakeDuration);
        }

        private void OnStomped(Vector2 feet)
        {
            if (_hitStop != null)
                _hitStop.Trigger(_stompHitStop);
            if (_camera != null)
                _camera.Shake(_stompShakeAmplitude, _stompShakeDuration);
        }

        private void OnArrowHit(DamageInfo damage) => OnStrikeHit(1, damage.IsCritical);

        // A small kick when the heavy third shot of the bow combo leaves the string.
        private void OnBowFired(Vector2 muzzle, int facing, bool heavy)
        {
            if (heavy && _camera != null)
                _camera.Shake(_finisherShakeAmplitude, _finisherShakeDuration);
        }

        private void OnDamageTaken(DamageInfo info)
        {
            if (_camera != null)
                _camera.Shake(_hurtShakeAmplitude, _hurtShakeDuration);
            SetFlash(1f);
            _flashUntil = Time.unscaledTime + _flashDuration;
        }

        private void SetFlash(float amount)
        {
            _sprite.GetPropertyBlock(_block);
            _block.SetFloat(FlashId, amount);
            _sprite.SetPropertyBlock(_block);
        }
    }
}
