using System.Data;
using Etl.Application.Abstractions;
using Etl.Domain;
using Etl.Domain.Entities;
using Etl.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Extractors;

/// <summary>
/// Extrae las ventas desde las tablas transaccionales (Ventas, DetalleVentas, Productos,
/// Categorias, Clientes). El query vive en un archivo .sql configurable.
/// </summary>
public sealed class DatabaseExtractor : IExtractor
{
    private readonly DbSourceOptions _options;

    public DatabaseExtractor(IOptions<DbSourceOptions> options) => _options = options.Value;

    public string SourceName => "DB_EXTERNA";
    public int Priority => 3;

    public async Task<ExtractionOutput> ExtractAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new InvalidOperationException(
                "Falta Sources:Database:ConnectionString (configúrala con User Secrets o variables de entorno).");

        var queryPath = Path.IsPathRooted(_options.QueryFile)
            ? _options.QueryFile
            : Path.Combine(AppContext.BaseDirectory, _options.QueryFile);
        var sql = await File.ReadAllTextAsync(queryPath, ct);

        var records = new List<VentaRecord>();

        await using var conn = new SqlConnection(_options.ConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = _options.CommandTimeoutSeconds };
        cmd.Parameters.Add("@desde", SqlDbType.DateTime).Value = _options.Desde ?? new DateTime(1900, 1, 1);

        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            // El orden de columnas lo define Queries/ventas_db_externa.sql
            records.Add(new VentaRecord(
                NumeroFactura: rd.GetString(0),
                LineaFactura: rd.GetInt16(1),
                Fecha: rd.GetDateTime(2),
                ProductoCodigo: rd.GetString(3),
                ProductoNombre: rd.GetString(4),
                Categoria: rd.GetString(5),
                Subcategoria: null,
                Marca: null,
                PrecioLista: rd.GetDecimal(6),
                CostoUnitario: rd.GetDecimal(7),
                ClienteCodigo: rd.GetString(8),
                ClienteNombre: rd.GetString(9),
                TipoCliente: TextoExtensions.NoDisponible,   // la BD transaccional no maneja tipo de cliente
                Email: rd.IsDBNull(10) ? null : rd.GetString(10),
                Pais: rd.GetString(11),
                Region: TextoExtensions.NoDisponible,        // tampoco región
                Ciudad: rd.GetString(12),
                VendedorCodigo: null,                        // ni vendedor (vendedor_id es NULL en Fact_Ventas)
                VendedorNombre: null,
                VendedorRegion: null,
                Cantidad: rd.GetInt32(13),
                MontoTotal: rd.GetDecimal(14),
                Costo: rd.GetDecimal(15),
                OrigenFuente: "DB_EXTERNA"));
        }

        return new ExtractionOutput(records, records.Count, 0);
    }
}
