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
            List<ProductoResponseDto> productos = await _context.Productos
                .Select(p => MapToDto(p))
                .ToListAsync();

            return Result<List<ProductoResponseDto>>.Ok(productos);
        }

        public async Task<Result<ProductoResponseDto>> ObtenerPorIdAsync(int id)
        {
            Producto? producto = await _context.Productos.FindAsync(id);

            if (producto is null)
                return Result<ProductoResponseDto>.Fail("El producto no existe.");

            return Result<ProductoResponseDto>.Ok(MapToDto(producto));
        }

        public async Task<Result<ProductoResponseDto>> CrearAsync(ProductoCreacionDto dto)
        {
            if (!string.IsNullOrEmpty(dto.CodigoBarras))
            {
                bool existeCodigoBarras = await _context.Productos
                    .AnyAsync(p => p.CodigoBarras == dto.CodigoBarras);

                if (existeCodigoBarras)
                    return Result<ProductoResponseDto>.Fail("Ya existe un producto con ese código de barras.");
            }

            Producto producto = new()
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Precio = dto.Precio,
                TieneNumeroSerie = dto.TieneNumeroSerie,
                TieneLote = dto.TieneLote,
                CodigoBarras = dto.CodigoBarras,
                TipoISV = dto.TipoISV,
                Activo = true
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return Result<ProductoResponseDto>.Ok(MapToDto(producto));
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

            return Result<ProductoResponseDto>.Ok(MapToDto(producto));
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
