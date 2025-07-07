namespace TwoSleepyCats.CSVReader.Core
{
    public interface ICsvModel
    {
        /// <summary>
        /// Return CSV filename without path. Example: "characters.csv"
        /// </summary>
        string GetCsvFileName();
        
        /// <summary>
        /// Called after all data is loaded and mapped. Use for post-processing.
        /// </summary>
        void OnDataLoaded();
        
        /// <summary>
        /// Validate the loaded data. Return false to mark as invalid.
        /// </summary>
        bool ValidateData();
    }
}