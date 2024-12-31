using SafraCoinContractsService.Core.Interfaces.Services;

namespace SafraCoinContractsService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IImplementContractService _implementContractService;

    public Worker(ILogger<Worker> logger, IImplementContractService implementContractService)
    {
        _logger = logger;
        _implementContractService = implementContractService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _implementContractService.CompileContracts();
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
