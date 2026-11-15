using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    public sealed class StatUpgradeOption : MonoBehaviour
    {
        [SerializeField] private CombatStat _stat;
        [SerializeField] private Button _button;
        [SerializeField] private Text _preview;
        [SerializeField] private Text _hudLevel;

        public CombatStat Stat => _stat;
        public Button Button => _button;

        public void RenderLevel(PlayerRunStats stats) => _hudLevel.text = stats.Level(_stat).ToString();

        public void RenderPreview(PlayerRunStats stats, int currentMaxHealth)
        {
            float gain = (float)(stats.PreviewMaxHealth(_stat) - currentMaxHealth) / currentMaxHealth;
            _preview.text = $"Cấp {stats.Level(_stat)} → {stats.Level(_stat) + 1}\n" +
                $"+{PlayerRunStats.DamageGainPerLevel:P0} sát thương cùng hệ\n+{gain:P0} máu tối đa";
        }
    }
}
