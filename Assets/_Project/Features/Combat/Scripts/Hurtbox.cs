using UnityEngine;

namespace DeadCells.Combat
{
    /// <summary>
    /// Marks a collider as hittable and points it at the Health that takes the damage.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] private Health _health;

        public IDamageable Target => _health;
    }
}
