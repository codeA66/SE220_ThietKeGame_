using UnityEngine;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LevelExitTrigger : MonoBehaviour
    {
        [SerializeField] private RunSession _session;

        public RunSession Session => _session;

        public void Configure(RunSession session) => _session = session;

        private void Reset()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
                box.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_session == null || _session.IsEnded)
                return;

            // Player layer is index 7
            if (other.gameObject.layer == 7 || other.CompareTag("Player"))
                _session.CompleteRun();
        }
    }
}
