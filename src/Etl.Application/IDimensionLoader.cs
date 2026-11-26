namespace Etl.Application;

public record DimensionSourceRow(
    DateTime Fecha,
    string ProductoCodigo,
    string ProductoNombre,
    string Categoria,
    string Subcategoria,
    string Marca,
    decimal PrecioLista,
    decimal CostoUnitario,
    string ClienteCodigo,
    string ClienteNombre,
    string TipoCliente,
    string Email,
    string Pais,
    string Region,
    string Ciudad,
    string OrigenFuente,
    string VendedorCodigo,
    string VendedorNombre,
    string VendedorRegion);

public record DimensionStats(string Tabla, int Leidas, int Insertadas, int Actualizadas);

public record DimensionLoadResult(IReadOnlyList<DimensionStats> Tablas, long DuracionMs);

public interface IDimensionLoader
{
    Task<DimensionLoadResult> LoadAsync(IReadOnlyList<DimensionSourceRow> registros, CancellationToken ct = default);
}
