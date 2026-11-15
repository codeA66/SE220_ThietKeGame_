using System;
using DeadCells.Combat;
using UnityEngine;

namespace DeadCells.Player
{
    public sealed class PlayerRunStats
    {
        public const float DamageGainPerLevel = 0.15f;

        private readonly Health _health;
        private readonly int _baseHealth;
        private readonly int[] _invested = new int[4];
        private PowerScrollPickup _pendingScroll;
        private int _closedFrame = -1;

        public PlayerRunStats(Health health)
        {
            _health = health;
            _baseHealth = health.Max;
        }

        public event Action<PowerScrollPickup> ChoiceOpened;
        public event Action Changed;

        public bool IsChoosing => _pendingScroll != null;
        public bool BlocksGameplayInput => IsChoosing || _closedFrame == Time.frameCount;

        public int Level(CombatStat stat) => _invested[(int)stat] + 1;

        public int ScaleDamage(int baseDamage, CombatStat stat)
        {
            int invested = _invested[(int)stat];
            return invested == 0 ? baseDamage : Mathf.RoundToInt(baseDamage * Mathf.Pow(1f + DamageGainPerLevel, invested));
        }

        public int PreviewMaxHealth(CombatStat stat) => CalculateMaxHealth(stat);

        internal bool TryBeginChoice(PowerScrollPickup scroll)
        {
            if (_pendingScroll != null || !scroll.IsAvailable || ChoiceOpened == null)
                return false;

            _pendingScroll = scroll;
            ChoiceOpened.Invoke(scroll);
            return true;
        }

        public bool TryChoose(CombatStat stat)
        {
            if (_pendingScroll == null || !_health.IsAlive ||
                stat < CombatStat.Brutality || stat > CombatStat.Survival)
                return false;

            _pendingScroll.Consume();
            CloseChoice();
            _invested[(int)stat]++;
            _health.SetMaxPreservingHealthRatio(CalculateMaxHealth(CombatStat.None));
            Changed?.Invoke();
            return true;
        }

        public void CancelChoice()
        {
            if (_pendingScroll != null)
                CloseChoice();
        }

        private void CloseChoice()
        {
            _pendingScroll = null;
            _closedFrame = Time.frameCount;
        }

        private int CalculateMaxHealth(CombatStat preview)
        {
            float brutality = HealthMultiplier(Invested(CombatStat.Brutality, preview), 0.65f, 34);
            float tactics = HealthMultiplier(Invested(CombatStat.Tactics, preview), 0.50f, 24);
            float survival = HealthMultiplier(Invested(CombatStat.Survival, preview), 0.70f, 59);
            return Mathf.RoundToInt(_baseHealth * brutality * tactics * survival);
        }

        private int Invested(CombatStat stat, CombatStat preview) => _invested[(int)stat] + (stat == preview ? 1 : 0);

        // Each colour has its own diminishing health curve. Add the decreasing increments within a colour.
        private static float HealthMultiplier(int invested, float firstGain, int plateau)
        {
            int levels = Mathf.Min(invested, plateau);
            return 1f + firstGain * levels * (1f - (levels - 1f) / (2f * plateau));
        }
    }
}
