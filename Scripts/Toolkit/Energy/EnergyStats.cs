using OctoberStudio.Save;
using System;
using UnityEngine;

namespace OctoberStudio
{
    [System.Serializable]
    public class EnergyStats : ISave
    {
        [SerializeField] private int energy = 45;

        public int Energy => energy;

        // Constructor để debug
        public EnergyStats()
        {
            UnityEngine.Debug.Log($"[EnergyStats] Constructor called, energy: {energy}");
        }

        public event Action<int> onEnergyChanged;

        public void Deposit(int amount)
        {
            UnityEngine.Debug.Log($"[EnergyStats] Deposit {amount}, {energy} → {energy + amount}");
            energy += amount;
            onEnergyChanged?.Invoke(energy);
        }

        public void Withdraw(int amount)
        {
            UnityEngine.Debug.Log($"[EnergyStats] Withdraw {amount}, {energy} → {energy - amount}");
            energy -= amount;
            if (energy < 0) energy = 0;
            onEnergyChanged?.Invoke(energy);
        }

        public bool TryWithdraw(int amount)
        {
            if (!CanAfford(amount))
            {
                UnityEngine.Debug.Log($"[EnergyStats] TryWithdraw {amount} FAILED - insufficient energy ({energy})");
                return false;
            }

            UnityEngine.Debug.Log($"[EnergyStats] TryWithdraw {amount} SUCCESS, {energy} → {energy - amount}");
            energy -= amount;
            onEnergyChanged?.Invoke(energy);
            return true;
        }

        public bool CanAfford(int requiredAmount)
        {
            return energy >= requiredAmount;
        }

        public void Flush()
        {
            // Optional: logic trước khi save
        }

        public void Clear()
        {
            UnityEngine.Debug.Log($"[EnergyStats] Clear() called, resetting from {energy} to 45");
            energy = 45;
            onEnergyChanged?.Invoke(energy);
        }
    }
}