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
            if (string.IsNullOrWhiteSpace(dto.CAI))
                return Result<ConfiguracionSARDto>.Fail("El CAI es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.RTN))
                return Result<ConfiguracionSARDto>.Fail("El RTN es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.RangoDesde) || string.IsNullOrWhiteSpace(dto.RangoHasta))
                return Result<ConfiguracionSARDto>.Fail("El rango de facturación (Desde / Hasta) es obligatorio.", isValidation: true);

            if (dto.FechaLimiteEmision.Date < DateTime.UtcNow.Date)
                return Result<ConfiguracionSARDto>.Fail("La fecha límite de emisión debe ser igual o posterior a la fecha actual.", isValidation: true);

            ConfiguracionSAR? configExistente = await _context.ConfiguracionSAR
                .FirstOrDefaultAsync(c => c.SucursalId == dto.SucursalId && c.Activo);

            if (configExistente != null)
            {
                configExistente.RTN = dto.RTN.Trim();
                configExistente.CAI = dto.CAI.Trim();
                configExistente.RangoDesde = dto.RangoDesde.Trim();
                configExistente.RangoHasta = dto.RangoHasta.Trim();
                configExistente.FechaLimiteEmision = dto.FechaLimiteEmision;

                if (dto.ResetearCorrelativo)
                {
                    configExistente.CorrelativoActual = 0;
                }

                await _context.SaveChangesAsync();
                return Result<ConfiguracionSARDto>.Ok(MapToDto(configExistente));
            }

            ConfiguracionSAR nuevaConfig = new()
            {
                SucursalId = dto.SucursalId,
                RTN = dto.RTN.Trim(),
                CAI = dto.CAI.Trim(),
                RangoDesde = dto.RangoDesde.Trim(),
                RangoHasta = dto.RangoHasta.Trim(),
                FechaLimiteEmision = dto.FechaLimiteEmision,
                CorrelativoActual = 0,
                Activo = true
            };

            _context.ConfiguracionSAR.Add(nuevaConfig);
            await _context.SaveChangesAsync();

            return Result<ConfiguracionSARDto>.Ok(MapToDto(nuevaConfig));
        }

        public async Task<Result<ConfiguracionSARDto>> ObtenerConfiguracionActualAsync(int sucursalId)
        {
            ConfiguracionSAR? config = await _context.ConfiguracionSAR
                .FirstOrDefaultAsync(c => c.SucursalId == sucursalId && c.Activo);

            if (config is null)
            {
                // Si no hay configuración aún, retornamos una plantilla por defecto no guardada para guiar al usuario
                return Result<ConfiguracionSARDto>.Ok(new ConfiguracionSARDto
                {
                    Id = 0,
                    SucursalId = sucursalId,
                    RTN = "08011990123456",
                    CAI = "3B8E12-4C67D8-9A10B2-3C4D5E-6F7A8B-9C",
                    RangoDesde = "000-001-01-00000001",
                    RangoHasta = "000-001-01-00005000",
                    FechaLimiteEmision = DateTime.UtcNow.AddMonths(12).Date,
                    CorrelativoActual = 0,
                    SiguienteFactura = "000-001-01-00000001",
                    FacturasDisponibles = 5000,
                    Vigente = false
                });
            }

            return Result<ConfiguracionSARDto>.Ok(MapToDto(config));
        }

        public async Task<Result<FacturaEmitidaResponseDto>> EmitirFacturaAsync(int ventaId, int sucursalId)
        {
            FacturasEmitidas? facturaExistente = await _context.FacturasEmitidas
                .FirstOrDefaultAsync(f => f.VentaId == ventaId && f.Activo);

            if (facturaExistente != null)
                return Result<FacturaEmitidaResponseDto>.Ok(MapToDto(facturaExistente));

            Venta? venta = await _context.Ventas
                .Include(v => v.Detalles)
                    .ThenInclude(vd => vd.Producto)
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta is null)
                return Result<FacturaEmitidaResponseDto>.Fail("La venta solicitada no existe.");

            if (venta.EstadoVentaId == (int)SistemaVisionTech.Features.Ventas.Enums.EstadoVentaEnum.Anulada)
                return Result<FacturaEmitidaResponseDto>.Fail("El documento se encuentra anulado y no admite emisión fiscal ni asignación de CAI.");

            ConfiguracionSAR? config = await _context.ConfiguracionSAR
                .FirstOrDefaultAsync(c => c.SucursalId == sucursalId && c.Activo);

            if (config is null)
            {
                // Auto-inicializamos una configuración base si no se había configurado aún para que no se bloquee la venta
                config = new ConfiguracionSAR
                {
                    SucursalId = sucursalId,
                    RTN = "08011990123456",
                    CAI = "3B8E12-4C67D8-9A10B2-3C4D5E-6F7A8B-9C",
                    RangoDesde = "000-001-01-00000001",
                    RangoHasta = "000-001-01-00005000",
                    FechaLimiteEmision = DateTime.UtcNow.AddYears(1).Date,
                    CorrelativoActual = 0,
                    Activo = true
                };
                _context.ConfiguracionSAR.Add(config);
                await _context.SaveChangesAsync();
            }

            if (DateTime.UtcNow.Date > config.FechaLimiteEmision.Date)
                return Result<FacturaEmitidaResponseDto>.Fail(
                    $"El CAI ha vencido el {config.FechaLimiteEmision:dd/MM/yyyy}. Debe renovar la configuración del CAI en el módulo SAR.");

            int limiteHasta = ExtraerNumeroCorrelativo(config.RangoHasta);
            int inicioDesde = ExtraerNumeroCorrelativo(config.RangoDesde);
            string prefijo = ExtraerPrefijo(config.RangoDesde);

            int siguienteCorrelativo = config.CorrelativoActual <= 0
                ? (inicioDesde > 0 ? inicioDesde : 1)
                : config.CorrelativoActual + 1;

            if (limiteHasta > 0 && siguienteCorrelativo > limiteHasta)
                return Result<FacturaEmitidaResponseDto>.Fail(
                    $"Se ha alcanzado el límite de correlativos autorizados del SAR ({config.RangoHasta}). Debe solicitar un nuevo rango.");

            decimal exento = 0, gravado15 = 0, gravado18 = 0;

            foreach (var detalle in venta.Detalles)
            {
                string tipo = detalle.Producto?.TipoISV ?? "ISV15";
                switch (tipo)
                {
                    case "Exento":
                        exento += detalle.Total;
                        break;
                    case "ISV18":
                        gravado18 += detalle.Total;
                        break;
                    default:
                        gravado15 += detalle.Total;
                        break;
                }
            }

            decimal isv15 = Math.Round(gravado15 * 0.15m, 2);
            decimal isv18 = Math.Round(gravado18 * 0.18m, 2);
            decimal totalCalculado = exento + gravado15 + gravado18 + isv15 + isv18;

            config.CorrelativoActual = siguienteCorrelativo;
            string numeroFactura = $"{prefijo}{siguienteCorrelativo:D8}";

            FacturasEmitidas factura = new()
            {
                VentaId = ventaId,
                SucursalId = sucursalId,
                NumeroFactura = numeroFactura,
                CAI = config.CAI,
                FechaEmision = DateTime.UtcNow,
                RTNCliente = venta.Cliente?.RTN ?? venta.Rtn,
                NombreCliente = string.IsNullOrWhiteSpace(venta.Cliente?.Nombre) ? "Consumidor Final" : venta.Cliente.Nombre,
                MontoExento = exento,
                MontoGravado15 = gravado15,
                MontoGravado18 = gravado18,
                ISV15 = isv15,
                ISV18 = isv18,
                Total = totalCalculado > 0 ? totalCalculado : venta.Total,
                Activo = true
            };

            _context.FacturasEmitidas.Add(factura);
            await _context.SaveChangesAsync();

            var dto = MapToDto(factura);
            dto.RangoDesde = config.RangoDesde;
            dto.RangoHasta = config.RangoHasta;
            dto.FechaLimiteEmision = config.FechaLimiteEmision;
            return Result<FacturaEmitidaResponseDto>.Ok(dto);
        }

        public async Task<Result<FacturaEmitidaResponseDto>> ObtenerFacturaAsync(int ventaId)
        {
            FacturasEmitidas? factura = await _context.FacturasEmitidas
                .FirstOrDefaultAsync(f => f.VentaId == ventaId && f.Activo);

            if (factura is null)
                return Result<FacturaEmitidaResponseDto>.Fail("No se encontró factura emitida para esta venta.");

            var config = await _context.ConfiguracionSAR
                .FirstOrDefaultAsync(c => c.SucursalId == factura.SucursalId && c.Activo);

            var dto = MapToDto(factura);
            if (config != null)
            {
                dto.RangoDesde = config.RangoDesde;
                dto.RangoHasta = config.RangoHasta;
                dto.FechaLimiteEmision = config.FechaLimiteEmision;
            }

            return Result<FacturaEmitidaResponseDto>.Ok(dto);
        }

        private static int ExtraerNumeroCorrelativo(string rango)
        {
            if (string.IsNullOrWhiteSpace(rango)) return 0;
            string parteFinal = rango.Contains('-') ? rango.Split('-').Last() : rango;
            string soloDigitos = new(parteFinal.Where(char.IsDigit).ToArray());
            int.TryParse(soloDigitos, out int num);
            return num;
        }

        private static string ExtraerPrefijo(string rango)
        {
            if (string.IsNullOrWhiteSpace(rango) || !rango.Contains('-'))
                return "000-001-01-";

            var partes = rango.Split('-');
            if (partes.Length >= 4)
                return $"{partes[0]}-{partes[1]}-{partes[2]}-";

            return "000-001-01-";
        }

        private static ConfiguracionSARDto MapToDto(ConfiguracionSAR c)
        {
            int inicio = ExtraerNumeroCorrelativo(c.RangoDesde);
            int limite = ExtraerNumeroCorrelativo(c.RangoHasta);
            string prefijo = ExtraerPrefijo(c.RangoDesde);
            int siguiente = c.CorrelativoActual <= 0 ? (inicio > 0 ? inicio : 1) : c.CorrelativoActual + 1;
            int disponibles = limite > 0 ? Math.Max(0, limite - c.CorrelativoActual) : 9999;
            bool vigente = DateTime.UtcNow.Date <= c.FechaLimiteEmision.Date && (limite <= 0 || c.CorrelativoActual < limite);

            return new ConfiguracionSARDto
            {
                Id = c.Id,
                SucursalId = c.SucursalId,
                RTN = c.RTN,
                CAI = c.CAI,
                RangoDesde = c.RangoDesde,
                RangoHasta = c.RangoHasta,
                FechaLimiteEmision = c.FechaLimiteEmision,
                CorrelativoActual = c.CorrelativoActual,
                SiguienteFactura = $"{prefijo}{siguiente:D8}",
                FacturasDisponibles = disponibles,
                Vigente = vigente
            };
        }

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
