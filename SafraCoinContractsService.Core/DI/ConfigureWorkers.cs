using Microsoft.Extensions.DependencyInjection;
using SafraCoinContractsService.Core.Workers;

namespace SafraCoinContractsService.Core.DI;

public static class ConfigureWorkers
{
    public static IServiceCollection AddWorkers(this IServiceCollection services)
    {
        services.AddHostedService<OracleWorker>();
        services.AddHostedService<CropProcessingWorker>();
        return services;
    }
}
