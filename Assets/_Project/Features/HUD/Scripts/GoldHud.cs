using DeadCells.Currency;
using UnityEngine;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    public sealed class GoldHud : MonoBehaviour
    {
        [SerializeField] private GoldWallet _wallet;
        [SerializeField] private Text _amount;

        public Text Amount => _amount;

        private void OnEnable()
        {
            if (_wallet == null)
                return;
            _wallet.Changed += Render;
            Render(_wallet.Balance);
        }

        private void OnDisable()
        {
            if (_wallet != null)
                _wallet.Changed -= Render;
        }

        private void Render(int balance)
        {
            if (_amount != null)
                _amount.text = balance.ToString();
        }
    }
}
