namespace TwoSleepyCats.Samples.AdvancedSample
{
    public enum GuildType
    {
        Casual,
        Competitive,
        Roleplay,
        Social
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    [System.Serializable]
    public class ItemStats
    {
        public int Attack;
        public int Defense;
        public int Speed;
        public float CritChance;
        
        public override string ToString()
        {
            return $"ATK:{Attack} DEF:{Defense} SPD:{Speed} CRIT:{CritChance:P1}";
        }
    }
}