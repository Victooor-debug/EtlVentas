-- Seguridad: el ETL usa dos usuarios con permisos mínimos.
-- Cambia las contraseñas y guárdalas SOLO en User Secrets / variables de entorno.
USE master;
GO
CREATE LOGIN etl_lector   WITH PASSWORD = '<<CAMBIAR_CONTRASENA_1>>';
CREATE LOGIN etl_escritor WITH PASSWORD = '<<CAMBIAR_CONTRASENA_2>>';
GO
USE sistema_analisis_ventas;
GO
CREATE USER etl_lector   FOR LOGIN etl_lector;
CREATE USER etl_escritor FOR LOGIN etl_escritor;

-- Solo lectura sobre las tablas fuente (DatabaseExtractor)
GRANT SELECT ON dbo.Ventas        TO etl_lector;
GRANT SELECT ON dbo.DetalleVentas TO etl_lector;
GRANT SELECT ON dbo.Productos     TO etl_lector;
GRANT SELECT ON dbo.Categorias    TO etl_lector;
GRANT SELECT ON dbo.Clientes      TO etl_lector;

-- Solo inserción en la bitácora (SqlEtlAuditLog)
GRANT INSERT ON dbo.etl_log TO etl_escritor;
GO
