namespace Etl.Domain.Entities;

/// <summary>
/// Modelo común de una línea de venta. Cada fuente (CSV, API, BD) se transforma a este
/// registro, que contiene lo necesario para poblar Fact_Ventas y sus dimensiones.
/// </summary>
public sealed record VentaRecord(
    string NumeroFactura,
    short LineaFactura,
    DateTime Fecha,
    string ProductoCodigo,
    string ProductoNombre,
    string Categoria,
    string? Subcategoria,
    string? Marca,
    decimal? PrecioLista,
    decimal? CostoUnitario,
    string ClienteCodigo,
    string ClienteNombre,
    string TipoCliente,
    string? Email,
    string Pais,
    string Region,
    string Ciudad,
    string? VendedorCodigo,
    string? VendedorNombre,
    string? VendedorRegion,
    int Cantidad,
    decimal MontoTotal,
    decimal Costo,
    string OrigenFuente);   // "CSV" | "API" | "DB_EXTERNA" (coincide con el CHECK de Fact_Ventas)
