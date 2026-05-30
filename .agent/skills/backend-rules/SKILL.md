---
name: sistemavisiontech
description: >
  Contexto completo de la API SistemaVisionTech (WebWaveProject), un backend empresarial en .NET 10 / C# 12+
  con EF Core, SQL Server y autenticación JWT. Usa esta skill siempre que el usuario pida crear, modificar
  o extender cualquier parte de la API: endpoints, servicios, DTOs, entidades, migraciones manuales,
  controladores, permisos, ventas, compras, inventario o acceso. También actívala cuando el usuario mencione
  términos como "Result(T)", "BaseApiController", "WebWaveDbContext", "Screaming Architecture" aplicada a
  este proyecto, o cualquier módulo de Acceso / Ventas / Compras / Inventario en el contexto de este sistema.
  No esperes que el usuario diga "usa la skill" — si la pregunta involucra código o decisiones de diseño para
  SistemaVisionTech, carga este contexto de inmediato.
---

# SistemaVisionTech — Skill de Contexto

Esta skill provee el contexto de referencia completo para trabajar con la API SistemaVisionTech.
Lee `references/arquitectura.md` para el detalle exhaustivo de cada módulo, entidad y convención.

---

## Resumen Rápido

| Aspecto | Valor |
|---|---|
| Framework | .NET 10 — ASP.NET Core Web API |
| Lenguaje | C# 12+ |
| ORM | Entity Framework Core 10.x |
| Base de datos | SQL Server (Database-First — **sin migraciones EF**) |
| Auth | JWT (Bearer) + BCrypt.Net-Next |
| Docs | Swagger (Swashbuckle) |
| Extras | AutoMapper, JsonStringEnumConverter |

---

## Arquitectura: Screaming (Feature-based) + Controlador-Servicio

```
SistemaVisionTech/
├── Common/               # Result.cs — Patrón Result<T>
├── Controllers/          # BaseApiController + 4 controladores de módulo
├── Features/
│   ├── Acceso/           # Auth, Usuarios, Roles, Permisos
│   ├── Compras/          # Proveedores, Órdenes de Compra
│   ├── Inventario/       # Stock, Historial/Kardex
│   └── Ventas/           # Clientes, Ventas, Pagos
├── Infrastructure/
│   ├── Entities/         # POCOs mapeados a tablas SQL
│   ├── Maps/             # Fluent API configurations
│   └── WebWaveDbContext.cs
├── Kubernetes/
├── appsettings.json
└── Program.cs
```

Cada módulo en `Features/` tiene siempre tres subcarpetas: **Dtos/** · **Interfaces/** · **Services/**

---

## Reglas Inquebrantables del Proyecto

1. **Sin migraciones EF Core.** Cualquier cambio de esquema se aplica con `CREATE TABLE` / `ALTER TABLE` directamente en SQL Server.
2. **Soft Delete global.** Toda entidad principal tiene campo `Activo (bool)` y un `HasQueryFilter(e => e.Activo)` en `WebWaveDbContext`.
3. **Patrón Result<T>.** Los servicios devuelven `Result.Ok(data)` o `Result.Fail("mensaje")`. Nunca lanzan excepciones para flujo de negocio.
4. **Controladores "tontos".** Cero lógica de negocio en Controllers. Solo llaman al servicio e invocan `HandleResult(resultado)`.
5. **DTOs en la frontera.** Las APIs nunca exponen entidades directamente. Siempre usar clases sufijadas con `Dto`.
6. **LINQ compatible con SQL Server.** Usar `==` para comparar strings, no `StringComparison.OrdinalIgnoreCase`.
7. **DI con interfaces.** Cada servicio se registra como `AddScoped<IXxxService, XxxService>()` en `Program.cs`.

---

## Flujo para Nuevas Funcionalidades

Sigue este orden siempre — no saltar pasos:

```
1. DTOs         →  Features/{Modulo}/Dtos/
2. Interfaz     →  Features/{Modulo}/Interfaces/  (I{Nombre}Service)
3. Servicio     →  Features/{Modulo}/Services/    (retorna Result<T>)
4. Controller   →  Controllers/{Modulo}Controller (HandleResult)
5. BD (si aplica) → Script SQL manual + DbSet en WebWaveDbContext + QueryFilter
```

---

## Módulos y Dominios

### Acceso (Auth + RBAC)
- **Entidades:** `Usuarios`, `Perfiles`, `Permisos`, `UsuariosPerfiles`, `PerfilesPermisos`, `Empresas`, `Sucursales`
- **Servicios clave:** `AuthService` (login + BCrypt verify), `JwtTokenService` (genera token), `UsuariosService`, `PerfilesService`
- **Nota RBAC:** Un usuario tiene Perfiles; un Perfil tiene Permisos. Borrar un Perfil con usuarios vinculados debe fallar con `Result.Fail`.

### Ventas
- **Entidades:** `Ventas` (cabecera), `VentasDetalles` (líneas), `PagosVenta` (abonos), `EstadosVenta`, `Clientes`, `MetodosPago`
- **VentasService:** Gestiona el ciclo completo — creación, actualización de estado, registro de pagos.

### Compras
- **Entidades:** `Compras` (cabecera), `ComprasDetalles`, `PagosCompra`, `EstadosCompra`, `Proveedores`
- **ComprasService:** Similar a Ventas pero orientado a proveedores.

### Inventario
- **Entidades:** `Inventario` (stock actual por producto/sucursal), `HistorialMovimientoInventario` (Kardex)
- **InventarioService:** Registra entradas/salidas/ajustes y actualiza `Inventario`.

---

## Patrones de Código — Plantillas

### Result<T> — Uso en servicios
```csharp
// Éxito
return Result<ProductoDto>.Ok(productoDto);

// Fallo de negocio
return Result<ProductoDto>.Fail("El producto no existe o fue eliminado.");
```

### Controller — Llamada estándar
```csharp
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> ObtenerProducto(int id)
{
    var resultado = await _productosService.ObtenerPorIdAsync(id);
    return HandleResult(resultado);
}
```

### Nuevo endpoint completo — Ejemplo mínimo

**1. DTO**
```csharp
// Features/Inventario/Dtos/AjusteInventarioDto.cs
public class AjusteInventarioRequestDto
{
    public int ProductoId { get; set; }
    public int SucursalId { get; set; }
    public int Cantidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
```

**2. Interfaz**
```csharp
// Features/Inventario/Interfaces/IInventarioService.cs
Task<Result<bool>> AjustarStockAsync(AjusteInventarioRequestDto dto);
```

**3. Servicio**
```csharp
// Features/Inventario/Services/InventarioService.cs
public async Task<Result<bool>> AjustarStockAsync(AjusteInventarioRequestDto dto)
{
    var registro = await _context.Inventario
        .FirstOrDefaultAsync(i => i.ProductoId == dto.ProductoId && i.SucursalId == dto.SucursalId);

    if (registro is null)
        return Result<bool>.Fail("No existe registro de inventario para ese producto/sucursal.");

    registro.Cantidad += dto.Cantidad;

    _context.HistorialMovimientoInventario.Add(new HistorialMovimientoInventario
    {
        ProductoId = dto.ProductoId,
        SucursalId = dto.SucursalId,
        Cantidad = dto.Cantidad,
        Motivo = dto.Motivo,
        Fecha = DateTime.UtcNow,
        Activo = true
    });

    await _context.SaveChangesAsync();
    return Result<bool>.Ok(true);
}
```

**4. Controller**
```csharp
[HttpPost("ajustar")]
[Authorize]
public async Task<IActionResult> AjustarStock([FromBody] AjusteInventarioRequestDto dto)
{
    var resultado = await _inventarioService.AjustarStockAsync(dto);
    return HandleResult(resultado);
}
```

**5. SQL (si hay tabla nueva)**
```sql
-- Ejecutar directamente en SQL Server
CREATE TABLE NuevaTabla (
    Id INT PRIMARY KEY IDENTITY,
    Campo NVARCHAR(100) NOT NULL,
    Activo BIT NOT NULL DEFAULT 1
);
```

---

## Referencia Detallada

Para información exhaustiva (todas las entidades, configuraciones Fluent API, pipeline de Program.cs,
configuración JWT, estructura de appsettings.json), consulta:

📄 `references/arquitectura.md`
