using DeadCells.CameraSystem;
using DeadCells.Combat;
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

        [Header("On Damage Taken")]
        [SerializeField] private float _hurtShakeAmplitude = 0.2f;
        [SerializeField] private float _hurtShakeDuration = 0.2f;
        [SerializeField] private float _flashDuration = 0.1f;

        private MaterialPropertyBlock _block;
        private float _flashUntil = -1f;
        private PlayerCombat _combat;
        private PlayerBow _bow;
        private Health _health;

        private void Start()
        {
            _block = new MaterialPropertyBlock();
            _combat = _controller.Context.Combat;
            _bow = _controller.Context.Bow;
            _health = _controller.Health;
            _combat.StrikeHit += OnStrikeHit;
            _bow.HitDealt += OnArrowHit;
            _health.Damaged += OnDamageTaken;
            _health.Died += OnDamageTaken;
        }

        private void OnDestroy()
        {
            if (_combat != null)
                _combat.StrikeHit -= OnStrikeHit;
            if (_bow != null)
                _bow.HitDealt -= OnArrowHit;
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
        }

        private void OnArrowHit(DamageInfo damage) => OnStrikeHit(1, damage.IsCritical);

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
