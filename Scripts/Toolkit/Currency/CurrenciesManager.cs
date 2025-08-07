using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;

namespace OctoberStudio.Currency
{
    public class CurrenciesManager : MonoSingleton<CurrenciesManager>
    {

        [SerializeField] CurrenciesDatabase database;


        public Sprite GetIcon(string currencyId)
        {
            var data = database.GetCurrency(currencyId);

            if(data == null) return null;

            return data.Icon;
        }

        public string GetName(string currencyId)
        {
            var data = database.GetCurrency(currencyId);

            if (data == null) return null;

            return data.Name;
        }
        public void Add(string currencyId, int amount)
        {
            var save = GameController.SaveManager.GetSave<CurrencySave>(currencyId);

            if (save == null)
            {
                Debug.LogWarning($"❌ CurrencySave không tồn tại cho ID: {currencyId}");
                return;
            }

            save.Deposit(amount);

            Debug.Log($"✅ Đã cộng {amount} vào {currencyId}. Tổng: {save.Amount}");
        }
    }
}