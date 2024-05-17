namespace PowerPositionReporter.Services
{
    public interface ICsvManager
    {
        Task GenerateReportAsync(string path, int retryAttempt, string location);
    }
}
