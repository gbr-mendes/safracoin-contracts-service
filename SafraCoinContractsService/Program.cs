using SafraCoinContractsService;
using SafraCoinContractsService.Core.DI;
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
            services.AddCoreServices();
            services.AddInfraServices();
            
            services.AddHostedService<Worker>();
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
