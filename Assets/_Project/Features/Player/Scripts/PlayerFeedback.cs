using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
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

        private const float LightHitStop = 0.045f;
        private const float CritHitStop = 0.075f;
        private const float KillHitStop = 0.095f;
        private const float ArrowHitStop = 0.035f;
        private const float ArrowCritHitStop = 0.065f;
        private const float HurtHitStop = 0.060f;
        private const float StaggerHitStop = 0.055f;
        private const float PerfectDodgeHitStop = 0.055f;

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
            _combat = _context.Combat;
            _bow = _context.Bow;
            _health = _controller.Health;
            _combat.ImpactDealt += OnImpactDealt;
            _bow.HitDealt += OnArrowHit;
            _health.Damaged += OnDamageTaken;
            _health.Died += OnDamageTaken;
            _controller.Staggered += OnStaggered;
            _context.PerfectDodged += OnPerfectDodged;
        }

        private void OnDestroy()
        {
            if (_combat != null)
                _combat.ImpactDealt -= OnImpactDealt;
            if (_bow != null)
                _bow.HitDealt -= OnArrowHit;
            if (_health != null)
            {
                _health.Damaged -= OnDamageTaken;
                _health.Died -= OnDamageTaken;
            }
            if (_controller != null)
                _controller.Staggered -= OnStaggered;
            if (_context != null)
                _context.PerfectDodged -= OnPerfectDodged;
        }

        private void Update()
        {
            if (_flashUntil >= 0f && Time.unscaledTime >= _flashUntil)
            {
                _flashUntil = -1f;
                SetFlash(0f);
            }
        }

        private void OnImpactDealt(int hits, bool critical, bool fatal, int direction)
        {
            float hitStop = fatal ? KillHitStop : critical ? CritHitStop : LightHitStop;
            float punchAmp = fatal ? 0.26f : critical ? 0.22f : 0.12f;
            float punchDur = fatal ? 0.13f : critical ? 0.11f : 0.08f;
            float shakeAmp = fatal ? 0.18f : critical ? 0.14f : 0.06f;
            float shakeDur = fatal ? 0.14f : critical ? 0.12f : 0.08f;
            Vector2 punchDir = direction != 0 ? new Vector2(direction, 0f) : Vector2.down;

            TriggerCameraAndStop(hitStop, punchDir, punchAmp, punchDur, shakeAmp, shakeDur);
        }

        private void OnArrowHit(DamageInfo damage)
        {
            float hitStop = damage.IsCritical ? ArrowCritHitStop : ArrowHitStop;
            int dir = damage.Direction != 0 ? damage.Direction : (_context != null ? _context.FacingDirection : 1);
            TriggerCameraAndStop(hitStop, new Vector2(dir, 0f), 0.10f, 0.07f, 0f, 0f);
            CombatAudioHub.Play(SfxId.ArrowHit, 0.75f);
        }

        private void OnDamageTaken(DamageInfo info)
        {
            if (_hitStop != null)
                _hitStop.Trigger(HurtHitStop);
            if (_camera != null)
                _camera.Shake(0.24f, 0.18f);
            TriggerFlash(0.10f);
            bool dead = _health != null && !_health.IsAlive;
            CombatAudioHub.Play(dead ? SfxId.PlayerDeath : SfxId.PlayerHurt, 0.85f);
        }

        private void OnStaggered(int direction)
        {
            Vector2 punchDir = direction != 0 ? new Vector2(direction, 0f) : Vector2.left;
            TriggerCameraAndStop(StaggerHitStop, punchDir, 0.18f, 0.10f, 0.10f, 0.10f);
            TriggerFlash(0.08f);
            CombatAudioHub.Play(SfxId.ShieldClang, 0.85f);
        }

        private void OnPerfectDodged()
        {
            if (_hitStop != null)
                _hitStop.Trigger(PerfectDodgeHitStop);
            TriggerFlash(0.06f);
        }

        private void TriggerCameraAndStop(float hitStop, Vector2 punchDir, float punchAmp, float punchDur, float shakeAmp, float shakeDur)
        {
            if (_hitStop != null)
                _hitStop.Trigger(hitStop);
            if (_camera != null)
            {
                if (punchAmp > 0f && punchDur > 0f)
                    _camera.Punch(punchDir, punchAmp, punchDur);
                if (shakeAmp > 0f && shakeDur > 0f)
                    _camera.Shake(shakeAmp, shakeDur);
            }
        }

        private void TriggerFlash(float duration)
        {
            SetFlash(1f);
            _flashUntil = Time.unscaledTime + duration;
        }

        private void SetFlash(float amount)
        {
            if (_sprite == null)
                return;
            _block ??= new MaterialPropertyBlock();
            _sprite.GetPropertyBlock(_block);
            _block.SetFloat(FlashId, amount);
            _sprite.SetPropertyBlock(_block);
        }
    }
}
