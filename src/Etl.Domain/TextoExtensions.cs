namespace Etl.Domain;

public static class TextoExtensions
{
    public const string NoDisponible = "N/D";

    /// <summary>Texto limpio o "N/D" cuando viene vacío (campos NOT NULL del DW).</summary>
    public static string OrNd(this string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? NoDisponible : valor.Trim();

    /// <summary>Texto sin espacios sobrantes; cadena vacía si es nulo.</summary>
    public static string Limpio(this string? valor) => valor?.Trim() ?? string.Empty;
}
