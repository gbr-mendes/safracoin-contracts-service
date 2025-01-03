using SafraCoinContractsService.Core.Interfaces.Services;
using SafraCoinContractsService.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SafraCoinContractsService.Core.DI;

public static class ConfigureCoreServices
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IImplantContractsService, ImplantContractsService>();
        services.AddScoped<IOracleService, OracleService>();
        return services;
    }
}
