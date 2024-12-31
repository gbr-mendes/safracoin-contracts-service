using SafraCoinContractsService;
using SafraCoinContractsService.Core.Interfaces;
using SafraCoinContractsService.Core.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IImplementContractService, ImplementContractService>();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
