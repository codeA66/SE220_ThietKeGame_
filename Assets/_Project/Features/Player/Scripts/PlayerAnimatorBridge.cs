using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Pushes gameplay state into the Animator. Holds no gameplay logic.
    /// </summary>
    public class PlayerAnimatorBridge : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int VelYHash = Animator.StringToHash("VelY");
        private static readonly int RollingHash = Animator.StringToHash("Rolling");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int ClimbingHash = Animator.StringToHash("Climbing");
        private static readonly int AirJumpHash = Animator.StringToHash("AirJump");

        [SerializeField] private Animator _animator;

        public void SetLocomotion(float speed, bool grounded, float velY)
        {
            _animator.SetFloat(SpeedHash, speed);
            _animator.SetBool(GroundedHash, grounded);
            _animator.SetFloat(VelYHash, velY);
        }

        public void SetRolling(bool rolling) => _animator.SetBool(RollingHash, rolling);

        public void SetAttack(int comboIndex) => _animator.SetInteger(AttackHash, comboIndex);

        public void SetClimbing(bool climbing) => _animator.SetBool(ClimbingHash, climbing);

        public void TriggerAirJump() => _animator.SetTrigger(AirJumpHash);

        /// <summary>
        /// Length in seconds of a clip in the controller, or 0 if it is missing.
        /// </summary>
        public float GetClipLength(string clipName)
        {
            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
                if (clip.name == clipName)
                    return clip.length;
            return 0f;
        }
    }
}
