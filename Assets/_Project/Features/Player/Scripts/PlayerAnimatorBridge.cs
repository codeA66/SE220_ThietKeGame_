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
        private static readonly int BowChargeHash = Animator.StringToHash("Bow.BowCharge");
        private static readonly int BowCrouchChargeHash = Animator.StringToHash("Bow.BowCrouchCharge");
        private static readonly int BowReleaseHash = Animator.StringToHash("Bow.BowRelease");
        private static readonly int BowCrouchReleaseHash = Animator.StringToHash("Bow.BowCrouchRelease");
        private static readonly int BowReleaseSpeedHash = Animator.StringToHash("BowReleaseSpeed");
        private static readonly int SpawnPhaseHash = Animator.StringToHash("SpawnPhase");

        [SerializeField] private Animator _animator;
        private int _bowLayer;
        private float _bowReleaseLength;
        private float _bowCrouchReleaseLength;

        public float RebirthLength { get; private set; }

        private void Awake()
        {
            RebirthLength = GetClipLength("Player_Rebirth");
            _bowLayer = _animator.GetLayerIndex("Bow");
            if (_bowLayer < 0)
                throw new System.InvalidOperationException("Player Animator requires the Bow layer.");
            _bowReleaseLength = GetClipLength("Player_BowRelease");
            _bowCrouchReleaseLength = GetClipLength("Player_BowCrouchRelease");
            if (_bowReleaseLength <= 0f || _bowCrouchReleaseLength <= 0f)
                throw new System.InvalidOperationException("Player Animator requires both bow release clips.");
        }

        public void DrawBow(bool crouching, float normalizedTime)
        {
            _animator.SetLayerWeight(_bowLayer, 1f);
            _animator.Play(crouching ? BowCrouchChargeHash : BowChargeHash, _bowLayer,
                Mathf.Min(normalizedTime, 0.999f));
        }

        public float ReleaseBow(bool crouching, float speed)
        {
            _animator.SetFloat(BowReleaseSpeedHash, speed);
            _animator.Play(crouching ? BowCrouchReleaseHash : BowReleaseHash, _bowLayer, 0f);
            return (crouching ? _bowCrouchReleaseLength : _bowReleaseLength) / speed;
        }

        public void HideBow() => _animator.SetLayerWeight(_bowLayer, 0f);

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

        /// <summary>0 = none, 1 = lying headless, 2 = rising (rebirth).</summary>
        public void SetSpawnPhase(int phase) => _animator.SetInteger(SpawnPhaseHash, phase);

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
