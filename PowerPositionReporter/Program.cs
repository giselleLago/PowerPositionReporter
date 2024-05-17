using Axpo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PowerPositionReporter.Services;
using Serilog;
using TranscriptsProcessor.Services;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/powerPositionReporter.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        IConfiguration configuration = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .Build();

        var serviceProvider = ConfigureServices(configuration);

        try
        {
            var managerLogger = serviceProvider.GetService<ILogger<CsvManager>>();
            var schedulerLogger = serviceProvider.GetService<ILogger<Scheduler>>();
            var powerService = serviceProvider.GetService<IPowerService>();
            var manager = serviceProvider.GetService<ICsvManager>();

            async Task extractAction()
            {
                var path = configuration.GetValue<string>("Path");
                var location = configuration.GetValue<string>("Location");
                var maxRetryAttempt = configuration.GetValue<int>("MaxRetryAttempt");
                var manager = new CsvManager(managerLogger, powerService);
                await manager.GenerateReportAsync(path, maxRetryAttempt, location);
            }

            var intervalInMinutes = configuration.GetValue<int>("IntervalInMinutes");

            var scheduler = new Scheduler(schedulerLogger, extractAction, intervalInMinutes);
            await scheduler.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Something went wrong: " + ex.Message);
        }
    }

    private static ServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddSerilog();
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            })
            .AddSingleton<ICsvManager, CsvManager>()
            .AddSingleton<IPowerService, PowerService>()
            .BuildServiceProvider();

        return serviceProvider;
    }
}
