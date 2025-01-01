using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SafraCoinContractsService.Core.Settings;

namespace SafraCoinContractsService.Core.DI;

public static class ConfigureCoreAppSettings
{
    public static IServiceCollection AddCoreAppSettings(this IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        services.Configure<BlockChainSettings>(options =>
        {
            configuration.GetSection("BlockChain").Bind(options);
        });
        return services;
    }
}
