using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Auditoria.Interfaces;
using SistemaVisionTech.Features.Clientes.Dtos;
using SistemaVisionTech.Features.Clientes.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Clientes.Services
{
    public class ClientesService : IClientesService
    {
        private readonly WebWaveDbContext _context;
        private readonly IAuditoriaService _auditoriaService;

        public ClientesService(WebWaveDbContext context, IAuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
        }

        private static string NormalizarRTN(string? rtn)
        {
            if (string.IsNullOrWhiteSpace(rtn)) return string.Empty;
            return rtn.Replace("-", "").Replace(" ", "").Trim();
        }

        public async Task<Result<List<ClienteResponseDto>>> ListarAsync(int? page = null, int? pageSize = null)
        {
            var query = _context.Clientes
                .AsNoTracking()
                .OrderBy(c => c.Nombre);

            List<Cliente> clientes;
            if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
            {
                clientes = await query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
            }
            else
            {
                clientes = await query.Take(500).ToListAsync();
            }

            return Result<List<ClienteResponseDto>>.Ok(
                clientes.Select(MapToDto).ToList());
        }

        public async Task<Result<ClienteResponseDto>> ObtenerPorIdAsync(int id)
        {
            Cliente? cliente = await _context.Clientes.FindAsync(id);

            if (cliente is null)
                return Result<ClienteResponseDto>.Fail("El cliente no existe.");

            return Result<ClienteResponseDto>.Ok(MapToDto(cliente));
        }

        public async Task<Result<ClienteResponseDto>> CrearAsync(ClienteCreacionDto dto, string? usuario = null)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<ClienteResponseDto>.Fail("El nombre del cliente es obligatorio.", isValidation: true);

            string rtn = dto.RTN?.Trim() ?? string.Empty;
            string rtnLimpio = NormalizarRTN(rtn);

            if (!string.IsNullOrWhiteSpace(rtnLimpio) && rtnLimpio != "00000000000000" && rtnLimpio != "000000000000")
            {
                var clientesConRtn = await _context.Clientes
                    .AsNoTracking()
                    .Where(c => c.Activo && c.RTN != null && c.RTN != "")
                    .Select(c => new { c.ClienteId, c.RTN, c.Nombre })
                    .ToListAsync();

                var duplicado = clientesConRtn.FirstOrDefault(c =>
                    c.RTN!.Trim().Equals(rtn, StringComparison.OrdinalIgnoreCase) ||
                    NormalizarRTN(c.RTN).Equals(rtnLimpio, StringComparison.OrdinalIgnoreCase)
                );

                if (duplicado != null)
                {
                    return Result<ClienteResponseDto>.Fail(
                        $"Ya existe un cliente registrado con el RTN '{rtn}' ({duplicado.Nombre}).", isValidation: true);
                }
            }

            Cliente cliente = new()
            {
                Nombre = dto.Nombre.Trim(),
                Direccion = dto.Direccion?.Trim() ?? string.Empty,
                Telefono = dto.Telefono?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                TipoCliente = string.IsNullOrWhiteSpace(dto.TipoCliente) ? "Natural" : dto.TipoCliente.Trim(),
                RTN = string.IsNullOrWhiteSpace(rtn) ? null : rtn,
                Activo = true
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarEventoAsync(
                "CLIENTE",
                cliente.ClienteId,
                cliente.Nombre,
                usuario,
                "CREACIÓN",
                $"Cliente registrado: '{cliente.Nombre}'. Tipo: {cliente.TipoCliente}, RTN: {cliente.RTN ?? "N/A"}, Tel: {cliente.Telefono ?? "N/A"}.",
                "-",
                cliente.Nombre,
                "Registro de cliente"
            );

            return Result<ClienteResponseDto>.Ok(MapToDto(cliente));
        }

        public async Task<Result<ClienteResponseDto>> EditarAsync(int id, ClienteCreacionDto dto, string? usuario = null)
        {
            Cliente? cliente = await _context.Clientes.FindAsync(id);

            if (cliente is null)
                return Result<ClienteResponseDto>.Fail("El cliente no existe.");

            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<ClienteResponseDto>.Fail("El nombre del cliente es obligatorio.", isValidation: true);

            string rtn = dto.RTN?.Trim() ?? string.Empty;
            string rtnLimpio = NormalizarRTN(rtn);

            if (!string.IsNullOrWhiteSpace(rtnLimpio) && rtnLimpio != "00000000000000" && rtnLimpio != "000000000000")
            {
                var clientesConRtn = await _context.Clientes
                    .AsNoTracking()
                    .Where(c => c.Activo && c.ClienteId != id && c.RTN != null && c.RTN != "")
                    .Select(c => new { c.ClienteId, c.RTN, c.Nombre })
                    .ToListAsync();

                var duplicado = clientesConRtn.FirstOrDefault(c =>
                    c.RTN!.Trim().Equals(rtn, StringComparison.OrdinalIgnoreCase) ||
                    NormalizarRTN(c.RTN).Equals(rtnLimpio, StringComparison.OrdinalIgnoreCase)
                );

                if (duplicado != null)
                {
                    return Result<ClienteResponseDto>.Fail(
                        $"Ya existe otro cliente registrado con el RTN '{rtn}' ({duplicado.Nombre}).", isValidation: true);
                }
            }

            var cambios = new List<string>();
            if (cliente.Nombre != dto.Nombre.Trim())
                cambios.Add($"Nombre: '{cliente.Nombre}' -> '{dto.Nombre.Trim()}'");
            if (cliente.RTN != dto.RTN)
                cambios.Add($"RTN: '{cliente.RTN}' -> '{dto.RTN}'");
            if (cliente.Telefono != dto.Telefono)
                cambios.Add($"Tel: '{cliente.Telefono}' -> '{dto.Telefono}'");
            if (cliente.Email != dto.Email)
                cambios.Add($"Email: '{cliente.Email}' -> '{dto.Email}'");
            if (cliente.Direccion != dto.Direccion)
                cambios.Add("Dirección modificada");
            if (cliente.TipoCliente != dto.TipoCliente)
                cambios.Add($"Tipo: '{cliente.TipoCliente}' -> '{dto.TipoCliente}'");

            string nombreAnterior = cliente.Nombre;

            cliente.Nombre = dto.Nombre.Trim();
            cliente.Direccion = dto.Direccion?.Trim() ?? string.Empty;
            cliente.Telefono = dto.Telefono?.Trim() ?? string.Empty;
            cliente.Email = dto.Email?.Trim() ?? string.Empty;
            cliente.TipoCliente = string.IsNullOrWhiteSpace(dto.TipoCliente) ? "Natural" : dto.TipoCliente.Trim();
            cliente.RTN = string.IsNullOrWhiteSpace(rtn) ? null : rtn;

            await _context.SaveChangesAsync();

            if (cambios.Count > 0)
            {
                await _auditoriaService.RegistrarEventoAsync(
                    "CLIENTE",
                    id,
                    dto.Nombre.Trim(),
                    usuario,
                    "MODIFICACIÓN",
                    string.Join(" | ", cambios),
                    nombreAnterior,
                    dto.Nombre.Trim(),
                    "Actualización de cliente"
                );
            }

            return Result<ClienteResponseDto>.Ok(MapToDto(cliente));
        }

        public async Task<Result<ImportacionLoteResultadoDto>> ImportarLoteAsync(List<ClienteCreacionDto> dtos, string? usuario = null)
        {
            var resultado = new ImportacionLoteResultadoDto
            {
                TotalRecibidos = dtos.Count
            };

            if (dtos == null || dtos.Count == 0)
                return Result<ImportacionLoteResultadoDto>.Fail("No se proporcionaron clientes para importar.", isValidation: true);

            var rtnsExistentes = (await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Activo && c.RTN != null && c.RTN != "")
                .Select(c => c.RTN!)
                .ToListAsync())
                .Select(NormalizarRTN)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var rtnsEnLote = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dto in dtos)
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                {
                    resultado.Omitidos++;
                    resultado.Mensajes.Add("Omitido: Cliente con nombre vacío.");
                    continue;
                }

                string rtn = dto.RTN?.Trim() ?? string.Empty;
                string rtnLimpio = NormalizarRTN(rtn);
                if (!string.IsNullOrEmpty(rtnLimpio) && rtnLimpio != "00000000000000" && rtnLimpio != "000000000000")
                {
                    if (rtnsExistentes.Contains(rtnLimpio) || rtnsEnLote.Contains(rtnLimpio))
                    {
                        resultado.Omitidos++;
                        resultado.Mensajes.Add($"Omitido: RTN '{rtn}' ya registrado.");
                        continue;
                    }
                    rtnsEnLote.Add(rtnLimpio);
                }

                var cliente = new Cliente
                {
                    Nombre = dto.Nombre.Trim(),
                    Direccion = dto.Direccion?.Trim() ?? string.Empty,
                    Telefono = dto.Telefono?.Trim() ?? string.Empty,
                    Email = dto.Email?.Trim() ?? string.Empty,
                    TipoCliente = string.IsNullOrWhiteSpace(dto.TipoCliente) ? "Natural" : dto.TipoCliente.Trim(),
                    RTN = string.IsNullOrWhiteSpace(rtn) ? null : rtn,
                    Activo = true
                };

                _context.Clientes.Add(cliente);
                resultado.Insertados++;
            }

            await _context.SaveChangesAsync();

            if (resultado.Insertados > 0)
            {
                await _auditoriaService.RegistrarEventoAsync(
                    "CLIENTE",
                    0,
                    $"{resultado.Insertados} clientes",
                    usuario,
                    "IMPORTACIÓN MASIVA",
                    $"Importación masiva: {resultado.Insertados} clientes registrados, {resultado.Omitidos} omitidos.",
                    "-",
                    $"{resultado.Insertados} clientes",
                    "Carga masiva"
                );
            }

            return Result<ImportacionLoteResultadoDto>.Ok(resultado);
        }

        public async Task<Result<bool>> EliminarAsync(int id, string? usuario = null)
        {
            Cliente? cliente = await _context.Clientes
                .Include(c => c.Ventas)
                .FirstOrDefaultAsync(c => c.ClienteId == id);

            if (cliente is null)
                return Result<bool>.Fail("El cliente no existe.");

            bool tieneVentasActivas = cliente.Ventas
                .Any(v => v.EstadoVentaId != 3);

            if (tieneVentasActivas)
                return Result<bool>.Fail("No se puede eliminar un cliente que tiene ventas activas.");

            cliente.Activo = false;
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarEventoAsync(
                "CLIENTE",
                id,
                cliente.Nombre,
                usuario,
                "ELIMINACIÓN",
                $"Cliente '{cliente.Nombre}' marcado como inactivo (Borrado Lógico).",
                cliente.Nombre,
                "Inactivo (Eliminado)",
                "Eliminación de cliente"
            );

            return Result<bool>.Ok(true);
        }

        private static ClienteResponseDto MapToDto(Cliente c) => new()
        {
            ClienteId = c.ClienteId,
            Nombre = c.Nombre,
            Direccion = c.Direccion,
            Telefono = c.Telefono,
            Email = c.Email,
            TipoCliente = c.TipoCliente,
            RTN = c.RTN,
            Activo = c.Activo
        };
    }
}
