using Etl.Domain.Entities;

namespace Etl.Application.Abstractions;

/// <summary>Resultado crudo de una extracción: registros mapeados al modelo común y contadores.</summary>
public sealed record ExtractionOutput(IReadOnlyList<VentaRecord> Records, int RowsRead, int RowsRejected);

/// <summary>Contrato común de todas las fuentes. Agregar una fuente = implementar esta interfaz.</summary>
public interface IExtractor
{
    /// <summary>Nombre de la fuente; coincide con Fact_Ventas.origen_fuente (CSV, API, DB_EXTERNA).</summary>
    string SourceName { get; }

    /// <summary>1 = máxima prioridad. Ante la misma factura/línea gana la fuente con menor número.</summary>
    int Priority { get; }

    Task<ExtractionOutput> ExtractAsync(CancellationToken ct);
}
