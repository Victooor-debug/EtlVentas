using System.Diagnostics;
using System.Text.Json;
using Etl.Application;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Etl.Infrastructure;

public sealed class SqlDimensionLoader(IConfiguration config, ILogger<SqlDimensionLoader> logger) : IDimensionLoader
{
    private sealed class StagingJsonItem
    {
        public DateTime Fecha { get; set; }
        public string ProductoCodigo { get; set; } = "";
        public string ProductoNombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Subcategoria { get; set; } = "";
        public string Marca { get; set; } = "";
        public decimal PrecioLista { get; set; }
        public decimal CostoUnitario { get; set; }
        public string ClienteCodigo { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
        public string TipoCliente { get; set; } = "";
        public string Email { get; set; } = "";
        public string Pais { get; set; } = "";
        public string Region { get; set; } = "";
        public string Ciudad { get; set; } = "";
        public string OrigenFuente { get; set; } = "";
        public string VendedorCodigo { get; set; } = "";
        public string VendedorNombre { get; set; } = "";
        public string VendedorRegion { get; set; } = "";
    }

    private sealed record TableMeta(HashSet<string> Columns, HashSet<string> IdentityColumns);

    public static async Task RunFromLatestStagingAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<SqlDimensionLoader>>();
        var stagingDir = Path.Combine(AppContext.BaseDirectory, "staging");

        if (!Directory.Exists(stagingDir))
        {
            logger.LogWarning("No existe la carpeta staging en {Dir}", stagingDir);
            return;
        }

        var latestFile = Directory.GetFiles(stagingDir, "ventas_consolidado_*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (latestFile is null)
        {
            logger.LogWarning("No se encontró archivo ventas_consolidado_*.json en {Dir}", stagingDir);
            return;
        }

        var json = await File.ReadAllTextAsync(latestFile, ct);
        var items = JsonSerializer.Deserialize<List<StagingJsonItem>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        var rows = items.Select(i => new DimensionSourceRow(
            i.Fecha, i.ProductoCodigo, i.ProductoNombre, i.Categoria, i.Subcategoria, i.Marca,
            i.PrecioLista, i.CostoUnitario, i.ClienteCodigo, i.ClienteNombre, i.TipoCliente,
            i.Email, i.Pais, i.Region, i.Ciudad, i.OrigenFuente,
            i.VendedorCodigo, i.VendedorNombre, i.VendedorRegion)).ToList();

        logger.LogInformation("Iniciando carga de dimensiones desde {File} ({Count} registros consolidados)...",
            Path.GetFileName(latestFile), rows.Count);

        var loader = new SqlDimensionLoader(config, logger);
        await loader.LoadAsync(rows, ct);
    }

    public async Task<DimensionLoadResult> LoadAsync(IReadOnlyList<DimensionSourceRow> registros, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var connStr = ResolveConnectionString();
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        var stats = new List<DimensionStats>
        {
            await LoadTiempoAsync(conn, DimensionMapper.BuildTiempo(registros), ct),
            await LoadClientesAsync(conn, DimensionMapper.BuildClientes(registros), ct),
            await LoadProductosAsync(conn, DimensionMapper.BuildProductos(registros), ct),
            await LoadVendedoresAsync(conn, DimensionMapper.BuildVendedores(registros), ct)
        };

        sw.Stop();
        foreach (var s in stats)
        {
            logger.LogInformation("Dimensión {Tabla}: únicas={Leidas}, insertadas={Ins}, actualizadas={Act}",
                s.Tabla, s.Leidas, s.Insertadas, s.Actualizadas);
            await WriteAuditAsync(conn, s, ct);
        }

        logger.LogInformation("Carga de dimensiones finalizada (OK) en {Ms} ms.", sw.ElapsedMilliseconds);
        return new DimensionLoadResult(stats, sw.ElapsedMilliseconds);
    }

    private string ResolveConnectionString()
    {
        var all = config.AsEnumerable()
            .Where(c => !string.IsNullOrWhiteSpace(c.Value) &&
                        (c.Value.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                         c.Value.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var dw = all.FirstOrDefault(c =>
            c.Key.Contains("Warehouse", StringComparison.OrdinalIgnoreCase) ||
            c.Key.Contains("Destino", StringComparison.OrdinalIgnoreCase) ||
            c.Key.Contains("Audit", StringComparison.OrdinalIgnoreCase) ||
            (c.Value?.Contains("sistema_analisis_ventas", StringComparison.OrdinalIgnoreCase) ?? false));

        return dw.Value ?? all.FirstOrDefault().Value
            ?? throw new InvalidOperationException("No se encontró cadena de conexión SQL Server.");
    }

    private static async Task<TableMeta> GetMetaAsync(SqlConnection conn, string table, CancellationToken ct)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        const string sql = """
            SELECT COLUMN_NAME,
                   COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity')
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE LOWER(TABLE_NAME) = LOWER(@t)
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@t", table);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            var col = rd.GetString(0);
            cols.Add(col);
            if (!rd.IsDBNull(1) && rd.GetInt32(1) == 1)
                ids.Add(col);
        }

        return new TableMeta(cols, ids);
    }

    private static async Task<DimensionStats> LoadTiempoAsync(SqlConnection conn, IReadOnlyList<DimTiempoRow> rows, CancellationToken ct)
    {
        var meta = await GetMetaAsync(conn, "Dim_Tiempo", ct);
        if (meta.Columns.Count == 0) return new DimensionStats("Dim_Tiempo", rows.Count, 0, 0);

        int ins = 0;
        foreach (var r in rows)
        {
            await using var chk = new SqlCommand("SELECT COUNT(1) FROM dbo.Dim_Tiempo WHERE CAST(fecha AS DATE) = @f", conn);
            chk.Parameters.AddWithValue("@f", r.Fecha);
            if (Convert.ToInt32(await chk.ExecuteScalarAsync(ct)) > 0) continue;

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["tiempo_id"] = r.TiempoId,
                ["fecha"] = r.Fecha,
                ["dia"] = r.Dia,
                ["mes"] = r.Mes,
                ["nombre_mes"] = r.NombreMes,
                ["trimestre"] = r.Trimestre,
                ["anio"] = r.Anio
            };

            await ExecuteInsertAsync(conn, "Dim_Tiempo", meta, values, ct);
            ins++;
        }

        return new DimensionStats("Dim_Tiempo", rows.Count, ins, 0);
    }

    private static async Task<DimensionStats> LoadClientesAsync(SqlConnection conn, IReadOnlyList<DimClienteRow> rows, CancellationToken ct)
    {
        var meta = await GetMetaAsync(conn, "Dim_Cliente", ct);
        if (meta.Columns.Count == 0) return new DimensionStats("Dim_Cliente", rows.Count, 0, 0);

        var keyCol = meta.Columns.Contains("cliente_codigo") ? "cliente_codigo" : "nombre";
        int ins = 0, act = 0;

        foreach (var r in rows)
        {
            var keyVal = keyCol == "cliente_codigo" && !string.IsNullOrWhiteSpace(r.Codigo) ? r.Codigo : r.Nombre;
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["cliente_codigo"] = r.Codigo,
                ["nombre"] = r.Nombre,
                ["tipo_cliente"] = r.TipoCliente,
                ["email"] = r.Email,
                ["pais"] = r.Pais,
                ["region"] = r.Region,
                ["ciudad"] = r.Ciudad,
                ["fuente"] = r.Fuente,
                ["origen_fuente"] = r.Fuente,
                ["fecha_actualizacion"] = DateTime.Now
            };

            if (await UpsertRowAsync(conn, "Dim_Cliente", meta, keyCol, keyVal, values, ct)) ins++; else act++;
        }

        return new DimensionStats("Dim_Cliente", rows.Count, ins, act);
    }

    private static async Task<DimensionStats> LoadProductosAsync(SqlConnection conn, IReadOnlyList<DimProductoRow> rows, CancellationToken ct)
    {
        var meta = await GetMetaAsync(conn, "Dim_Producto", ct);
        if (meta.Columns.Count == 0) return new DimensionStats("Dim_Producto", rows.Count, 0, 0);

        var keyCol = meta.Columns.Contains("producto_codigo") ? "producto_codigo" : "nombre_producto";
        int ins = 0, act = 0;

        foreach (var r in rows)
        {
            var keyVal = keyCol == "producto_codigo" && !string.IsNullOrWhiteSpace(r.Codigo) ? r.Codigo : r.Nombre;
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["producto_codigo"] = r.Codigo,
                ["nombre_producto"] = r.Nombre,
                ["categoria"] = r.Categoria,
                ["subcategoria"] = r.Subcategoria,
                ["marca"] = r.Marca,
                ["precio_actual"] = r.PrecioActual,
                ["costo_unitario"] = r.CostoUnitario,
                ["fuente"] = r.Fuente,
                ["origen_fuente"] = r.Fuente,
                ["fecha_actualizacion"] = DateTime.Now
            };

            if (await UpsertRowAsync(conn, "Dim_Producto", meta, keyCol, keyVal, values, ct)) ins++; else act++;
        }

        return new DimensionStats("Dim_Producto", rows.Count, ins, act);
    }

    private static async Task<DimensionStats> LoadVendedoresAsync(SqlConnection conn, IReadOnlyList<DimVendedorRow> rows, CancellationToken ct)
    {
        var meta = await GetMetaAsync(conn, "Dim_Vendedor", ct);
        if (meta.Columns.Count == 0) return new DimensionStats("Dim_Vendedor", rows.Count, 0, 0);

        var keyCol = meta.Columns.Contains("vendedor_codigo") ? "vendedor_codigo" : "nombre";
        int ins = 0, act = 0;

        foreach (var r in rows)
        {
            var keyVal = keyCol == "vendedor_codigo" && !string.IsNullOrWhiteSpace(r.Codigo) ? r.Codigo : r.Nombre;
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["vendedor_codigo"] = r.Codigo,
                ["nombre"] = r.Nombre,
                ["region"] = r.Region,
                ["fuente"] = r.Fuente,
                ["origen_fuente"] = r.Fuente,
                ["fecha_actualizacion"] = DateTime.Now
            };

            if (await UpsertRowAsync(conn, "Dim_Vendedor", meta, keyCol, keyVal, values, ct)) ins++; else act++;
        }

        return new DimensionStats("Dim_Vendedor", rows.Count, ins, act);
    }

    private static async Task<bool> UpsertRowAsync(
        SqlConnection conn, string table, TableMeta meta, string keyCol, object keyVal,
        Dictionary<string, object?> values, CancellationToken ct)
    {
        await using var chk = new SqlCommand($"SELECT COUNT(1) FROM dbo.{table} WHERE {keyCol} = @k", conn);
        chk.Parameters.AddWithValue("@k", keyVal);
        var exists = Convert.ToInt32(await chk.ExecuteScalarAsync(ct)) > 0;

        if (!exists)
        {
            await ExecuteInsertAsync(conn, table, meta, values, ct);
            return true;
        }

        var updatable = values.Keys
            .Where(c => meta.Columns.Contains(c) && !meta.IdentityColumns.Contains(c) && !c.Equals(keyCol, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (updatable.Count == 0) return false;

        var setClause = string.Join(", ", updatable.Select((c, i) => $"{c} = @u{i}"));
        await using var upd = new SqlCommand($"UPDATE dbo.{table} SET {setClause} WHERE {keyCol} = @k", conn);
        upd.Parameters.AddWithValue("@k", keyVal);
        for (int i = 0; i < updatable.Count; i++)
            upd.Parameters.AddWithValue($"@u{i}", values[updatable[i]] ?? DBNull.Value);

        await upd.ExecuteNonQueryAsync(ct);
        return false;
    }

    private static async Task ExecuteInsertAsync(
        SqlConnection conn, string table, TableMeta meta,
        Dictionary<string, object?> values, CancellationToken ct)
    {
        var cols = values.Keys
            .Where(c => meta.Columns.Contains(c) && !meta.IdentityColumns.Contains(c))
            .ToList();

        var colList = string.Join(", ", cols);
        var paramList = string.Join(", ", cols.Select((_, i) => $"@p{i}"));

        await using var ins = new SqlCommand($"INSERT INTO dbo.{table} ({colList}) VALUES ({paramList})", conn);
        for (int i = 0; i < cols.Count; i++)
            ins.Parameters.AddWithValue($"@p{i}", values[cols[i]] ?? DBNull.Value);

        await ins.ExecuteNonQueryAsync(ct);
    }

    private static async Task WriteAuditAsync(SqlConnection conn, DimensionStats s, CancellationToken ct)
    {
        var meta = await GetMetaAsync(conn, "etl_log", ct);
        if (meta.Columns.Count == 0) return;

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["fecha_ejecucion"] = DateTime.Now,
            ["fuente"] = "STAGING_JSON",
            ["entidad"] = s.Tabla,
            ["filas_leidas"] = s.Leidas,
            ["filas_insertadas"] = s.Insertadas,
            ["filas_actualizadas"] = s.Actualizadas,
            ["filas_rechazadas"] = 0,
            ["estado"] = "OK",
            ["mensaje"] = $"Carga de {s.Tabla} completada"
        };

        await ExecuteInsertAsync(conn, "etl_log", meta, values, ct);
    }
}

