using System.Net.Http.Json;
using System.Text.Json;
using Etl.Application.Abstractions;
using Etl.Domain;
using Etl.Domain.Entities;
using Etl.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Extractors;

public sealed class ApiExtractor : IExtractor
{
    public const string ClientName = "VentasApi";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _factory;
    private readonly ApiSourceOptions _options;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(IHttpClientFactory factory, IOptions<ApiSourceOptions> options, ILogger<ApiExtractor> logger)
    {
        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    public string SourceName => "API";
    public int Priority => 1;

    public async Task<ExtractionOutput> ExtractAsync(CancellationToken ct)
    {
        var client = _factory.CreateClient(ClientName);
        var pageSize = Math.Max(1, _options.PageSize);

        var records = new List<VentaRecord>();
        int page = 1, read = 0, total;

        // Paginación: evita traer volúmenes grandes en una sola respuesta.
        do
        {
            ct.ThrowIfCancellationRequested();

            using var response = await client.GetAsync($"{_options.Endpoint}?page={page}&pageSize={pageSize}", ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<ApiPage>(JsonOptions, ct) ?? new ApiPage();
            var data = body.Data ?? new List<VentaApiDto>();
            total = body.Total;
            read += data.Count;
            records.AddRange(data.Select(d => d.ToDomain()));

            _logger.LogDebug("API: página {Page} ({Count} registros, acumulado {Read}/{Total})", page, data.Count, read, total);

            if (data.Count == 0) break;
            page++;
        }
        while (read < total);

        return new ExtractionOutput(records, read, 0);
    }
}

internal sealed class ApiPage
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<VentaApiDto>? Data { get; set; }
}

/// <summary>Forma del JSON que devuelve la API (camelCase).</summary>
internal sealed class VentaApiDto
{
    public string? NumeroFactura { get; set; }
    public short LineaFactura { get; set; }
    public DateTime Fecha { get; set; }
    public string? ProductoCodigo { get; set; }
    public string? ProductoNombre { get; set; }
    public string? Categoria { get; set; }
    public string? Subcategoria { get; set; }
    public string? Marca { get; set; }
    public decimal? PrecioLista { get; set; }
    public decimal? CostoUnitario { get; set; }
    public string? ClienteCodigo { get; set; }
    public string? ClienteNombre { get; set; }
    public string? TipoCliente { get; set; }
    public string? Email { get; set; }
    public string? Pais { get; set; }
    public string? Region { get; set; }
    public string? Ciudad { get; set; }
    public string? VendedorCodigo { get; set; }
    public string? VendedorNombre { get; set; }
    public string? VendedorRegion { get; set; }
    public int Cantidad { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal Costo { get; set; }

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
        OrigenFuente: "API");
}
