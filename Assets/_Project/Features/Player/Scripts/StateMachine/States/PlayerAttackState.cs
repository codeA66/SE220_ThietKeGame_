using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// One class for the 3-hit sword combo, on the ground or in the air. The hit number comes from
    /// PlayerContext.PendingComboIndex. Pressing attack during a swing queues the next hit, which starts
    /// once the swing passes ComboChainNormalizedTime. The player keeps moving at a reduced speed.
    /// </summary>
    public class PlayerAttackState : PlayerStateBase
    {
        private const int MaxCombo = 3;
        private static readonly string[] ClipNames = { "Player_AttackA", "Player_AttackB", "Player_AttackC" };

        private readonly float[] _clipLengths = new float[MaxCombo];
        private bool _clipLengthsCached;

        private int _comboIndex;
        private float _timer;
        private bool _nextQueued;

        public PlayerAttackState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public override void Enter()
        {
            CacheClipLengths();
            _comboIndex = Mathf.Clamp(Ctx.PendingComboIndex, 1, MaxCombo);
            _timer = 0f;
            _nextQueued = false;
            Ctx.FaceInput();
            Ctx.Animator.SetAttack(_comboIndex);
        }

        public override void Exit()
        {
            Ctx.Animator.SetAttack(0);
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
        }

        public override void Tick()
        {
            // Rolling out of a swing keeps the combo: the next attack continues from the following hit.
            if (Ctx.Input.RollPressed && Ctx.CanRoll)
            {
                Ctx.StoreComboCarry(_comboIndex < MaxCombo ? _comboIndex + 1 : 1);
                Machine.ChangeState(Machine.Roll);
                return;
            }

            if (Ctx.Input.AttackPressed && _comboIndex < MaxCombo)
                _nextQueued = true;

            _timer += Time.deltaTime;
            float duration = _clipLengths[_comboIndex - 1];
            float normalized = duration > 0f ? _timer / duration : 1f;

            if (_nextQueued && normalized >= Ctx.Config.ComboChainNormalizedTime)
            {
                Ctx.PendingComboIndex = _comboIndex + 1;
                Machine.ChangeState(Machine.Attack);
                return;
            }

            if (normalized >= 1f)
                ReturnToDefault();
        }

        public override void FixedTick()
        {
            if (!Ctx.Config.AttackLockFacing)
                Ctx.FaceInput();

            bool grounded = Ctx.IsGrounded;
            float baseSpeed = grounded ? Ctx.Config.RunSpeed : Ctx.Config.AirSpeed;
            Ctx.SetHorizontalVelocity(Ctx.Input.MoveX * baseSpeed * Ctx.Config.AttackMoveSpeedMultiplier);

            if (grounded)
                return;

            // Airborne swing: same gravity rules as Jump/Fall.
            bool falling = Ctx.Velocity.y < 0f;
            Ctx.SetGravityScale(falling
                ? Ctx.Config.GravityScale * Ctx.Config.FallGravityMultiplier
                : Ctx.Config.GravityScale);
            Ctx.ClampFallSpeed();
        }

        private void CacheClipLengths()
        {
            if (_clipLengthsCached)
                return;
            for (int i = 0; i < MaxCombo; i++)
                _clipLengths[i] = Ctx.Animator.GetClipLength(ClipNames[i]);
            _clipLengthsCached = true;
        }
    }
}
