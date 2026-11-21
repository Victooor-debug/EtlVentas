using Etl.Application.Services;
using Etl.Infrastructure;
using Etl.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Logging estructurado (ILogger + Serilog): consola y archivo en logs/
builder.Services.AddSerilog((_, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext());

builder.Services.Configure<ScheduleOptions>(builder.Configuration.GetSection("Schedule"));

// Composición: infraestructura (fuentes según appsettings.json) + caso de uso
builder.Services.AddEtlInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ExtractionOrchestrator>();
builder.Services.AddHostedService<EtlWorker>();

await builder.Build().RunAsync();
