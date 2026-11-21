-- Datos de prueba para la fuente DB_EXTERNA (tablas transaccionales).
-- IMPORTANTE: ejecutar solo con las tablas transaccionales VACÍAS (los IDs son IDENTITY).
USE sistema_analisis_ventas;
GO

INSERT INTO dbo.FuentesDatos (nombre_fuente, tipo_fuente)
VALUES ('Sistema transaccional de ventas', 'DB_EXTERNA');

INSERT INTO dbo.Categorias (nombre_categoria, descripcion) VALUES
 ('Accesorios',   'Periféricos'),
 ('Pantallas',    'Monitores'),
 ('Computadoras', 'Laptops y PCs');

INSERT INTO dbo.Productos (categoria_id, nombre_producto, sku, precio_unitario, costo_unitario) VALUES
 (1, 'Teclado mecánico',    'P001',  1500.00,  1000.00),
 (1, 'Mouse inalámbrico',   'P002',   800.00,   500.00),
 (2, 'Monitor 24 pulgadas', 'P003',  9500.00,  7000.00),
 (3, 'Laptop 15 pulgadas',  'P004', 45000.00, 36000.00);

INSERT INTO dbo.Clientes (nombre, apellido, email, telefono, ciudad, pais) VALUES
 ('Ana',    'Pérez', 'ana@ejemplo.com',    '809-555-0101', 'Santo Domingo Este', 'República Dominicana'),
 ('Carlos', 'Ruiz',  'carlos@empresa.com', '809-555-0102', 'Santiago',           'República Dominicana'),
 ('Rosa',   'Mena',  'rosa@ejemplo.com',   NULL,           'La Vega',            'República Dominicana'),
 ('Pedro',  'Sosa',  'pedro@ejemplo.com',  NULL,           'Puerto Plata',       'República Dominicana');

INSERT INTO dbo.Ventas (cliente_id, fuente_id, fecha_venta, canal_venta, monto_total) VALUES
 (1, 1, '2026-01-15', 'Tienda',  3800.00),
 (4, 1, '2026-01-18', 'Online',  4000.00),
 (2, 1, '2026-01-20', 'Tienda', 45000.00);

INSERT INTO dbo.DetalleVentas (venta_id, producto_id, cantidad, precio_unitario) VALUES
 (1, 1, 2, 1500.00),   -- V-1 línea 1
 (1, 2, 1,  800.00),   -- V-1 línea 2
 (2, 2, 5,  800.00),   -- V-2 línea 1
 (3, 4, 1, 45000.00);  -- V-3 línea 1
GO
