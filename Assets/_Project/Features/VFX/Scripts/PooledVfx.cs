using System;
using UnityEngine;

namespace DeadCells.VFX
{
    /// <summary>
    /// Plays a VfxDefinition's frames once, then hands itself back to its pool.
    /// Frame stepping is done here instead of an Animator to keep each effect cheap.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PooledVfx : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private float _frameDuration;
        private float _timer;
        private int _frame;
        private Action<PooledVfx> _release;

        public VfxDefinition Definition { get; private set; }

        /// <summary>
        /// Called once right after creation. The object is still inactive here, so Awake has not run:
        /// the renderer is fetched here instead.
        /// </summary>
        public void Init(VfxDefinition definition, Action<PooledVfx> release)
        {
            _renderer = GetComponent<SpriteRenderer>();
            Definition = definition;
            _release = release;
            _frames = definition.Frames;
            _frameDuration = 1f / Mathf.Max(definition.Fps, 0.01f);
            _renderer.sortingOrder = definition.SortingOrder;
            transform.localScale = Vector3.one * definition.Scale;
        }

        public void Play(Vector3 position, bool flipX)
        {
            transform.position = position;
            _renderer.flipX = flipX;
            _timer = 0f;
            _frame = 0;
            _renderer.sprite = _frames.Length > 0 ? _frames[0] : null;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < _frameDuration)
                return;

            _timer -= _frameDuration;
            _frame++;
            if (_frame >= _frames.Length)
            {
                gameObject.SetActive(false);
                _release(this);
                return;
            }
            _renderer.sprite = _frames[_frame];
        }
    }
}
