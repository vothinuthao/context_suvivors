using OctoberStudio.UI;
using UnityEngine;

namespace OctoberStudio.Currency
{
    public class CurrencyScreenIncicatorBehavior : ScalingLabelBehavior
    {
        [Tooltip("The unique identificator of the currency that is attached to this ui. There must be an entry with the same id in the Currencies Database")]
        [SerializeField] string currencyID;

        public CurrencySave Currency { get; private set; }

        private void Start()
        {
            if (GameController.CurrenciesManager != null)
            {
                Currency = GameController.SaveManager.GetSave<CurrencySave>(currencyID);

                SetAmount(Currency.Amount);

                icon.sprite = GameController.CurrenciesManager.GetIcon(currencyID);

                Currency.onGoldAmountChanged += SetAmount;

            }
            else
            {
                Debug.LogWarning("CurrenciesManager is still null in CurrencyScreenIncicatorBehavior!");
            }
        }

        private void OnDestroy()
        {
            Currency.onGoldAmountChanged -= SetAmount;
        }
    }
}