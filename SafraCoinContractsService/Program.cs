using SafraCoinContractsService.Core.DI;
using SafraCoinContractsService.Core.Interfaces.Services;
using SafraCoinContractsService.Infra.DI;
using Serilog;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.WithProperty("ApplicationName", "SafraCoinContractsService")
    .CreateLogger();

try
{
    Log.Information("Starting up the application");

    IHost host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureAppConfiguration((hostingContext, builder) =>{
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureServices(services =>
        {
            
            services.AddCoreAppSettings();
            services.AddInfraAppSettings();
            services.AddCoreServices();
            services.AddInfraServices();
            services.AddWorkers();

            ImplantSmartContracts(services).GetAwaiter().GetResult();
        })
        .Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task ImplantSmartContracts(IServiceCollection services)
{
    var contractService = services.BuildServiceProvider().GetRequiredService<IImplantContractsService>();
    await contractService.CompileContracts();
    await contractService.DeployContractsOnBlockChain();
}
