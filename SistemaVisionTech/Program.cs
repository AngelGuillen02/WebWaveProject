using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaVisionTech.Features.Acceso.Authorization;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Features.Acceso.Services;
using SistemaVisionTech.Features.Auditoria.Interfaces;
using SistemaVisionTech.Features.Auditoria.Services;
using SistemaVisionTech.Features.Caja.Interfaces;
using SistemaVisionTech.Features.Caja.Services;
using SistemaVisionTech.Features.Clientes.Interfaces;
using SistemaVisionTech.Features.Clientes.Services;
using SistemaVisionTech.Features.Compras.Interfaces;
using SistemaVisionTech.Features.Compras.Services;
using SistemaVisionTech.Features.Inventario.Interfaces;
using SistemaVisionTech.Features.Inventario.Service;
using SistemaVisionTech.Features.Navegacion.Interfaces;
using SistemaVisionTech.Features.Navegacion.Services;
using SistemaVisionTech.Features.Productos.Interfaces;
using SistemaVisionTech.Features.Productos.Services;
using SistemaVisionTech.Features.SAR.Interfaces;
using SistemaVisionTech.Features.SAR.Services;
using SistemaVisionTech.Features.Trazabilidad.Interfaces;
using SistemaVisionTech.Features.Trazabilidad.Services;
using SistemaVisionTech.Features.Ventas.Interfaces;
using SistemaVisionTech.Features.Ventas.Services;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Middleware;
using System.Text;
using System.Threading.RateLimiting;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Ocultar cabecera Server en Kestrel por seguridad
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

var connectionString = 
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("SupabaseConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? string.Empty;

var supabaseConn = builder.Configuration.GetConnectionString("SupabaseConnection");
if (!string.IsNullOrWhiteSpace(supabaseConn) && !supabaseConn.Contains("[TU_CONTRASEÑA]") && !supabaseConn.Contains("[TU_REF]"))
{
    connectionString = supabaseConn;
}

if (!string.IsNullOrWhiteSpace(connectionString) &&
    (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
     connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var user = Uri.UnescapeDataString(userInfo[0]);
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var db = uri.AbsolutePath.TrimStart('/');
        connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;Pooling=true;";
    }
    catch
    {
        // Mantener cadena original si falla el parseo URI
    }
}

// Normalización ultra-defensiva de cadena de conexión para Render
if (!string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = connectionString.Trim();

    // Corregir si se recortó "aws-1-ca-central-" en el panel de Render
    if (connectionString.Contains("1.pooler.supabase.com") && !connectionString.Contains("aws-1-ca-central-1.pooler.supabase.com"))
    {
        connectionString = connectionString.Replace("1.pooler.supabase.com", "aws-1-ca-central-1.pooler.supabase.com");
    }

    if (connectionString.Contains("postgres.apiikgdlrnwbtpyojig", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = connectionString.Replace("postgres.apiikgdlrnwbtpyojig", "postgres.apiikgdljrnwbtpyojig");
    }

    // Asegurar que comience con Host=
    if (!connectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) &&
        connectionString.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = "Host=" + connectionString;
    }

    // Corregir si quedó cortado como "Trust Server"
    if (connectionString.Contains("Trust Server", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.Contains("Trust Server Certificate", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = connectionString.Replace("Trust Server", "Trust Server Certificate=true;Pooling=true;");
    }

    if (!connectionString.Contains("SSL Mode=", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = connectionString.TrimEnd(';') + ";SSL Mode=Require;";
    }

    if (!connectionString.Contains("Trust Server Certificate=", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = connectionString.TrimEnd(';') + ";Trust Server Certificate=true;Pooling=true;";
    }
}

string? configuredJwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(configuredJwtKey) || configuredJwtKey.StartsWith("[CONFIGURAR"))
{
    configuredJwtKey = null;
}

var jwtKey = 
    Environment.GetEnvironmentVariable("JWT_KEY")
    ?? Environment.GetEnvironmentVariable("Jwt__Key")
    ?? configuredJwtKey
    ?? builder.Configuration["Jwt__Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "DevOnly_SistemaVisionTech_S3cr3tK3y_#2026!_MustBe32Chars+";
    }
    else
    {
        throw new InvalidOperationException("CRÍTICO DE SEGURIDAD: La variable 'JWT_KEY' no está configurada o tiene menos de 32 caracteres. Configure una clave segura en las variables de entorno.");
    }
}
var jwtIssuer = 
    Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? builder.Configuration["Jwt__Issuer"]
    ?? "SistemaVisionTech.API";
var jwtAudience = 
    Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? builder.Configuration["Jwt__Audience"]
    ?? "SistemaVisionTech.Client";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

var allowedOriginsEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
var allowedOrigins = !string.IsNullOrWhiteSpace(allowedOriginsEnv)
    ? allowedOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "https://webwave-sigma.vercel.app", "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configuración de HSTS para entornos productivos
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// Configuración de cookies seguras
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

// Rate Limiting para mitigar fuerza bruta y sobrecarga
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"exito\":false,\"mensaje\":\"Demasiadas peticiones. Por favor, espere un momento antes de reintentar.\"}",
            cancellationToken: token);
    };

    // Política para acciones sensibles (registro de usuarios, pagos)
    options.AddPolicy("SensitiveActionPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon-sensitive",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Política específica para login (5 intentos por minuto por IP)
    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon-login",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Límite global por IP (120 peticiones por minuto)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon-global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SistemaVisionTech API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token JWT. Ejemplo: Bearer eyJhbGci..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPermisosCacheService, PermisosCacheService>();
builder.Services.AddScoped<IAuthorizationHandler, PermisoAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new PermisoRequirement())
        .Build();
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IPerfilesService, PerfilesService>();

Action<DbContextOptionsBuilder> configureDb = options =>
{
    if (!string.IsNullOrEmpty(connectionString) &&
        (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("User Id=postgres", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase)))
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(30);
        });
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
};

builder.Services.AddDbContext<WebWaveDbContext>(configureDb);
builder.Services.AddDbContextFactory<WebWaveDbContext>(configureDb);

builder.Services.AddScoped<IPermisosService, PermisosService>();
builder.Services.AddScoped<IEmpresasService, EmpresasService>();
builder.Services.AddScoped<ISucursalesService, SucursalesService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IComprasService, ComprasService>();
builder.Services.AddScoped<IProveedoresService, ProveedoresService>();
builder.Services.AddScoped<IClientesService, ClientesService>();
builder.Services.AddScoped<IProductosService, ProductosService>();
builder.Services.AddScoped<ISeriesProductoService, SeriesProductoService>();
builder.Services.AddScoped<ILotesProductoService, LotesProductoService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<ISARService, SARService>();
builder.Services.AddScoped<INavegacionService, NavegacionService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();



var app = builder.Build();

// 0. Trazabilidad y correlación de peticiones (Request-ID)
app.UseMiddleware<RequestIdMiddleware>();

// 1. Manejo global de excepciones (evita filtración de stack traces y rutas del servidor)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2. Cabeceras de seguridad (CSP, Clickjacking, no-cache en API, etc.)
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SistemaVisionTech API v1");
        c.RoutePrefix = string.Empty;
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCookiePolicy();
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health checks para monitoreo en Render / balanceadores
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    servicio = "SistemaVisionTech API",
    version = "1.0.4"
})).AllowAnonymous();

app.MapGet("/ready", async (WebWaveDbContext db) =>
{
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();
        return Results.Ok(new { status = "Ready", database = "Connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "Error",
            tipo = ex.GetType().Name,
            mensaje = ex.Message,
            inner = ex.InnerException?.Message
        }, statusCode: 503);
    }
}).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<WebWaveDbContext>();
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE dbo.""Proveedores"" ADD COLUMN IF NOT EXISTS ""Contacto"" VARCHAR(255) NULL;
            ALTER TABLE dbo.""Proveedores"" ADD COLUMN IF NOT EXISTS ""RTN"" VARCHAR(50) NULL;

            CREATE TABLE IF NOT EXISTS dbo.""CierresCaja"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""SucursalId"" INT NOT NULL,
                ""UsuarioId"" INT NOT NULL,
                ""FechaApertura"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
                ""FechaCierre"" TIMESTAMP WITHOUT TIME ZONE NULL,
                ""MontoApertura"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""MontoEfectivoFinal"" NUMERIC(18,2) NULL,
                ""TotalVentasEfectivo"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""TotalVentasTarjeta"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""TotalVentasOtros"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""Diferencia"" NUMERIC(18,2) NULL,
                ""Estado"" VARCHAR(20) NOT NULL DEFAULT 'Abierto',
                ""Activo"" BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE TABLE IF NOT EXISTS dbo.""ConfiguracionSAR"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""SucursalId"" INT NOT NULL,
                ""RTN"" VARCHAR(20) NOT NULL,
                ""CAI"" VARCHAR(50) NOT NULL,
                ""RangoDesde"" VARCHAR(20) NOT NULL,
                ""RangoHasta"" VARCHAR(20) NOT NULL,
                ""FechaLimiteEmision"" DATE NOT NULL,
                ""CorrelativoActual"" INT NOT NULL DEFAULT 0,
                ""Activo"" BOOLEAN NOT NULL DEFAULT TRUE
            );

            INSERT INTO dbo.""ConfiguracionSAR"" (""SucursalId"", ""RTN"", ""CAI"", ""RangoDesde"", ""RangoHasta"", ""FechaLimiteEmision"", ""CorrelativoActual"", ""Activo"")
            SELECT 1, '1804200401929', '434A40-6203AB-EDBCE0-63BE03-09090C-EC', '000-001-01-00000001', '000-001-01-00005000', '2027-12-31'::date, 0, TRUE
            WHERE NOT EXISTS (SELECT 1 FROM dbo.""ConfiguracionSAR"" WHERE ""SucursalId"" = 1);
            CREATE TABLE IF NOT EXISTS dbo.""FacturasEmitidas"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""VentaId"" INT NOT NULL,
                ""SucursalId"" INT NOT NULL,
                ""NumeroFactura"" VARCHAR(30) NOT NULL,
                ""CAI"" VARCHAR(50) NOT NULL,
                ""FechaEmision"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
                ""RTNCliente"" VARCHAR(20) NULL,
                ""NombreCliente"" VARCHAR(200) NOT NULL,
                ""MontoExento"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""MontoGravado15"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""MontoGravado18"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""ISV15"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""ISV18"" NUMERIC(18,2) NOT NULL DEFAULT 0,
                ""Total"" NUMERIC(18,2) NOT NULL,
                ""Activo"" BOOLEAN NOT NULL DEFAULT TRUE
            );

            ALTER TABLE dbo.""Ventas"" ADD COLUMN IF NOT EXISTS ""EsCotizacion"" BOOLEAN NOT NULL DEFAULT FALSE;
            ALTER TABLE dbo.""Ventas"" ADD COLUMN IF NOT EXISTS ""NumeroCotizacion"" VARCHAR(50) NULL;
            ALTER TABLE dbo.""HistorialMovimientoInventario"" ADD COLUMN IF NOT EXISTS ""Usuario"" VARCHAR(100) NULL;

            INSERT INTO dbo.""EstadosVenta"" (""EstadoId"", ""Nombre"", ""Activo"")
            VALUES (4, 'Cotizacion', TRUE)
            ON CONFLICT (""EstadoId"") DO NOTHING;

            CREATE TABLE IF NOT EXISTS dbo.""CotizacionesHistorial"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""VentaId"" INT NOT NULL REFERENCES dbo.""Ventas""(""VentaId"") ON DELETE CASCADE,
                ""NumeroCotizacion"" VARCHAR(50) NULL,
                ""Usuario"" VARCHAR(100) NOT NULL,
                ""FechaModificacion"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
                ""Accion"" VARCHAR(50) NOT NULL,
                ""DetalleCambios"" TEXT NOT NULL,
                ""TotalAnterior"" NUMERIC(18,2) NULL,
                ""TotalNuevo"" NUMERIC(18,2) NOT NULL,
                ""Motivo"" VARCHAR(255) NULL,
                ""Activo"" BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE TABLE IF NOT EXISTS dbo.""ProductosHistorial"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""ProductoId"" INT NOT NULL REFERENCES dbo.""Productos""(""ProductoId"") ON DELETE CASCADE,
                ""Usuario"" VARCHAR(100) NOT NULL,
                ""FechaModificacion"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
                ""Accion"" VARCHAR(50) NOT NULL,
                ""DetalleCambios"" TEXT NOT NULL,
                ""PrecioAnterior"" NUMERIC(18,2) NULL,
                ""PrecioNuevo"" NUMERIC(18,2) NOT NULL,
                ""Activo"" BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE TABLE IF NOT EXISTS dbo.""Auditoria"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Tipo"" VARCHAR(50) NOT NULL,
                ""EntidadId"" INT NOT NULL,
                ""Referencia"" VARCHAR(200) NOT NULL,
                ""Usuario"" VARCHAR(100) NOT NULL,
                ""Fecha"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
                ""Accion"" VARCHAR(50) NOT NULL,
                ""Detalle"" TEXT NOT NULL,
                ""ValorAnterior"" VARCHAR(200) NULL,
                ""ValorNuevo"" VARCHAR(200) NULL,
                ""Motivo"" VARCHAR(255) NULL,
                ""Activo"" BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE INDEX IF NOT EXISTS ""IX_Auditoria_Tipo"" ON dbo.""Auditoria"" (""Tipo"");
            CREATE INDEX IF NOT EXISTS ""IX_Auditoria_Fecha"" ON dbo.""Auditoria"" (""Fecha"");

            CREATE INDEX IF NOT EXISTS ""IX_Usuarios_Email"" ON dbo.""Usuarios"" (""Email"");
            ALTER TABLE dbo.""Usuarios"" DROP COLUMN IF EXISTS ""ClavePlana"";

            -- Índices de rendimiento y unicidad para producción
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_FacturasEmitidas_NumeroFactura"" ON dbo.""FacturasEmitidas"" (""NumeroFactura"");
            CREATE INDEX IF NOT EXISTS ""IX_FacturasEmitidas_VentaId"" ON dbo.""FacturasEmitidas"" (""VentaId"");
            CREATE INDEX IF NOT EXISTS ""IX_Ventas_FechaVenta"" ON dbo.""Ventas"" (""FechaVenta"" DESC);
            CREATE INDEX IF NOT EXISTS ""IX_Ventas_ClienteId"" ON dbo.""Ventas"" (""ClienteId"");
            CREATE INDEX IF NOT EXISTS ""IX_Ventas_EsCotizacion"" ON dbo.""Ventas"" (""EsCotizacion"");
            CREATE INDEX IF NOT EXISTS ""IX_VentasDetalles_VentaId"" ON dbo.""VentasDetalles"" (""VentaId"");
            CREATE INDEX IF NOT EXISTS ""IX_VentasDetalles_ProductoId"" ON dbo.""VentasDetalles"" (""ProductoId"");
            CREATE INDEX IF NOT EXISTS ""IX_Inventarios_ProductoId_SucursalId"" ON dbo.""Inventario"" (""ProductoId"");
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB INIT WARNING]: {ex.Message}");
    }
}

app.Run();
