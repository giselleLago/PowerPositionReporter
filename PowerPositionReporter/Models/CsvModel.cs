namespace PowerPositionReporter.Models
{
    internal record CsvModel
    {
        public DateTime Datetime { get; set; }

        public double Volume { get; set; }
    }
}
