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

        public async Task<Result<List<ClienteResponseDto>>> ListarAsync()
        {
            List<ClienteResponseDto> clientes = await _context.Clientes
                .Select(c => MapToDto(c))
                .ToListAsync();

            return Result<List<ClienteResponseDto>>.Ok(clientes);
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
