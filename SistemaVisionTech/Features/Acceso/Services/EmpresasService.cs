using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Empresas;
using SistemaVisionTech.Features.Acceso.Dtos.Sucursales;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class EmpresasService : IEmpresasService
    {
        private readonly WebWaveDbContext _context;

        public EmpresasService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<EmpresasDto>>> ObtenerEmpresasAsync()
        {
            var empresas = await _context.Empresas
                .AsNoTracking()
                .Include(e => e.Sucursales)
                .Select(e => new EmpresasDto
                {
                    EmpresaId = e.EmpresaId,
                    Nombre = e.Nombre,
                    Direccion = e.Direccion,
                    Telefono = e.Telefono,
                    Email = e.Email,
                    Rtn = e.Rtn,
                    Sucursales = e.Sucursales.Select(s => new SucursalesDto
                    {
                        SucursalId = s.SucursalId,
                        Nombre = s.Nombre,
                        Direccion = s.Direccion,
                        EmpresaId = s.EmpresaId,
                        Empresa = e.Nombre
                    }).ToList()
                })
                .ToListAsync();

            return Result<IEnumerable<EmpresasDto>>.Ok(empresas);
        }

        public async Task<Result<EmpresasDto>> ObtenerEmpresaPorIdAsync(int empresaId)
        {
            var empresa = await _context.Empresas
                .AsNoTracking()
                .Include(e => e.Sucursales)
                .FirstOrDefaultAsync(e => e.EmpresaId == empresaId);

            if (empresa is null)
                return Result<EmpresasDto>.Fail($"Empresa con Id {empresaId} no encontrada.");

            return Result<EmpresasDto>.Ok(new EmpresasDto
            {
                EmpresaId = empresa.EmpresaId,
                Nombre = empresa.Nombre,
                Direccion = empresa.Direccion,
                Telefono = empresa.Telefono,
                Email = empresa.Email,
                Rtn = empresa.Rtn,
                Sucursales = empresa.Sucursales.Select(s => new SucursalesDto
                {
                    SucursalId = s.SucursalId,
                    Nombre = s.Nombre,
                    Direccion = s.Direccion,
                    EmpresaId = s.EmpresaId,
                    Empresa = empresa.Nombre
                }).ToList()
            });
        }

        public async Task<Result<EmpresasDto>> CrearEmpresaAsync(EmpresasCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<EmpresasDto>.Fail("El nombre de la empresa es obligatorio.", isValidation: true);

            var existe = await _context.Empresas
                .AnyAsync(e => e.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase));

            if (existe)
                return Result<EmpresasDto>.Fail($"Ya existe una empresa con el nombre '{dto.Nombre}'.");

            var empresa = new Empresas
            {
                Nombre = dto.Nombre.Trim(),
                Direccion = dto.Direccion?.Trim()!,
                Telefono = dto.Telefono?.Trim()!,
                Email = dto.Email?.Trim().ToLower()!,
                Rtn = dto.Rtn?.Trim()!
            };

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            return await ObtenerEmpresaPorIdAsync(empresa.EmpresaId);
        }

        public async Task<Result<EmpresasDto>> ActualizarEmpresaAsync(int empresaId, EmpresasActualizacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<EmpresasDto>.Fail("El nombre de la empresa es obligatorio.", isValidation: true);

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.EmpresaId == empresaId);

            if (empresa is null)
                return Result<EmpresasDto>.Fail($"La empresa con Id {empresaId} no existe.");

            var nombreDuplicado = await _context.Empresas
                .AnyAsync(e => e.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase) && e.EmpresaId != empresaId);

            if (nombreDuplicado)
                return Result<EmpresasDto>.Fail($"Ya existe otra empresa con el nombre '{dto.Nombre}'.");

            empresa.Nombre = dto.Nombre.Trim();
            empresa.Direccion = dto.Direccion?.Trim()!;
            empresa.Telefono = dto.Telefono?.Trim()!;
            empresa.Email = dto.Email?.Trim().ToLower()!;
            empresa.Rtn = dto.Rtn?.Trim()!;

            await _context.SaveChangesAsync();

            return await ObtenerEmpresaPorIdAsync(empresaId);
        }

        public async Task<Result> EliminarEmpresaAsync(int empresaId)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Sucursales)
                .FirstOrDefaultAsync(e => e.EmpresaId == empresaId);

            if (empresa is null)
                return Result.Fail($"La empresa con Id {empresaId} no existe.");

            if (empresa.Sucursales.Count != 0)
                return Result.Fail(
                    $"No se puede eliminar la empresa '{empresa.Nombre}' " +
                    $"porque tiene {empresa.Sucursales.Count} sucursal(es) asignada(s).");

            empresa.Activo = false;
            _context.Empresas.Update(empresa);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }
    }
}
