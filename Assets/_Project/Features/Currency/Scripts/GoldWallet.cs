using System;
using DeadCells.Core;
using UnityEngine;

namespace DeadCells.Currency
{
    // Scene-facing view of the run's gold. The gold itself lives in DeadCells.Core.Wallet, filled by the
    // coins and gems that LootDropper throws out of dead enemies, so GoldHud, the pickup feedback and the
    // top-right RunHud all show the same number. Changed only fires when the gold amount changes, not
    // when a gem is picked up.
    [DisallowMultipleComponent]
    public sealed class GoldWallet : MonoBehaviour
    {
        private int _lastGold;

        public int Balance => Wallet.Gold;
        public event Action<int> Changed;

        public void Credit(int amount) => Wallet.AddGold(amount);

        private void OnEnable()
        {
            _lastGold = Wallet.Gold;
            Wallet.Changed += OnWalletChanged;
        }

        private void OnDisable() => Wallet.Changed -= OnWalletChanged;

        private void OnWalletChanged(int gold, int gems)
        {
            if (gold == _lastGold)
                return;
            _lastGold = gold;
            Changed?.Invoke(gold);
        }
    }
}
