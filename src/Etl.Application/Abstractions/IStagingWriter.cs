namespace Etl.Application.Abstractions;

public interface IStagingWriter
{
    /// <summary>Guarda los datos en el área de staging y devuelve la ruta del archivo generado.</summary>
    Task<string> WriteAsync<T>(string name, IReadOnlyCollection<T> data, CancellationToken ct);
}
