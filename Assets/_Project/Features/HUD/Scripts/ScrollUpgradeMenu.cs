using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;

namespace DeadCells.HUD
{
    public sealed class ScrollUpgradeMenu : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private HitStop _gameplayTime;
        [SerializeField] private GameObject _panel;
        [SerializeField] private StatUpgradeOption[] _options;

        private PlayerRunStats _stats;

        private void Start() => Bind();

        private void OnEnable()
        {
            if (_player.RunStats != null)
                Bind();
        }

        private void OnDisable()
        {
            if (_stats == null)
                return;

            _stats.ChoiceOpened -= Open;
            _stats.Changed -= RefreshLevels;
            _stats.CancelChoice();
            _stats = null;
            Close();
        }

        public void ChooseBrutality() => Choose(CombatStat.Brutality);
        public void ChooseTactics() => Choose(CombatStat.Tactics);
        public void ChooseSurvival() => Choose(CombatStat.Survival);

        private void Bind()
        {
            if (_stats != null)
                return;

            _stats = _player.RunStats;
            _stats.ChoiceOpened += Open;
            _stats.Changed += RefreshLevels;
            RefreshLevels();
        }

        private void Open()
        {
            foreach (var option in _options)
                option.RenderPreview(_stats, _player.Health.Max);
            _gameplayTime.SetMenuPaused(true);
            _panel.SetActive(true);
            _options[0].Button.Select();
        }

        private void Close()
        {
            if (_panel != null && _panel.activeSelf)
            {
                _panel.SetActive(false);
                if (_gameplayTime != null)
                    _gameplayTime.SetMenuPaused(false);
            }
        }

        private void Choose(CombatStat stat)
        {
            if (_stats != null && _stats.TryChoose(stat))
                Close();
        }

        private void RefreshLevels()
        {
            foreach (var option in _options)
                option.RenderLevel(_stats);
        }
    }
}
