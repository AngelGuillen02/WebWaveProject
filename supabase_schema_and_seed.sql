-- ============================================================================
-- SISTEMA VISION TECH — ESQUEMA COMPLETO Y DATOS SEMILLA PARA SUPABASE (POSTGRESQL)
-- ============================================================================

CREATE SCHEMA IF NOT EXISTS dbo;

-- 1. Perfiles
CREATE TABLE IF NOT EXISTS dbo."Perfiles" (
    "PerfilId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 2. Permisos
CREATE TABLE IF NOT EXISTS dbo."Permisos" (
    "PermisoId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 3. PerfilesPermisos
CREATE TABLE IF NOT EXISTS dbo."PerfilesPermisos" (
    "PerfilPermisoId" SERIAL PRIMARY KEY,
    "PerfilId" INT NOT NULL REFERENCES dbo."Perfiles"("PerfilId"),
    "PermisoId" INT NOT NULL REFERENCES dbo."Permisos"("PermisoId"),
    CONSTRAINT "UQ_PerfilesPermisos" UNIQUE ("PerfilId", "PermisoId")
);

-- 4. Empresas
CREATE TABLE IF NOT EXISTS dbo."Empresas" (
    "EmpresaId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Direccion" VARCHAR(255) NULL,
    "Telefono" VARCHAR(50) NULL,
    "Email" VARCHAR(255) NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 5. Sucursales
CREATE TABLE IF NOT EXISTS dbo."Sucursales" (
    "SucursalId" SERIAL PRIMARY KEY,
    "EmpresaId" INT NOT NULL REFERENCES dbo."Empresas"("EmpresaId"),
    "Nombre" VARCHAR(255) NOT NULL,
    "Direccion" VARCHAR(255) NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 6. Usuarios
CREATE TABLE IF NOT EXISTS dbo."Usuarios" (
    "UsuarioId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Email" VARCHAR(255) NOT NULL,
    "Contraseña" VARCHAR(255) NOT NULL,
    "PerfilId" INT NOT NULL REFERENCES dbo."Perfiles"("PerfilId"),
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "UQ_Usuarios_Email" UNIQUE ("Email")
);

-- 7. UsuariosPerfiles
CREATE TABLE IF NOT EXISTS dbo."UsuariosPerfiles" (
    "IdUsuariosPerfiles" SERIAL PRIMARY KEY,
    "UsuarioId" INT NOT NULL REFERENCES dbo."Usuarios"("UsuarioId"),
    "PerfilId" INT NOT NULL REFERENCES dbo."Perfiles"("PerfilId")
);

-- 8. Productos
CREATE TABLE IF NOT EXISTS dbo."Productos" (
    "ProductoId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Descripcion" VARCHAR(500) NULL,
    "Precio" NUMERIC(18,2) NOT NULL,
    "TieneNumeroSerie" BOOLEAN NOT NULL DEFAULT FALSE,
    "TieneLote" BOOLEAN NOT NULL DEFAULT FALSE,
    "CodigoBarras" VARCHAR(100) NULL,
    "TipoISV" VARCHAR(10) NOT NULL DEFAULT 'ISV15',
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 9. Clientes
CREATE TABLE IF NOT EXISTS dbo."Clientes" (
    "ClienteId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Direccion" VARCHAR(255) NULL,
    "Telefono" VARCHAR(50) NULL,
    "Email" VARCHAR(255) NULL,
    "RTN" VARCHAR(50) NULL,
    "TipoCliente" VARCHAR(20) NOT NULL DEFAULT 'Natural',
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 10. Proveedores
CREATE TABLE IF NOT EXISTS dbo."Proveedores" (
    "ProveedorId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Direccion" VARCHAR(255) NULL,
    "Telefono" VARCHAR(50) NULL,
    "Email" VARCHAR(255) NULL,
    "RTN" VARCHAR(50) NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 11. MetodosPago
CREATE TABLE IF NOT EXISTS dbo."MetodosPago" (
    "MetodoPagoId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 12. EstadosVenta
CREATE TABLE IF NOT EXISTS dbo."EstadosVenta" (
    "EstadoId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 13. Ventas
CREATE TABLE IF NOT EXISTS dbo."Ventas" (
    "VentaId" SERIAL PRIMARY KEY,
    "ClienteId" INT NOT NULL REFERENCES dbo."Clientes"("ClienteId"),
    "FechaVenta" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "Total" NUMERIC(18,2) NOT NULL,
    "EstadoVentaId" INT NOT NULL REFERENCES dbo."EstadosVenta"("EstadoId"),
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 14. VentasDetalles
CREATE TABLE IF NOT EXISTS dbo."VentasDetalles" (
    "VentaDetalleId" SERIAL PRIMARY KEY,
    "VentaId" INT NOT NULL REFERENCES dbo."Ventas"("VentaId"),
    "ProductoId" INT NOT NULL REFERENCES dbo."Productos"("ProductoId"),
    "Cantidad" INT NOT NULL,
    "Precio" NUMERIC(18,2) NOT NULL,
    "Total" NUMERIC(18,2) NOT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 15. PagosVenta
CREATE TABLE IF NOT EXISTS dbo."PagosVenta" (
    "PagoVentaId" SERIAL PRIMARY KEY,
    "VentaId" INT NOT NULL REFERENCES dbo."Ventas"("VentaId"),
    "MetodoPagoId" INT NOT NULL REFERENCES dbo."MetodosPago"("MetodoPagoId"),
    "Monto" NUMERIC(18,2) NOT NULL,
    "FechaPago" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 16. EstadosCompra
CREATE TABLE IF NOT EXISTS dbo."EstadosCompra" (
    "EstadoId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 17. Compras
CREATE TABLE IF NOT EXISTS dbo."Compras" (
    "CompraId" SERIAL PRIMARY KEY,
    "ProveedorId" INT NOT NULL REFERENCES dbo."Proveedores"("ProveedorId"),
    "FechaCompra" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "Total" NUMERIC(18,2) NOT NULL,
    "EstadoCompraId" INT NOT NULL REFERENCES dbo."EstadosCompra"("EstadoId"),
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 18. ComprasDetalles
CREATE TABLE IF NOT EXISTS dbo."ComprasDetalles" (
    "CompraDetalleId" SERIAL PRIMARY KEY,
    "CompraId" INT NOT NULL REFERENCES dbo."Compras"("CompraId"),
    "ProductoId" INT NOT NULL REFERENCES dbo."Productos"("ProductoId"),
    "Cantidad" INT NOT NULL,
    "PrecioUnitario" NUMERIC(18,2) NOT NULL,
    "Total" NUMERIC(18,2) NOT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 19. PagosCompra
CREATE TABLE IF NOT EXISTS dbo."PagosCompra" (
    "PagoCompraId" SERIAL PRIMARY KEY,
    "CompraId" INT NOT NULL REFERENCES dbo."Compras"("CompraId"),
    "MetodoPagoId" INT NOT NULL REFERENCES dbo."MetodosPago"("MetodoPagoId"),
    "Monto" NUMERIC(18,2) NOT NULL,
    "FechaPago" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 20. Inventario
CREATE TABLE IF NOT EXISTS dbo."Inventario" (
    "InventarioId" SERIAL PRIMARY KEY,
    "ProductoId" INT NOT NULL REFERENCES dbo."Productos"("ProductoId"),
    "Cantidad" INT NOT NULL DEFAULT 0,
    "FechaIngreso" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 21. HistorialMovimientoInventario
CREATE TABLE IF NOT EXISTS dbo."HistorialMovimientoInventario" (
    "MovimientoId" SERIAL PRIMARY KEY,
    "InventarioId" INT NOT NULL REFERENCES dbo."Inventario"("InventarioId"),
    "Cantidad" INT NOT NULL,
    "TipoMovimiento" VARCHAR(50) NOT NULL,
    "FechaMovimiento" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "OrigenTipo" VARCHAR(20) NULL,
    "OrigenId" INT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 22. SeriesProducto
CREATE TABLE IF NOT EXISTS dbo."SeriesProducto" (
    "Id" SERIAL PRIMARY KEY,
    "ProductoId" INT NOT NULL REFERENCES dbo."Productos"("ProductoId"),
    "NumeroSerie" VARCHAR(100) NOT NULL,
    "CompraDetalleId" INT NULL REFERENCES dbo."ComprasDetalles"("CompraDetalleId"),
    "VentaDetalleId" INT NULL REFERENCES dbo."VentasDetalles"("VentaDetalleId"),
    "Estado" VARCHAR(20) NOT NULL DEFAULT 'Disponible',
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 23. LotesProducto
CREATE TABLE IF NOT EXISTS dbo."LotesProducto" (
    "Id" SERIAL PRIMARY KEY,
    "ProductoId" INT NOT NULL REFERENCES dbo."Productos"("ProductoId"),
    "NumeroLote" VARCHAR(100) NOT NULL,
    "FechaVencimiento" DATE NULL,
    "Cantidad" INT NOT NULL DEFAULT 0,
    "CompraDetalleId" INT NULL REFERENCES dbo."ComprasDetalles"("CompraDetalleId"),
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 24. CierresCaja
CREATE TABLE IF NOT EXISTS dbo."CierresCaja" (
    "Id" SERIAL PRIMARY KEY,
    "SucursalId" INT NOT NULL REFERENCES dbo."Sucursales"("SucursalId"),
    "UsuarioId" INT NOT NULL REFERENCES dbo."Usuarios"("UsuarioId"),
    "FechaApertura" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "FechaCierre" TIMESTAMP WITHOUT TIME ZONE NULL,
    "MontoApertura" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "MontoEfectivoFinal" NUMERIC(18,2) NULL,
    "TotalVentasEfectivo" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "TotalVentasTarjeta" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "TotalVentasOtros" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Diferencia" NUMERIC(18,2) NULL,
    "Estado" VARCHAR(20) NOT NULL DEFAULT 'Abierto',
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 25. ConfiguracionSAR
CREATE TABLE IF NOT EXISTS dbo."ConfiguracionSAR" (
    "Id" SERIAL PRIMARY KEY,
    "SucursalId" INT NOT NULL REFERENCES dbo."Sucursales"("SucursalId"),
    "RTN" VARCHAR(20) NOT NULL,
    "CAI" VARCHAR(50) NOT NULL,
    "RangoDesde" VARCHAR(20) NOT NULL,
    "RangoHasta" VARCHAR(20) NOT NULL,
    "FechaLimiteEmision" DATE NOT NULL,
    "CorrelativoActual" INT NOT NULL DEFAULT 0,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 26. FacturasEmitidas
CREATE TABLE IF NOT EXISTS dbo."FacturasEmitidas" (
    "Id" SERIAL PRIMARY KEY,
    "VentaId" INT NOT NULL REFERENCES dbo."Ventas"("VentaId"),
    "SucursalId" INT NOT NULL REFERENCES dbo."Sucursales"("SucursalId"),
    "NumeroFactura" VARCHAR(30) NOT NULL,
    "CAI" VARCHAR(50) NOT NULL,
    "FechaEmision" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "RTNCliente" VARCHAR(20) NULL,
    "NombreCliente" VARCHAR(200) NOT NULL,
    "MontoExento" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "MontoGravado15" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "MontoGravado18" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "ISV15" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "ISV18" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Total" NUMERIC(18,2) NOT NULL,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- 27. Navegacion
CREATE TABLE IF NOT EXISTS dbo."NavNodos" (
    "NodoId" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(255) NOT NULL,
    "Tipo" VARCHAR(50) NOT NULL DEFAULT 'NODO',
    "PadreId" INT NULL REFERENCES dbo."NavNodos"("NodoId"),
    "Orden" INT NOT NULL DEFAULT 0,
    "Icono" VARCHAR(100) NULL,
    "Controller" VARCHAR(100) NULL,
    "Action" VARCHAR(100) NULL,
    "Visible" BOOLEAN NOT NULL DEFAULT TRUE,
    "Activo" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS dbo."NavRestricciones" (
    "Id" SERIAL PRIMARY KEY,
    "NodoId" INT NOT NULL REFERENCES dbo."NavNodos"("NodoId"),
    "PerfilId" INT NOT NULL REFERENCES dbo."Perfiles"("PerfilId")
);

-- ============================================================================
-- DATOS SEMILLA (SEED DATA)
-- ============================================================================

-- Roles y Permisos (con comodín *.* para bypass de autorización total)
INSERT INTO dbo."Perfiles" ("PerfilId", "Nombre", "Activo")
VALUES (1, 'Administrador', TRUE)
ON CONFLICT ("PerfilId") DO NOTHING;

INSERT INTO dbo."Permisos" ("PermisoId", "Nombre", "Activo")
VALUES (1, '*.*', TRUE)
ON CONFLICT ("PermisoId") DO NOTHING;

INSERT INTO dbo."PerfilesPermisos" ("PerfilPermisoId", "PerfilId", "PermisoId")
VALUES (1, 1, 1)
ON CONFLICT ("PerfilPermisoId") DO NOTHING;

-- Empresa y Sucursal base
INSERT INTO dbo."Empresas" ("EmpresaId", "Nombre", "Direccion", "Telefono", "Email", "Activo")
VALUES (1, 'Sistema Vision Tech S. de R.L.', 'Tegucigalpa, Honduras', '+(504) 2233-4455', 'contacto@visiontech.hn', TRUE)
ON CONFLICT ("EmpresaId") DO NOTHING;

INSERT INTO dbo."Sucursales" ("SucursalId", "EmpresaId", "Nombre", "Direccion", "Activo")
VALUES (1, 1, 'Sucursal Central', 'Boulevard Morazán', TRUE)
ON CONFLICT ("SucursalId") DO NOTHING;

-- Usuario Administrador (Contraseña: Admin123!)
-- Hash BCrypt: $2a$11$EMwNNqvi2Y7VLrQzMz26Pu5cvayNj7VFKQBVpBGzbGe4yjQyBz8W2
INSERT INTO dbo."Usuarios" ("UsuarioId", "Nombre", "Email", "Contraseña", "PerfilId", "Activo")
VALUES (1, 'Angel Guillen', 'admin@visiontech.com', '$2a$11$EMwNNqvi2Y7VLrQzMz26Pu5cvayNj7VFKQBVpBGzbGe4yjQyBz8W2', 1, TRUE)
ON CONFLICT ("UsuarioId") DO NOTHING;

INSERT INTO dbo."UsuariosPerfiles" ("IdUsuariosPerfiles", "UsuarioId", "PerfilId")
VALUES (1, 1, 1)
ON CONFLICT ("IdUsuariosPerfiles") DO NOTHING;

-- Catálogos auxiliares
INSERT INTO dbo."MetodosPago" ("MetodoPagoId", "Nombre", "Activo") VALUES
(1, 'Efectivo', TRUE),
(2, 'Tarjeta de Crédito/Débito', TRUE),
(3, 'Transferencia Bancaria', TRUE)
ON CONFLICT ("MetodoPagoId") DO NOTHING;

INSERT INTO dbo."EstadosVenta" ("EstadoId", "Nombre", "Activo") VALUES
(1, 'Pendiente', TRUE),
(2, 'Pagado', TRUE),
(3, 'Cancelado', TRUE)
ON CONFLICT ("EstadoId") DO NOTHING;

INSERT INTO dbo."EstadosCompra" ("EstadoId", "Nombre", "Activo") VALUES
(1, 'Pendiente', TRUE),
(2, 'Recibida', TRUE),
(3, 'Cancelada', TRUE)
ON CONFLICT ("EstadoId") DO NOTHING;

-- Productos de demostración
INSERT INTO dbo."Productos" ("ProductoId", "Nombre", "Descripcion", "Precio", "TieneNumeroSerie", "TieneLote", "CodigoBarras", "TipoISV", "Activo") VALUES
(1, 'Lente Antirreflejo HD', 'Cristales con protección UV y filtro azul', 1250.00, FALSE, FALSE, 'LNT-001', 'ISV15', TRUE),
(2, 'Montura Titanio Classic', 'Armazón ultraligero unisex', 2400.00, TRUE, FALSE, 'MON-002', 'ISV15', TRUE),
(3, 'Solución Limpiadora 120ml', 'Líquido especial para cristales oftálmicos', 180.00, FALSE, TRUE, 'SOL-003', 'ISV15', TRUE)
ON CONFLICT ("ProductoId") DO NOTHING;

-- Stock en Inventario
INSERT INTO dbo."Inventario" ("InventarioId", "ProductoId", "Cantidad", "FechaIngreso", "Activo") VALUES
(1, 1, 45, NOW(), TRUE),
(2, 2, 18, NOW(), TRUE),
(3, 3, 120, NOW(), TRUE)
ON CONFLICT ("InventarioId") DO NOTHING;

-- Clientes y Proveedores de prueba
INSERT INTO dbo."Clientes" ("ClienteId", "Nombre", "Direccion", "Telefono", "Email", "RTN", "TipoCliente", "Activo") VALUES
(1, 'Distribuidora El Sol', 'San Pedro Sula', '9988-7766', 'contacto@elsol.hn', '08011990123456', 'Jurídico', TRUE),
(2, 'Laura Hernández', 'Tegucigalpa', '3322-1100', 'laura.h@gmail.com', '08011995001122', 'Natural', TRUE)
ON CONFLICT ("ClienteId") DO NOTHING;

INSERT INTO dbo."Proveedores" ("ProveedorId", "Nombre", "Direccion", "Telefono", "Email", "RTN", "Activo") VALUES
(1, 'Óptica Global Supplier', 'Miami, FL / Importadora', '+1 305 555 0199', 'orders@globalsupplier.com', '08019999000111', TRUE)
ON CONFLICT ("ProveedorId") DO NOTHING;

-- Ajustar las secuencias de PostgreSQL para que los siguientes INSERTs no colisionen
SELECT setval('dbo."Perfiles_PerfilId_seq"', COALESCE((SELECT MAX("PerfilId") FROM dbo."Perfiles"), 1));
SELECT setval('dbo."Permisos_PermisoId_seq"', COALESCE((SELECT MAX("PermisoId") FROM dbo."Permisos"), 1));
SELECT setval('dbo."PerfilesPermisos_PerfilPermisoId_seq"', COALESCE((SELECT MAX("PerfilPermisoId") FROM dbo."PerfilesPermisos"), 1));
SELECT setval('dbo."Empresas_EmpresaId_seq"', COALESCE((SELECT MAX("EmpresaId") FROM dbo."Empresas"), 1));
SELECT setval('dbo."Sucursales_SucursalId_seq"', COALESCE((SELECT MAX("SucursalId") FROM dbo."Sucursales"), 1));
SELECT setval('dbo."Usuarios_UsuarioId_seq"', COALESCE((SELECT MAX("UsuarioId") FROM dbo."Usuarios"), 1));
SELECT setval('dbo."UsuariosPerfiles_IdUsuariosPerfiles_seq"', COALESCE((SELECT MAX("IdUsuariosPerfiles") FROM dbo."UsuariosPerfiles"), 1));
SELECT setval('dbo."Productos_ProductoId_seq"', COALESCE((SELECT MAX("ProductoId") FROM dbo."Productos"), 1));
SELECT setval('dbo."Clientes_ClienteId_seq"', COALESCE((SELECT MAX("ClienteId") FROM dbo."Clientes"), 1));
SELECT setval('dbo."Proveedores_ProveedorId_seq"', COALESCE((SELECT MAX("ProveedorId") FROM dbo."Proveedores"), 1));
SELECT setval('dbo."MetodosPago_MetodoPagoId_seq"', COALESCE((SELECT MAX("MetodoPagoId") FROM dbo."MetodosPago"), 1));
SELECT setval('dbo."EstadosVenta_EstadoId_seq"', COALESCE((SELECT MAX("EstadoId") FROM dbo."EstadosVenta"), 1));
SELECT setval('dbo."EstadosCompra_EstadoId_seq"', COALESCE((SELECT MAX("EstadoId") FROM dbo."EstadosCompra"), 1));
SELECT setval('dbo."Inventario_InventarioId_seq"', COALESCE((SELECT MAX("InventarioId") FROM dbo."Inventario"), 1));
