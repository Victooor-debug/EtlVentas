using System.Diagnostics;
using Etl.Application.Abstractions;
using Etl.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Etl.Application.Services;

/// <summary>
/// Caso de uso principal: extrae todas las fuentes en paralelo, valida, guarda en staging,
/// consolida por prioridad y registra el resultado. No conoce ningún detalle de CSV/BD/API.
/// </summary>
public sealed class ExtractionOrchestrator
{
    private readonly IReadOnlyList<IExtractor> _extractors;
    private readonly IStagingWriter _staging;
    private readonly IEtlAuditLog _audit;
    private readonly ILogger<ExtractionOrchestrator> _logger;

    public ExtractionOrchestrator(
        IEnumerable<IExtractor> extractors,
        IStagingWriter staging,
        IEtlAuditLog audit,
        ILogger<ExtractionOrchestrator> logger)
    {
        _extractors = extractors.OrderBy(e => e.Priority).ToList();
        _staging = staging;
        _audit = audit;
        _logger = logger;
    }

    public async Task<RunSummary> RunAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Inicio de extracción. Fuentes activas ({Count}): {Sources}",
            _extractors.Count, string.Join(", ", _extractors.Select(e => e.SourceName)));

        // Las fuentes se extraen en paralelo; Task.WhenAll conserva el orden (prioridad) de entrada.
        var results = await Task.WhenAll(_extractors.Select(e => ExtractSourceAsync(e, ct)));

        var exitosas = results.Where(r => r.Exitosa).ToList();
        var consolidado = VentaConsolidator.Consolidar(exitosas.Select(r => r.Registros));

        string? ruta = null;
        if (consolidado.Registros.Count > 0)
            ruta = await _staging.WriteAsync("ventas_consolidado", consolidado.Registros, ct);

        var estado = exitosas.Count == results.Length ? EtlEstados.Ok
                   : exitosas.Count == 0 ? EtlEstados.Error
                   : EtlEstados.Parcial;

        await _audit.RegistrarAsync(new EtlLogEntry(
            "CONSOLIDADO", "Ventas", exitosas.Sum(r => r.Validas), 0, estado,
            $"{consolidado.Registros.Count} filas únicas; {consolidado.Descartados} descartadas por prioridad de fuente"), ct);

        sw.Stop();
        _logger.LogInformation(
            "Extracción finalizada ({Estado}): {Unicas} filas únicas, {Descartadas} descartadas por prioridad, tiempo total {Ms} ms. Staging: {Ruta}",
            estado, consolidado.Registros.Count, consolidado.Descartados, sw.ElapsedMilliseconds, ruta ?? "(sin datos)");

        return new RunSummary(results, consolidado.Registros.Count, consolidado.Descartados, ruta, sw.Elapsed);
    }

    private async Task<SourceResult> ExtractSourceAsync(IExtractor extractor, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var output = await extractor.ExtractAsync(ct);

            var validos = new List<VentaRecord>(output.Records.Count);
            var motivos = new Dictionary<string, int>();
            foreach (var registro in output.Records)
            {
                var motivo = VentaValidator.Validar(registro);
                if (motivo is null)
                    validos.Add(registro);
                else
                    motivos[motivo] = motivos.TryGetValue(motivo, out var n) ? n + 1 : 1;
            }

            foreach (var (motivo, cantidad) in motivos)
                _logger.LogWarning("{Source}: {Count} fila(s) rechazada(s) por: {Motivo}", extractor.SourceName, cantidad, motivo);

            var rechazadas = output.RowsRejected + motivos.Values.Sum();

            string? ruta = null;
            if (validos.Count > 0)
                ruta = await _staging.WriteAsync($"ventas_{extractor.SourceName.ToLowerInvariant()}", validos, ct);

            sw.Stop();
            _logger.LogInformation(
                "{Source}: leídas={Read} válidas={Valid} rechazadas={Rejected} en {Ms} ms -> {Ruta}",
                extractor.SourceName, output.RowsRead, validos.Count, rechazadas, sw.ElapsedMilliseconds, ruta ?? "(sin datos)");

            await _audit.RegistrarAsync(new EtlLogEntry(
                extractor.SourceName, "Ventas", output.RowsRead, rechazadas, EtlEstados.Ok, null), ct);

            return new SourceResult(extractor.SourceName, true, output.RowsRead, validos.Count, rechazadas, sw.Elapsed, validos);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Una fuente caída no detiene a las demás (aislamiento de fallos).
            sw.Stop();
            _logger.LogError(ex, "{Source}: error durante la extracción", extractor.SourceName);
            await _audit.RegistrarAsync(new EtlLogEntry(
                extractor.SourceName, "Ventas", 0, 0, EtlEstados.Error, ex.Message), ct);
            return SourceResult.Fallida(extractor.SourceName, sw.Elapsed);
        }
    }
}
