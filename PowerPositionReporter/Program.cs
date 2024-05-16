using Axpo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PowerPositionReporter.Services;
using TranscriptsProcessor.Services;

internal class Program
{
    private static void Main(string[] args)
    {
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

            void extractAction()
            {
                Console.WriteLine("Running extract...");
                var path = configuration.GetValue<string>("Path");
                var manager = new CsvManager(managerLogger, powerService);
                Task.Run(() => manager.GenerateReportAsync(path));
            }

            var intervalInMinutes = configuration.GetValue<int>("IntervalInMinutes");

            var scheduler = new Scheduler(schedulerLogger, extractAction, intervalInMinutes);
            scheduler.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Something went wrong: " + ex.Message);
        }
    }

    static IServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            })
            .AddSingleton<ICsvManager, CsvManager>()
            .AddSingleton<IPowerService, PowerService>()
            .BuildServiceProvider();

        return serviceProvider;
    }
}
