using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SafraCoinContractsService.Core.Interfaces.Services;

namespace SafraCoinContractsService.Core.Workers;

public class OracleWorker : BackgroundService
{
    private readonly ILogger<OracleWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OracleWorker(ILogger<OracleWorker> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var oracleService = scope.ServiceProvider.GetRequiredService<IOracleService>();
            var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();

            // TODO: Move to config file hardcoded properties
            await redisService.CreateConsumerGroupIfNotExists(
                "FarmerAccounts",
                "FarmersConsumerGroup",
                "1");

            _logger.LogInformation("[{workerName}] Starting jobs...", nameof(OracleWorker));
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessAccounts(redisService, oracleService);
            }
        }
        catch(Exception ex)
        {
            _logger.LogError("[{workerName}] Unexpected Error: {message}", nameof(OracleWorker), ex.Message);
            throw;
        }
    }

    private async Task ProcessAccounts(IRedisService redisService, IOracleService oracleService)
    {
        var farmerAccounts = await redisService.ReadEntriesFromRedisStream(
            "FarmerAccounts",
            "FarmersConsumerGroup",
            "SafraCoinContractsService",
            ">",
            1,
            FarmerAccount.Parser.ParseFrom
        );

        if (!farmerAccounts.Any())
        {
            _logger.LogDebug("[{workerName}] No accounts found for processing", nameof(OracleWorker));
            await Task.Delay(1000);
            return;
        }

        foreach (var entry in farmerAccounts)
        {
            var account = entry.Value;
            _logger.LogInformation("[{workerName}] Processing account {account}", nameof(OracleWorker), account.Address);
            await oracleService.SetAuthorization(account.Address);
            await redisService.AckEntryStreamGroupAsync(
                "FarmerAccounts",
                "FarmersConsumerGroup",
                entry.Key
            );
        }
        await Task.Delay(1000);
        return;
    }
}
