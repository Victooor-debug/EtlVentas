using Etl.Domain.Entities;

namespace Etl.Application.Services;

public sealed record SourceResult(
    string Fuente,
    bool Exitosa,
    int Leidas,
    int Validas,
    int Rechazadas,
    TimeSpan Duracion,
    IReadOnlyList<VentaRecord> Registros)
{
    public static SourceResult Fallida(string fuente, TimeSpan duracion) =>
        new(fuente, false, 0, 0, 0, duracion, Array.Empty<VentaRecord>());
}

public sealed record RunSummary(
    IReadOnlyList<SourceResult> Fuentes,
    int Consolidadas,
    int Descartadas,
    string? RutaConsolidado,
    TimeSpan Duracion);
