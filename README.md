# EtlVentas - Sistema de Analisis de Ventas (ETL)

Worker Service en .NET 8 bajo Clean Architecture para la extraccion, validacion, consolidacion y carga de dimensiones del Data Warehouse sistema_analisis_ventas en SQL Server.

## Estructura
- Etl.Domain: Modelo canonico VentaRecord (24 campos) y normalizacion de texto.
- Etl.Application: Contratos, ExtractionOrchestrator, VentaValidator, VentaConsolidator y DimensionMapper.
- Etl.Infrastructure: Extractores (CSV, SQL Server, API REST), JsonStagingWriter, SqlDimensionLoader y auditoria en dbo.etl_log.
- Etl.Worker: Servicio anfitrion (BackgroundService) con configuracion segura y Serilog.
- Etl.MockApi y Etl.Tests: API REST simulada y pruebas unitarias con xUnit.

## Ejecucion
    dotnet test
    dotnet run --project src/Etl.Worker/Etl.Worker.csproj
