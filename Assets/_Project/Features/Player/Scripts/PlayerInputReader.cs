using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadCells.Player
{
    /// <summary>
    /// Reads the "Player" action map. Polled once per frame by PlayerController so input is
    /// always fresh before the state machine ticks.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;

        private InputAction _move;
        private InputAction _jump;
        private InputAction _attack;
        private InputAction _roll;
        private InputAction _interact;
        private InputAction _equipSword;
        private InputAction _equipBow;
        private InputAction _crouch;

        public float MoveX { get; private set; }
        public float MoveY { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackReleased { get; private set; }
        public bool RollPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool EquipSwordPressed { get; private set; }
        public bool EquipBowPressed { get; private set; }
        public bool CrouchHeld { get; private set; }

        private void Awake()
        {
            var map = _actions.FindActionMap("Player", throwIfNotFound: true);
            _move = map.FindAction("Move", throwIfNotFound: true);
            _jump = map.FindAction("Jump", throwIfNotFound: true);
            _attack = map.FindAction("Attack", throwIfNotFound: true);
            _roll = map.FindAction("Roll", throwIfNotFound: true);
            _interact = map.FindAction("Interact", throwIfNotFound: true);
            _equipSword = map.FindAction("EquipSword", throwIfNotFound: true);
            _equipBow = map.FindAction("EquipBow", throwIfNotFound: true);
            _crouch = map.FindAction("Crouch", throwIfNotFound: false);
        }

        private void OnEnable()
        {
            if (_actions != null)
                _actions.FindActionMap("Player")?.Enable();
        }

        private void OnDisable()
        {
            if (_actions != null)
                _actions.FindActionMap("Player")?.Disable();
            MoveX = 0f;
            MoveY = 0f;
            JumpPressed = JumpHeld = AttackPressed = AttackHeld = AttackReleased =
                RollPressed = InteractPressed = EquipSwordPressed = EquipBowPressed = CrouchHeld = false;
        }

        public void Poll()
        {
            var move = _move.ReadValue<Vector2>();
            MoveX = move.x;
            MoveY = move.y;
            JumpPressed = _jump.WasPressedThisFrame();
            JumpHeld = _jump.IsPressed();
            AttackPressed = _attack.WasPressedThisFrame();
            AttackHeld = _attack.IsPressed();
            AttackReleased = _attack.WasReleasedThisFrame();
            RollPressed = _roll.WasPressedThisFrame();
            InteractPressed = _interact.WasPressedThisFrame();
            EquipSwordPressed = _equipSword.WasPressedThisFrame();
            EquipBowPressed = _equipBow.WasPressedThisFrame();
            CrouchHeld = _crouch != null && _crouch.IsPressed();
        }
    }
}
