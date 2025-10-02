using UnityEngine;
using OctoberStudio.Save;

[System.Serializable]
public class HarvestSave : ISave
{
    [SerializeField] private int remainingQuickHarvests = 3;
    [SerializeField] private float remainingSeconds = 86400f; // ví dụ 24h
    [SerializeField] private bool canHarvest = false;
    [SerializeField] private long lastHarvestTicks; // Thời điểm harvest cuối cùng
    [SerializeField] private bool isInitialized = false; // Đã khởi tạo chưa

    // Constructor - chỉ khởi tạo cho người chơi mới
    public HarvestSave()
    {
        // Chỉ khởi tạo nếu thực sự là save mới (chưa có thời gian harvest)
        if (!isInitialized && lastHarvestTicks == 0)
        {
            UnityEngine.Debug.Log("[HarvestSave] New save detected, initializing...");
            Reset();
        }
        else if (isInitialized && lastHarvestTicks > 0)
        {
            UnityEngine.Debug.Log($"[HarvestSave] Existing save loaded, preserving data. LastHarvest: {new System.DateTime(lastHarvestTicks):yyyy-MM-dd HH:mm:ss}");
        }
    }
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
        // Chỉ khởi tạo cho người chơi mới
        remainingQuickHarvests = 3;
        remainingSeconds = 86400f;
        canHarvest = false;
        lastHarvestTicks = System.DateTime.UtcNow.Ticks; // Lưu thời gian vào game lần đầu
        isInitialized = true;

        UnityEngine.Debug.Log($"[HarvestSave] Reset() completed. New harvest time: {new System.DateTime(lastHarvestTicks):yyyy-MM-dd HH:mm:ss}");
    }
    
    public long LastHarvestTicks
    {
        get => lastHarvestTicks;
        set => lastHarvestTicks = value;
    }

    // Phương thức cập nhật thời gian harvest
    public void UpdateHarvestTime()
    {
        lastHarvestTicks = System.DateTime.UtcNow.Ticks;
    }

}