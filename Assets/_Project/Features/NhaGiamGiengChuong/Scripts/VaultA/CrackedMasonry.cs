using DeadCells.Combat;
using DeadCells.Core.Audio;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Breakable weathered wall section backed by a 1-HP Health + Hurtbox on the Enemy layer.
    /// When struck, hides the intact masonry, plays debris feedback, reveals the dark fissure,
    /// and unlocks the linked PassageTransition.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrackedMasonry : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Collider2D _hurtboxCollider;
        [SerializeField] private GameObject _intactVisual;
        [SerializeField] private GameObject _fissureVisual;
        [SerializeField] private PassageTransition _entryPassage;
        [SerializeField] private ParticleSystem _breakParticles;

        private bool _isBroken;
        private bool _subscribed;

        public bool IsBroken => _isBroken;
        public Health WallHealth => _health;
        public Collider2D HurtboxCollider => _hurtboxCollider;
        public PassageTransition EntryPassage => _entryPassage;

        public void Configure(
            Health health,
            Collider2D hurtboxCollider,
            GameObject intactVisual,
            GameObject fissureVisual,
            PassageTransition entryPassage,
            ParticleSystem breakParticles = null)
        {
            Unsubscribe();
            _health = health;
            _hurtboxCollider = hurtboxCollider;
            _intactVisual = intactVisual;
            _fissureVisual = fissureVisual;
            _entryPassage = entryPassage;
            _breakParticles = breakParticles;
            _isBroken = false;
            ApplyVisualState();
            Subscribe();
        }

        private void Awake()
        {
            ApplyVisualState();
            Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _health == null)
                return;
            _health.Died += OnWallBroken;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _health == null)
                return;
            _health.Died -= OnWallBroken;
            _subscribed = false;
        }

        private void OnWallBroken(DamageInfo info)
        {
            if (_isBroken)
                return;
            _isBroken = true;
            ApplyVisualState();
            CombatAudioHub.Play(SfxId.StompImpact, 0.78f);
            if (_breakParticles != null)
                _breakParticles.Play();
            if (_entryPassage != null)
                _entryPassage.Unlock();
        }

        private void ApplyVisualState()
        {
            if (_intactVisual != null)
                _intactVisual.SetActive(!_isBroken);
            if (_fissureVisual != null)
                _fissureVisual.SetActive(_isBroken);
            if (_hurtboxCollider != null)
                _hurtboxCollider.enabled = !_isBroken;
        }
    }
}
