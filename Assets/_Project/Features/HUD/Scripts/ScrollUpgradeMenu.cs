using System.Collections;
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
        [SerializeField] private ScrollPickupPresentation _presentation;

        private PlayerRunStats _stats;
        private PlayerInputReader _playerInput;
        private Coroutine _reveal;
        private bool _hasPause;
        private bool _disabledPlayerInput;

        private void Start() => Bind();

        private void OnEnable()
        {
            if (_player != null && _player.RunStats != null)
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
            Close(false);
        }

        public void ChooseBrutality() => Choose(CombatStat.Brutality);
        public void ChooseTactics() => Choose(CombatStat.Tactics);
        public void ChooseSurvival() => Choose(CombatStat.Survival);

        private void Bind()
        {
            if (_stats != null || _player == null || _player.RunStats == null)
                return;

            _playerInput = _player.GetComponent<PlayerInputReader>();
            _stats = _player.RunStats;
            _stats.ChoiceOpened += Open;
            _stats.Changed += RefreshLevels;
            RefreshLevels();
        }

        private void Open(PowerScrollPickup pickup)
        {
            foreach (var option in _options)
                option.RenderPreview(_stats, _player.Health.Max);
            if (_playerInput == null && _player != null)
                _playerInput = _player.GetComponent<PlayerInputReader>();
            if (_playerInput != null && _playerInput.enabled)
            {
                _playerInput.enabled = false;
                _disabledPlayerInput = true;
            }
            if (_gameplayTime != null)
            {
                _gameplayTime.SetMenuPaused(true);
                _hasPause = true;
            }
            _reveal = StartCoroutine(Reveal(pickup));
        }

        private IEnumerator Reveal(PowerScrollPickup pickup)
        {
            if (_presentation != null)
                yield return _presentation.Play(pickup);

            _panel.SetActive(true);
            _options[0].Button.Select();
            _reveal = null;
        }

        private void Close(bool completed)
        {
            if (_reveal != null)
                StopCoroutine(_reveal);
            _reveal = null;
            if (_panel != null)
                _panel.SetActive(false);
            if (_presentation != null)
                _presentation.Finish(completed);
            if (_hasPause && _gameplayTime != null)
                _gameplayTime.SetMenuPaused(false);
            _hasPause = false;
            if (_disabledPlayerInput)
            {
                _disabledPlayerInput = false;
                if (_playerInput != null &&
                    _player != null &&
                    _player.enabled &&
                    (_gameplayTime == null || !_gameplayTime.IsMenuPaused))
                {
                    _playerInput.enabled = true;
                }
            }
        }

        private void Choose(CombatStat stat)
        {
            if (_stats == null || !_stats.TryChoose(stat))
                return;

            Close(true);
            if (_presentation != null)
                _presentation.Confirm(stat);
        }

        private void RefreshLevels()
        {
            foreach (var option in _options)
                option.RenderLevel(_stats);
        }
    }
}
