using Microsoft.Extensions.Logging;

namespace TranscriptsProcessor.Services
{

    public sealed class Scheduler
    {
        public Scheduler(ILogger<Scheduler> logger,
                         Action extractAction,
                         int intervalInMinutes)
        {
            Logger = logger;
            ExtractAction = extractAction ?? throw new ArgumentNullException(nameof(extractAction));
            IntervalInMinutes = intervalInMinutes;
            Random = new Random();
        }

        public void Start()
        {
            Logger.LogInformation("Starting scheduler service.");
            var intervalInMilliseconds = IntervalInMinutes * 60 * 1000;

            Logger.LogInformation("Executing extract action.");
            ExtractAction();

            while (true)
            {
                // Calculate a random offset within +/- 1 minute
                var offset = Random.Next(-1, 2) * 60 * 1000;

                Logger.LogInformation($"Waiting {(intervalInMilliseconds + offset)/60/1000} minutes to execute extract action.");
                // Sleep for the interval with the offset
                Thread.Sleep(intervalInMilliseconds + offset);

                Logger.LogInformation("Executing extract action.");
                ExtractAction();
            }
        }

        private readonly ILogger<Scheduler> Logger;
        private readonly Action ExtractAction;
        private readonly int IntervalInMinutes;
        private readonly Random Random;
    }
}
