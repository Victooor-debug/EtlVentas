namespace Etl.Infrastructure.Options;

public sealed class CsvSourceOptions
{
    public bool Enabled { get; set; } = true;
    public string FilePath { get; set; } = "Data/ventas.csv";
}

public sealed class DbSourceOptions
{
    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = string.Empty;   // va en User Secrets / variables de entorno
    public string QueryFile { get; set; } = "Queries/ventas_db_externa.sql";
    public DateTime? Desde { get; set; }                            // extracción incremental (fecha_venta > Desde)
    public int CommandTimeoutSeconds { get; set; } = 60;
}

public sealed class ApiSourceOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:5100/";
    public string Endpoint { get; set; } = "ventas";
    public int PageSize { get; set; } = 500;
    public string Token { get; set; } = string.Empty;               // va en User Secrets / variables de entorno
}

public sealed class StagingOptions
{
    public string Folder { get; set; } = "staging";
    public bool Indented { get; set; }
}

public sealed class AnalyticsOptions
{
    /// <summary>Conexión a la BD analítica (para escribir en etl_log). Vacía = no se registra en BD.</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
