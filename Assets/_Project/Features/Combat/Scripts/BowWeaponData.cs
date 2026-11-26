using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace DeadCells.Combat
{
    /// <summary>Bow balance and release positions. Gameplay reads this asset without changing it.</summary>
    [CreateAssetMenu(fileName = "Bow_", menuName = "DeadCells/Combat/Bow")]
    public class BowWeaponData : ScriptableObject
    {
        [Serializable]
        public struct Shot
        {
            [FormerlySerializedAs("Damage"), SerializeField, Min(1)] private int _damage;
            [FormerlySerializedAs("ChargeTime"), SerializeField, Min(0.01f)] private float _chargeTime;
            [FormerlySerializedAs("RecoveryTime"), SerializeField, Min(0f)] private float _recoveryTime;
            [FormerlySerializedAs("Cooldown"), SerializeField, Min(0f)] private float _cooldown;
            [FormerlySerializedAs("AnimationSpeed"), SerializeField, Min(0.01f)] private float _animationSpeed;

            public Shot(int damage, float chargeTime, float recoveryTime = 0f, float cooldown = 0f, float animationSpeed = 1f)
            {
                _damage = damage;
                _chargeTime = chargeTime;
                _recoveryTime = recoveryTime;
                _cooldown = cooldown;
                _animationSpeed = animationSpeed;
            }

            public int Damage => _damage;
            public float ChargeTime => _chargeTime;
            public float RecoveryTime => _recoveryTime;
            public float Cooldown => _cooldown;
            public float AnimationSpeed => _animationSpeed;
        }

        [SerializeField] private CombatStat _scalingStat;
        [SerializeField, Min(1)] private int _ammoCapacity;
        [SerializeField, Min(0.01f)] private float _projectileSpeed;
        [SerializeField, Min(0.01f)] private float _flightLifetime;
        [SerializeField, Min(0.01f)] private float _embeddedLifetime;
        [SerializeField, Min(0.01f)] private float _comboResetTime;
        [SerializeField] private Vector2 _standingMuzzleOffset;
        [SerializeField] private Vector2 _crouchingMuzzleOffset;
        [SerializeField] private Shot[] _shots;

        public CombatStat ScalingStat => _scalingStat;
        public int AmmoCapacity => _ammoCapacity;
        public float ProjectileSpeed => _projectileSpeed;
        public float FlightLifetime => _flightLifetime;
        public float EmbeddedLifetime => _embeddedLifetime;
        public float ComboResetTime => _comboResetTime;
        public int ShotCount => _shots.Length;

        public Shot GetShot(int index) => _shots[index];

        public Vector2 GetMuzzleOffset(bool crouching) =>
            crouching ? _crouchingMuzzleOffset : _standingMuzzleOffset;

        public bool IsValid => _ammoCapacity > 0 && _projectileSpeed > 0f && _flightLifetime > 0f
            && _embeddedLifetime > 0f && _comboResetTime > 0f && _shots != null && _shots.Length > 0
            && Array.TrueForAll(_shots, shot => shot.Damage > 0 && shot.ChargeTime > 0f
                && shot.RecoveryTime >= 0f && shot.Cooldown >= 0f && shot.AnimationSpeed > 0f);
    }
}
