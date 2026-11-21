using Etl.Domain.Entities;

namespace Etl.Application.Services;

/// <summary>
/// Reglas mínimas de calidad, alineadas con los CHECK y NOT NULL de Fact_Ventas,
/// para rechazar filas antes de que lleguen al DW.
/// </summary>
public static class VentaValidator
{
    private static readonly HashSet<string> FuentesValidas = new(StringComparer.Ordinal)
    {
        "CSV", "API", "DB_EXTERNA"
    };

    /// <returns>El motivo del rechazo, o null si la fila es válida.</returns>
    public static string? Validar(VentaRecord v)
    {
        if (string.IsNullOrWhiteSpace(v.NumeroFactura)) return "numero_factura vacío";
        if (v.NumeroFactura.Length > 40) return "numero_factura excede 40 caracteres";
        if (v.LineaFactura <= 0) return "linea_factura debe ser mayor que 0";
        if (string.IsNullOrWhiteSpace(v.ProductoCodigo)) return "producto_codigo vacío";
        if (string.IsNullOrWhiteSpace(v.ClienteCodigo)) return "cliente_codigo vacío";
        if (v.Cantidad <= 0) return "cantidad debe ser mayor que 0";
        if (v.MontoTotal < 0) return "monto_total negativo";
        if (v.Costo < 0) return "costo negativo";
        if (v.Fecha == default) return "fecha inválida";
        if (!FuentesValidas.Contains(v.OrigenFuente)) return "origen_fuente inválido";
        return null;
    }
}
