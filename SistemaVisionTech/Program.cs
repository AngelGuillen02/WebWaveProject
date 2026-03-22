using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Features.Acceso.Services;
using SistemaVisionTech.Features.Compras.Interfeces;
using SistemaVisionTech.Features.Compras.Services;
using SistemaVisionTech.Features.Inventario.Interfaces;
using SistemaVisionTech.Features.Inventario.Service;
using SistemaVisionTech.Features.Ventas.Interfaces;
using SistemaVisionTech.Features.Ventas.Services;
using SistemaVisionTech.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<WebWaveDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAccesosService, AccesosService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IComprasService, ComprasService>(); 



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
