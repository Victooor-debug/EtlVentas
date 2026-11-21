namespace Etl.Application.Abstractions;

public static class EtlEstados
{
    public const string Ok = "OK";
    public const string Parcial = "PARCIAL";
    public const string Error = "ERROR";
}

public sealed record EtlLogEntry(
    string Fuente,
    string Entidad,
    int FilasLeidas,
    int FilasRechazadas,
    string Estado,
    string? Mensaje);

/// <summary>Bitácora persistente (tabla etl_log). Las implementaciones no deben lanzar excepciones.</summary>
public interface IEtlAuditLog
{
    Task RegistrarAsync(EtlLogEntry entry, CancellationToken ct);
}
