using UnityEngine;

namespace DeadCells.Enemies
{
    /// <summary>
    /// Plays enemy animation states by name; the FSM is the single source of truth, so the controller
    /// has states only and no transitions. Also drives the hit flash on the flesh-swap material.
    /// </summary>
    public class EnemyAnimator : MonoBehaviour
    {
        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Walk = Animator.StringToHash("Walk");
        public static readonly int Run = Animator.StringToHash("Run");
        public static readonly int AtkLoad = Animator.StringToHash("AtkLoad");
        public static readonly int Atk = Animator.StringToHash("Atk");
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Stun = Animator.StringToHash("Stun");
        public static readonly int Fall = Animator.StringToHash("Fall");

        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _renderer;

        private MaterialPropertyBlock _block;
        private int _current;

        public SpriteRenderer Renderer => _renderer;

        private void Awake() => _block = new MaterialPropertyBlock();

        /// <summary>Plays a state from its start; falls back to Idle if this enemy lacks it.</summary>
        public void Play(int state, float speed = 1f, bool restart = true)
        {
            if (!_animator.HasState(0, state))
                state = Idle;
            _animator.speed = speed;
            if (!restart && state == _current)
                return;
            _current = state;
            _animator.Play(state, 0, 0f);
        }

        public bool HasState(int state) => _animator.HasState(0, state);

        public void SetFlash(float amount) => SetFlash(amount, Color.white);

        /// <summary>Blends the sprite toward a colour (white for hit flashes, the mob's glow for telegraphs).</summary>
        public void SetFlash(float amount, Color color)
        {
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(FlashId, amount);
            _block.SetColor(FlashColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        public void SetAlpha(float alpha)
        {
            var c = _renderer.color;
            c.a = alpha;
            _renderer.color = c;
        }
    }
}
