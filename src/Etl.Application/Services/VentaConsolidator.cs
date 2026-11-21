using Etl.Domain.Entities;

namespace Etl.Application.Services;

public sealed record ConsolidationResult(IReadOnlyList<VentaRecord> Registros, int Descartados);

/// <summary>
/// Unifica las fuentes aplicando la prioridad API > CSV > DB_EXTERNA:
/// ante la misma (numero_factura, linea_factura) se conserva el registro de la fuente prioritaria.
/// </summary>
public static class VentaConsolidator
{
    /// <param name="fuentesOrdenadasPorPrioridad">Listas de registros, de mayor a menor prioridad.</param>
    public static ConsolidationResult Consolidar(IEnumerable<IReadOnlyList<VentaRecord>> fuentesOrdenadasPorPrioridad)
    {
        var vistos = new HashSet<(string Factura, short Linea)>();
        var resultado = new List<VentaRecord>();
        var descartados = 0;

        foreach (var fuente in fuentesOrdenadasPorPrioridad)
        {
            foreach (var venta in fuente)
            {
                var clave = (venta.NumeroFactura.Trim().ToUpperInvariant(), venta.LineaFactura);
                if (vistos.Add(clave))
                    resultado.Add(venta);
                else
                    descartados++;
            }
        }

        return new ConsolidationResult(resultado, descartados);
    }
}
