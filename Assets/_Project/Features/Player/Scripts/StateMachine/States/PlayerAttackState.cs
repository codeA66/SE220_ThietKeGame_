using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Weapon combo, on the ground or in the air. Hit timing, damage and reach come from the equipped
    /// WeaponData (DC: weapon@strikeChain). The hit index comes from PlayerContext.PendingComboIndex.
    /// Pressing attack during a swing queues the next hit, which starts once the swing passes
    /// ComboChainNormalizedTime. The player keeps moving at a reduced speed.
    /// </summary>
    public class PlayerAttackState : PlayerStateBase
    {
        private const float ClipFps = 30f;

        private int _comboIndex;
        private float _timer;
        private float _duration;
        private float _hitTime;
        private bool _hitDone;
        private bool _nextQueued;
        private WeaponData.Strike _strike;

        public PlayerAttackState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        private WeaponData Weapon => Ctx.Combat.MeleeWeapon;

        public override void Enter()
        {
            _comboIndex = Mathf.Clamp(Ctx.PendingComboIndex, 1, Weapon.ComboLength);
            _strike = Weapon.GetStrike(_comboIndex - 1);
            float speed = _strike.AnimSpeed > 0f ? _strike.AnimSpeed : 1f;
            _duration = Ctx.Animator.GetClipLength(_strike.ClipName) / speed;
            _hitTime = _strike.HitFrame / ClipFps / speed;
            _timer = 0f;
            _hitDone = false;
            _nextQueued = false;

            Ctx.FaceInput();
            Ctx.Combat.BeginSwing();
            Ctx.Animator.SetAttackAnimSpeed(speed);
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
                Ctx.StoreComboCarry(_comboIndex < Weapon.ComboLength ? _comboIndex + 1 : 1);
                Machine.ChangeState(Machine.Roll);
                return;
            }

            if (Ctx.Input.AttackPressed && _comboIndex < Weapon.ComboLength)
                _nextQueued = true;

            _timer += Time.deltaTime;

            if (!_hitDone && _timer >= _hitTime)
            {
                _hitDone = true;
                Ctx.Combat.PerformStrike(_strike, Ctx.FacingDirection);
            }

            float normalized = _duration > 0f ? _timer / _duration : 1f;

            if (_nextQueued && _hitDone && normalized >= Ctx.Config.ComboChainNormalizedTime)
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
    }
}
