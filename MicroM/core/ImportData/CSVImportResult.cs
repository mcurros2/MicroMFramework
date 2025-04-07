namespace MicroM.ImportData
{
    public class CSVImportResult
    {
        public int ProcessedCount { get; set; } = 0;
        public int SuccessCount { get; set; } = 0;
        public int ErrorCount { get; set; } = 0;
        public Dictionary<int, string> Errors { get; set; } = [];
    }
}
