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
    }
}