using DeadCells.Combat;
using DeadCells.VFX;
using UnityEngine;

namespace DeadCells.HUD
{
    public sealed class EnemyImpactFeedback : MonoBehaviour
    {
        [SerializeField] private Health _target;
        [SerializeField] private BoxCollider2D _body;
        [SerializeField] private VfxSpawner _vfx;
        [SerializeField] private VfxDefinition _impactBlood;
        [SerializeField] private VfxDefinition _defeatBlood;
        [SerializeField] private VfxDefinition _impactFlash;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _fatalSound;
        [SerializeField] private FloatingDamageNumber _numberPrefab;
        [SerializeField] private GameObject _bar;
        [SerializeField] private Transform _fill;

        private float _hideBarAt;

        private void OnEnable()
        {
            _target.Damaged += OnHit;
            _target.Died += OnDefeated;
        }

        private void OnDisable()
        {
            if (_target != null)
            {
                _target.Damaged -= OnHit;
                _target.Died -= OnDefeated;
            }
        }

        private void LateUpdate()
        {
            if (!_bar.activeSelf)
                return;

            if (_target == null || !_target.IsAlive || Time.time >= _hideBarAt)
                _bar.SetActive(false);
            else
                _bar.transform.position = BarPosition();
        }

        private void OnHit(DamageInfo damage)
        {
            PlayContact(damage, _impactBlood, _hitSound);
            _fill.localScale = new Vector3(0.65f * _target.Current / _target.Max, 0.065f, 1f);
            _hideBarAt = Time.time + 1.1f;
            _bar.transform.position = BarPosition();
            _bar.SetActive(true);
        }

        private void OnDefeated(DamageInfo damage)
        {
            PlayContact(damage, _defeatBlood, _fatalSound);
            _bar.SetActive(false);
        }

        private Vector3 BarPosition() => _target.transform.position + Vector3.up * (_body.offset.y + _body.size.y / 2f + 0.19f);

        private void PlayContact(DamageInfo damage, VfxDefinition blood, AudioClip sound)
        {
            Vector3 position = _target.transform.position + Vector3.up * _body.offset.y;
            _vfx.Play(blood, position, damage.Direction < 0);
            _vfx.Play(_impactFlash, position);
            _audio.PlayOneShot(sound, 0.75f);
            var number = Instantiate(_numberPrefab, transform);
            number.Show(BarPosition() + new Vector3(0.45f, -0.20f), damage.Amount, damage.IsCritical);
        }
    }
}
