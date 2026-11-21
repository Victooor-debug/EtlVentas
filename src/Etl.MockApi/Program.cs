// API REST simulada para probar el ApiExtractor sin depender de un servicio externo.
// Requiere el encabezado:  Authorization: Bearer <token>   (por defecto "dev-token-demo")
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var token = builder.Configuration["MockApi:Token"] ?? "dev-token-demo";

var ventas = new List<VentaDto>
{
    // Misma factura/línea que el CSV pero con otro monto: debe ganar la API (prioridad 1).
    new("F-0002", 1, new DateTime(2026, 1, 16), "P003", "Monitor 24 pulgadas", "Pantallas", "Monitores", "Dell", 9500m, 7000m,
        "C-011", "Carlos Ruiz", "Corporativo", "carlos@empresa.com", "República Dominicana", "Santiago", "Santiago",
        "V02", "María Díaz", "Santiago", 1, 9800m, 7000m),

    new("F-0003", 1, new DateTime(2026, 1, 17), "P001", "Teclado mecánico", "Accesorios", "Periféricos", "Logitech", 1500m, 1000m,
        "C-012", "Rosa Mena", "Individual", "rosa@ejemplo.com", "República Dominicana", "Cibao", "La Vega",
        "V01", "Luis Gómez", "Santo Domingo", 3, 4500m, 3000m),

    // Misma factura/línea que la BD (V-3): debe ganar la API sobre DB_EXTERNA.
    new("V-3", 1, new DateTime(2026, 1, 20), "P004", "Laptop 15 pulgadas", "Computadoras", "Laptops", "Lenovo", 45000m, 36000m,
        "C2", "Carlos Ruiz", "Corporativo", "carlos@empresa.com", "República Dominicana", "Santiago", "Santiago",
        "V02", "María Díaz", "Santiago", 1, 46000m, 36000m),

    new("F-0004", 1, new DateTime(2026, 1, 22), "P004", "Laptop 15 pulgadas", "Computadoras", "Laptops", "Lenovo", 45000m, 36000m,
        "C-013", "Pedro Sosa", "Individual", "pedro@ejemplo.com", "República Dominicana", "Norte", "Puerto Plata",
        "V01", "Luis Gómez", "Santo Domingo", 1, 45000m, 36000m)
};

app.MapGet("/ventas", (HttpRequest request, int? page, int? pageSize) =>
{
    if (request.Headers.Authorization != $"Bearer {token}")
        return Results.Unauthorized();

    var p = Math.Max(1, page ?? 1);
    var ps = Math.Clamp(pageSize ?? 100, 1, 1000);
    var data = ventas.Skip((p - 1) * ps).Take(ps).ToList();

    return Results.Ok(new { page = p, pageSize = ps, total = ventas.Count, data });
});

app.Run("http://localhost:5100");

record VentaDto(
    string NumeroFactura, short LineaFactura, DateTime Fecha,
    string ProductoCodigo, string ProductoNombre, string Categoria, string? Subcategoria, string? Marca,
    decimal? PrecioLista, decimal? CostoUnitario,
    string ClienteCodigo, string ClienteNombre, string TipoCliente, string? Email,
    string Pais, string Region, string Ciudad,
    string? VendedorCodigo, string? VendedorNombre, string? VendedorRegion,
    int Cantidad, decimal MontoTotal, decimal Costo);
