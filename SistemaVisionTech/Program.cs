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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
var supabaseConn = builder.Configuration.GetConnectionString("SupabaseConnection");
if (!string.IsNullOrWhiteSpace(supabaseConn) && !supabaseConn.Contains("[TU_CONTRASEÑA]") && !supabaseConn.Contains("[TU_REF]"))
{
    connectionString = supabaseConn;
}

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200", "https://localhost:4200" };

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

    // Política específica para login (5 intentos por minuto por IP)
    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon-login",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
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

builder.Services.AddDbContextFactory<WebWaveDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) &&
        (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("User Id=postgres", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase)))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

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

builder.Services.AddScoped<WebWaveDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<WebWaveDbContext>>().CreateDbContext());

var app = builder.Build();

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
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB INIT WARNING]: {ex.Message}");
    }
}

app.Run();
