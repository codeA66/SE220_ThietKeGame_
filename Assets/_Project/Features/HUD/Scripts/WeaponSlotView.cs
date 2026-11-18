using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    [DisallowMultipleComponent]
    public sealed class WeaponSlotView : MonoBehaviour
    {
        [SerializeField] private Graphic _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private Color _emptyColor;
        [SerializeField] private Color _ownedColor;
        [SerializeField] private Color _selectedColor;

        public void Render(bool owned, bool equipped)
        {
            _icon.enabled = owned;
            _frame.color = equipped ? _selectedColor : owned ? _ownedColor : _emptyColor;
        }
    }
}
