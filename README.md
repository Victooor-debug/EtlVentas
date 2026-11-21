# EtlVentas – Proceso de extracción (E de ETL) con Worker Service .NET 8

Extrae ventas desde **CSV**, **base de datos relacional (SQL Server)** y **API REST**, valida,
guarda en **staging (JSON)**, consolida con prioridad **API > CSV > DB_EXTERNA** y registra logs
(`ILogger` + Serilog en consola/archivo, y tabla `etl_log`).

## Arquitectura (Clean Architecture)

```
Etl.Worker  ──►  Etl.Infrastructure  ──►  Etl.Application  ──►  Etl.Domain
(host, config)   (CSV/BD/API, staging,    (IExtractor, orquestador,  (VentaRecord)
                  etl_log, DI)             validador, consolidador)
```

- `IExtractor` abstrae cada fuente; `CsvExtractor`, `DatabaseExtractor` y `ApiExtractor` la implementan.
- Las fuentes se activan/desactivan en `appsettings.json` (`Sources:*:Enabled`).
- Credenciales fuera del repo (User Secrets / variables de entorno).

## Requisitos

- .NET 8 SDK
- SQL Server con la base `sistema_analisis_ventas` (script del modelado)

## Puesta en marcha

1. **Datos de prueba en la BD** (tablas transaccionales vacías): ejecutar `sql/01_datos_prueba.sql`.
   Opcional: `sql/02_usuarios_minimo_privilegio.sql` (usuario solo lectura / solo escritura de bitácora).
2. **Secretos** (desde la carpeta raíz):
   ```bash
   dotnet user-secrets set "Sources:Api:Token" "dev-token-demo" --project src/Etl.Worker
   dotnet user-secrets set "Sources:Database:ConnectionString" "Server=localhost;Database=sistema_analisis_ventas;Trusted_Connection=True;TrustServerCertificate=True;" --project src/Etl.Worker
   dotnet user-secrets set "Analytics:ConnectionString" "Server=localhost;Database=sistema_analisis_ventas;Trusted_Connection=True;TrustServerCertificate=True;" --project src/Etl.Worker
   ```
   (`Analytics:ConnectionString` es opcional: si queda vacía no se escribe en `etl_log`.)
   Equivalente con variables de entorno: `Sources__Api__Token`, `Sources__Database__ConnectionString`, etc.
3. **API simulada** (terminal 1): `dotnet run --project src/Etl.MockApi`
4. **Worker** (terminal 2): `dotnet run --project src/Etl.Worker`
5. **Pruebas unitarias**: `dotnet test`

Los archivos de staging quedan en `src/Etl.Worker/bin/Debug/net8.0/staging/` y los logs en `.../logs/`.

## Resultado esperado con los datos de ejemplo

| Fuente | Leídas | Válidas | Rechazadas |
|---|---|---|---|
| API | 4 | 4 | 0 |
| CSV | 6 | 4 | 2 (fecha inválida y cantidad = 0) |
| DB_EXTERNA | 4 | 4 | 0 |

Consolidado: **9 filas únicas, 3 descartadas por prioridad**
(`F-0002/1` gana la API; `V-3/1` gana la API; `V-1/1` gana el CSV).

## Agregar una fuente nueva

1. Crear una clase que implemente `IExtractor` (nombre de fuente, prioridad y `ExtractAsync`).
2. Registrarla en `Etl.Infrastructure/DependencyInjection.cs` (con su sección de configuración).
3. Si la fuente es nueva para el DW, agregar su valor al CHECK `CK_Fact_Ventas_fuente`.

## Alcance

Esta entrega cubre la **extracción**: lectura, validación, staging, consolidación y logs.
La carga a `Dim_*` / `Fact_Ventas` (`DataLoader`) corresponde a la siguiente etapa.
