using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Trazabilidad.Dtos;
using SistemaVisionTech.Features.Trazabilidad.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using Entities = SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Trazabilidad.Services
{
    public class SeriesProductoService : ISeriesProductoService
    {
        private readonly WebWaveDbContext _context;

        public SeriesProductoService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<SerieProductoResponseDto>>> ListarAsync()
        {
            List<SerieProductoResponseDto> series = await _context.SeriesProducto
                .Select(s => MapToDto(s))
                .ToListAsync();

            return Result<List<SerieProductoResponseDto>>.Ok(series);
        }

        public async Task<Result<SerieProductoResponseDto>> ObtenerPorIdAsync(int id)
        {
            SeriesProducto? serie = await _context.SeriesProducto.FindAsync(id);

            if (serie is null)
                return Result<SerieProductoResponseDto>.Fail("La serie del producto no existe.");

            return Result<SerieProductoResponseDto>.Ok(MapToDto(serie));
        }

        public async Task<Result<SerieProductoResponseDto>> CrearAsync(SerieProductoCreacionDto dto)
        {
            bool productoExiste = await _context.Productos.AnyAsync(p => p.ProductoId == dto.ProductoId);
            if (!productoExiste)
                return Result<SerieProductoResponseDto>.Fail("El producto no existe.");

            bool existeSerie = await _context.SeriesProducto
                .AnyAsync(s => s.NumeroSerie == dto.NumeroSerie);

            if (existeSerie)
                return Result<SerieProductoResponseDto>.Fail("Ya existe una serie con ese número.");

            SeriesProducto serie = new()
            {
                ProductoId = dto.ProductoId,
                NumeroSerie = dto.NumeroSerie,
                CompraDetalleId = dto.CompraDetalleId,
                Estado = "Disponible",
                Activo = true
            };

            _context.SeriesProducto.Add(serie);
            await _context.SaveChangesAsync();

            return Result<SerieProductoResponseDto>.Ok(MapToDto(serie));
        }

        public async Task<Result<bool>> EliminarAsync(int id)
        {
            SeriesProducto? serie = await _context.SeriesProducto.FindAsync(id);

            if (serie is null)
                return Result<bool>.Fail("La serie del producto no existe.");

            serie.Activo = false;
            await _context.SaveChangesAsync();

            return Result<bool>.Ok(true);
        }

        private static SerieProductoResponseDto MapToDto(SeriesProducto s) => new()
        {
            Id = s.Id,
            ProductoId = s.ProductoId,
            NumeroSerie = s.NumeroSerie,
            CompraDetalleId = s.CompraDetalleId,
            VentaDetalleId = s.VentaDetalleId,
            Estado = s.Estado,
            Activo = s.Activo
        };
    }

    public class LotesProductoService : ILotesProductoService
    {
        private readonly WebWaveDbContext _context;

        public LotesProductoService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<LoteProductoResponseDto>>> ListarAsync()
        {
            List<LoteProductoResponseDto> lotes = await _context.LotesProducto
                .Select(l => MapToDto(l))
                .ToListAsync();

            return Result<List<LoteProductoResponseDto>>.Ok(lotes);
        }

        public async Task<Result<LoteProductoResponseDto>> ObtenerPorIdAsync(int id)
        {
            LotesProducto? lote = await _context.LotesProducto.FindAsync(id);

            if (lote is null)
                return Result<LoteProductoResponseDto>.Fail("El lote del producto no existe.");

            return Result<LoteProductoResponseDto>.Ok(MapToDto(lote));
        }

        public async Task<Result<LoteProductoResponseDto>> CrearAsync(LoteProductoCreacionDto dto)
        {
            bool productoExiste = await _context.Productos.AnyAsync(p => p.ProductoId == dto.ProductoId);
            if (!productoExiste)
                return Result<LoteProductoResponseDto>.Fail("El producto no existe.");

            LotesProducto lote = new()
            {
                ProductoId = dto.ProductoId,
                NumeroLote = dto.NumeroLote,
                FechaVencimiento = dto.FechaVencimiento,
                Cantidad = dto.Cantidad,
                CompraDetalleId = dto.CompraDetalleId,
                Activo = true
            };

            _context.LotesProducto.Add(lote);
            await _context.SaveChangesAsync();

            return Result<LoteProductoResponseDto>.Ok(MapToDto(lote));
        }

        public async Task<Result<LoteProductoResponseDto>> EditarAsync(int id, LoteProductoCreacionDto dto)
        {
            LotesProducto? lote = await _context.LotesProducto.FindAsync(id);

            if (lote is null)
                return Result<LoteProductoResponseDto>.Fail("El lote del producto no existe.");

            lote.NumeroLote = dto.NumeroLote;
            lote.FechaVencimiento = dto.FechaVencimiento;
            lote.Cantidad = dto.Cantidad;

            await _context.SaveChangesAsync();

            return Result<LoteProductoResponseDto>.Ok(MapToDto(lote));
        }

        public async Task<Result<bool>> EliminarAsync(int id)
        {
            LotesProducto? lote = await _context.LotesProducto.FindAsync(id);

            if (lote is null)
                return Result<bool>.Fail("El lote del producto no existe.");

            lote.Activo = false;
            await _context.SaveChangesAsync();

            return Result<bool>.Ok(true);
        }

        private static LoteProductoResponseDto MapToDto(LotesProducto l) => new()
        {
            Id = l.Id,
            ProductoId = l.ProductoId,
            NumeroLote = l.NumeroLote,
            FechaVencimiento = l.FechaVencimiento,
            Cantidad = l.Cantidad,
            CompraDetalleId = l.CompraDetalleId,
            Activo = l.Activo
        };
    }
}
