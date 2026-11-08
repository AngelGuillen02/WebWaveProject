using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Caja.Dtos;
using SistemaVisionTech.Features.Caja.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using Entities = SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Caja.Services
{
    public class CajaService : ICajaService
    {
        private readonly WebWaveDbContext _context;

        public CajaService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<CierreCajaResponseDto>> AbrirCajaAsync(AbrirCajaDto dto, int usuarioId)
        {
            bool cajaAbierta = await _context.CierresCaja
                .AnyAsync(c => c.SucursalId == dto.SucursalId && c.Estado == "Abierto");

            if (cajaAbierta)
                return Result<CierreCajaResponseDto>.Fail("Ya existe una caja abierta para esta sucursal.");

            CierresCaja caja = new()
            {
                SucursalId = dto.SucursalId,
                UsuarioId = usuarioId,
                FechaApertura = DateTime.UtcNow,
                MontoApertura = dto.MontoApertura,
                Estado = "Abierto",
                Activo = true
            };

            _context.CierresCaja.Add(caja);
            await _context.SaveChangesAsync();

            return Result<CierreCajaResponseDto>.Ok(MapToDto(caja));
        }

        public async Task<Result<CierreCajaResponseDto>> CerrarCajaAsync(int cajaId, CerrarCajaDto dto)
        {
            CierresCaja? caja = await _context.CierresCaja.FindAsync(cajaId);

            if (caja is null)
                return Result<CierreCajaResponseDto>.Fail("La caja no existe.");

            if (caja.Estado != "Abierto")
                return Result<CierreCajaResponseDto>.Fail("La caja no está abierta.");

            DateTime fechaInicio = caja.FechaApertura.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            List<PagosVenta> ventasDelDia = await _context.PagosVenta
                .AsNoTracking()
                .Where(p => p.Venta.FechaVenta >= fechaInicio
                         && p.Venta.FechaVenta < fechaFin
                         && p.Venta.Activo)
                .Include(p => p.MetodoPago)
                .ToListAsync();

            decimal totalEfectivo = 0;
            decimal totalTarjeta = 0;
            decimal totalOtros = 0;

            foreach (var pago in ventasDelDia)
            {
                if (pago.MetodoPago?.Nombre?.ToLower() == "efectivo")
                    totalEfectivo += pago.Monto;
                else if (pago.MetodoPago?.Nombre?.ToLower() == "tarjeta")
                    totalTarjeta += pago.Monto;
                else
                    totalOtros += pago.Monto;
            }

            decimal diferencia = dto.MontoEfectivoFinal - totalEfectivo;

            caja.MontoEfectivoFinal = dto.MontoEfectivoFinal;
            caja.TotalVentasEfectivo = totalEfectivo;
            caja.TotalVentasTarjeta = totalTarjeta;
            caja.TotalVentasOtros = totalOtros;
            caja.Diferencia = diferencia;
            caja.FechaCierre = DateTime.UtcNow;
            caja.Estado = "Cerrado";

            await _context.SaveChangesAsync();

            return Result<CierreCajaResponseDto>.Ok(MapToDto(caja));
        }

        public async Task<Result<EstadoCajaResponseDto>> ObtenerEstadoActualAsync(int sucursalId)
        {
            CierresCaja? cajaAbierta = await _context.CierresCaja
                .FirstOrDefaultAsync(c => c.SucursalId == sucursalId && c.Estado == "Abierto");

            if (cajaAbierta is null)
            {
                return Result<EstadoCajaResponseDto>.Ok(new EstadoCajaResponseDto
                {
                    Id = 0,
                    Estado = "Cerrado",
                    FechaApertura = DateTime.UtcNow,
                    MontoApertura = 0,
                    TotalVentasEfectivo = 0,
                    TotalVentasTarjeta = 0,
                    TotalVentasOtros = 0
                });
            }

            DateTime hoyInicio = DateTime.UtcNow.Date;
            DateTime hoyFin = hoyInicio.AddDays(1);

            List<PagosVenta> ventasDelDia = await _context.PagosVenta
                .AsNoTracking()
                .Where(p => p.Venta.FechaVenta >= hoyInicio
                         && p.Venta.FechaVenta < hoyFin
                         && p.Venta.Activo)
                .Include(p => p.MetodoPago)
                .ToListAsync();

            decimal totalEfectivo = 0;
            decimal totalTarjeta = 0;
            decimal totalOtros = 0;

            foreach (var pago in ventasDelDia)
            {
                if (pago.MetodoPago?.Nombre?.ToLower() == "efectivo")
                    totalEfectivo += pago.Monto;
                else if (pago.MetodoPago?.Nombre?.ToLower() == "tarjeta")
                    totalTarjeta += pago.Monto;
                else
                    totalOtros += pago.Monto;
            }

            cajaAbierta.TotalVentasEfectivo = totalEfectivo;
            cajaAbierta.TotalVentasTarjeta = totalTarjeta;
            cajaAbierta.TotalVentasOtros = totalOtros;

            return Result<EstadoCajaResponseDto>.Ok(MapToEstadoDto(cajaAbierta));
        }

        public async Task<Result<List<CierreCajaResponseDto>>> ObtenerHistorialAsync(int sucursalId)
        {
            List<CierreCajaResponseDto> cierres = await _context.CierresCaja
                .AsNoTracking()
                .Where(c => c.SucursalId == sucursalId)
                .OrderByDescending(c => c.FechaApertura)
                .Take(100)
                .Select(c => MapToDto(c))
                .ToListAsync();

            return Result<List<CierreCajaResponseDto>>.Ok(cierres);
        }

        private static CierreCajaResponseDto MapToDto(Entities.CierresCaja c) => new()
        {
            Id = c.Id,
            SucursalId = c.SucursalId,
            UsuarioId = c.UsuarioId,
            FechaApertura = c.FechaApertura,
            FechaCierre = c.FechaCierre,
            MontoApertura = c.MontoApertura,
            MontoEfectivoFinal = c.MontoEfectivoFinal,
            TotalVentasEfectivo = c.TotalVentasEfectivo,
            TotalVentasTarjeta = c.TotalVentasTarjeta,
            TotalVentasOtros = c.TotalVentasOtros,
            Diferencia = c.Diferencia,
            Estado = c.Estado
        };

        private static EstadoCajaResponseDto MapToEstadoDto(Entities.CierresCaja c) => new()
        {
            Id = c.Id,
            Estado = c.Estado,
            FechaApertura = c.FechaApertura,
            MontoApertura = c.MontoApertura,
            TotalVentasEfectivo = c.TotalVentasEfectivo,
            TotalVentasTarjeta = c.TotalVentasTarjeta,
            TotalVentasOtros = c.TotalVentasOtros
        };
    }
}
