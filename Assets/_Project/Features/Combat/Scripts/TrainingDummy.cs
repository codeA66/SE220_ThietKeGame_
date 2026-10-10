using DeadCells.Core;
using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Test target: logs every hit, tints briefly, and refills its health when "killed".
    /// </summary>
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color _hitTint = new Color(1f, 0.5f, 0.5f);
        [SerializeField] private float _tintDuration = 0.1f;

        private Color _baseColor;
        private float _tintUntil = -1f;

        public int LastDamage { get; private set; }
        public int HitCount { get; private set; }

        private void Awake() => _baseColor = _sprite.color;

        private void OnEnable()
        {
            _health.Damaged += OnHit;
            _health.Died += OnKilled;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnHit;
            _health.Died -= OnKilled;
        }

        private void Update()
        {
            if (_tintUntil >= 0f && Time.time >= _tintUntil)
            {
                _tintUntil = -1f;
                _sprite.color = _baseColor;
            }
        }

        private void OnHit(DamageInfo info)
        {
            LastDamage = info.Amount;
            HitCount++;
            _sprite.color = _hitTint;
            _tintUntil = Time.time + _tintDuration;
            GameDebug.Log($"{name} took {info.Amount} (stun {info.Stun}s) -> {_health.Current}/{_health.Max}", LogTopic.Combat);
        }

        private void OnKilled(DamageInfo info)
        {
            OnHit(info);
            _health.RestoreFull();
        }
    }
}
