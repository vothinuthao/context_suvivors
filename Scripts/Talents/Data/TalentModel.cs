namespace Talents.Data
{
    public class TalentModel
    {
        // create a model for the talent system
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconName { get; set; }
        public int Level { get; set; } = 1;
    }
}