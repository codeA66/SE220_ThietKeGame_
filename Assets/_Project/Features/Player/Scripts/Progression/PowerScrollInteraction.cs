using UnityEngine;

namespace DeadCells.Player
{
    public sealed class PowerScrollInteraction : MonoBehaviour
    {
        [SerializeField] private PowerScrollPickup[] _scrolls;

        private PowerScrollPickup _focused;

        public bool Tick(Vector2 position, bool canInteract, bool pressed, PlayerRunStats stats)
        {
            var nearest = canInteract ? FindNearest(position) : null;
            if (_focused != nearest)
            {
                if (_focused != null)
                    _focused.SetFocused(false);
                _focused = nearest;
                if (_focused != null)
                    _focused.SetFocused(true);
            }

            return pressed && _focused != null && stats.TryBeginChoice(_focused);
        }

        private PowerScrollPickup FindNearest(Vector2 position)
        {
            PowerScrollPickup nearest = null;
            float closest = float.PositiveInfinity;
            foreach (var scroll in _scrolls)
            {
                if (scroll == null || !scroll.TryGetDistance(position, out float distance) || distance >= closest)
                    continue;

                nearest = scroll;
                closest = distance;
            }
            return nearest;
        }
    }
}
