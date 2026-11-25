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
        // Kept serialized for existing scene wiring (MapWiringTests.A17); owned by EnemyController.
#pragma warning disable CS0169
        [SerializeField] private FloatingDamageNumber _numberPrefab;
        [SerializeField] private Transform _fill;
#pragma warning restore CS0169
        [SerializeField] private GameObject _bar;

        private void Awake()
        {
            if (_bar != null)
                _bar.SetActive(false);
        }

        private void OnEnable()
        {
            if (_target != null)
            {
                _target.Damaged += OnHit;
                _target.Died += OnDefeated;
            }
        }

        private void OnDisable()
        {
            if (_target != null)
            {
                _target.Damaged -= OnHit;
                _target.Died -= OnDefeated;
            }
        }

        private void OnHit(DamageInfo damage)
        {
            PlayContact(damage, _impactBlood, _hitSound);
        }

        private void OnDefeated(DamageInfo damage)
        {
            PlayContact(damage, _defeatBlood, _fatalSound);
            if (_bar != null)
                _bar.SetActive(false);
        }

        private void PlayContact(DamageInfo damage, VfxDefinition blood, AudioClip sound)
        {
            if (_vfx != null && _target != null && _body != null)
            {
                Vector3 position = _target.transform.position + Vector3.up * _body.offset.y;
                // The baked blood sprites are drawn red; enemy blood is blue, so play a recoloured copy.
                _vfx.Play(VfxRecolor.Blue(blood), position, damage.Direction < 0);
                _vfx.Play(_impactFlash, position);
            }

            if (_audio != null && sound != null)
                _audio.PlayOneShot(sound, 0.75f);
        }
    }
}
