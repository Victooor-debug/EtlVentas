namespace Etl.Application;

public record DimTiempoRow(int TiempoId, DateTime Fecha, int Dia, int Mes, string NombreMes, int Trimestre, int Anio);
public record DimClienteRow(string Codigo, string Nombre, string TipoCliente, string Email, string Pais, string Region, string Ciudad, string Fuente);
public record DimProductoRow(string Codigo, string Nombre, string Categoria, string Subcategoria, string Marca, decimal PrecioActual, decimal CostoUnitario, string Fuente);
public record DimVendedorRow(string Codigo, string Nombre, string Region, string Fuente);

public static class DimensionMapper
{
    private static readonly string[] Meses =
    [
        "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    public static string ObtenerNombreMes(int mes) => mes is >= 1 and <= 12 ? Meses[mes] : "Desconocido";
    public static int ObtenerTrimestre(int mes) => (mes - 1) / 3 + 1;

    public static IReadOnlyList<DimTiempoRow> BuildTiempo(IEnumerable<DimensionSourceRow> rows) =>
        rows.Select(r => r.Fecha.Date)
            .Distinct()
            .OrderBy(d => d)
            .Select(d => new DimTiempoRow(
                int.Parse(d.ToString("yyyyMMdd")),
                d,
                d.Day,
                d.Month,
                ObtenerNombreMes(d.Month),
                ObtenerTrimestre(d.Month),
                d.Year))
            .ToList();

    public static IReadOnlyList<DimClienteRow> BuildClientes(IEnumerable<DimensionSourceRow> rows) =>
        rows.Where(r => !string.IsNullOrWhiteSpace(r.ClienteCodigo) || !string.IsNullOrWhiteSpace(r.ClienteNombre))
            .GroupBy(r => (string.IsNullOrWhiteSpace(r.ClienteCodigo) ? r.ClienteNombre : r.ClienteCodigo).Trim().ToUpperInvariant())
            .Select(g =>
            {
                var r = g.First();
                return new DimClienteRow(
                    r.ClienteCodigo?.Trim() ?? "",
                    r.ClienteNombre?.Trim() ?? "",
                    r.TipoCliente?.Trim() ?? "",
                    r.Email?.Trim() ?? "",
                    r.Pais?.Trim() ?? "",
                    r.Region?.Trim() ?? "",
                    r.Ciudad?.Trim() ?? "",
                    r.OrigenFuente?.Trim() ?? "");
            })
            .ToList();

    public static IReadOnlyList<DimProductoRow> BuildProductos(IEnumerable<DimensionSourceRow> rows) =>
        rows.Where(r => !string.IsNullOrWhiteSpace(r.ProductoCodigo) || !string.IsNullOrWhiteSpace(r.ProductoNombre))
            .GroupBy(r => (string.IsNullOrWhiteSpace(r.ProductoCodigo) ? r.ProductoNombre : r.ProductoCodigo).Trim().ToUpperInvariant())
            .Select(g =>
            {
                var r = g.First();
                return new DimProductoRow(
                    r.ProductoCodigo?.Trim() ?? "",
                    r.ProductoNombre?.Trim() ?? "",
                    r.Categoria?.Trim() ?? "",
                    r.Subcategoria?.Trim() ?? "",
                    r.Marca?.Trim() ?? "",
                    r.PrecioLista,
                    r.CostoUnitario,
                    r.OrigenFuente?.Trim() ?? "");
            })
            .ToList();

    public static IReadOnlyList<DimVendedorRow> BuildVendedores(IEnumerable<DimensionSourceRow> rows) =>
        rows.Where(r => !string.IsNullOrWhiteSpace(r.VendedorCodigo) || !string.IsNullOrWhiteSpace(r.VendedorNombre))
            .GroupBy(r => (string.IsNullOrWhiteSpace(r.VendedorCodigo) ? r.VendedorNombre : r.VendedorCodigo).Trim().ToUpperInvariant())
            .Select(g =>
            {
                var r = g.First();
                return new DimVendedorRow(
                    r.VendedorCodigo?.Trim() ?? "",
                    r.VendedorNombre?.Trim() ?? "",
                    r.VendedorRegion?.Trim() ?? "",
                    r.OrigenFuente?.Trim() ?? "");
            })
            .ToList();
}
