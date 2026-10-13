using System;
using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>Bow balance and release positions. Gameplay reads this asset without changing it.</summary>
    [CreateAssetMenu(fileName = "Bow_", menuName = "DeadCells/Combat/Bow")]
    public class BowWeaponData : ScriptableObject
    {
        [Serializable]
        public struct Shot
        {
            [Min(1)] public int Damage;
            [Min(0.01f)] public float ChargeTime;
            [Min(0f)] public float RecoveryTime;
            [Min(0f)] public float Cooldown;
            [Min(0.01f)] public float AnimationSpeed;
        }

        [SerializeField, Min(1)] private int _ammoCapacity;
        [SerializeField, Min(0.01f)] private float _projectileSpeed;
        [SerializeField, Min(0.01f)] private float _flightLifetime;
        [SerializeField, Min(0.01f)] private float _embeddedLifetime;
        [SerializeField, Min(0.01f)] private float _comboResetTime;
        [SerializeField] private Vector2 _standingMuzzleOffset;
        [SerializeField] private Vector2 _crouchingMuzzleOffset;
        [SerializeField] private Shot[] _shots;

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
