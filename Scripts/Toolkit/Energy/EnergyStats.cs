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

        public event Action<int> onEnergyChanged;

        public void Deposit(int amount)
        {
            energy += amount;
            onEnergyChanged?.Invoke(energy);
        }

        public void Withdraw(int amount)
        {
            energy -= amount;
            if (energy < 0) energy = 0;
            onEnergyChanged?.Invoke(energy);
        }

        public bool TryWithdraw(int amount)
        {
            if (!CanAfford(amount)) return false;

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
            energy = 45;
            onEnergyChanged?.Invoke(energy);
        }
    }
}