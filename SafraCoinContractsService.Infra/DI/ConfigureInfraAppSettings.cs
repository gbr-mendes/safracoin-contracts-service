using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SafraCoinContractsService.Infra.Settings;

namespace SafraCoinContractsService.Infra.DI;

public static class ConfigureInfraAppSettings
{
    public static IServiceCollection AddInfraAppSettings(this IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        services.Configure<RedisSettings>(options =>
        {
            configuration.GetSection("Redis").Bind(options);
        });
        return services;
    }
}
