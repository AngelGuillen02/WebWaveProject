using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Inventario.Dtos;
using SistemaVisionTech.Features.Inventario.Dtos.Inventario;
using SistemaVisionTech.Features.Inventario.Dtos.Movimientos;
using SistemaVisionTech.Features.Inventario.Enums;
using SistemaVisionTech.Features.Inventario.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using Entities = SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Inventario.Service
{
    public class InventarioService : IInventarioService
    {
        private readonly WebWaveDbContext _context;

        public InventarioService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<InventarioDto>>> ObtenerInventarioAsync()
        {
            var inventario = await _context.Inventario
                .AsNoTracking()
                .Include(i => i.Producto)
                .Select(i => new InventarioDto
                {
                    InventarioId = i.InventarioId,
                    ProductoId = i.ProductoId,
                    Producto = i.Producto.Nombre,
                    Precio = i.Producto.Precio,
                    Cantidad = i.Cantidad,
                    FechaIngreso = i.FechaIngreso
                })
                .ToListAsync();

            return Result<IEnumerable<InventarioDto>>.Ok(inventario);
        }

        public async Task<Result<InventarioDto>> ObtenerInventarioPorProductoAsync(
            int productoId)
        {
            var registro = await _context.Inventario
                .AsNoTracking()
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i => i.ProductoId == productoId);

            if (registro is null)
                return Result<InventarioDto>.Fail(
                    $"No hay registro de inventario para el producto con Id {productoId}.");

            return Result<InventarioDto>.Ok(new InventarioDto
            {
                InventarioId = registro.InventarioId,
                ProductoId = registro.ProductoId,
                Producto = registro.Producto.Nombre,
                Precio = registro.Producto.Precio,
                Cantidad = registro.Cantidad,
                FechaIngreso = registro.FechaIngreso
            });
        }


        public async Task<Result<MovimientoDto>> RegistrarMovimientoAsync(
            CrearMovimientoInventarioDto dto)
        {
            if (dto.Cantidad <= 0)
                return Result<MovimientoDto>.Fail(
                    "La cantidad debe ser mayor a cero.", isValidation: true);

            if (!Enum.IsDefined(dto.TipoMovimiento))
                return Result<MovimientoDto>.Fail(
                    "Tipo de movimiento inválido. " +
                    "Valores válidos: ENTRADA, SALIDA, AJUSTE.", isValidation: true);

            var productoExiste = await _context.Productos
                .AnyAsync(p => p.ProductoId == dto.ProductoId);

            if (!productoExiste)
                return Result<MovimientoDto>.Fail(
                    $"El producto con Id {dto.ProductoId} no existe.");

            var inventario = await _context.Inventario
                .FirstOrDefaultAsync(i => i.ProductoId == dto.ProductoId);

            if (inventario is null &&
                dto.TipoMovimiento == TipoMovimientoEnum.ENTRADA)
            {
                inventario = new Entities.Inventario
                {
                    ProductoId = dto.ProductoId,
                    Cantidad = 0,
                    FechaIngreso = DateTime.Now
                };
                _context.Inventario.Add(inventario);
                await _context.SaveChangesAsync();
            }

            if (inventario is null)
                return Result<MovimientoDto>.Fail(
                    "No existe registro de inventario para este producto. " +
                    "Registre una entrada primero.");

            if (dto.TipoMovimiento == TipoMovimientoEnum.SALIDA &&
                inventario.Cantidad < dto.Cantidad)
            {
                return Result<MovimientoDto>.Fail(
                    $"Stock insuficiente. " +
                    $"Disponible: {inventario.Cantidad}, " +
                    $"Solicitado: {dto.Cantidad}.");
            }

            switch (dto.TipoMovimiento)
            {
                case TipoMovimientoEnum.ENTRADA:
                    inventario.Cantidad += dto.Cantidad;
                    break;
                case TipoMovimientoEnum.SALIDA:
                    inventario.Cantidad -= dto.Cantidad;
                    break;
            }
    
            var tipoStr = dto.TipoMovimiento.ToString();

            var movimiento = new HistorialMovimientoInventario
            {
                InventarioId = inventario.InventarioId,
                Cantidad = dto.Cantidad,
                TipoMovimiento = tipoStr,
                FechaMovimiento = DateTime.Now
            };

            _context.HistorialMovimientoInventario.Add(movimiento);
            await _context.SaveChangesAsync();

            var nombreProducto = await _context.Productos
                .Where(p => p.ProductoId == dto.ProductoId)
                .Select(p => p.Nombre)
                .FirstAsync();

            return Result<MovimientoDto>.Ok(new MovimientoDto
            {
                MovimientoId = movimiento.MovimientoId,
                InventarioId = inventario.InventarioId,
                ProductoId = dto.ProductoId,
                Producto = nombreProducto,
                Cantidad = movimiento.Cantidad,
                TipoMovimiento = movimiento.TipoMovimiento,
                FechaMovimiento = movimiento.FechaMovimiento
            });
        }

        public async Task<Result<IEnumerable<MovimientoDto>>> ObtenerMovimientosAsync(int? productoId = null)
        {
            var query = _context.HistorialMovimientoInventario
                .AsNoTracking()
                .Include(m => m.Inventario)
                    .ThenInclude(i => i.Producto)
                .AsQueryable();

            if (productoId.HasValue)
                query = query.Where(
                    m => m.Inventario.ProductoId == productoId.Value);

            var movimientos = await query
                .OrderByDescending(m => m.FechaMovimiento)
                .Select(m => new MovimientoDto
                {
                    MovimientoId = m.MovimientoId,
                    InventarioId = m.InventarioId,
                    ProductoId = m.Inventario.ProductoId,
                    Producto = m.Inventario.Producto.Nombre,
                    Cantidad = m.Cantidad,
                    TipoMovimiento = m.TipoMovimiento,
                    FechaMovimiento = m.FechaMovimiento
                })
                .ToListAsync();

            return Result<IEnumerable<MovimientoDto>>.Ok(movimientos);
        }

        public async Task<Result<InventarioDto>> AjustarInventarioAsync(
            AjusteInventarioDto dto)
        {
            if (dto.CantidadNueva < 0)
                return Result<InventarioDto>.Fail(
                    "La cantidad nueva no puede ser negativa.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Motivo))
                return Result<InventarioDto>.Fail(
                    "El motivo del ajuste es obligatorio.", isValidation: true);

            var productoExiste = await _context.Productos
                .AnyAsync(p => p.ProductoId == dto.ProductoId);

            if (!productoExiste)
                return Result<InventarioDto>.Fail(
                    $"El producto con Id {dto.ProductoId} no existe.");

            var inventario = await _context.Inventario
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i => i.ProductoId == dto.ProductoId);

            if (inventario is null)
            {
                inventario = new Entities.Inventario
                {
                    ProductoId = dto.ProductoId,
                    Cantidad = dto.CantidadNueva,
                    FechaIngreso = DateTime.Now
                };
                _context.Inventario.Add(inventario);
                await _context.SaveChangesAsync();
            }

            var diferencia = dto.CantidadNueva - inventario.Cantidad;

            inventario.Cantidad = dto.CantidadNueva;

            var movimiento = new HistorialMovimientoInventario
            {
                InventarioId = inventario.InventarioId,
                Cantidad = Math.Abs(diferencia),
                TipoMovimiento = TipoMovimientoEnum.AJUSTE.ToString(), 
                FechaMovimiento = DateTime.Now
            };

            _context.HistorialMovimientoInventario.Add(movimiento);
            await _context.SaveChangesAsync();

            // Recargar Producto si no se cargó (caso de inventario nuevo)
            if (inventario.Producto is null)
            {
                await _context.Entry(inventario)
                    .Reference(i => i.Producto)
                    .LoadAsync();
            }

            return Result<InventarioDto>.Ok(new InventarioDto
            {
                InventarioId = inventario.InventarioId,
                ProductoId = inventario.ProductoId,
                Producto = inventario.Producto?.Nombre ?? string.Empty,
                Precio = inventario.Producto?.Precio ?? 0,
                Cantidad = inventario.Cantidad,
                FechaIngreso = inventario.FechaIngreso
            });
        }
    }
}