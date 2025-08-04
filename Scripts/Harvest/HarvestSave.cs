using UnityEngine;
using OctoberStudio.Save;

[System.Serializable]
public class HarvestSave : ISave
{
    [SerializeField] private int remainingQuickHarvests = 3;
    [SerializeField] private float remainingSeconds = 86400f; // ví dụ 24h
    [SerializeField] private bool canHarvest = false;
    [SerializeField] private long lastHarvestTicks;
    public int RemainingQuickHarvests
    {
        get => remainingQuickHarvests;
        set => remainingQuickHarvests = value;
    }

    public float RemainingSeconds
    {
        get => remainingSeconds;
        set => remainingSeconds = value;
    }

    public bool CanHarvest
    {
        get => canHarvest;
        set => canHarvest = value;
    }

    // Bắt buộc phải có theo ISave interface
    public void Flush()
    {
        // Nếu cần xử lý gì đó trước khi lưu (ví dụ clamp giá trị), xử lý ở đây
        remainingQuickHarvests = Mathf.Clamp(remainingQuickHarvests, 0, 99);
        remainingSeconds = Mathf.Max(0f, remainingSeconds);
    }
    
    public void Reset()
    {
        remainingQuickHarvests = 3;
        remainingSeconds = 86400f;
        canHarvest = false;
    }
    
    public long LastHarvestTicks
    {
        get => lastHarvestTicks;
        set => lastHarvestTicks = value;
    }

}