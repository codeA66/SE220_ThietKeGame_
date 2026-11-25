using UnityEngine;

namespace DeadCells.VFX
{
    /// <summary>
    /// Binds per-instance _UseWorldYPin, _PinTopWorldY, and _SwaySpanY via MaterialPropertyBlock
    /// so multi-piece RoomBanner_* props at any dungeon elevation sway with zero seam tear.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class VertexSwayPin2D : MonoBehaviour
    {
        private static readonly int UseWorldYPinId = Shader.PropertyToID("_UseWorldYPin");
        private static readonly int PinTopWorldYId = Shader.PropertyToID("_PinTopWorldY");
        private static readonly int SwaySpanYId = Shader.PropertyToID("_SwaySpanY");

        [SerializeField] private bool _useWorldYPin = true;
        [SerializeField] private float _pinTopWorldY = 7.6f;
        [SerializeField] private float _swaySpanY = 4.0f;

        private MaterialPropertyBlock _propertyBlock;

        public bool UseWorldYPin => _useWorldYPin;
        public float PinTopWorldY => _pinTopWorldY;
        public float SwaySpanY => _swaySpanY;

        private void Awake() => ApplyPropertyBlock();

        private void OnEnable() => ApplyPropertyBlock();

        private void OnValidate() => ApplyPropertyBlock();

        public void Configure(float pinTopWorldY, float swaySpanY = 3.0f, bool useWorldYPin = true)
        {
            _pinTopWorldY = pinTopWorldY;
            _swaySpanY = Mathf.Max(0.01f, swaySpanY);
            _useWorldYPin = useWorldYPin;
            ApplyPropertyBlock();
        }

        public void ApplyPropertyBlock()
        {
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                if (sr == null)
                    continue;

                sr.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(UseWorldYPinId, _useWorldYPin ? 1.0f : 0.0f);
                _propertyBlock.SetFloat(PinTopWorldYId, _pinTopWorldY);
                _propertyBlock.SetFloat(SwaySpanYId, Mathf.Max(0.01f, _swaySpanY));
                sr.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
