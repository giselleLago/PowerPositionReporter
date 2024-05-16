namespace PowerPositionReporter.Services
{
    public interface ICsvManager
    {
        Task GenerateReportAsync(string storingPath);
    }
}
