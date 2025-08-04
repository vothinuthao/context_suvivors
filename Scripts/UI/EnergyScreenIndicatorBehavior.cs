using TMPro;
using UnityEngine;

namespace OctoberStudio.UI
{
    public class EnergyScreenIndicatorBehavior : ScalingLabelBehavior
    {
        private void Start()
        {
            if (GameController.EnergyManager != null)
            {
                GameController.EnergyManager.onEnergyChanged.AddListener(UpdateEnergyUI);
                UpdateEnergyUI(GameController.EnergyManager.Energy);
            }
            else
            {
                Debug.LogWarning("EnergyManager is still null in EnergyScreenIndicatorBehavior!");
            }
        }

        private void OnDestroy()
        {
            if (GameController.EnergyManager != null)
            {
                GameController.EnergyManager.onEnergyChanged.RemoveListener(UpdateEnergyUI);
            }
        }

        private void UpdateEnergyUI(int current)
        {
            if (GameController.EnergyManager == null)
                return;

            int max = GameController.EnergyManager.MaxRecoverableEnergy;
            string labelText = current > max ? $"{current}/{max}+" : $"{current}/{max}";

            label.text = labelText;
        }
    }
}