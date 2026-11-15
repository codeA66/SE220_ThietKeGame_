using UnityEngine;

namespace DeadCells.HUD
{
    public sealed class FloatingDamageNumber : MonoBehaviour
    {
        [SerializeField] private TextMesh _label;
        [SerializeField] private TextMesh _shadow;

        private Vector3 _origin;
        private float _startedAt;
        private Color _color;

        public void Show(Vector3 position, int damage, bool critical)
        {
            _origin = transform.position = position;
            _startedAt = Time.unscaledTime;
            _color = critical ? new Color(1f, 0.82f, 0.32f) : Color.white;
            _label.text = _shadow.text = damage.ToString();
            _label.color = _color;
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - _startedAt;
            transform.position = _origin + Vector3.up * (0.9f * elapsed);
            float alpha = 1f - Mathf.InverseLerp(0.22f, 0.55f, elapsed);
            _label.color = new Color(_color.r, _color.g, _color.b, alpha);
            _shadow.color = new Color(0.04f, 0.05f, 0.07f, 0.85f * alpha);
            if (alpha <= 0f)
                Destroy(gameObject);
        }
    }
}
