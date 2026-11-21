-- Extracción de ventas desde las tablas transaccionales (fuente DB_EXTERNA).
-- Parámetro: @desde (extracción incremental por fecha_venta).
-- El ORDEN de las columnas es un contrato con DatabaseExtractor.cs.
SELECT
    CONCAT('V-', v.venta_id)                                                              AS numero_factura,   -- 0
    CAST(ROW_NUMBER() OVER (PARTITION BY v.venta_id ORDER BY d.detalle_id) AS smallint)   AS linea_factura,    -- 1
    v.fecha_venta                                                                         AS fecha_venta,      -- 2
    p.sku                                                                                 AS producto_codigo,  -- 3
    p.nombre_producto                                                                     AS nombre_producto,  -- 4
    c.nombre_categoria                                                                    AS categoria,        -- 5
    p.precio_unitario                                                                     AS precio_lista,     -- 6
    p.costo_unitario                                                                      AS costo_unitario,   -- 7
    CONCAT('C', cl.cliente_id)                                                            AS cliente_codigo,   -- 8
    CONCAT(cl.nombre, ' ', cl.apellido)                                                   AS cliente_nombre,   -- 9
    cl.email                                                                              AS email,            -- 10
    ISNULL(cl.pais, 'N/D')                                                                AS pais,             -- 11
    ISNULL(cl.ciudad, 'N/D')                                                              AS ciudad,           -- 12
    d.cantidad                                                                            AS cantidad,         -- 13
    d.subtotal                                                                            AS monto_total,      -- 14
    d.cantidad * p.costo_unitario                                                         AS costo             -- 15
FROM dbo.Ventas        AS v
JOIN dbo.DetalleVentas AS d  ON d.venta_id     = v.venta_id
JOIN dbo.Productos     AS p  ON p.producto_id  = d.producto_id
JOIN dbo.Categorias    AS c  ON c.categoria_id = p.categoria_id
JOIN dbo.Clientes      AS cl ON cl.cliente_id  = v.cliente_id
WHERE v.fecha_venta IS NOT NULL
  AND v.fecha_venta > @desde;
