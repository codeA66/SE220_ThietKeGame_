using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    [DisallowMultipleComponent]
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Text _value;

        public void Render(int current, int max)
        {
            float fraction = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            Vector2 end = _fill.anchorMax;
            end.x = fraction;
            _fill.anchorMax = end;
            _value.text = $"{current} / {max}";
        }
    }
}
