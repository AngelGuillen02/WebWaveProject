using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Auditoria.Interfaces;
using SistemaVisionTech.Features.Compras.Dtos;
using SistemaVisionTech.Features.Compras.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Compras.Services
{
    public class ProveedoresService : IProveedoresService
    {
        private readonly WebWaveDbContext _context;
        private readonly IAuditoriaService _auditoriaService;

        public ProveedoresService(WebWaveDbContext context, IAuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
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

        public async Task<Result<ProveedorResponseDto>> CrearAsync(ProveedorCreacionDto dto, string? usuario = null)
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

            await _auditoriaService.RegistrarEventoAsync(
                "PROVEEDOR",
                proveedor.ProveedorId,
                proveedor.Nombre,
                usuario,
                "CREACIÓN",
                $"Proveedor registrado: '{proveedor.Nombre}'. Contacto: {proveedor.Contacto ?? "N/A"}, RTN: {proveedor.RTN ?? "N/A"}, Tel: {proveedor.Telefono ?? "N/A"}.",
                "-",
                proveedor.Nombre,
                "Registro de proveedor"
            );

            return Result<ProveedorResponseDto>.Ok(MapToDto(proveedor));
        }

        public async Task<Result<ProveedorResponseDto>> EditarAsync(int id, ProveedorCreacionDto dto, string? usuario = null)
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

            var cambios = new List<string>();
            if (proveedor.Nombre != dto.Nombre)
                cambios.Add($"Nombre: '{proveedor.Nombre}' -> '{dto.Nombre}'");
            if (proveedor.RTN != dto.RTN)
                cambios.Add($"RTN: '{proveedor.RTN}' -> '{dto.RTN}'");
            if (proveedor.Telefono != dto.Telefono)
                cambios.Add($"Tel: '{proveedor.Telefono}' -> '{dto.Telefono}'");
            if (proveedor.Email != dto.Email)
                cambios.Add($"Email: '{proveedor.Email}' -> '{dto.Email}'");
            if (proveedor.Contacto != dto.Contacto)
                cambios.Add($"Contacto: '{proveedor.Contacto}' -> '{dto.Contacto}'");
            if (proveedor.Direccion != dto.Direccion)
                cambios.Add("Dirección modificada");

            string nombreAnterior = proveedor.Nombre;

            proveedor.Nombre = dto.Nombre;
            proveedor.Direccion = dto.Direccion;
            proveedor.Telefono = dto.Telefono;
            proveedor.Email = dto.Email;
            proveedor.Contacto = dto.Contacto;
            proveedor.RTN = dto.RTN;

            await _context.SaveChangesAsync();

            if (cambios.Count > 0)
            {
                await _auditoriaService.RegistrarEventoAsync(
                    "PROVEEDOR",
                    id,
                    dto.Nombre,
                    usuario,
                    "MODIFICACIÓN",
                    string.Join(" | ", cambios),
                    nombreAnterior,
                    dto.Nombre,
                    "Actualización de proveedor"
                );
            }

            return Result<ProveedorResponseDto>.Ok(MapToDto(proveedor));
        }

        public async Task<Result<ImportacionLoteResultadoDto>> ImportarLoteAsync(List<ProveedorCreacionDto> dtos, string? usuario = null)
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

            if (resultado.Insertados > 0)
            {
                await _auditoriaService.RegistrarEventoAsync(
                    "PROVEEDOR",
                    0,
                    $"{resultado.Insertados} proveedores",
                    usuario,
                    "IMPORTACIÓN MASIVA",
                    $"Importación masiva: {resultado.Insertados} proveedores registrados, {resultado.Omitidos} omitidos.",
                    "-",
                    $"{resultado.Insertados} proveedores",
                    "Carga masiva"
                );
            }

            return Result<ImportacionLoteResultadoDto>.Ok(resultado);
        }

        public async Task<Result<bool>> EliminarAsync(int id, string? usuario = null)
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

            await _auditoriaService.RegistrarEventoAsync(
                "PROVEEDOR",
                id,
                proveedor.Nombre,
                usuario,
                "ELIMINACIÓN",
                $"Proveedor '{proveedor.Nombre}' marcado como inactivo (Borrado Lógico).",
                proveedor.Nombre,
                "Inactivo (Eliminado)",
                "Eliminación de proveedor"
            );

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
