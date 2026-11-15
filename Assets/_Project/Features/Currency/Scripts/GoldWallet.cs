using System;
using UnityEngine;

namespace DeadCells.Currency
{
    [DisallowMultipleComponent]
    public sealed class GoldWallet : MonoBehaviour
    {
        public int Balance { get; private set; }
        public event Action<int> Changed;

        public void Credit(int amount)
        {
            if (amount <= 0)
                return;

            Balance += amount;
            Changed?.Invoke(Balance);
        }
    }
}
