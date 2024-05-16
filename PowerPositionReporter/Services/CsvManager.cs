using Axpo;
using Microsoft.Extensions.Logging;
using PowerPositionReporter.Models;
using System.Globalization;

namespace PowerPositionReporter.Services
{
    public sealed class CsvManager : ICsvManager
    {
        public CsvManager(ILogger<CsvManager> logger,
                          IPowerService powerService)
        {
            Logger = logger;
            PowerService = powerService;
        }

        public async Task GenerateReportAsync(string outputPath)
        {
            try
            {
                Logger.LogInformation("Generating power position report.");
                var trades = await PowerService.GetTradesAsync(DateTime.UtcNow.AddDays(1));
                //var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
                //var date = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, timeZone);

                var hourlyVolumes = ExtractData(trades);

                var csvContent = GetContent(hourlyVolumes);
                var filename = GetFileName();
                var fullPath = Path.Combine(outputPath, filename);

                await WriteToFileAsync(fullPath, csvContent);
            }
            catch (PowerServiceException ex)
            {
                Logger.LogWarning("Something went wrong while extracting data: {ex}", ex);
                await GenerateReportAsync(outputPath);
            }
        }

        private List<CsvModel> ExtractData(IEnumerable<PowerTrade> trades)
        {
            Logger.LogInformation("Extracting power trades.");
            return trades
                    .SelectMany(trade => trade.Periods)
                    .GroupBy(period => period.Period)
                    .Select(g => new CsvModel { Datetime = DateTime.Today.AddDays(1).AddHours(g.Key - 1).ToUniversalTime(), Volume = g.Sum(p => p.Volume) })
                    .ToList();
        }

        private string GetContent(IEnumerable<CsvModel> csv)
        {
            Logger.LogInformation("Getting power position content.");
            return "Datetime;Volume\n" + string.Join("\n", csv.Select(v => $"{v.Datetime:yyyy-MM-ddTHH:mm:ssZ};{v.Volume.ToString("F2", CultureInfo.InvariantCulture)}"));
        }

        private string GetFileName()
        {
            Logger.LogInformation("Getting file name.");
            return $"PowerPosition_{DateTime.UtcNow.AddDays(1):yyyyMMdd}_{DateTime.UtcNow:yyyyMMddHHmm}.csv";
        }

        private Task WriteToFileAsync(string fullPath, string csvContent)
        {
            Logger.LogInformation("Writing content to CSV file.");
            return File.WriteAllTextAsync(fullPath, csvContent);
        }

        private readonly ILogger<CsvManager> Logger;
        private readonly IPowerService PowerService;
    }
}
