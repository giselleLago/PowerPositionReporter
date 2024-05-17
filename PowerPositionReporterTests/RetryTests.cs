using Axpo;
using Microsoft.Extensions.Logging;
using Moq;
using PowerPositionReporter.Services;

namespace PowerPositionReporterTests
{
    public class RetryTests
    {
        [Fact]
        public async Task GetTradesReturnsCollectionOfPowerTrade()
        {
            var powerTrade1 = PowerTrade.Create(DateTime.UtcNow.AddDays(1), 24);
            var powerTrade2 = PowerTrade.Create(DateTime.UtcNow.AddDays(1), 24);
            var powerTrades = new List<PowerTrade> { powerTrade1, powerTrade2 };

            var mockLogger = new Mock<ILogger<CsvManager>>();
            var mockPowerService = new Mock<IPowerService>();

            mockPowerService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>()))
             .ReturnsAsync(() => powerTrades);

            var filePath = $".\\";
            var retryAttempts = 3;
            var location = "Europe/Berlin";
            
            var manager = new CsvManager(mockLogger.Object, mockPowerService.Object);
            await manager.GenerateReportAsync(filePath, retryAttempts, location);

            mockPowerService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task GetTradesReturnsException_RetryIsExecuted()
        {
            var isFirstCall = true;

            var powerTrade1 = PowerTrade.Create(DateTime.UtcNow.AddDays(1), 24);
            var powerTrade2 = PowerTrade.Create(DateTime.UtcNow.AddDays(1), 24);
            var powerTrades = new List<PowerTrade> { powerTrade1, powerTrade2 };

            var mockLogger = new Mock<ILogger<CsvManager>>();
            var mockPowerService = new Mock<IPowerService>();

            mockPowerService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>()))
             .Returns(() =>
             {
                 if (isFirstCall)
                 {
                     isFirstCall = false;
                     throw new PowerServiceException("Error retrieving power volumes");
                 }

                 return Task.FromResult(powerTrades as IEnumerable<PowerTrade>);
             });

            var filePath = $".\\";
            var retryAttempts = 3;
            var location = "Europe/Berlin";

            var manager = new CsvManager(mockLogger.Object, mockPowerService.Object);
            await manager.GenerateReportAsync(filePath, retryAttempts, location);

            mockPowerService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetTradesReturnsException_RetryIsExecuted_NotAbleToExtractData()
        {

            var powerTrade1 = PowerTrade.Create(DateTime.UtcNow.AddDays(1), 24);
            var powerTrade2 = PowerTrade.Create(DateTime.UtcNow.AddDays(1), 24);
            var powerTrades = new List<PowerTrade> { powerTrade1, powerTrade2 };

            var mockLogger = new Mock<ILogger<CsvManager>>();
            var mockPowerService = new Mock<IPowerService>();

            mockPowerService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>()))
             .Throws(new PowerServiceException("Error retrieving power volumes"));

            var filePath = $".\\";
            var retryAttempts = 3;
            var location = "Europe/Berlin";

            var manager = new CsvManager(mockLogger.Object, mockPowerService.Object);
            await manager.GenerateReportAsync(filePath, retryAttempts, location);

            mockPowerService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>()), Times.Exactly(3));
        }
    }
}
