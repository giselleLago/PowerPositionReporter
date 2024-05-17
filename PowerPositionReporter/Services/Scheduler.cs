using Microsoft.Extensions.Logging;

namespace TranscriptsProcessor.Services
{

    public sealed class Scheduler
    {
        public Scheduler(ILogger<Scheduler> logger,
                         Func<Task> extractAction,
                         int intervalInMinutes)
        {
            Logger = logger;
            ExtractAction = extractAction ?? throw new ArgumentNullException(nameof(extractAction));
            IntervalInMinutes = intervalInMinutes;
        }

        public async Task Start()
        {
            Logger.LogInformation("Starting scheduler service.");
            var intervalInMilliseconds = IntervalInMinutes * 60 * 1000;

            do
            {
                Logger.LogInformation("Executing extract action.");
                await ExtractAction();

                Logger.LogInformation($"Waiting {IntervalInMinutes} minutes to execute extract action.");
                await Task.Delay(intervalInMilliseconds);
            } while (true);
        }

        private readonly ILogger<Scheduler> Logger;
        private readonly Func<Task> ExtractAction;
        private readonly int IntervalInMinutes;
    }
}
