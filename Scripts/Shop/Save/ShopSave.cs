using OctoberStudio.Save;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OctoberStudio.Shop
{
    [System.Serializable]
    public class ShopSave : ISave
    {
        [System.Serializable]
        public class PurchaseRecord
        {
            [SerializeField] public int itemId;
            [SerializeField] public int purchaseCount;
            [SerializeField] public DateTime lastPurchaseTime;
            [SerializeField] public string lastPurchaseTimeString; // Serializable version

            public PurchaseRecord()
            {
                lastPurchaseTime = DateTime.Now;
                lastPurchaseTimeString = lastPurchaseTime.ToBinary().ToString();
            }

            public PurchaseRecord(int id)
            {
                itemId = id;
                purchaseCount = 0;
                lastPurchaseTime = DateTime.Now;
                lastPurchaseTimeString = lastPurchaseTime.ToBinary().ToString();
            }

            public void AddPurchase()
            {
                purchaseCount++;
                lastPurchaseTime = DateTime.Now;
                lastPurchaseTimeString = lastPurchaseTime.ToBinary().ToString();
            }

            public void ResetPurchases()
            {
                purchaseCount = 0;
                lastPurchaseTime = DateTime.Now;
                lastPurchaseTimeString = lastPurchaseTime.ToBinary().ToString();
            }

            public void RestoreDateTime()
            {
                if (!string.IsNullOrEmpty(lastPurchaseTimeString))
                {
                    try
                    {
                        long binary = Convert.ToInt64(lastPurchaseTimeString);
                        lastPurchaseTime = DateTime.FromBinary(binary);
                    }
                    catch
                    {
                        lastPurchaseTime = DateTime.Now;
                        lastPurchaseTimeString = lastPurchaseTime.ToBinary().ToString();
                    }
                }
                else
                {
                    lastPurchaseTime = DateTime.Now;
                    lastPurchaseTimeString = lastPurchaseTime.ToBinary().ToString();
                }
            }
        }

        [System.Serializable]
        public class GachaPityRecord
        {
            [SerializeField] public string gachaType; // "rare" or "epic"
            [SerializeField] public int currentPity;
            [SerializeField] public int totalPulls;
            [SerializeField] public DateTime lastPullTime;
            [SerializeField] public string lastPullTimeString; // Serializable version

            public GachaPityRecord()
            {
                lastPullTime = DateTime.Now;
                lastPullTimeString = lastPullTime.ToBinary().ToString();
            }

            public GachaPityRecord(string type)
            {
                gachaType = type;
                currentPity = 0;
                totalPulls = 0;
                lastPullTime = DateTime.Now;
                lastPullTimeString = lastPullTime.ToBinary().ToString();
            }

            public void AddPull(int count = 1)
            {
                currentPity += count;
                totalPulls += count;
                lastPullTime = DateTime.Now;
                lastPullTimeString = lastPullTime.ToBinary().ToString();
            }

            public void ResetPity()
            {
                currentPity = 0;
                lastPullTime = DateTime.Now;
                lastPullTimeString = lastPullTime.ToBinary().ToString();
            }

            public void RestoreDateTime()
            {
                if (!string.IsNullOrEmpty(lastPullTimeString))
                {
                    try
                    {
                        long binary = Convert.ToInt64(lastPullTimeString);
                        lastPullTime = DateTime.FromBinary(binary);
                    }
                    catch
                    {
                        lastPullTime = DateTime.Now;
                        lastPullTimeString = lastPullTime.ToBinary().ToString();
                    }
                }
                else
                {
                    lastPullTime = DateTime.Now;
                    lastPullTimeString = lastPullTime.ToBinary().ToString();
                }
            }
        }

        [SerializeField] public PurchaseRecord[] purchaseRecords = new PurchaseRecord[0];
        [SerializeField] public GachaPityRecord[] gachaPityRecords = new GachaPityRecord[0];
        [SerializeField] public int totalGems;
        [SerializeField] public DateTime lastShopRefreshTime;
        [SerializeField] public string lastShopRefreshTimeString;

        // Cached lists for performance
        private List<PurchaseRecord> _purchaseList;
        private List<GachaPityRecord> _gachaPityList;

        public List<PurchaseRecord> PurchaseList
        {
            get
            {
                if (_purchaseList == null)
                {
                    _purchaseList = new List<PurchaseRecord>(purchaseRecords);
                    foreach (var record in _purchaseList)
                    {
                        record.RestoreDateTime();
                    }
                }
                return _purchaseList;
            }
        }

        public List<GachaPityRecord> GachaPityList
        {
            get
            {
                if (_gachaPityList == null)
                {
                    _gachaPityList = new List<GachaPityRecord>(gachaPityRecords);
                    foreach (var record in _gachaPityList)
                    {
                        record.RestoreDateTime();
                    }
                }
                return _gachaPityList;
            }
        }

        public void Init()
        {
            if (purchaseRecords == null)
                purchaseRecords = new PurchaseRecord[0];

            if (gachaPityRecords == null)
                gachaPityRecords = new GachaPityRecord[0];

            _purchaseList = new List<PurchaseRecord>(purchaseRecords);
            _gachaPityList = new List<GachaPityRecord>(gachaPityRecords);

            // Restore DateTime objects
            foreach (var record in _purchaseList)
            {
                record.RestoreDateTime();
            }

            foreach (var record in _gachaPityList)
            {
                record.RestoreDateTime();
            }

            // Restore last refresh time
            if (!string.IsNullOrEmpty(lastShopRefreshTimeString))
            {
                try
                {
                    long binary = Convert.ToInt64(lastShopRefreshTimeString);
                    lastShopRefreshTime = DateTime.FromBinary(binary);
                }
                catch
                {
                    lastShopRefreshTime = DateTime.Now;
                    lastShopRefreshTimeString = lastShopRefreshTime.ToBinary().ToString();
                }
            }
            else
            {
                lastShopRefreshTime = DateTime.Now;
                lastShopRefreshTimeString = lastShopRefreshTime.ToBinary().ToString();
            }
        }

        public void Flush()
        {
            // Update DateTime strings before saving
            foreach (var record in PurchaseList)
            {
                record.lastPurchaseTimeString = record.lastPurchaseTime.ToBinary().ToString();
            }

            foreach (var record in GachaPityList)
            {
                record.lastPullTimeString = record.lastPullTime.ToBinary().ToString();
            }

            lastShopRefreshTimeString = lastShopRefreshTime.ToBinary().ToString();

            // Sync lists to arrays
            SyncListsToArrays();
        }

        private void SyncListsToArrays()
        {
            if (_purchaseList != null)
                purchaseRecords = _purchaseList.ToArray();

            if (_gachaPityList != null)
                gachaPityRecords = _gachaPityList.ToArray();
        }

        public void ForceSync()
        {
            SyncListsToArrays();
        }

        public void Clear()
        {
            if (_purchaseList != null)
                _purchaseList.Clear();

            if (_gachaPityList != null)
                _gachaPityList.Clear();

            purchaseRecords = new PurchaseRecord[0];
            gachaPityRecords = new GachaPityRecord[0];
            totalGems = 0;
            lastShopRefreshTime = DateTime.Now;
            lastShopRefreshTimeString = lastShopRefreshTime.ToBinary().ToString();
        }

        // Purchase record methods
        public PurchaseRecord GetPurchaseRecord(int itemId)
        {
            return PurchaseList.FirstOrDefault(r => r.itemId == itemId);
        }

        public int GetPurchaseCount(int itemId)
        {
            var record = GetPurchaseRecord(itemId);
            return record?.purchaseCount ?? 0;
        }

        public DateTime GetLastPurchaseTime(int itemId)
        {
            var record = GetPurchaseRecord(itemId);
            return record?.lastPurchaseTime ?? DateTime.MinValue;
        }

        public void AddPurchase(int itemId)
        {
            var record = GetPurchaseRecord(itemId);
            if (record == null)
            {
                record = new PurchaseRecord(itemId);
                PurchaseList.Add(record);
            }

            record.AddPurchase();
            SyncListsToArrays();
        }

        public void ResetPurchaseCount(int itemId)
        {
            var record = GetPurchaseRecord(itemId);
            if (record != null)
            {
                record.ResetPurchases();
                SyncListsToArrays();
            }
        }

        public bool ShouldResetPurchases(ShopItemModel item)
        {
            if (item.ResetTimeHours == 0) return false;

            var lastPurchaseTime = GetLastPurchaseTime(item.ID);
            if (lastPurchaseTime == DateTime.MinValue) return false;

            var timeSinceLastPurchase = DateTime.Now - lastPurchaseTime;
            return timeSinceLastPurchase.TotalHours >= item.ResetTimeHours;
        }

        // Gacha pity methods
        public GachaPityRecord GetGachaPityRecord(string gachaType)
        {
            return GachaPityList.FirstOrDefault(r => r.gachaType == gachaType);
        }

        public int GetCurrentPity(string gachaType)
        {
            var record = GetGachaPityRecord(gachaType);
            return record?.currentPity ?? 0;
        }

        public int GetTotalPulls(string gachaType)
        {
            var record = GetGachaPityRecord(gachaType);
            return record?.totalPulls ?? 0;
        }

        public void AddGachaPull(string gachaType, int count = 1)
        {
            var record = GetGachaPityRecord(gachaType);
            if (record == null)
            {
                record = new GachaPityRecord(gachaType);
                GachaPityList.Add(record);
            }

            record.AddPull(count);
            SyncListsToArrays();
        }

        public void ResetGachaPity(string gachaType)
        {
            var record = GetGachaPityRecord(gachaType);
            if (record != null)
            {
                record.ResetPity();
                SyncListsToArrays();
            }
        }

        // Currency methods
        public bool CanAfford(ShopItemModel item)
        {
            switch (item.PriceType)
            {
                case PriceType.gold:
                    return GameController.TempGold != null && 
                           GameController.TempGold.Amount >= item.PriceAmount;
                case PriceType.gem:
                    return totalGems >= item.PriceAmount;
                case PriceType.usd:
                    return true; // Real money purchases are handled by platform
                default:
                    return false;
            }
        }

        public bool SpendCurrency(ShopItemModel item)
        {
            if (!CanAfford(item)) return false;

            switch (item.PriceType)
            {
                case PriceType.gold:
                    GameController.TempGold?.Withdraw((int)item.PriceAmount);
                    return true;
                case PriceType.gem:
                    totalGems -= (int)item.PriceAmount;
                    return true;
                case PriceType.usd:
                    // Real money purchases handled by platform
                    return true;
                default:
                    return false;
            }
        }

        public void AddGems(int amount)
        {
            totalGems += amount;
            if (totalGems < 0) totalGems = 0;
        }

        public void AddGold(int amount)
        {
            GameController.TempGold?.Deposit(amount);
        }

        // Shop refresh methods
        public void UpdateLastRefreshTime()
        {
            lastShopRefreshTime = DateTime.Now;
            lastShopRefreshTimeString = lastShopRefreshTime.ToBinary().ToString();
        }

        public bool ShouldRefreshDailyItems()
        {
            var timeSinceRefresh = DateTime.Now - lastShopRefreshTime;
            return timeSinceRefresh.TotalHours >= 24;
        }

        // Statistics methods
        public Dictionary<string, object> GetShopStatistics()
        {
            var stats = new Dictionary<string, object>();
            
            stats["TotalPurchases"] = PurchaseList.Sum(r => r.purchaseCount);
            stats["TotalGems"] = totalGems;
            stats["TotalGachaPulls"] = GachaPityList.Sum(r => r.totalPulls);
            stats["UniqueItemsPurchased"] = PurchaseList.Count(r => r.purchaseCount > 0);
            
            return stats;
        }

        public List<PurchaseRecord> GetRecentPurchases(int hours = 24)
        {
            var cutoffTime = DateTime.Now.AddHours(-hours);
            return PurchaseList.Where(r => r.lastPurchaseTime > cutoffTime).ToList();
        }
    }
}