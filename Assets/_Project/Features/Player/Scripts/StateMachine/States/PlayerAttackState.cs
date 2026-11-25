using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Player.StateMachine.States
{
    /// <summary>
    /// Weapon combo, on the ground or in the air. Hit timing, damage and reach come from the equipped
    /// WeaponData (DC: weapon@strikeChain). The hit index comes from PlayerContext.PendingComboIndex.
    /// Pressing attack during a swing queues the next hit, which starts once the swing passes
    /// ComboChainNormalizedTime; the chain wraps so hit 3 loops back into hit 1. The player keeps moving
    /// at a reduced speed, and the hit frame adds a forward lunge that survives FixedTick.
    /// </summary>
    public class PlayerAttackState : PlayerStateBase
    {
        private const float ClipFps = 30f;
        /// <summary>Seconds the forward lunge set on the hit frame keeps pushing the body.</summary>
        private const float LungeDuration = 0.095f;
        /// <summary>Ground lunge speed for the opening hits of the chain (units/s).</summary>
        private const float LungeSpeed = 7.2f;
        /// <summary>Ground lunge speed for the chain finisher, which steps further in (units/s).</summary>
        private const float FinisherLungeSpeed = 9.5f;
        /// <summary>Air lunge speed: a swing in the air drifts forward instead of launching (units/s).</summary>
        private const float AirLungeSpeed = 2.8f;
        /// <summary>Seconds past the hit frame during which the hit box keeps re-resolving.</summary>
        private const float ActiveHitWindow = 0.065f;
        /// <summary>Seconds an air hit holds the body up so the impact reads before the fall resumes.</summary>
        private const float AirStallDuration = 0.12f;
        /// <summary>Upward velocity an air hit-stall gives the body (units/s).</summary>
        private const float AirStallMinRiseSpeed = 2.2f;
        /// <summary>Gravity during the air hit-stall, as a fraction of GravityScale.</summary>
        private const float AirStallGravityMultiplier = 0.35f;

        private int _comboIndex;
        private float _timer;
        private float _duration;
        private float _hitTime;
        private bool _hitDone;
        private bool _nextQueued;
        private WeaponData.Strike _strike;
        private float _lungeSpeed;
        private float _lungeTimer;
        private float _airStallTimer;
        private bool _airStallApplied;

        public PlayerAttackState(PlayerContext context, PlayerStateMachine machine) : base(context, machine) { }

        public bool CanWeaponSwapCancel => _hitDone;

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
            _lungeSpeed = 0f;
            _lungeTimer = 0f;
            _airStallTimer = 0f;
            _airStallApplied = false;

            Ctx.FaceInput();
            Ctx.Combat.BeginSwing();
            Ctx.Animator.SetAttackAnimSpeed(speed);
            Ctx.Animator.SetAttack(_comboIndex);
        }

        public override void Exit()
        {
            Ctx.Animator.SetAttack(0);
            // The air hit-stall leaves gravity below GravityScale, so the hand-off must always restore it.
            Ctx.SetGravityScale(Ctx.Config.GravityScale);
        }

        public override void Tick() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            // Rolling out of a swing keeps the combo: the next attack continues from the following hit.
            if (Ctx.Input.RollPressed && Ctx.CanRoll)
            {
                Ctx.StoreComboCarry((_comboIndex % Weapon.ComboLength) + 1);
                Machine.ChangeState(Machine.Roll);
                return;
            }

            if (Ctx.Input.AttackPressed)
                _nextQueued = true;

            _timer += deltaTime;

            if (!_hitDone && _timer >= _hitTime)
            {
                _hitDone = true;
                StartLunge();
                // The first active frame owns the swing event: one slash VFX and one sword whoosh.
                int hits = Ctx.Combat.PerformStrike(_strike, Ctx.FacingDirection);
                if (Machine.Current != this)
                    return;
                ApplyAirHitStall(hits);
            }
            else if (_hitDone && _timer <= _hitTime + ActiveHitWindow)
            {
                // Keep re-resolving for the rest of the active arc so a target that walks in mid-swing is
                // still caught. HitQuery._alreadyHit keeps every target to a single hit per swing.
                int hits = Ctx.Combat.PerformStrike(_strike, Ctx.FacingDirection, emitSwingEvent: false);
                if (Machine.Current != this)
                    return;
                ApplyAirHitStall(hits);
            }

            float normalized = _duration > 0f ? _timer / _duration : 1f;

            // The chain may only take over once this swing's active window has closed: changing state
            // re-enters this state and resets the arc, which would otherwise cut the hit box mid swing.
            bool windowClosed = _hitDone && _timer >= _hitTime + ActiveHitWindow;

            if (_nextQueued && windowClosed && normalized >= Ctx.Config.ComboChainNormalizedTime)
            {
                // Changing state on this same instance runs Exit() then Enter(), and Enter() starts the next
                // swing from a clean slate. The step-in and the air hit-stall belong to the character's
                // momentum, not to one clip, so they are handed over the transition instead of being wiped
                // mid fly. A swing that is not a chain continuation still starts clean.
                //
                // Only a window that is still live is carried. Handing over a spent window would disarm the
                // incoming swing instead of helping it: _airStallApplied is only retired on landing, so a
                // carried `true` with _airStallTimer == 0 makes ApplyAirHitStall bail on every hit of the new
                // swing, and every chained airborne hit after the first would lose its own lift.
                int previousPending = Ctx.PendingComboIndex;
                float lungeSpeed = _lungeTimer > 0f ? _lungeSpeed : 0f;
                float lungeTimer = _lungeTimer;
                float airStallTimer = _airStallTimer;
                bool airStallApplied = _airStallTimer > 0f && _airStallApplied;

                // Wrap after the finisher so the chain loops 1 -> 2 -> 3 -> 1 without a dead press.
                Ctx.PendingComboIndex = (_comboIndex % Weapon.ComboLength) + 1;
                if (!Machine.ChangeState(Machine.Attack))
                {
                    // The transition was refused for this frame: leave the swing untouched and retry on the
                    // next tick, so nothing is carried into a state that never started.
                    Ctx.PendingComboIndex = previousPending;
                    return;
                }

                _lungeSpeed = lungeSpeed;
                _lungeTimer = lungeTimer;
                _airStallTimer = airStallTimer;
                _airStallApplied = airStallApplied;
                return;
            }

            if (_hitDone && normalized >= Ctx.Config.ComboChainNormalizedTime && Ctx.Input.JumpPressed && !Ctx.IsDownHeld)
            {
                if (Ctx.IsGrounded || Ctx.WithinCoyoteTime)
                {
                    Machine.ChangeState(Machine.Jump);
                    return;
                }
                if (TryAirJump())
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
            float inputVx = Ctx.Input.MoveX * baseSpeed * Ctx.Config.AttackMoveSpeedMultiplier;

            if (_lungeTimer > 0f)
            {
                _lungeTimer = Mathf.Max(0f, _lungeTimer - Time.fixedDeltaTime);
                float lungeVx = Ctx.FacingDirection * Mathf.Lerp(0f, _lungeSpeed, _lungeTimer / LungeDuration);
                // Whichever is stronger wins, so the lunge is never wiped by the attack walk and back
                // input can never cancel the step in.
                Ctx.SetHorizontalVelocity(Mathf.Abs(lungeVx) >= Mathf.Abs(inputVx) ? lungeVx : inputVx);
            }
            else
            {
                Ctx.SetHorizontalVelocity(inputVx);
            }

            if (grounded)
            {
                // Landing mid-stall must not leave the reduced-gravity window alive: walking off the ledge
                // afterwards would resume the fall at AirStallGravityMultiplier for the rest of the stall.
                if (_airStallTimer > 0f || _airStallApplied)
                {
                    _airStallTimer = 0f;
                    _airStallApplied = false;
                    Ctx.SetGravityScale(Ctx.Config.GravityScale);
                }
                return;
            }

            if (_airStallTimer > 0f)
                _airStallTimer = Mathf.Max(0f, _airStallTimer - Time.fixedDeltaTime);

            if (_airStallTimer > 0f)
            {
                Ctx.SetGravityScale(Ctx.Config.GravityScale * AirStallGravityMultiplier);
            }
            else
            {
                // Airborne swing: same gravity rules as Jump/Fall.
                bool falling = Ctx.Velocity.y < 0f;
                Ctx.SetGravityScale(falling
                    ? Ctx.Config.GravityScale * Ctx.Config.FallGravityMultiplier
                    : Ctx.Config.GravityScale);
            }
            Ctx.ClampFallSpeed();
        }

        /// <summary>
        /// Applies the forward step of the swing: heavier on the finisher, a drift in the air. The speed
        /// is applied immediately so the hit frame itself carries the body, and FixedTick then decays it.
        /// </summary>
        private void StartLunge()
        {
            _lungeSpeed = Ctx.IsGrounded
                ? (_comboIndex >= Weapon.ComboLength ? FinisherLungeSpeed : LungeSpeed)
                : AirLungeSpeed;
            _lungeTimer = LungeDuration;
            Ctx.SetHorizontalVelocity(Ctx.FacingDirection * _lungeSpeed);
        }

        /// <summary>
        /// First hit landed while airborne: hold the body up briefly so the impact reads, then fall again.
        /// </summary>
        private void ApplyAirHitStall(int hits)
        {
            if (hits <= 0 || Ctx.IsGrounded || _airStallApplied)
                return;
            _airStallApplied = true;
            _airStallTimer = AirStallDuration;
            Ctx.SetVerticalVelocity(Mathf.Max(Ctx.Velocity.y, AirStallMinRiseSpeed));
        }
    }
}
