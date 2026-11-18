using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    [DisallowMultipleComponent]
    public sealed class BowAmmoView : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private Color _emptyColor;
        [SerializeField] private Color _availableColor;

        public void Render(bool owned, int current, int max)
        {
            _label.enabled = owned;
            _label.text = $"{current}/{max}";
            _label.color = current > 0 ? _availableColor : _emptyColor;
        }
    }
}
