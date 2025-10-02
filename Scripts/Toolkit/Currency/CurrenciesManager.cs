using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio.Currency
{
    public class CurrenciesManager : MonoSingleton<CurrenciesManager>
    {
        [SerializeField] CurrenciesDatabase database;

        private CurrencySave goldCurrency;
        private CurrencySave gemCurrency;
        private CurrencySave orcCurrency;
        private CurrencySave piecesCurrency;

        protected override void Initialize()
        {
            base.Initialize();
            
            // Initialize currency references when manager starts
            InitializeCurrencyReferences();
        }

        /// <summary>
        /// Initialize currency references - FIXED: Using CurrencySave system
        /// </summary>
        private void InitializeCurrencyReferences()
        {
            if (GameController.SaveManager != null)
            {
                goldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");
                gemCurrency = GameController.SaveManager.GetSave<CurrencySave>("gem");
                orcCurrency = GameController.SaveManager.GetSave<CurrencySave>("orc");
                piecesCurrency = GameController.SaveManager.GetSave<CurrencySave>("pieces");

                Debug.Log($"[CurrenciesManager] Currency references initialized - Gold: {goldCurrency?.Amount ?? 0}, Gems: {gemCurrency?.Amount ?? 0}, Orc: {orcCurrency?.Amount ?? 0}, Pieces: {piecesCurrency?.Amount ?? 0}");
            }
            else
            {
                Debug.LogWarning("[CurrenciesManager] SaveManager not ready, currencies will be initialized later");
            }
        }

        /// <summary>
        /// Get currency icon by ID
        /// </summary>
        public Sprite GetIcon(string currencyId)
        {
            var data = database.GetCurrency(currencyId);

            if(data == null) return null;

            return data.Icon;
        }

        /// <summary>
        /// Get currency name by ID
        /// </summary>
        public string GetName(string currencyId)
        {
            var data = database.GetCurrency(currencyId);

            if (data == null) return null;

            return data.Name;
        }

        /// <summary>
        /// Add currency amount - FIXED: Using CurrencySave system
        /// </summary>
        public void Add(string currencyId, int amount)
        {
            var save = GetCurrencySave(currencyId);

            if (save == null)
            {
                Debug.LogWarning($"❌ CurrencySave không tồn tại cho ID: {currencyId}");
                return;
            }

            save.Deposit(amount);

            Debug.Log($"✅ Đã cộng {amount} vào {currencyId}. Tổng: {save.Amount}");
        }

        /// <summary>
        /// Try to spend currency - NEW: Added spending functionality
        /// </summary>
        public bool TrySpend(string currencyId, int amount)
        {
            var save = GetCurrencySave(currencyId);

            if (save == null)
            {
                Debug.LogWarning($"❌ CurrencySave không tồn tại cho ID: {currencyId}");
                return false;
            }

            bool success = save.TryWithdraw(amount);
            
            if (success)
            {
                Debug.Log($"✅ Đã trừ {amount} từ {currencyId}. Còn lại: {save.Amount}");
            }
            else
            {
                Debug.Log($"❌ Không đủ {currencyId} để trừ {amount}. Hiện có: {save.Amount}");
            }

            return success;
        }

        /// <summary>
        /// Get current currency amount - NEW: Added getter functionality
        /// </summary>
        public int GetAmount(string currencyId)
        {
            var save = GetCurrencySave(currencyId);
            return save?.Amount ?? 0;
        }

        /// <summary>
        /// Check if player can afford amount - NEW: Added afford check
        /// </summary>
        public bool CanAfford(string currencyId, int amount)
        {
            var save = GetCurrencySave(currencyId);
            return save != null && save.CanAfford(amount);
        }

        /// <summary>
        /// Get CurrencySave for given currency ID - FIXED: Centralized currency retrieval
        /// </summary>
        private CurrencySave GetCurrencySave(string currencyId)
        {
            if (GameController.SaveManager == null)
            {
                Debug.LogError("[CurrenciesManager] SaveManager is null!");
                return null;
            }

            // Use cached references for common currencies
            switch (currencyId.ToLower())
            {
                case "gold":
                    if (goldCurrency == null)
                    {
                        goldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");
                    }
                    return goldCurrency;

                case "gem":
                case "gems":
                    if (gemCurrency == null)
                    {
                        gemCurrency = GameController.SaveManager.GetSave<CurrencySave>("gem");
                    }
                    return gemCurrency;

                case "orc":
                case "orcs":
                    if (orcCurrency == null)
                    {
                        orcCurrency = GameController.SaveManager.GetSave<CurrencySave>("orc");
                    }
                    return orcCurrency;

                case "pieces":
                case "piece":
                    if (piecesCurrency == null)
                    {
                        piecesCurrency = GameController.SaveManager.GetSave<CurrencySave>("pieces");
                    }
                    return piecesCurrency;

                default:
                    // For other currencies, get directly from SaveManager
                    return GameController.SaveManager.GetSave<CurrencySave>(currencyId);
            }
        }

        /// <summary>
        /// Subscribe to currency amount changes - NEW: Event subscription
        /// </summary>
        public void SubscribeToCurrencyChanges(string currencyId, System.Action<int> onAmountChanged)
        {
            var save = GetCurrencySave(currencyId);
            if (save != null)
            {
                save.onGoldAmountChanged += onAmountChanged;
                Debug.Log($"[CurrenciesManager] Subscribed to {currencyId} changes");
            }
            else
            {
                Debug.LogWarning($"[CurrenciesManager] Cannot subscribe to {currencyId} - currency not found");
            }
        }

        /// <summary>
        /// Unsubscribe from currency amount changes - NEW: Event unsubscription
        /// </summary>
        public void UnsubscribeFromCurrencyChanges(string currencyId, System.Action<int> onAmountChanged)
        {
            var save = GetCurrencySave(currencyId);
            if (save != null)
            {
                save.onGoldAmountChanged -= onAmountChanged;
                Debug.Log($"[CurrenciesManager] Unsubscribed from {currencyId} changes");
            }
        }

        /// <summary>
        /// Force refresh currency references - NEW: Manual refresh capability
        /// </summary>
        public void RefreshCurrencyReferences()
        {
            goldCurrency = null;
            gemCurrency = null;
            orcCurrency = null;
            piecesCurrency = null;
            InitializeCurrencyReferences();
        }

        /// <summary>
        /// Get all available currency IDs from database - NEW: Database querying
        /// </summary>
        public string[] GetAvailableCurrencyIds()
        {
            if (database == null) return new string[0];

            // This assumes CurrenciesDatabase has a method to get all currency IDs
            // You might need to add this method to CurrenciesDatabase
            var currencies = new System.Collections.Generic.List<string>();
            
            // Add standard currencies
            currencies.Add("gold");
            currencies.Add("gem");
            currencies.Add("orc");
            currencies.Add("pieces");
            
            return currencies.ToArray();
        }

        /// <summary>
        /// Validate currency exists in database - NEW: Validation
        /// </summary>
        public bool IsCurrencyValid(string currencyId)
        {
            if (database == null) return false;
            return database.GetCurrency(currencyId) != null;
        }

        /// <summary>
        /// Get formatted currency display text - NEW: Display formatting
        /// </summary>
        public string GetFormattedAmount(string currencyId, int amount)
        {
            var currencyName = GetName(currencyId);
            
            if (string.IsNullOrEmpty(currencyName))
            {
                currencyName = currencyId;
            }

            // Format large numbers
            if (amount >= 1000000)
            {
                return $"{amount / 1000000f:F1}M {currencyName}";
            }
            else if (amount >= 1000)
            {
                return $"{amount / 1000f:F1}K {currencyName}";
            }
            else
            {
                return $"{amount} {currencyName}";
            }
        }

        /// <summary>
        /// Debug method to log all currency amounts - NEW: Debugging
        /// </summary>
        [ContextMenu("Log All Currency Amounts")]
        public void LogAllCurrencyAmounts()
        {
            Debug.Log("[CurrenciesManager] Current Currency Amounts:");

            var goldAmount = GetAmount("gold");
            var gemAmount = GetAmount("gem");
            var orcAmount = GetAmount("orc");
            var piecesAmount = GetAmount("pieces");

            Debug.Log($"  Gold: {goldAmount}");
            Debug.Log($"  Gems: {gemAmount}");
            Debug.Log($"  Orc: {orcAmount}");
            Debug.Log($"  Pieces: {piecesAmount}");
        }

        /// <summary>
        /// Quick access properties for common currencies - NEW: Convenience properties
        /// </summary>
        public int GoldAmount => GetAmount("gold");
        public int GemAmount => GetAmount("gem");
        public int OrcAmount => GetAmount("orc");
        public int PiecesAmount => GetAmount("pieces");

        /// <summary>
        /// Quick access methods for common operations - NEW: Convenience methods
        /// </summary>
        public void AddGold(int amount) => Add("gold", amount);
        public void AddGems(int amount) => Add("gem", amount);
        public void AddOrc(int amount) => Add("orc", amount);
        public void AddPieces(int amount) => Add("pieces", amount);
        public bool TrySpendGold(int amount) => TrySpend("gold", amount);
        public bool TrySpendGems(int amount) => TrySpend("gem", amount);
        public bool TrySpendOrc(int amount) => TrySpend("orc", amount);
        public bool TrySpendPieces(int amount) => TrySpend("pieces", amount);
        public bool CanAffordGold(int amount) => CanAfford("gold", amount);
        public bool CanAffordGems(int amount) => CanAfford("gem", amount);
        public bool CanAffordOrc(int amount) => CanAfford("orc", amount);
        public bool CanAffordPieces(int amount) => CanAfford("pieces", amount);
    }
}