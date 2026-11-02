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
            List<Proveedores> proveedores = await _context.Proveedores
                .AsNoTracking()
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return Result<List<ProveedorResponseDto>>.Ok(
                proveedores.Select(MapToDto).ToList());
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

        public async Task<Result<ImportacionLoteResultadoDto>> ImportarLoteAsync(List<ProveedorCreacionDto> dtos)
        {
            var resultado = new ImportacionLoteResultadoDto
            {
                TotalRecibidos = dtos.Count
            };

            if (dtos == null || dtos.Count == 0)
                return Result<ImportacionLoteResultadoDto>.Fail("No se proporcionaron proveedores para importar.", isValidation: true);

            var rtnsExistentes = await _context.Proveedores
                .Where(p => !string.IsNullOrEmpty(p.RTN))
                .Select(p => p.RTN!.Trim())
                .ToHashSetAsync();

            var rtnsEnLote = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dto in dtos)
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                {
                    resultado.Omitidos++;
                    resultado.Mensajes.Add("Omitido: Proveedor con nombre vacío.");
                    continue;
                }

                string rtn = dto.RTN?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(rtn))
                {
                    if (rtnsExistentes.Contains(rtn) || rtnsEnLote.Contains(rtn))
                    {
                        resultado.Omitidos++;
                        resultado.Mensajes.Add($"Omitido: RTN '{rtn}' ya registrado.");
                        continue;
                    }
                    rtnsEnLote.Add(rtn);
                }

                var proveedor = new Proveedores
                {
                    Nombre = dto.Nombre.Trim(),
                    Direccion = dto.Direccion?.Trim(),
                    Telefono = dto.Telefono?.Trim(),
                    Email = dto.Email?.Trim(),
                    Contacto = dto.Contacto?.Trim(),
                    RTN = string.IsNullOrWhiteSpace(rtn) ? null : rtn,
                    Activo = true
                };

                _context.Proveedores.Add(proveedor);
                resultado.Insertados++;
            }

            await _context.SaveChangesAsync();
            return Result<ImportacionLoteResultadoDto>.Ok(resultado);
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
