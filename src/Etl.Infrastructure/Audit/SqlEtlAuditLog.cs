using System.Data;
using Etl.Application.Abstractions;
using Etl.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Etl.Infrastructure.Audit;

/// <summary>Escribe cada ejecución en dbo.etl_log. Si falla, solo avisa por log: el ETL no se detiene.</summary>
public sealed class SqlEtlAuditLog : IEtlAuditLog
{
    private const string InsertSql = @"
INSERT INTO dbo.etl_log (fuente, entidad, filas_leidas, filas_insertadas, filas_actualizadas, filas_rechazadas, estado, mensaje)
VALUES (@fuente, @entidad, @leidas, 0, 0, @rechazadas, @estado, @mensaje);";

    private readonly string _connectionString;
    private readonly ILogger<SqlEtlAuditLog> _logger;

    public SqlEtlAuditLog(IOptions<AnalyticsOptions> options, ILogger<SqlEtlAuditLog> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task RegistrarAsync(EtlLogEntry entry, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(InsertSql, conn);
            cmd.Parameters.Add("@fuente", SqlDbType.VarChar, 20).Value = Truncar(entry.Fuente, 20);
            cmd.Parameters.Add("@entidad", SqlDbType.VarChar, 40).Value = Truncar(entry.Entidad, 40);
            cmd.Parameters.Add("@leidas", SqlDbType.Int).Value = entry.FilasLeidas;
            cmd.Parameters.Add("@rechazadas", SqlDbType.Int).Value = entry.FilasRechazadas;
            cmd.Parameters.Add("@estado", SqlDbType.VarChar, 10).Value = Truncar(entry.Estado, 10);
            cmd.Parameters.Add("@mensaje", SqlDbType.NVarChar, 500).Value =
                entry.Mensaje is null ? DBNull.Value : Truncar(entry.Mensaje, 500);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "No se pudo escribir en etl_log (fuente {Fuente})", entry.Fuente);
        }
    }

    private static string Truncar(string valor, int max) => valor.Length <= max ? valor : valor[..max];
}

/// <summary>Se usa cuando no hay cadena de conexión analítica configurada.</summary>
public sealed class NullEtlAuditLog : IEtlAuditLog
{
    public Task RegistrarAsync(EtlLogEntry entry, CancellationToken ct) => Task.CompletedTask;
}
