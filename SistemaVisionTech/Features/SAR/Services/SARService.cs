using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.SAR.Dtos;
using SistemaVisionTech.Features.SAR.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.SAR.Services
{
    public class SARService : ISARService
    {
        private readonly WebWaveDbContext _context;

        public SARService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<ConfiguracionSARDto>> CrearConfiguracionAsync(CrearConfiguracionSARDto dto)
        {
            bool existeConfig = await _context.ConfiguracionSAR
                .AnyAsync(c => c.SucursalId == dto.SucursalId && c.Activo);

            if (existeConfig)
                return Result<ConfiguracionSARDto>.Fail("Ya existe una configuración SAR activa para esta sucursal.");

            ConfiguracionSAR config = new()
            {
                SucursalId = dto.SucursalId,
                RTN = dto.RTN,
                CAI = dto.CAI,
                RangoDesde = dto.RangoDesde,
                RangoHasta = dto.RangoHasta,
                FechaLimiteEmision = dto.FechaLimiteEmision,
                CorrelativoActual = 0,
                Activo = true
            };

            _context.ConfiguracionSAR.Add(config);
            await _context.SaveChangesAsync();

            return Result<ConfiguracionSARDto>.Ok(MapToDto(config));
        }

        public async Task<Result<ConfiguracionSARDto>> ObtenerConfiguracionActualAsync(int sucursalId)
        {
            ConfiguracionSAR? config = await _context.ConfiguracionSAR
                .FirstOrDefaultAsync(c => c.SucursalId == sucursalId && c.Activo);

            if (config is null)
                return Result<ConfiguracionSARDto>.Fail("No hay configuración SAR activa para esta sucursal.");

            return Result<ConfiguracionSARDto>.Ok(MapToDto(config));
        }

        public async Task<Result<FacturaEmitidaResponseDto>> EmitirFacturaAsync(int ventaId, int sucursalId)
        {
            Venta? venta = await _context.Ventas
                .Include(v => v.Detalles)
                .ThenInclude(vd => vd.Producto)
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.VentaId ==ventaId);

            if (venta is null)
                return Result<FacturaEmitidaResponseDto>.Fail("La venta no existe.");

            ConfiguracionSAR? config = await _context.ConfiguracionSAR
                .FirstOrDefaultAsync(c => c.SucursalId == sucursalId && c.Activo);

            if (config is null)
                return Result<FacturaEmitidaResponseDto>.Fail("No hay configuración SAR activa para esta sucursal.");

            if (DateTime.UtcNow.Date > config.FechaLimiteEmision)
                return Result<FacturaEmitidaResponseDto>.Fail("El CAI ha vencido. Debe renovar la configuración SAR.");

            if (config.CorrelativoActual >= int.Parse(config.RangoHasta))
                return Result<FacturaEmitidaResponseDto>.Fail("Se ha alcanzado el límite de correlativos autorizados.");

            decimal exento = 0, gravado15 = 0, gravado18 = 0;

            foreach (var detalle in venta.Detalles)
            {
                switch (detalle.Producto?.TipoISV)
                {
                    case "Exento":
                        exento += detalle.Total;
                        break;
                    case "ISV15":
                        gravado15 += detalle.Total;
                        break;
                    case "ISV18":
                        gravado18 += detalle.Total;
                        break;
                }
            }

            decimal isv15 = gravado15 * 0.15m;
            decimal isv18 = gravado18 * 0.18m;
            decimal total = exento + gravado15 + gravado18 + isv15 + isv18;

            config.CorrelativoActual++;
            string numeroFactura = FormatearNumeroFactura(config.CorrelativoActual);

            FacturasEmitidas factura = new()
            {
                VentaId = ventaId,
                SucursalId = sucursalId,
                NumeroFactura = numeroFactura,
                CAI = config.CAI,
                FechaEmision = DateTime.UtcNow,
                //RTNCliente = venta.Cliente?.Rtn,
                NombreCliente = venta.Cliente?.Nombre ?? "Consumidor Final",
                MontoExento = exento,
                MontoGravado15 = gravado15,
                MontoGravado18 = gravado18,
                ISV15 = isv15,
                ISV18 = isv18,
                Total = total,
                Activo = true
            };

            _context.FacturasEmitidas.Add(factura);
            await _context.SaveChangesAsync();

            return Result<FacturaEmitidaResponseDto>.Ok(MapToDto(factura));
        }

        public async Task<Result<FacturaEmitidaResponseDto>> ObtenerFacturaAsync(int ventaId)
        {
            FacturasEmitidas? factura = await _context.FacturasEmitidas
                .FirstOrDefaultAsync(f => f.VentaId == ventaId);

            if (factura is null)
                return Result<FacturaEmitidaResponseDto>.Fail("La factura no existe para esta venta.");

            return Result<FacturaEmitidaResponseDto>.Ok(MapToDto(factura));
        }

        private string FormatearNumeroFactura(int correlativo) => $"000-001-01-{correlativo:D8}";

        private static ConfiguracionSARDto MapToDto(ConfiguracionSAR c) => new()
        {
            Id = c.Id,
            SucursalId = c.SucursalId,
            RTN = c.RTN,
            CAI = c.CAI,
            RangoDesde = c.RangoDesde,
            RangoHasta = c.RangoHasta,
            FechaLimiteEmision = c.FechaLimiteEmision,
            CorrelativoActual = c.CorrelativoActual
        };

        private static FacturaEmitidaResponseDto MapToDto(FacturasEmitidas f) => new()
        {
            Id = f.Id,
            VentaId = f.VentaId,
            NumeroFactura = f.NumeroFactura,
            CAI = f.CAI,
            FechaEmision = f.FechaEmision,
            RTNCliente = f.RTNCliente,
            NombreCliente = f.NombreCliente,
            MontoExento = f.MontoExento,
            MontoGravado15 = f.MontoGravado15,
            MontoGravado18 = f.MontoGravado18,
            ISV15 = f.ISV15,
            ISV18 = f.ISV18,
            Total = f.Total
        };
    }
}
