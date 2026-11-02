using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Clientes.Dtos;
using SistemaVisionTech.Features.Clientes.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Clientes.Services
{
    public class ClientesService : IClientesService
    {
        private readonly WebWaveDbContext _context;

        public ClientesService(WebWaveDbContext context)
        {
            _context = context;
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

        public async Task<Result<ClienteResponseDto>> CrearAsync(ClienteCreacionDto dto)
        {
            Cliente cliente = new()
            {
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Email = dto.Email,
                TipoCliente = dto.TipoCliente,
                RTN = dto.RTN,
                Activo = true
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return Result<ClienteResponseDto>.Ok(MapToDto(cliente));
        }

        public async Task<Result<ClienteResponseDto>> EditarAsync(int id, ClienteCreacionDto dto)
        {
            Cliente? cliente = await _context.Clientes.FindAsync(id);

            if (cliente is null)
                return Result<ClienteResponseDto>.Fail("El cliente no existe.");

            cliente.Nombre = dto.Nombre;
            cliente.Direccion = dto.Direccion;
            cliente.Telefono = dto.Telefono;
            cliente.Email = dto.Email;
            cliente.TipoCliente = dto.TipoCliente;
            cliente.RTN = dto.RTN;

            await _context.SaveChangesAsync();

            return Result<ClienteResponseDto>.Ok(MapToDto(cliente));
        }

        public async Task<Result<ImportacionLoteResultadoDto>> ImportarLoteAsync(List<ClienteCreacionDto> dtos)
        {
            var resultado = new ImportacionLoteResultadoDto
            {
                TotalRecibidos = dtos.Count
            };

            if (dtos == null || dtos.Count == 0)
                return Result<ImportacionLoteResultadoDto>.Fail("No se proporcionaron clientes para importar.", isValidation: true);

            var rtnsExistentes = await _context.Clientes
                .Where(c => !string.IsNullOrEmpty(c.RTN))
                .Select(c => c.RTN!.Trim())
                .ToHashSetAsync();

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
                if (!string.IsNullOrEmpty(rtn) && rtn != "0000-0000-00000")
                {
                    if (rtnsExistentes.Contains(rtn) || rtnsEnLote.Contains(rtn))
                    {
                        resultado.Omitidos++;
                        resultado.Mensajes.Add($"Omitido: RTN '{rtn}' ya registrado.");
                        continue;
                    }
                    rtnsEnLote.Add(rtn);
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
            return Result<ImportacionLoteResultadoDto>.Ok(resultado);
        }

        public async Task<Result<bool>> EliminarAsync(int id)
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
