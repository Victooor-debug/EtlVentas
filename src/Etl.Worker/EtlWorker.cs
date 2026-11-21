using Etl.Application.Services;
using Microsoft.Extensions.Options;

namespace Etl.Worker;

public sealed class ScheduleOptions
{
    /// <summary>0 = ejecuta una sola vez y se detiene. Mayor que 0 = repite cada N minutos.</summary>
    public int IntervalMinutes { get; set; }
}

public sealed class EtlWorker : BackgroundService
{
    private readonly ExtractionOrchestrator _orchestrator;
    private readonly ScheduleOptions _schedule;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<EtlWorker> _logger;

    public EtlWorker(
        ExtractionOrchestrator orchestrator,
        IOptions<ScheduleOptions> schedule,
        IHostApplicationLifetime lifetime,
        ILogger<EtlWorker> logger)
    {
        _orchestrator = orchestrator;
        _schedule = schedule.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunSafeAsync(stoppingToken);

            if (_schedule.IntervalMinutes > 0)
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_schedule.IntervalMinutes));
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await RunSafeAsync(stoppingToken);
            }
            else
            {
                _logger.LogInformation("Ejecución única completada; se detiene el servicio.");
                _lifetime.StopApplication();
            }
        }
        catch (OperationCanceledException)
        {
            // apagado normal del servicio
        }
    }

    private async Task RunSafeAsync(CancellationToken ct)
    {
        try
        {
            await _orchestrator.RunAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la ejecución del ETL; se reintentará en el próximo ciclo (si hay programación).");
        }
    }
}
