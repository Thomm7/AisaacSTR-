


-- 1. TABLA: USUARIOS
CREATE TABLE IF NOT EXISTS usuarios (
    id SERIAL PRIMARY KEY,
    nombre_completo VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    rol VARCHAR(50) NOT NULL DEFAULT 'Cajero', -- 'Administrador' o 'Cajero'
    telefono VARCHAR(30),
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE EXTENSION IF NOT EXISTS pgcrypto;

UPDATE usuarios
SET password_hash = crypt('Admin123*', gen_salt('bf', 11))
WHERE lower(email) = 'admin@aisaacstr.com';

UPDATE usuarios
SET password_hash = crypt('Cajero123*', gen_salt('bf', 11))
WHERE lower(email) = 'cajero@aisaacstr.com';

CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios(email);
CREATE INDEX IF NOT EXISTS idx_usuarios_rol ON usuarios(rol);

-- 2. TABLA: PRODUCTOS (Base)
CREATE TABLE IF NOT EXISTS productos (
    id SERIAL PRIMARY KEY,
    codigo_base VARCHAR(50) NOT NULL UNIQUE,
    nombre VARCHAR(150) NOT NULL,
    descripcion VARCHAR(500),
    categoria VARCHAR(80) NOT NULL DEFAULT 'Prendas de Vestir',
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_productos_codigo_base ON productos(codigo_base);
CREATE INDEX IF NOT EXISTS idx_productos_categoria ON productos(categoria);

-- 3. TABLA: VARIANTE_PRODUCTOS (Matriz Talla/Marca/Color/Stock/Precios)
CREATE TABLE IF NOT EXISTS variante_productos (
    id SERIAL PRIMARY KEY,
    producto_id INTEGER NOT NULL REFERENCES productos(id) ON DELETE CASCADE,
    talla VARCHAR(20) NOT NULL,
    marca VARCHAR(50) NOT NULL,
    color VARCHAR(30) NOT NULL,
    sku VARCHAR(60) NOT NULL UNIQUE,
    codigo_barras VARCHAR(60),
    precio_compra NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    precio_venta NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    stock_actual INTEGER NOT NULL DEFAULT 0,
    stock_minimo INTEGER NOT NULL DEFAULT 5,
    stock_maximo INTEGER NOT NULL DEFAULT 50,
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS idx_variantes_sku ON variante_productos(sku);
CREATE INDEX IF NOT EXISTS idx_variantes_codigo_barras ON variante_productos(codigo_barras);
CREATE INDEX IF NOT EXISTS idx_variantes_producto_id ON variante_productos(producto_id);
CREATE INDEX IF NOT EXISTS idx_variantes_stock_alerta ON variante_productos(stock_actual, stock_minimo);

-- 4. TABLA: MEDIOS_PAGO
CREATE TABLE IF NOT EXISTS medios_pago (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(30) NOT NULL UNIQUE,
    nombre VARCHAR(60) NOT NULL,
    categoria_flujo VARCHAR(30) NOT NULL DEFAULT 'Efectivo', -- 'Efectivo', 'Transferencia', 'Credito'
    descripcion VARCHAR(100),
    requiere_referencia BOOLEAN NOT NULL DEFAULT FALSE,
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 5. TABLA: CIERRES_CAJA
CREATE TABLE IF NOT EXISTS cierres_caja (
    id SERIAL PRIMARY KEY,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    fecha_apertura TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_cierre TIMESTAMP WITH TIME ZONE,
    monto_inicial NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    total_ventas_efectivo NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    total_ventas_transferencia NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    total_ventas_credito NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    total_pagos_proveedores_efectivo NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    total_esperado NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    total_real_contado NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    diferencia NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    observaciones VARCHAR(500),
    estado VARCHAR(30) NOT NULL DEFAULT 'Cerrada'
);

ALTER TABLE cierres_caja ADD COLUMN IF NOT EXISTS total_pagos_proveedores_efectivo NUMERIC(12,2) NOT NULL DEFAULT 0.00;

CREATE INDEX IF NOT EXISTS idx_cierres_fecha_apertura ON cierres_caja(fecha_apertura);

-- 6. TABLA: PROVEEDORES
CREATE TABLE IF NOT EXISTS proveedores (
    id SERIAL PRIMARY KEY,
    documento_nit VARCHAR(30) NOT NULL UNIQUE,
    razon_social VARCHAR(150) NOT NULL,
    contacto VARCHAR(100),
    telefono VARCHAR(30),
    email VARCHAR(100),
    direccion VARCHAR(200),
    ciudad VARCHAR(60) NOT NULL DEFAULT 'Neiva',
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_registro TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_proveedores_nit ON proveedores(documento_nit);

-- 7. TABLA: COMPRAS_PROVEEDOR
CREATE TABLE IF NOT EXISTS compras_proveedor (
    id SERIAL PRIMARY KEY,
    numero_factura VARCHAR(50) NOT NULL,
    proveedor_id INTEGER NOT NULL REFERENCES proveedores(id) ON DELETE RESTRICT,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    fecha TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    total NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    medio_pago VARCHAR(50) NOT NULL DEFAULT 'Efectivo',
    descontar_caja BOOLEAN NOT NULL DEFAULT TRUE,
    cierre_caja_id INTEGER REFERENCES cierres_caja(id) ON DELETE SET NULL,
    estado VARCHAR(30) NOT NULL DEFAULT 'Registrada',
    observaciones VARCHAR(500)
);

ALTER TABLE compras_proveedor ADD COLUMN IF NOT EXISTS medio_pago VARCHAR(50) NOT NULL DEFAULT 'Efectivo';
ALTER TABLE compras_proveedor ADD COLUMN IF NOT EXISTS descontar_caja BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE compras_proveedor ADD COLUMN IF NOT EXISTS cierre_caja_id INTEGER REFERENCES cierres_caja(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_compras_proveedor_id ON compras_proveedor(proveedor_id);
CREATE INDEX IF NOT EXISTS idx_compras_cierre_caja_id ON compras_proveedor(cierre_caja_id);

-- 8. TABLA: DETALLE_COMPRAS
CREATE TABLE IF NOT EXISTS detalle_compras (
    id SERIAL PRIMARY KEY,
    compra_id INTEGER NOT NULL REFERENCES compras_proveedor(id) ON DELETE CASCADE,
    variante_producto_id INTEGER NOT NULL REFERENCES variante_productos(id) ON DELETE RESTRICT,
    cantidad INTEGER NOT NULL,
    costo_unitario NUMERIC(12,2) NOT NULL,
    subtotal NUMERIC(12,2) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_detalle_compras_compra_id ON detalle_compras(compra_id);

-- 9. TABLA: VENTAS
CREATE TABLE IF NOT EXISTS ventas (
    id SERIAL PRIMARY KEY,
    numero_factura VARCHAR(50) NOT NULL UNIQUE,
    fecha TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    medio_pago_id INTEGER NOT NULL REFERENCES medios_pago(id) ON DELETE RESTRICT,
    referencia_pago VARCHAR(100),
    subtotal NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    descuento NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    iva NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    total NUMERIC(12,2) NOT NULL DEFAULT 0.00,
    estado VARCHAR(30) NOT NULL DEFAULT 'Completada',
    cierre_caja_id INTEGER REFERENCES cierres_caja(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_ventas_numero_factura ON ventas(numero_factura);
CREATE INDEX IF NOT EXISTS idx_ventas_fecha ON ventas(fecha);
CREATE INDEX IF NOT EXISTS idx_ventas_medio_pago ON ventas(medio_pago_id);

-- 10. TABLA: DETALLE_VENTAS
CREATE TABLE IF NOT EXISTS detalle_ventas (
    id SERIAL PRIMARY KEY,
    venta_id INTEGER NOT NULL REFERENCES ventas(id) ON DELETE CASCADE,
    variante_producto_id INTEGER NOT NULL REFERENCES variante_productos(id) ON DELETE RESTRICT,
    cantidad INTEGER NOT NULL,
    precio_unitario NUMERIC(12,2) NOT NULL,
    subtotal NUMERIC(12,2) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_detalle_ventas_venta_id ON detalle_ventas(venta_id);
CREATE INDEX IF NOT EXISTS idx_detalle_ventas_variante_id ON detalle_ventas(variante_producto_id);

-- =============================================================================
-- SEED DATA INICIAL
-- =============================================================================

-- Usuarios (Contraseñas con BCrypt: Admin123* y Cajero123*)
INSERT INTO usuarios (id, nombre_completo, email, password_hash, rol, telefono, activo, fecha_creacion)
VALUES 
(1, 'Thomas Santiago Gonzalez (Gerente)', 'admin@aisaacstr.com', crypt('Admin123*', gen_salt('bf', 11)), 'Administrador', '+57 312 456 7890', true, CURRENT_TIMESTAMP),
(2, 'Sharith Rincon (Cajera Principal)', 'cajero@aisaacstr.com', crypt('Cajero123*', gen_salt('bf', 11)), 'Cajero', '+57 320 987 6543', true, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- Medios de Pago
INSERT INTO medios_pago (id, codigo, nombre, categoria_flujo, descripcion, requiere_referencia, activo)
VALUES
(1, 'EFECTIVO', 'Efectivo', 'Efectivo', 'Pago directo en billetes y monedas', false, true),
(2, 'TRANSFERENCIA', 'Transferencia Bancaria', 'Transferencia', 'Nequi · Daviplata · Bancolombia · PSE', true, true),
(3, 'SISTECREDITO', 'Sistecrédito (Crédito)', 'Credito', 'Financiación de cartera vía Sistecrédito', true, true),
(4, 'ADDI', 'Addi (Crédito)', 'Credito', 'Financiación en cuotas con Addi', true, true),
(5, 'TARJETA', 'Tarjeta Débito / Crédito', 'Transferencia', 'Datáfono Redeban / Credibanco', true, true)
ON CONFLICT (id) DO NOTHING;

-- Proveedores
INSERT INTO proveedores (id, documento_nit, razon_social, contacto, telefono, email, direccion, ciudad, activo)
VALUES
(1, '900.123.456-1', 'Arturo Calle Proveedores S.A.S.', 'Carlos Morales', '3104561234', 'ventas@arturocalle.co', 'Av. Calle 100 # 45-20', 'Bogotá', true),
(2, '891.901.234-5', 'Confecciones del Huila S.A.S.', 'Lucía Perdomo', '3158904321', 'pedidos@confeccioneshuila.com', 'Cra 5 # 12-40 Centro', 'Neiva', true),
(3, '800.567.890-2', 'Calzado Rivera & Moda Ltda.', 'Hernando Silva', '3187654321', 'distribucion@calzadorivera.com', 'Calle 8 # 3-15', 'Rivera', true),
(4, '901.345.678-9', 'Distribuciones Textiles del Sur S.A.S.', 'Mariana Vargas', '3143322110', 'contacto@textilesdelsur.com', 'Calle 20 Sur # 14-80', 'Neiva', true)
ON CONFLICT (id) DO NOTHING;

-- Productos
INSERT INTO productos (id, codigo_base, nombre, descripcion, categoria, activo)
VALUES
(1, 'CAM-OXF', 'Camisa Oxford', 'Camisa clásica 100% algodón corte regular', 'Camisas', true),
(2, 'PAN-CHI', 'Pantalón Chino', 'Pantalón casual gabardina stretch', 'Pantalones', true),
(3, 'ZAP-CUE', 'Zapato Cuero', 'Zapato formal mocasín en puro cuero vacuno', 'Calzado', true),
(4, 'CIN-TEJ', 'Cinturón Tejido', 'Cinturón artesanal en cuero y fibra textil', 'Accesorios', true),
(5, 'CAM-LIN', 'Camisa Lino', 'Camisa de lino manga larga diseño fresco', 'Camisas', true),
(6, 'PAN-DRE', 'Pantalón Dress', 'Pantalón formal corte slim paño fino', 'Pantalones', true),
(7, 'POL-PIM', 'Polo Pima', 'Camiseta cuello polo en algodón Pima peinado', 'Polos', true),
(8, 'BER-CAS', 'Bermuda Casual Drill', 'Bermuda estilo playero con bolsillos laterales', 'Bermudas', true)
ON CONFLICT (id) DO NOTHING;

-- Variante Productos (con alertas de stock mínimo para demostración de reabastecimiento)
INSERT INTO variante_productos (id, producto_id, talla, marca, color, sku, codigo_barras, precio_compra, precio_venta, stock_actual, stock_minimo, stock_maximo, activo)
VALUES
(1, 1, 'M', 'Arturo Calle', 'Blanco', 'CAM-001', '770123400101', 50000.00, 89300.00, 14, 5, 30, true),
(2, 1, 'L', 'Arturo Calle', 'Blanco', 'CAM-002', '770123400102', 50000.00, 89300.00, 8, 4, 25, true),
(3, 1, 'M', 'Arturo Calle', 'Azul', 'CAM-003', '770123400103', 50000.00, 89300.00, 11, 5, 30, true),
(4, 2, '32', 'Arturo Calle', 'Khaki', 'PAN-007', '770123400701', 85000.00, 145300.00, 9, 4, 20, true),
(5, 2, '34', 'Arturo Calle', 'Khaki', 'PAN-008', '770123400702', 85000.00, 145300.00, 7, 4, 20, true),
(6, 2, '32', 'Arturo Calle', 'Negro', 'PAN-009', '770123400703', 85000.00, 145300.00, 6, 4, 20, true),
(7, 3, '42', 'Arturo Calle', 'Negro', 'ZAP-016', '770123401601', 190000.00, 320000.00, 5, 3, 15, true),
(8, 3, '40', 'Calzado Rivera', 'Café', 'ZAP-017', '770123401602', 185000.00, 310000.00, 4, 3, 15, true),
(9, 4, 'Único', 'Arturo Calle', 'Café', 'CIN-004', '770123400401', 30000.00, 55300.00, 22, 6, 40, true),
(10, 4, 'Único', 'Arturo Calle', 'Negro', 'CIN-005', '770123400402', 30000.00, 55300.00, 18, 6, 40, true),
(11, 5, 'L', 'Arturo Calle', 'Azul', 'CAM-012', '770123401201', 58000.00, 95000.00, 7, 4, 20, true),
(12, 5, 'M', 'Confecciones Huila', 'Blanco', 'CAM-013', '770123401202', 58000.00, 95000.00, 8, 4, 20, true),
(13, 6, '34', 'Arturo Calle', 'Negro', 'PAN-015', '770123401501', 98000.00, 165000.00, 6, 3, 18, true),
-- Variantes en alerta para evaluación automática de reabastecimiento:
(14, 7, 'M', 'Confecciones Huila', 'Verde Militar', 'POL-021', '770123402101', 38000.00, 72000.00, 2, 8, 35, true),
(15, 7, 'L', 'Confecciones Huila', 'Blanco', 'POL-022', '770123402102', 38000.00, 72000.00, 3, 8, 35, true),
(16, 8, '32', 'Arturo Calle', 'Beige', 'BER-030', '770123403001', 45000.00, 85000.00, 0, 6, 25, true)
ON CONFLICT (id) DO NOTHING;

-- Actualizar secuencias de IDs en PostgreSQL
SELECT setval('usuarios_id_seq', (SELECT MAX(id) FROM usuarios));
SELECT setval('medios_pago_id_seq', (SELECT MAX(id) FROM medios_pago));
SELECT setval('proveedores_id_seq', (SELECT MAX(id) FROM proveedores));
SELECT setval('productos_id_seq', (SELECT MAX(id) FROM productos));
SELECT setval('variante_productos_id_seq', (SELECT MAX(id) FROM variante_productos));
