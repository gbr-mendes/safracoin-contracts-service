using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SafraCoinContractsService.Core.Interfaces.Repositories;
using SafraCoinContractsService.Core.Interfaces.Services;
using SafraCoinContractsService.Core.ValueObjects;

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
            var redisRepository = scope.ServiceProvider.GetRequiredService<IRedisRepository>();
            var oracleService = scope.ServiceProvider.GetRequiredService<IOracleService>();

            // TODO: Move to config file hardcoded properties
            await CreateConsumerGroupIfNotExists(
                redisRepository,
                "FarmerAccounts",
                "FarmersConsumerGroup",
                "1");

            _logger.LogInformation("[{workerName}] Starting jobs...", nameof(OracleWorker));
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessAccounts(redisRepository, oracleService);
            }
        }
        catch(Exception ex)
        {
            _logger.LogError("[{workerName}] Unexpected Error: {message}", nameof(OracleWorker), ex.Message);
            throw;
        }
    }

    private async Task CreateConsumerGroupIfNotExists(
        IRedisRepository redisRepository,
        string streamKey,
        string groupName,
        string beginPosition)
    {
        var groupCreatedSuccessfully = await redisRepository.CreateGroupAsync(
            streamKey,
            groupName,
            beginPosition);

        var status = groupCreatedSuccessfully ? "Success" : "AlreadyExists";

        _logger.LogInformation("[{workerName}] Consumer group {groupName} created: Status: {status}",
            nameof(OracleWorker),
            groupName,
            status
        );
    }

    private static async Task<IEnumerable<AccountVO>> ReadFarmerAccountsFromRedisStream(IRedisRepository redisRepository)
    {
        var count = 1;
        var farmerAccounts = await redisRepository.ReadEntriesFromStreamAsync(
            "FarmerAccounts",
            "FarmersConsumerGroup",
            "SafraCoinContractsService",
            ">",
            count,
            (buffer, redisEntryId) => {
                var farmer = FarmerAccount.Parser.ParseFrom(buffer);
                var accountVO = new AccountVO
                {
                    RedisEntryId = redisEntryId,
                    Address = farmer.Address,
                    Email = farmer.Email
                };

                return accountVO;
            }
        );

        return farmerAccounts;
    }

    private async Task ProcessAccounts(IRedisRepository redisRepository, IOracleService oracleService)
    {
        var farmerAccounts = await ReadFarmerAccountsFromRedisStream(redisRepository);

        if (!farmerAccounts.Any())
        {
            _logger.LogDebug("[{workerName}] No accounts found for processing", nameof(OracleWorker));
            await Task.Delay(1000);
            return;
        }

        foreach (var account in farmerAccounts)
        {
            _logger.LogInformation("[{workerName}] Processing account {account}", nameof(OracleWorker), account.Address);
            await oracleService.SetAuthorization(account.Address);
            await redisRepository.AckEntryStreamGroupAsync(
                "FarmerAccounts",
                "FarmersConsumerGroup",
                account.RedisEntryId
            );
        }
        await Task.Delay(1000);
        return;
    }
}
