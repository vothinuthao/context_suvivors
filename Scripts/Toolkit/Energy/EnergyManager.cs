
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    public class EnergyManager : MonoSingleton<EnergyManager>
    {
        [Header("Energy Settings")]
        [SerializeField] private int defaultEnergy = 45;
        [SerializeField] private int maxRecoverableEnergy = 45;
        [SerializeField] private int recoverAmount = 5;
        [SerializeField] private float recoverInterval = 900f; // 15 phút

        [Header("Icon")]
        [SerializeField] private Sprite energyIcon;

        private float recoverTimer;
        private EnergyStats save;

        public int Energy => save?.Energy ?? 0;
        public int MaxRecoverableEnergy => maxRecoverableEnergy;

        public UnityEvent<int> onEnergyChanged = new UnityEvent<int>();

        public void Init()
        {
            Debug.Log("[EnergyManager] 🔄 Initializing...");
            save = GameController.SaveManager.GetSave<EnergyStats>("energy"); // Key chuẩn hóa giống coin

            if (save == null)
            {
                Debug.LogError("Failed to load EnergyStats for EnergyManager!");
                return;
            }

            Debug.Log($"[EnergyManager] 📁 Loaded energy: {save.Energy}");

            // Forward event từ Save → UI
            save.onEnergyChanged += (value) =>
            {
                Debug.Log($"[EnergyManager] ⚡ Energy changed: {value}");
                onEnergyChanged.Invoke(value);
            };

            // Khởi tạo nếu chưa có
            if (save.Energy <= 0)
            {
                Debug.Log($"[EnergyManager] 🆕 New save detected, setting to default: {defaultEnergy}");
                save.Deposit(defaultEnergy);
                GameController.SaveManager.Save();
            }
            else
            {
                Debug.Log($"[EnergyManager] ✅ Energy loaded successfully: {save.Energy}");
                onEnergyChanged.Invoke(save.Energy);
            }
        }

        private void Update()
        {
            if (save == null || save.Energy >= maxRecoverableEnergy)
                return;

            recoverTimer += Time.deltaTime;
            if (recoverTimer >= recoverInterval)
            {
                save.Deposit(recoverAmount);
                recoverTimer = 0f;

                if (save.Energy > maxRecoverableEnergy)
                {
                    save.Withdraw(save.Energy - maxRecoverableEnergy); // Trừ phần dư về max
                }

                GameController.SaveManager.Save();
            }
        }

        public void AddEnergy(int amount)
        {
            if (save == null) return;

            save.Deposit(amount);
            GameController.SaveManager.Save();
        }

        public bool TrySpendEnergy(int amount)
        {
            if (save == null)
            {
                Debug.LogWarning("[EnergyManager] ❌ Cannot spend energy - save is null!");
                return false;
            }

            if (!save.TryWithdraw(amount))
            {
                Debug.Log($"[EnergyManager] ❌ Not enough energy to spend {amount} (current: {save.Energy})");
                return false;
            }

            Debug.Log($"[EnergyManager] 💸 Spent {amount} energy (remaining: {save.Energy})");
            recoverTimer = 0f;
            GameController.SaveManager.Save();
            return true;
        }

        public void ResetEnergy()
        {
            if (save == null) return;

            Debug.Log($"[EnergyManager] 🔄 Resetting energy from {save.Energy} to {defaultEnergy}");
            save.Clear();
            GameController.SaveManager.Save();
            Debug.Log($"[EnergyManager] ✅ Energy reset complete: {save.Energy}");
        }

        public Sprite GetIcon()
        {
            return energyIcon;
        }
    }
}
