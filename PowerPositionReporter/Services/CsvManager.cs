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

        public async Task GenerateReportAsync(string path, int retryAttempt, string location = "Europe/Berlin")
        {
            try
            {
                //DST
                var date = GetDaylightSavingTime(location);

                Logger.LogInformation("Generating power position report.");
                var trades = await PowerService.GetTradesAsync(date.AddDays(1));


                var hourlyVolumes = ExtractData(trades);

                var csvContent = GetContent(hourlyVolumes);
                var filename = GetFileName(date);
                var fullPath = Path.Combine(path, filename);

                Logger.LogInformation("Writing content to file: {fileName}.", filename);
                await WriteToFileAsync(fullPath, csvContent);
            }
            catch (PowerServiceException ex)
            {
                //Here we can use a RetryPolicy installing "Polly" NuGet Package and define the basic or the exponential backoff 
                if (retryAttempt > 1)
                {
                    retryAttempt--;

                    Logger.LogWarning("Something went wrong while extracting data: {ex}.", ex);

                    await Task.Delay(1000);

                    Logger.LogInformation("Waiting 1 second before the next attempt.");
                    await GenerateReportAsync(path, retryAttempt, location);
                }
                else
                {
                    var date = GetDaylightSavingTime(location);
                    Logger.LogError("The program was not able to extract data at {date} .", date);
                }
            }
        }

        private static DateTime GetDaylightSavingTime(string location)
        {
            var localNow = DateTime.Now;
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(location);
            return TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTime(localNow, timeZone), timeZone);
        }

        private static List<CsvModel> ExtractData(IEnumerable<PowerTrade> trades)
        {
            return trades
                    .SelectMany(trade => trade.Periods)
                    .GroupBy(period => period.Period)
                    .Select(g => new CsvModel { Datetime = DateTime.Today.AddDays(1).AddHours(g.Key - 1).ToUniversalTime(), Volume = g.Sum(p => p.Volume) })
                    .ToList();
        }

        private static string GetContent(IEnumerable<CsvModel> csv)
        {
            return "Datetime;Volume\n" + string.Join("\n", csv.Select(v => $"{v.Datetime:yyyy-MM-ddTHH:mm:ssZ};{v.Volume.ToString("F2", CultureInfo.InvariantCulture)}"));
        }

        private static string GetFileName(DateTime date)
        {
            return $"PowerPosition_{date.AddDays(1):yyyyMMdd}_{date:yyyyMMddHHmm}.csv";
        }

        private static Task WriteToFileAsync(string fullPath, string csvContent)
        {
            return File.WriteAllTextAsync(fullPath, csvContent);
        }

        private readonly ILogger<CsvManager> Logger;
        private readonly IPowerService PowerService;
    }
}
