using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Sucursales;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class SucursalesService : ISucursalesService
    {
        private readonly WebWaveDbContext _context;

        public SucursalesService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<SucursalesDto>>> ObtenerSucursalesAsync()
        {
            var sucursales = await _context.Sucursales
                .AsNoTracking()
                .Include(s => s.Empresa)
                .Select(s => new SucursalesDto
                {
                    SucursalId = s.SucursalId,
                    Nombre = s.Nombre,
                    Direccion = s.Direccion,
                    EmpresaId = s.EmpresaId,
                    Empresa = s.Empresa.Nombre
                })
                .ToListAsync();

            return Result<IEnumerable<SucursalesDto>>.Ok(sucursales);
        }

        public async Task<Result<SucursalesDto>> ObtenerSucursalPorIdAsync(int sucursalId)
        {
            var sucursal = await _context.Sucursales
                .AsNoTracking()
                .Include(s => s.Empresa)
                .FirstOrDefaultAsync(s => s.SucursalId == sucursalId);

            if (sucursal is null)
                return Result<SucursalesDto>.Fail($"Sucursal con Id {sucursalId} no encontrada.");

            return Result<SucursalesDto>.Ok(new SucursalesDto
            {
                SucursalId = sucursal.SucursalId,
                Nombre = sucursal.Nombre,
                Direccion = sucursal.Direccion,
                EmpresaId = sucursal.EmpresaId,
                Empresa = sucursal.Empresa.Nombre
            });
        }

        public async Task<Result<SucursalesDto>> CrearSucursalAsync(SucursalesCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<SucursalesDto>.Fail("El nombre de la sucursal es obligatorio.", isValidation: true);

            var empresaExiste = await _context.Empresas
                .AnyAsync(e => e.EmpresaId == dto.EmpresaId);

            if (!empresaExiste)
                return Result<SucursalesDto>.Fail(
                    $"La empresa con Id {dto.EmpresaId} no existe.");

            var nombreDuplicado = await _context.Sucursales
                .AnyAsync(s => s.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase)
                            && s.EmpresaId == dto.EmpresaId);

            if (nombreDuplicado)
                return Result<SucursalesDto>.Fail(
                    $"Ya existe una sucursal con el nombre '{dto.Nombre}' " +
                    $"en esta empresa.");

            var sucursal = new Sucursales
            {
                Nombre = dto.Nombre.Trim(),
                Direccion = dto.Direccion?.Trim()!,
                EmpresaId = dto.EmpresaId
            };

            _context.Sucursales.Add(sucursal);
            await _context.SaveChangesAsync();

            return await ObtenerSucursalPorIdAsync(sucursal.SucursalId);
        }

        public async Task<Result<SucursalesDto>> ActualizarSucursalAsync(
            int sucursalId, SucursalesActualizacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<SucursalesDto>.Fail(
                    "El nombre de la sucursal es obligatorio.", isValidation: true);

            var sucursal = await _context.Sucursales
                .FirstOrDefaultAsync(s => s.SucursalId == sucursalId);

            if (sucursal is null)
                return Result<SucursalesDto>.Fail(
                    $"La sucursal con Id {sucursalId} no existe.");

            var nombreDuplicado = await _context.Sucursales
                .AnyAsync(s => s.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase)
                            && s.EmpresaId == sucursal.EmpresaId
                            && s.SucursalId != sucursalId);

            if (nombreDuplicado)
                return Result<SucursalesDto>.Fail(
                    $"Ya existe otra sucursal con el nombre '{dto.Nombre}' " +
                    $"en esta empresa.");

            sucursal.Nombre = dto.Nombre.Trim();
            sucursal.Direccion = dto.Direccion?.Trim()!;

            await _context.SaveChangesAsync();

            return await ObtenerSucursalPorIdAsync(sucursalId);
        }

        public async Task<Result> EliminarSucursalAsync(int sucursalId)
        {
            var sucursal = await _context.Sucursales
                .FirstOrDefaultAsync(s => s.SucursalId == sucursalId);

            if (sucursal is null)
                return Result.Fail($"La sucursal con Id {sucursalId} no existe.");

            sucursal.Activo = false;
            _context.Sucursales.Update(sucursal);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }
    }
}
