using DeadCells.Combat;
using DeadCells.Core;
using UnityEngine;

namespace DeadCells.HUD
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealthHud : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private HealthBarView _view;

        private void OnEnable()
        {
            if (_health == null || _view == null)
            {
                GameDebug.LogError("Assign the player health and health bar view.", LogTopic.UI);
                return;
            }

            _health.Changed += Refresh;
            Refresh(_health.Current, _health.Max);
        }

        private void Start()
        {
            // All scene objects have run Awake by this point, including Health.
            if (_health != null && _view != null)
                Refresh(_health.Current, _health.Max);
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Changed -= Refresh;
        }

        private void Refresh(int current, int max) => _view.Render(current, max);
    }
}
