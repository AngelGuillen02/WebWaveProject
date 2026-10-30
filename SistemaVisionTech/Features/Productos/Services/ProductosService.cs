using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Productos.Dtos;
using SistemaVisionTech.Features.Productos.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Productos.Services
{
    public class ProductosService : IProductosService
    {
        private readonly WebWaveDbContext _context;

        public ProductosService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<ProductoResponseDto>>> ListarAsync()
        {
            var inventarios = await _context.Inventario
                .ToDictionaryAsync(i => i.ProductoId, i => i.Cantidad);

            List<ProductoResponseDto> productos = await _context.Productos
                .Select(p => MapToDto(p))
                .ToListAsync();

            foreach (var p in productos)
            {
                p.Stock = inventarios.TryGetValue(p.ProductoId, out int cant) ? cant : 0;
            }

            return Result<List<ProductoResponseDto>>.Ok(productos);
        }

        public async Task<Result<ProductoResponseDto>> ObtenerPorIdAsync(int id)
        {
            Producto? producto = await _context.Productos.FindAsync(id);

            if (producto is null)
                return Result<ProductoResponseDto>.Fail("El producto no existe.");

            var dto = MapToDto(producto);
            var inv = await _context.Inventario.FirstOrDefaultAsync(i => i.ProductoId == id);
            dto.Stock = inv?.Cantidad ?? 0;

            return Result<ProductoResponseDto>.Ok(dto);
        }

        public async Task<Result<ProductoResponseDto>> CrearAsync(ProductoCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<ProductoResponseDto>.Fail("El nombre del producto es obligatorio.", isValidation: true);

            if (dto.Precio <= 0)
                return Result<ProductoResponseDto>.Fail("El precio de venta debe ser mayor a cero.", isValidation: true);

            if (!string.IsNullOrEmpty(dto.CodigoBarras))
            {
                bool existeCodigoBarras = await _context.Productos
                    .AnyAsync(p => p.CodigoBarras == dto.CodigoBarras);

                if (existeCodigoBarras)
                    return Result<ProductoResponseDto>.Fail("Ya existe un producto con ese código de barras.", isValidation: true);
            }

            Producto producto = new()
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
                Precio = dto.Precio,
                TieneNumeroSerie = dto.TieneNumeroSerie,
                TieneLote = dto.TieneLote,
                CodigoBarras = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras.Trim(),
                TipoISV = string.IsNullOrWhiteSpace(dto.TipoISV) ? "ISV15" : dto.TipoISV,
                Activo = true
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            int stockInicial = dto.StockInicial ?? 0;
            if (stockInicial >= 0)
            {
                Inventarios inventario = new()
                {
                    ProductoId = producto.ProductoId,
                    Cantidad = stockInicial,
                    FechaIngreso = DateTime.UtcNow
                };

                _context.Inventario.Add(inventario);
                await _context.SaveChangesAsync();
            }

            var response = MapToDto(producto);
            response.Stock = stockInicial;
            return Result<ProductoResponseDto>.Ok(response);
        }

        public async Task<Result<ProductoResponseDto>> EditarAsync(int id, ProductoCreacionDto dto)
        {
            Producto? producto = await _context.Productos.FindAsync(id);

            if (producto is null)
                return Result<ProductoResponseDto>.Fail("El producto no existe.");

            if (!string.IsNullOrEmpty(dto.CodigoBarras) && producto.CodigoBarras != dto.CodigoBarras)
            {
                bool existeCodigoBarras = await _context.Productos
                    .AnyAsync(p => p.CodigoBarras == dto.CodigoBarras);

                if (existeCodigoBarras)
                    return Result<ProductoResponseDto>.Fail("Ya existe otro producto con ese código de barras.");
            }

            producto.Nombre = dto.Nombre;
            producto.Descripcion = dto.Descripcion;
            producto.Precio = dto.Precio;
            producto.TieneNumeroSerie = dto.TieneNumeroSerie;
            producto.TieneLote = dto.TieneLote;
            producto.CodigoBarras = dto.CodigoBarras;
            producto.TipoISV = dto.TipoISV;

            await _context.SaveChangesAsync();

            var inv = await _context.Inventario.FirstOrDefaultAsync(i => i.ProductoId == id);
            var response = MapToDto(producto);
            response.Stock = inv?.Cantidad ?? 0;
            return Result<ProductoResponseDto>.Ok(response);
        }

        public async Task<Result<bool>> EliminarAsync(int id)
        {
            Producto? producto = await _context.Productos.FindAsync(id);

            if (producto is null)
                return Result<bool>.Fail("El producto no existe.");

            producto.Activo = false;
            await _context.SaveChangesAsync();

            return Result<bool>.Ok(true);
        }

        private static ProductoResponseDto MapToDto(Producto p) => new()
        {
            ProductoId = p.ProductoId,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            TieneNumeroSerie = p.TieneNumeroSerie,
            TieneLote = p.TieneLote,
            CodigoBarras = p.CodigoBarras,
            TipoISV = p.TipoISV,
            Activo = p.Activo
        };
    }
}
