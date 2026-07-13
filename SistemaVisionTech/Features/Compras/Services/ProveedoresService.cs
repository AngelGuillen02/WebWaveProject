using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Compras.Dtos;
using SistemaVisionTech.Features.Compras.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Compras.Services
{
    public class ProveedoresService : IProveedoresService
    {
        private readonly WebWaveDbContext _context;

        public ProveedoresService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<ProveedorResponseDto>>> ListarAsync()
        {
            List<ProveedorResponseDto> proveedores = await _context.Proveedores
                .Select(p => MapToDto(p))
                .ToListAsync();

            return Result<List<ProveedorResponseDto>>.Ok(proveedores);
        }

        public async Task<Result<ProveedorResponseDto>> ObtenerPorIdAsync(int id)
        {
            Proveedores? proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor is null)
                return Result<ProveedorResponseDto>.Fail("El proveedor no existe.");

            return Result<ProveedorResponseDto>.Ok(MapToDto(proveedor));
        }

        public async Task<Result<ProveedorResponseDto>> CrearAsync(ProveedorCreacionDto dto)
        {
            if (!string.IsNullOrEmpty(dto.RTN))
            {
                bool existeRtn = await _context.Proveedores.AnyAsync(p => p.RTN == dto.RTN);
                if (existeRtn)
                    return Result<ProveedorResponseDto>.Fail("Ya existe un proveedor con ese RTN.");
            }

            Proveedores proveedor = new()
            {
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Contacto = dto.Contacto,
                RTN = dto.RTN,
                Activo = true
            };

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            return Result<ProveedorResponseDto>.Ok(MapToDto(proveedor));
        }

        public async Task<Result<ProveedorResponseDto>> EditarAsync(int id, ProveedorCreacionDto dto)
        {
            Proveedores? proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor is null)
                return Result<ProveedorResponseDto>.Fail("El proveedor no existe.");

            if (!string.IsNullOrEmpty(dto.RTN) && proveedor.RTN != dto.RTN)
            {
                bool existeRtn = await _context.Proveedores.AnyAsync(p => p.RTN == dto.RTN);
                if (existeRtn)
                    return Result<ProveedorResponseDto>.Fail("Ya existe otro proveedor con ese RTN.");
            }

            proveedor.Nombre = dto.Nombre;
            proveedor.Direccion = dto.Direccion;
            proveedor.Telefono = dto.Telefono;
            proveedor.Email = dto.Email;
            proveedor.Contacto = dto.Contacto;
            proveedor.RTN = dto.RTN;

            await _context.SaveChangesAsync();

            return Result<ProveedorResponseDto>.Ok(MapToDto(proveedor));
        }

        public async Task<Result<bool>> EliminarAsync(int id)
        {
            Proveedores? proveedor = await _context.Proveedores
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(p => p.ProveedorId == id);

            if (proveedor is null)
                return Result<bool>.Fail("El proveedor no existe.");

            if (proveedor.Compras.Count != 0)
                return Result<bool>.Fail("No se puede eliminar un proveedor que tiene compras asociadas.");

            proveedor.Activo = false;
            await _context.SaveChangesAsync();

            return Result<bool>.Ok(true);
        }

        private static ProveedorResponseDto MapToDto(Proveedores p) => new()
        {
            ProveedorId = p.ProveedorId,
            Nombre = p.Nombre,
            Direccion = p.Direccion,
            Telefono = p.Telefono,
            Email = p.Email,
            Contacto = p.Contacto,
            RTN = p.RTN,
            Activo = p.Activo
        };
    }
}
