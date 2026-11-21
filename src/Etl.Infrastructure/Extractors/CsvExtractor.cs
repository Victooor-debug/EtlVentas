using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Etl.Application.Abstractions;
using Etl.Domain;
using Etl.Domain.Entities;
using Etl.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Extractors;

public sealed class CsvExtractor : IExtractor
{
    private readonly CsvSourceOptions _options;
    private readonly ILogger<CsvExtractor> _logger;

    public CsvExtractor(IOptions<CsvSourceOptions> options, ILogger<CsvExtractor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string SourceName => "CSV";
    public int Priority => 2;

    public async Task<ExtractionOutput> ExtractAsync(CancellationToken ct)
    {
        var path = Path.IsPathRooted(_options.FilePath)
            ? _options.FilePath
            : Path.Combine(AppContext.BaseDirectory, _options.FilePath);

        if (!File.Exists(path))
            throw new FileNotFoundException("No se encontró el archivo CSV configurado.", path);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,   // columnas opcionales ausentes no son error
            HeaderValidated = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(path, Encoding.UTF8);
        using var csv = new CsvReader(reader, config);

        var records = new List<VentaRecord>();
        int read = 0, rejected = 0;

        await csv.ReadAsync();
        csv.ReadHeader();

        // Lectura en streaming: no se carga el archivo completo en memoria como texto.
        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            read++;
            try
            {
                var row = csv.GetRecord<CsvVentaRow>();
                records.Add(row.ToDomain());
            }
            catch (CsvHelperException ex)
            {
                rejected++;
                _logger.LogWarning("CSV: fila {Row} rechazada ({Reason})",
                    csv.Parser.Row, ex.InnerException?.Message ?? ex.Message);
            }
        }

        return new ExtractionOutput(records, read, rejected);
    }
}

/// <summary>Estructura del CSV de ventas. Los nombres de columna son el contrato con el archivo.</summary>
internal sealed class CsvVentaRow
{
    [Name("numero_factura")] public string? NumeroFactura { get; set; }
    [Name("linea_factura")] public short LineaFactura { get; set; }
    [Name("fecha")] public DateTime Fecha { get; set; }
    [Name("producto_codigo")] public string? ProductoCodigo { get; set; }
    [Name("producto_nombre")] public string? ProductoNombre { get; set; }
    [Name("categoria")] public string? Categoria { get; set; }
    [Name("subcategoria")] public string? Subcategoria { get; set; }
    [Name("marca")] public string? Marca { get; set; }
    [Name("precio_lista")] public decimal? PrecioLista { get; set; }
    [Name("costo_unitario")] public decimal? CostoUnitario { get; set; }
    [Name("cliente_codigo")] public string? ClienteCodigo { get; set; }
    [Name("cliente_nombre")] public string? ClienteNombre { get; set; }
    [Name("tipo_cliente")] public string? TipoCliente { get; set; }
    [Name("email")] public string? Email { get; set; }
    [Name("pais")] public string? Pais { get; set; }
    [Name("region")] public string? Region { get; set; }
    [Name("ciudad")] public string? Ciudad { get; set; }
    [Name("vendedor_codigo")] public string? VendedorCodigo { get; set; }
    [Name("vendedor_nombre")] public string? VendedorNombre { get; set; }
    [Name("vendedor_region")] public string? VendedorRegion { get; set; }
    [Name("cantidad")] public int Cantidad { get; set; }
    [Name("monto_total")] public decimal MontoTotal { get; set; }
    [Name("costo")] public decimal Costo { get; set; }

    public VentaRecord ToDomain() => new(
        NumeroFactura: NumeroFactura.Limpio(),
        LineaFactura: LineaFactura,
        Fecha: Fecha,
        ProductoCodigo: ProductoCodigo.Limpio(),
        ProductoNombre: ProductoNombre.OrNd(),
        Categoria: Categoria.OrNd(),
        Subcategoria: string.IsNullOrWhiteSpace(Subcategoria) ? null : Subcategoria.Trim(),
        Marca: string.IsNullOrWhiteSpace(Marca) ? null : Marca.Trim(),
        PrecioLista: PrecioLista,
        CostoUnitario: CostoUnitario,
        ClienteCodigo: ClienteCodigo.Limpio(),
        ClienteNombre: ClienteNombre.OrNd(),
        TipoCliente: TipoCliente.OrNd(),
        Email: string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
        Pais: Pais.OrNd(),
        Region: Region.OrNd(),
        Ciudad: Ciudad.OrNd(),
        VendedorCodigo: string.IsNullOrWhiteSpace(VendedorCodigo) ? null : VendedorCodigo.Trim(),
        VendedorNombre: string.IsNullOrWhiteSpace(VendedorNombre) ? null : VendedorNombre.Trim(),
        VendedorRegion: string.IsNullOrWhiteSpace(VendedorRegion) ? null : VendedorRegion.Trim(),
        Cantidad: Cantidad,
        MontoTotal: MontoTotal,
        Costo: Costo,
        OrigenFuente: "CSV");
}
