using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SafraCoinContractsService.Core.Interfaces.Services;

namespace SafraCoinContractsService.Core.Workers;

public class CropProcessingWorker : BackgroundService
{
    private readonly ILogger<CropProcessingWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CropProcessingWorker(ILogger<CropProcessingWorker> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();
            await redisService.CreateConsumerGroupIfNotExists(
                "CropsToTokenize",
                "CropsConsumerGroup",
                "1");

            _logger.LogInformation("[{workerName}] Starting jobs...", nameof(CropProcessingWorker));
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessCrops(redisService);
            }
        }
        catch(Exception ex)
        {
            _logger.LogError("[{workerName}] Unexpected Error: {message}", nameof(CropProcessingWorker), ex.Message);
            throw;
        }
    }

    private async Task ProcessCrops(IRedisService redisService)
    {
        var crops = await redisService.ReadEntriesFromRedisStream(
            "CropsToTokenize",
            "CropsConsumerGroup",
            "SafraCoinContractsService",
            ">",
            1,
            CropTokenize.Parser.ParseFrom
        );

        foreach (var entry in crops)
        {
            var crop = entry.Value;
            _logger.LogInformation("[{workerName}] Processing crop {cropId}", nameof(CropProcessingWorker), crop.CropId);
        }
    }
}
