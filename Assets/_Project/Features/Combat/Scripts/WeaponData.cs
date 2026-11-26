using System;
using DeadCells.VFX;
using UnityEngine;
using UnityEngine.Serialization;

namespace DeadCells.Combat
{
    /// <summary>
    /// A melee weapon's combo. Field meanings follow Dead Cells' weapon@strikeChain table (data.cdb).
    /// Ranges are entered in Dead Cells cells and converted with CellSize.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "DeadCells/Combat/Weapon")]
    public class WeaponData : ScriptableObject
    {
        /// <summary>
        /// One Dead Cells cell in Unity units: the game's collision tiles are 24 px
        /// (tiled/col.tsx tilewidth=24) and sprites are imported at 32 px per unit.
        /// </summary>
        public const float CellSize = 24f / 32f;

        [Serializable]
        public struct Strike
        {
            [Tooltip("Animator state played for this hit (AttackA / AttackB / AttackC).")]
            [FormerlySerializedAs("AnimState"), SerializeField] private string _animState;
            [Tooltip("Clip name used to read the clip length.")]
            [FormerlySerializedAs("ClipName"), SerializeField] private string _clipName;
            [Tooltip("DC: power.")]
            [FormerlySerializedAs("Damage"), SerializeField] private int _damage;
            [Tooltip("Frame (at the clip's 30 fps, before AnimSpeed) on which the hit lands. DC: hitFrame.")]
            [FormerlySerializedAs("HitFrame"), SerializeField] private int _hitFrame;
            [Tooltip("Playback speed multiplier. DC: animSpd (empty = 1).")]
            [FormerlySerializedAs("AnimSpeed"), SerializeField] private float _animSpeed;
            [Tooltip("Hit box width in front of the player's centre, in cells. DC: area.width.")]
            [FormerlySerializedAs("RangeWidthCells"), SerializeField] private float _rangeWidthCells;
            [Tooltip("Hit box height in cells. DC: area.height; 0 = player height.")]
            [FormerlySerializedAs("RangeHeightCells"), SerializeField] private float _rangeHeightCells;
            [Tooltip("Shift of the hit box forward / up, in cells. DC: area.offsetCaseX / offsetCaseY.")]
            [FormerlySerializedAs("RangeOffsetCells"), SerializeField] private Vector2 _rangeOffsetCells;
            [Tooltip("Horizontal knockback speed given to the target (units/s). DC: props.bump (unit not verified).")]
            [FormerlySerializedAs("Knockback"), SerializeField] private float _knockback;
            // Crit feedback (off for every starter-sword strike, per the DC data). A crit hit gets a gold
            // impact flare with an X glint, a one-frame freeze then a short slow-motion and a bigger shake.
            // A crit KILL also throws a crimson gore burst: ~120 blood drops that land on floors and run
            // down walls, plus a blood mist. Crits deal normal damage: no crit multiplier exists yet.
            [Tooltip("DC: canCrit. On: hits from this strike are critical: gold flare, freeze + slow-motion, bigger shake; a crit kill throws a crimson gore burst (blood drops that land on floors and walls, plus mist). Damage is unchanged.")]
            [FormerlySerializedAs("CanCrit"), SerializeField] private bool _canCrit;
            [Tooltip("Optional stun duration dealt by this strike (s). Non-zero breaks front shields.")]
            [FormerlySerializedAs("Stun"), SerializeField] private float _stun;
            [Tooltip("DC: charge. Recorded for reference; timing uses HitFrame.")]
            [FormerlySerializedAs("Charge"), SerializeField] private float _charge;
            [Tooltip("Swing effect played on the hit frame. DC: fxId.")]
            [FormerlySerializedAs("SwingVfx"), SerializeField] private VfxDefinition _swingVfx;

            public Strike(
                int damage,
                float rangeWidthCells = 0f,
                float rangeHeightCells = 0f,
                Vector2 rangeOffsetCells = default,
                float knockback = 0f,
                bool canCrit = false,
                float stun = 0f,
                string animState = null,
                string clipName = null,
                int hitFrame = 0,
                float animSpeed = 0f,
                float charge = 0f,
                VfxDefinition swingVfx = null)
            {
                _animState = animState;
                _clipName = clipName;
                _damage = damage;
                _hitFrame = hitFrame;
                _animSpeed = animSpeed;
                _rangeWidthCells = rangeWidthCells;
                _rangeHeightCells = rangeHeightCells;
                _rangeOffsetCells = rangeOffsetCells;
                _knockback = knockback;
                _canCrit = canCrit;
                _stun = stun;
                _charge = charge;
                _swingVfx = swingVfx;
            }

            public string AnimState => _animState;
            public string ClipName => _clipName;
            public int Damage => _damage;
            public int HitFrame => _hitFrame;
            public float AnimSpeed => _animSpeed;
            public float RangeWidthCells => _rangeWidthCells;
            public float RangeHeightCells => _rangeHeightCells;
            public Vector2 RangeOffsetCells => _rangeOffsetCells;
            public float Knockback => _knockback;
            public bool CanCrit => _canCrit;
            public float Stun => _stun;
            public float Charge => _charge;
            public VfxDefinition SwingVfx => _swingVfx;

            public float RangeWidth => _rangeWidthCells * CellSize;
            public float RangeHeight => _rangeHeightCells * CellSize;
            public Vector2 RangeOffset => _rangeOffsetCells * CellSize;
        }

        [SerializeField] private CombatStat _scalingStat;
        [SerializeField] private Strike[] _combo;

        public CombatStat ScalingStat => _scalingStat;
        public int ComboLength => _combo.Length;

        public Strike GetStrike(int index) => _combo[Mathf.Clamp(index, 0, _combo.Length - 1)];
    }
}
