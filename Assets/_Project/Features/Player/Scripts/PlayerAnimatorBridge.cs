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
        private static readonly int RollAnimSpeedHash = Animator.StringToHash("RollAnimSpeed");
        private static readonly int OnChainHash = Animator.StringToHash("OnChain");
        private static readonly int ClimbAnimSpeedHash = Animator.StringToHash("ClimbAnimSpeed");
        private static readonly int CrouchingHash = Animator.StringToHash("Crouching");
        private static readonly int AttackAnimSpeedHash = Animator.StringToHash("AttackAnimSpeed");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

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

        public void SetRollAnimSpeed(float speed) => _animator.SetFloat(RollAnimSpeedHash, speed);

        public void SetClimbingChain(bool onChain) => _animator.SetBool(OnChainHash, onChain);

        public void SetClimbAnimSpeed(float speed) => _animator.SetFloat(ClimbAnimSpeedHash, speed);

        public void SetCrouching(bool crouching) => _animator.SetBool(CrouchingHash, crouching);

        public void SetAttackAnimSpeed(float speed) => _animator.SetFloat(AttackAnimSpeedHash, speed);

        public void SetHurt(bool hurt) => _animator.SetBool(HurtHash, hurt);

        public void SetDead(bool dead) => _animator.SetBool(DeadHash, dead);

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
