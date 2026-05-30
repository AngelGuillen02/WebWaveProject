# SistemaVisionTech — Arquitectura Detallada

## Tabla de Contenidos
1. [Stack Tecnológico Completo](#stack)
2. [Pipeline Program.cs](#pipeline)
3. [WebWaveDbContext y Entidades](#dbcontext)
4. [Configuración JWT y appsettings](#jwt)
5. [BaseApiController y Result<T>](#base)
6. [Módulo Acceso — Detalle](#acceso)
7. [Módulo Ventas — Detalle](#ventas)
8. [Módulo Compras — Detalle](#compras)
9. [Módulo Inventario — Detalle](#inventario)
10. [Convenciones y Restricciones Adicionales](#convenciones)

---

## 1. Stack Tecnológico Completo {#stack}

| Paquete / Tecnología | Propósito |
|---|---|
| .NET 10 / ASP.NET Core Web API | Framework base |
| C# 12+ | Lenguaje |
| Entity Framework Core 10.x | ORM — acceso a datos |
| SQL Server | Motor de BD |
| Microsoft.AspNetCore.Authentication.JwtBearer | Middleware JWT |
| BCrypt.Net-Next | Hash de contraseñas |
| Swashbuckle.AspNetCore | Swagger UI + OpenAPI |
| AutoMapper | Mapeo Entidad ↔ DTO (opcional por módulo) |
| Microsoft.Teams.AI.Models.OpenAI | Referenciado (no central) |
| System.Text.Json + JsonStringEnumConverter | Serialización con enums como strings |

---

## 2. Pipeline Program.cs {#pipeline}

Orden de configuración en `Program.cs`:

```csharp
// 1. DbContext
builder.Services.AddDbContext<WebWaveDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// 3. JSON Enum como string
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// 4. Swagger con Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
    c.AddSecurityRequirement(...);
});

// 5. Inyección de Dependencias (AddScoped por servicio)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IPerfilesService, PerfilesService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IComprasService, ComprasService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
// ... registrar nuevos servicios aquí

// 6. Middleware pipeline
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

---

## 3. WebWaveDbContext y Entidades {#dbcontext}

### Global Query Filters (Soft Delete)
```csharp
// En WebWaveDbContext.OnModelCreating():
modelBuilder.Entity<Usuario>().HasQueryFilter(e => e.Activo);
modelBuilder.Entity<Producto>().HasQueryFilter(e => e.Activo);
// ... aplicar a TODAS las entidades principales
```

Para consultas que necesiten incluir registros inactivos (ej: auditoría):
```csharp
_context.Usuarios.IgnoreQueryFilters().Where(...);
```

### Entidades por Dominio

#### Dominio Acceso
```csharp
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Email { get; set; }        // índice único
    public string PasswordHash { get; set; } // BCrypt
    public int EmpresaId { get; set; }
    public int SucursalId { get; set; }
    public bool Activo { get; set; }
    // Navegación
    public ICollection<UsuarioPerfil> UsuariosPerfiles { get; set; }
}

public class Perfil
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public bool Activo { get; set; }
    public ICollection<UsuarioPerfil> UsuariosPerfiles { get; set; }
    public ICollection<PerfilPermiso> PerfilesPermisos { get; set; }
}

public class Permiso
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Clave { get; set; }   // ej: "ventas.crear"
    public bool Activo { get; set; }
}

// Tablas intermedias RBAC
public class UsuarioPerfil { public int UsuarioId; public int PerfilId; }
public class PerfilPermiso  { public int PerfilId;  public int PermisoId; }

public class Empresa  { public int Id; public string Nombre; public bool Activo; }
public class Sucursal { public int Id; public int EmpresaId; public string Nombre; public bool Activo; }
```

#### Dominio Ventas
```csharp
public class Venta
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int EstadoVentaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public bool Activo { get; set; }
    public ICollection<VentaDetalle> VentasDetalles { get; set; }
    public ICollection<PagoVenta> PagosVenta { get; set; }
}

public class VentaDetalle
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public bool Activo { get; set; }
}

public class PagoVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int MetodoPagoId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public bool Activo { get; set; }
}

public class EstadoVenta { public int Id; public string Nombre; public bool Activo; }
public class Cliente      { public int Id; public string Nombre; public string Telefono; public bool Activo; }
public class MetodoPago   { public int Id; public string Nombre; public bool Activo; }
```

#### Dominio Compras
```csharp
public class Compra
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public int UsuarioId { get; set; }
    public int EstadoCompraId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public bool Activo { get; set; }
    public ICollection<CompraDetalle> ComprasDetalles { get; set; }
    public ICollection<PagoCompra> PagosCompra { get; set; }
}

public class CompraDetalle
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public bool Activo { get; set; }
}

public class PagoCompra
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int MetodoPagoId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public bool Activo { get; set; }
}

public class EstadoCompra { public int Id; public string Nombre; public bool Activo; }
public class Proveedor    { public int Id; public string Nombre; public string Contacto; public bool Activo; }
```

#### Dominio Inventario
```csharp
public class Inventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int SucursalId { get; set; }
    public int Cantidad { get; set; }
    public bool Activo { get; set; }
}

public class HistorialMovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int SucursalId { get; set; }
    public int Cantidad { get; set; }        // positivo = entrada, negativo = salida
    public string TipoMovimiento { get; set; } // "Entrada", "Salida", "Ajuste"
    public string Motivo { get; set; }
    public DateTime Fecha { get; set; }
    public bool Activo { get; set; }
}
```

#### Dominio Productos (compartido)
```csharp
public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Codigo { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal CostoCompra { get; set; }
    public bool Activo { get; set; }
}
```

---

## 4. Configuración JWT y appsettings {#jwt}

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=WebWaveDB;..."
  },
  "Jwt": {
    "Key": "clave-secreta-larga-minimo-32-chars",
    "Issuer": "SistemaVisionTech",
    "Audience": "SistemaVisionTechClients",
    "ExpiresInMinutes": 480
  }
}
```

---

## 5. BaseApiController y Result\<T\> {#base}

```csharp
// Common/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }

    public static Result<T> Ok(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Fail(string error) => new() { IsSuccess = false, Error = error };
}

// Controllers/BaseApiController.cs
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return result.Data is null ? NotFound() : Ok(result.Data);

        return BadRequest(result.Error);
    }
}
```

**Comportamiento de HandleResult:**
- `Result.Ok(data)` con data → `200 OK`
- `Result.Ok(null)` → `404 Not Found`
- `Result.Fail(msg)` → `400 Bad Request` con el mensaje de error

---

## 6. Módulo Acceso — Detalle {#acceso}

### AuthService
```csharp
public async Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto dto)
{
    var usuario = await _context.Usuarios
        .Include(u => u.UsuariosPerfiles)
            .ThenInclude(up => up.Perfil)
                .ThenInclude(p => p.PerfilesPermisos)
                    .ThenInclude(pp => pp.Permiso)
        .FirstOrDefaultAsync(u => u.Email == dto.Email);

    if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        return Result<TokenResponseDto>.Fail("Credenciales inválidas.");

    var token = _jwtService.GenerarToken(usuario);
    return Result<TokenResponseDto>.Ok(new TokenResponseDto { Token = token });
}
```

### JwtTokenService
```csharp
public string GenerarToken(Usuario usuario)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Email, usuario.Email),
        new("empresa", usuario.EmpresaId.ToString()),
        new("sucursal", usuario.SucursalId.ToString())
        // Añadir permisos como claims si se requiere autorización granular
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _config["Jwt:Issuer"],
        audience: _config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiresInMinutes"]!)),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Reglas de negocio RBAC
- No se puede eliminar un `Perfil` que tenga `UsuariosPerfiles` activos.
- No se puede asignar un `Permiso` que no exista en la BD.
- El borrado lógico (`Activo = false`) nunca elimina registros físicamente.

---

## 7. Módulo Ventas — Detalle {#ventas}

### Flujo típico de una Venta
1. `CrearVentaAsync(dto)` — Crea cabecera + detalles, calcula `Total`, asigna `EstadoVentaId` inicial.
2. `RegistrarPagoAsync(dto)` — Añade `PagoVenta`, verifica si el total pagado cubre el `Total` para cambiar estado.
3. `CancelarVentaAsync(id)` — Soft delete de la venta + detalles, revierte inventario si aplica.

### Validaciones clave
- Verificar existencia de `Cliente`, `Producto`, `MetodoPago` antes de insertar.
- El `Total` de la cabecera debe coincidir con la suma de `Subtotal` de los detalles.
- No vender si el stock en `Inventario` es insuficiente (coordinar con InventarioService).

---

## 8. Módulo Compras — Detalle {#compras}

### Flujo típico de una Compra
1. `CrearCompraAsync(dto)` — Crea cabecera + detalles con `CostoUnitario`.
2. `RegistrarPagoCompraAsync(dto)` — Añade `PagoCompra`.
3. `RecibirMercaderiaAsync(id)` — Actualiza `Inventario` con las cantidades recibidas + registra `HistorialMovimientoInventario`.

---

## 9. Módulo Inventario — Detalle {#inventario}

### Tipos de movimiento
| TipoMovimiento | Cantidad | Origen |
|---|---|---|
| Entrada | positivo | Recepción de compra |
| Salida | negativo | Venta confirmada |
| Ajuste | positivo o negativo | Corrección manual |

### Consulta de stock
```csharp
var stock = await _context.Inventario
    .Where(i => i.ProductoId == productoId && i.SucursalId == sucursalId)
    .Select(i => i.Cantidad)
    .FirstOrDefaultAsync();
```

---

## 10. Convenciones y Restricciones Adicionales {#convenciones}

### Nomenclatura
- **DTOs:** `{Entidad}CreacionDto`, `{Entidad}ActualizacionDto`, `{Entidad}ResponseDto`
- **Interfaces:** `I{Nombre}Service`
- **Servicios:** `{Nombre}Service`
- **Controllers:** `{Modulo}Controller`

### Restricciones LINQ → SQL Server
```csharp
// ✅ Correcto — compatible con traducción a SQL
.Where(u => u.Email == dto.Email)

// ❌ Incorrecto — StringComparison no se traduce a SQL
.Where(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
```

### Inyección de WebWaveDbContext en servicios
```csharp
public class VentasService : IVentasService
{
    private readonly WebWaveDbContext _context;

    public VentasService(WebWaveDbContext context)
    {
        _context = context;
    }
}
```

### Agregar nueva entidad — Checklist
- [ ] Crear clase POCO en `Infrastructure/Entities/`
- [ ] Crear configuración Fluent API en `Infrastructure/Maps/`
- [ ] Añadir `DbSet<NuevaEntidad>` en `WebWaveDbContext`
- [ ] Añadir `HasQueryFilter(e => e.Activo)` en `OnModelCreating`
- [ ] Ejecutar `CREATE TABLE` manualmente en SQL Server
- [ ] Registrar servicio en `Program.cs` si aplica
