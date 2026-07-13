using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
using SistemaVisionTech.Features.Productos.Interfaces;
using SistemaVisionTech.Features.Productos.Services;
using SistemaVisionTech.Features.SAR.Interfaces;
using SistemaVisionTech.Features.SAR.Services;
using SistemaVisionTech.Features.Trazabilidad.Interfaces;
using SistemaVisionTech.Features.Trazabilidad.Services;
using SistemaVisionTech.Features.Ventas.Interfaces;
using SistemaVisionTech.Features.Ventas.Services;
using SistemaVisionTech.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

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
builder.Services.AddDbContextFactory<WebWaveDbContext>(options => options.UseSqlServer(connectionString));
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

builder.Services.AddScoped<WebWaveDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<WebWaveDbContext>>().CreateDbContext());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SistemaVisionTech API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();