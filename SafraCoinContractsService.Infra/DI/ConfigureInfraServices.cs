using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafraCoinContractsService.Core.Interfaces.Repositories;
using SafraCoinContractsService.Core.Interfaces.Services;
using SafraCoinContractsService.Infra.Db;
using SafraCoinContractsService.Infra.Repositories.EntiteisRepositories;
using SafraCoinContractsService.Infra.Services;

namespace SafraCoinContractsService.Infra.DI;

public static class ConfigureInfraServices
{
    public static IServiceCollection AddInfraServices(this IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services.AddScoped<IDockerService, DockerService>();

        services.AddScoped<ISmartContractRepository, SmartContractRepository>();

        services.AddDbContext<AppDbContext>(options => {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("SafraCoinContractsService"));
        });

        return services;
    }
}