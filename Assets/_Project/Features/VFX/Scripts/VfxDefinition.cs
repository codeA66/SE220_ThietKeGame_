using UnityEngine;

namespace DeadCells.VFX
{
    /// <summary>
    /// A one-shot sprite-sequence effect: frames, playback rate and how many instances to pre-pool.
    /// </summary>
    [CreateAssetMenu(fileName = "Vfx_", menuName = "DeadCells/VFX/Definition")]
    public class VfxDefinition : ScriptableObject
    {
        [SerializeField] private Sprite[] _frames;
        [Tooltip("Playback rate in frames per second.")]
        [SerializeField] private float _fps = 30f;
        [Tooltip("Multiplied with the frames' colours. White frames (e.g. Dead Cells weapon swings) take this colour.")]
        [SerializeField] private Color _tint = Color.white;
        [Tooltip("Uniform scale applied when the effect plays.")]
        [Min(0.01f)]
        [SerializeField] private float _scale = 1f;
        [Tooltip("Sorting order relative to the default layer (player is 0).")]
        [SerializeField] private int _sortingOrder = 1;
        [Tooltip("Instances created up front so playing never allocates.")]
        [Min(1)]
        [SerializeField] private int _prewarmCount = 3;

        public Sprite[] Frames => _frames;
        public float Fps => _fps;
        public float Scale => _scale;
        public Color Tint => _tint;
        public int SortingOrder => _sortingOrder;
        public int PrewarmCount => _prewarmCount;
    }
}
