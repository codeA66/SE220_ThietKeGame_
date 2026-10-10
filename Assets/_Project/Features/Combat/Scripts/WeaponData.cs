using System;
using DeadCells.VFX;
using UnityEngine;

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
            public string AnimState;
            [Tooltip("Clip name used to read the clip length.")]
            public string ClipName;
            [Tooltip("DC: power.")]
            public int Damage;
            [Tooltip("Frame (at the clip's 30 fps, before AnimSpeed) on which the hit lands. DC: hitFrame.")]
            public int HitFrame;
            [Tooltip("Playback speed multiplier. DC: animSpd (empty = 1).")]
            public float AnimSpeed;
            [Tooltip("Hit box width in front of the player's centre, in cells. DC: area.width.")]
            public float RangeWidthCells;
            [Tooltip("Hit box height in cells. DC: area.height; 0 = player height.")]
            public float RangeHeightCells;
            [Tooltip("Shift of the hit box forward / up, in cells. DC: area.offsetCaseX / offsetCaseY.")]
            public Vector2 RangeOffsetCells;
            [Tooltip("Horizontal knockback speed given to the target (units/s). DC: props.bump (unit not verified).")]
            public float Knockback;
            [Tooltip("DC: canCrit.")]
            public bool CanCrit;
            [Tooltip("DC: charge. Recorded for reference; timing uses HitFrame.")]
            public float Charge;
            [Tooltip("Swing effect played on the hit frame. DC: fxId.")]
            public VfxDefinition SwingVfx;

            public float RangeWidth => RangeWidthCells * CellSize;
            public float RangeHeight => RangeHeightCells * CellSize;
            public Vector2 RangeOffset => RangeOffsetCells * CellSize;
        }

        [SerializeField] private Strike[] _combo;

        public int ComboLength => _combo.Length;

        public Strike GetStrike(int index) => _combo[Mathf.Clamp(index, 0, _combo.Length - 1)];
    }
}
