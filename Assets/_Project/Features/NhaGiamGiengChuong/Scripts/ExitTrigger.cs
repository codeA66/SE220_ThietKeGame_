using System;
using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Exit arch trigger (I5) in Room F at center (5, 33), size (2, 2).
    /// Displays an [E] prompt when unlocked and submits a win candidate only when the living Player
    /// presses Interact inside the trigger during Playing and I4 is open (no accidental touch-to-exit).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ExitTrigger : MonoBehaviour
    {
        [SerializeField] private LevelRunController _run;
        [SerializeField] private EncounterGate _gate;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _promptIndicator;

        private BoxCollider2D _triggerZone;
        private InputAction _interactAction;
        private bool _promptVisible;

        public LevelRunController Run => _run;
        public EncounterGate Gate => _gate;
        public PlayerController Player => _player;
        public bool PromptVisible => _promptVisible;
        public GameObject PromptIndicator => _promptIndicator;

        public event Action<int> WinCandidateSubmitted;

        public void Configure(
            LevelRunController run,
            EncounterGate gate,
            PlayerController player,
            Health playerHealth = null,
            BoxCollider2D playerCollider = null,
            InputActionAsset inputActions = null,
            GameObject promptIndicator = null)
        {
            UnsubscribeInput();
            _run = run;
            _gate = gate;
            _player = player;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            if (isActiveAndEnabled)
                SubscribeInput();
            RefreshVisualState();
        }

        private void Awake()
        {
            _triggerZone = GetComponent<BoxCollider2D>();
            RefreshVisualState();
        }

        private void OnEnable()
        {
            SubscribeInput();
            RefreshVisualState();
        }

        private void OnDisable() => UnsubscribeInput();

        private void OnDestroy() => UnsubscribeInput();

        private void Update() => RefreshVisualState();

        public void RefreshVisualState()
        {
            var health = ResolvePlayerHealth();
            bool alive = health != null && health.IsAlive;
            bool inTrigger = IsPlayerInTrigger();
            _promptVisible = _run != null && _run.IsPlaying
                && _gate != null && _gate.IsOpen
                && alive
                && !IsInputBlocked()
                && inTrigger;
            if (_promptIndicator != null)
                _promptIndicator.SetActive(_promptVisible);
        }

        public bool TryInteract()
        {
            if (IsInputBlocked() || !IsPlayerInTrigger())
                return false;
            return TrySubmitForCollider(ResolvePlayerCollider(), Time.frameCount);
        }

        /// <summary>
        /// Explicit submission handler for I5. Validates exact main Player collider, alive Health, Playing phase, and open I4.
        /// </summary>
        public bool TrySubmitForCollider(Collider2D candidateCollider, int frame)
        {
            if (candidateCollider == null)
                return false;

            if (_run == null || !_run.IsPlaying)
                return false;

            if (_gate == null || !_gate.IsOpen)
                return false;

            var expectedCollider = ResolvePlayerCollider();
            if (expectedCollider == null || candidateCollider != expectedCollider)
                return false;

            var health = ResolvePlayerHealth();
            if (health == null || !health.IsAlive)
                return false;

            if (!_run.RequestWin(frame))
                return false;

            WinCandidateSubmitted?.Invoke(frame);
            return true;
        }

        private void OnInteractStarted(InputAction.CallbackContext _) => TryInteract();

        private void SubscribeInput()
        {
            if (_inputActions == null || _interactAction != null)
                return;
            var map = _inputActions.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (map == null)
                return;
            _interactAction = map.FindAction(_interactActionName, throwIfNotFound: false);
            if (_interactAction != null)
                _interactAction.started += OnInteractStarted;
        }

        private void UnsubscribeInput()
        {
            if (_interactAction == null)
                return;
            _interactAction.started -= OnInteractStarted;
            _interactAction = null;
        }

        private bool IsPlayerInTrigger()
        {
            if (_triggerZone == null)
                _triggerZone = GetComponent<BoxCollider2D>();
            var col = ResolvePlayerCollider();
            if (col == null || _triggerZone == null)
                return false;
            Bounds tb = GetWorldBounds(_triggerZone);
            Bounds ab = GetWorldBounds(col);
            return ab.max.x >= tb.min.x && ab.min.x <= tb.max.x
                && ab.max.y >= tb.min.y && ab.min.y <= tb.max.y;
        }

        private static Bounds GetWorldBounds(Collider2D collider)
        {
            if (collider is BoxCollider2D box)
            {
                Vector3 center = box.transform.TransformPoint(box.offset);
                Vector3 scale = box.transform.lossyScale;
                Vector3 size = new Vector3(
                    Mathf.Abs(box.size.x * scale.x),
                    Mathf.Abs(box.size.y * scale.y),
                    1f);
                return new Bounds(center, size);
            }
            return collider.bounds;
        }

        private bool IsInputBlocked()
        {
            var p = ResolvePlayer();
            return p != null && ((p.RunStats != null && p.RunStats.BlocksGameplayInput) || p.IsInScriptedSequence);
        }

        private PlayerController ResolvePlayer() =>
            _player != null ? _player : (_run != null ? _run.Player : null);

        private BoxCollider2D ResolvePlayerCollider()
        {
            if (_playerCollider != null)
                return _playerCollider;
            var p = ResolvePlayer();
            return p != null ? p.GetComponent<BoxCollider2D>() : null;
        }

        private Health ResolvePlayerHealth()
        {
            if (_playerHealth != null)
                return _playerHealth;
            var p = ResolvePlayer();
            if (p != null)
                return p.Health != null ? p.Health : p.GetComponent<Health>();
            return _run != null ? _run.PlayerHealth : null;
        }
    }
}
