using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Acceso.Dtos.Empresas;
using SistemaVisionTech.Features.Acceso.Dtos.Perfiles;
using SistemaVisionTech.Features.Acceso.Dtos.Permisos;
using SistemaVisionTech.Features.Acceso.Dtos.Sucursales;
using SistemaVisionTech.Features.Acceso.Dtos.Usuarios;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SistemaVisionTech.Features.Acceso.Services
{
    public class AccesosService : IAccesosService
    {
        private readonly WebWaveDbContext _context;

        public AccesosService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<UsuariosDto>>> ObtenerUsuariosAsync()
        {
            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Perfil)
                .Select(u => new UsuariosDto
                {
                    UsuarioId = u.UsuarioId,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    PerfilId = u.PerfilId,
                    Perfil = u.Perfil.Nombre
                })
                .ToListAsync();

            return Result<IEnumerable<UsuariosDto>>.Ok(usuarios);
        }

        public async Task<Result<UsuariosDto>> ObtenerUsuarioPorIdAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario is null)
                return Result<UsuariosDto>.Fail(
                    $"Usuario con Id {usuarioId} no encontrado.");

            return Result<UsuariosDto>.Ok(new UsuariosDto
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                PerfilId = usuario.PerfilId,
                Perfil = usuario.Perfil.Nombre
            });
        }

        public async Task<Result<UsuariosDto>> CrearUsuarioAsync(UsuariosCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<UsuariosDto>.Fail("El nombre del usuario es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<UsuariosDto>.Fail("El email es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Contraseña))
                return Result<UsuariosDto>.Fail("La contraseña es obligatoria.", isValidation: true);

            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email.Equals(dto.Email, StringComparison.CurrentCultureIgnoreCase));

            if (emailExiste)
                return Result<UsuariosDto>.Fail($"Ya existe un usuario con el email '{dto.Email}'.");

            var perfilExiste = await _context.Perfiles
                .AnyAsync(p => p.PerfilId == dto.PerfilId);

            if (!perfilExiste)
                return Result<UsuariosDto>.Fail($"El perfil con Id {dto.PerfilId} no existe.");

            var usuario = new Usuarios
            {
                Nombre = dto.Nombre.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Contraseña = BCryptNet.HashPassword(dto.Contraseña),
                PerfilId = dto.PerfilId
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var resultado = await ObtenerUsuarioPorIdAsync(usuario.UsuarioId);
            return resultado;
        }

        public async Task<Result<UsuariosDto>> ActualizarUsuarioAsync(int usuarioId, UsuariosActualizacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<UsuariosDto>.Fail("El nombre es obligatorio.", isValidation: true);

            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<UsuariosDto>.Fail("El email es obligatorio.", isValidation: true);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario is null)
                return Result<UsuariosDto>.Fail(
                    $"El usuario con Id {usuarioId} no existe.");

            var emailDuplicado = await _context.Usuarios
                .AnyAsync(u => u.Email.Equals(dto.Email, StringComparison.CurrentCultureIgnoreCase)
                            && u.UsuarioId != usuarioId);

            if (emailDuplicado)
                return Result<UsuariosDto>.Fail(
                    $"Ya existe otro usuario con el email '{dto.Email}'.");

            var perfilExiste = await _context.Perfiles
                .AnyAsync(p => p.PerfilId == dto.PerfilId);

            if (!perfilExiste)
                return Result<UsuariosDto>.Fail($"El perfil con Id {dto.PerfilId} no existe.");

            usuario.Nombre = dto.Nombre.Trim();
            usuario.Email = dto.Email.Trim().ToLower();
            usuario.PerfilId = dto.PerfilId;

            await _context.SaveChangesAsync();

            return await ObtenerUsuarioPorIdAsync(usuarioId);
        }

        public async Task<Result> EliminarUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

            if (usuario is null)
                return Result.Fail($"El usuario con Id {usuarioId} no existe.");

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }

        public async Task<Result<IEnumerable<PerfilesDto>>> ObtenerPerfilesAsync()
        {
            var perfiles = await _context.Perfiles
                .AsNoTracking()
                .Include(p => p.Permisos)
                    .ThenInclude(pp => pp.Permiso)
                .ToListAsync();

            var resultado = perfiles.Select(p => new PerfilesDto
            {
                PerfilId = p.PerfilId,
                Nombre = p.Nombre,
                Permisos = p.Permisos
                    .Select(pp => pp.Permiso.Nombre)
                    .ToList()
            });

            return Result<IEnumerable<PerfilesDto>>.Ok(resultado);
        }

        public async Task<Result<PerfilesDto>> ObtenerPerfilPorIdAsync(int perfilId)
        {
            var perfil = await _context.Perfiles
                .AsNoTracking()
                .Include(p => p.Permisos)
                    .ThenInclude(pp => pp.Permiso)
                .FirstOrDefaultAsync(p => p.PerfilId == perfilId);

            if (perfil is null)
                return Result<PerfilesDto>.Fail($"Perfil con Id {perfilId} no encontrado.");

            return Result<PerfilesDto>.Ok(new PerfilesDto
            {
                PerfilId = perfil.PerfilId,
                Nombre = perfil.Nombre,
                Permisos = perfil.Permisos
                    .Select(pp => pp.Permiso.Nombre)
                    .ToList()
            });
        }

        public async Task<Result<PerfilesDto>> CrearPerfilAsync(PerfilesCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<PerfilesDto>.Fail("El nombre del perfil es obligatorio.", isValidation: true);

            var existe = await _context.Perfiles
                .AnyAsync(p => p.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase));

            if (existe)
                return Result<PerfilesDto>.Fail(
                    $"Ya existe un perfil con el nombre '{dto.Nombre}'.");

            if (dto.PermisosIds.Count != 0)
            {
                var permisosExistentes = await _context.Permisos
                    .Where(p => dto.PermisosIds.Contains(p.PermisoId))
                    .Select(p => p.PermisoId)
                    .ToListAsync();

                var noEncontrados = dto.PermisosIds
                    .Except(permisosExistentes)
                    .ToList();

                if (noEncontrados.Count != 0)
                    return Result<PerfilesDto>.Fail($"Los siguientes permisos no existen: " + $"{string.Join(", ", noEncontrados)}.");
            }

            var perfil = new Perfiles { Nombre = dto.Nombre.Trim() };
            _context.Perfiles.Add(perfil);
            await _context.SaveChangesAsync();

            if (dto.PermisosIds.Count != 0)
            {
                var perfilesPermisos = dto.PermisosIds
                    .Select(pid => new PerfilesPermisos
                    {
                        PerfilId = perfil.PerfilId,
                        PermisoId = pid
                    }).ToList();

                _context.PerfilesPermisos.AddRange(perfilesPermisos);
                await _context.SaveChangesAsync();
            }

            return await ObtenerPerfilPorIdAsync(perfil.PerfilId);
        }

        public async Task<Result<PerfilesDto>> ActualizarPerfilAsync(int perfilId, PerfilesActualizacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<PerfilesDto>.Fail("El nombre del perfil es obligatorio.", isValidation: true);

            var perfil = await _context.Perfiles
                .Include(p => p.Permisos)
                .FirstOrDefaultAsync(p => p.PerfilId == perfilId);

            if (perfil is null)
                return Result<PerfilesDto>.Fail($"El perfil con Id {perfilId} no existe.");

            var nombreDuplicado = await _context.Perfiles
                .AnyAsync(p => p.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase) && p.PerfilId != perfilId);

            if (nombreDuplicado)
                return Result<PerfilesDto>.Fail($"Ya existe otro perfil con el nombre '{dto.Nombre}'.");

            if (dto.PermisosIds.Count != 0)
            {
                var permisosExistentes = await _context.Permisos
                    .Where(p => dto.PermisosIds.Contains(p.PermisoId))
                    .Select(p => p.PermisoId)
                    .ToListAsync();

                var noEncontrados = dto.PermisosIds
                    .Except(permisosExistentes)
                    .ToList();

                if (noEncontrados.Count != 0)
                    return Result<PerfilesDto>.Fail($"Los siguientes permisos no existen: " + $"{string.Join(", ", noEncontrados)}.");
            }

            perfil.Nombre = dto.Nombre.Trim();

            var permisosActuales = perfil.Permisos
                .Select(p => p.PermisoId)
                .ToHashSet();

            var permisosNuevos = dto.PermisosIds.ToHashSet();

            var aEliminar = perfil.Permisos
                .Where(p => !permisosNuevos.Contains(p.PermisoId))
                .ToList();

            var aAgregar = permisosNuevos
                .Except(permisosActuales)
                .Select(pid => new PerfilesPermisos
                {
                    PerfilId = perfilId,
                    PermisoId = pid
                }).ToList();

            if (aEliminar.Count != 0)
                _context.PerfilesPermisos.RemoveRange(aEliminar);

            if (aAgregar.Count != 0)
                _context.PerfilesPermisos.AddRange(aAgregar);

            await _context.SaveChangesAsync();

            return await ObtenerPerfilPorIdAsync(perfilId);
        }

        public async Task<Result> EliminarPerfilAsync(int perfilId)
        {
            var perfil = await _context.Perfiles
                .Include(p => p.Permisos)
                .Include(p => p.Usuarios)
                .FirstOrDefaultAsync(p => p.PerfilId == perfilId);

            if (perfil is null)
                return Result.Fail($"El perfil con Id {perfilId} no existe.");

            if (perfil.Usuarios.Count != 0)
                return Result.Fail($"No se puede eliminar el perfil '{perfil.Nombre}' " +
                    $"porque tiene {perfil.Usuarios.Count} usuario(s) asignado(s).");

            _context.PerfilesPermisos.RemoveRange(perfil.Permisos);
            _context.Perfiles.Remove(perfil);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }

        public async Task<Result<IEnumerable<PermisosDto>>> ObtenerPermisosAsync()
        {
            var permisos = await _context.Permisos
                .AsNoTracking()
                .Select(p => new PermisosDto
                {
                    PermisoId = p.PermisoId,
                    Nombre = p.Nombre
                })
                .ToListAsync();

            return Result<IEnumerable<PermisosDto>>.Ok(permisos);
        }

        public async Task<Result<PermisosDto>> CrearPermisoAsync(PermisosCreacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<PermisosDto>.Fail("El nombre del permiso es obligatorio.", isValidation: true);

            var existe = await _context.Permisos
                .AnyAsync(p => p.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase));

            if (existe)
                return Result<PermisosDto>.Fail($"Ya existe un permiso con el nombre '{dto.Nombre}'.");

            var permiso = new Permisos { Nombre = dto.Nombre.Trim() };
            _context.Permisos.Add(permiso);
            await _context.SaveChangesAsync();

            return Result<PermisosDto>.Ok(new PermisosDto
            {
                PermisoId = permiso.PermisoId,
                Nombre = permiso.Nombre
            });
        }

        public async Task<Result<PermisosDto>> ActualizarPermisoAsync(int permisoId, PermisosActualizacionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result<PermisosDto>.Fail("El nombre del permiso es obligatorio.", isValidation: true);

            var permiso = await _context.Permisos
                .FirstOrDefaultAsync(p => p.PermisoId == permisoId);

            if (permiso is null)
                return Result<PermisosDto>.Fail($"El permiso con Id {permisoId} no existe.");

            var nombreDuplicado = await _context.Permisos
                .AnyAsync(p => p.Nombre.Equals(dto.Nombre, StringComparison.CurrentCultureIgnoreCase) && p.PermisoId != permisoId);

            if (nombreDuplicado)
                return Result<PermisosDto>.Fail($"Ya existe otro permiso con el nombre '{dto.Nombre}'.");

            permiso.Nombre = dto.Nombre.Trim();
            await _context.SaveChangesAsync();

            return Result<PermisosDto>.Ok(new PermisosDto
            {
                PermisoId = permiso.PermisoId,
                Nombre = permiso.Nombre
            });
        }

        public async Task<Result> EliminarPermisoAsync(int permisoId)
        {
            var permiso = await _context.Permisos
                .Include(p => p.Perfiles)
                .FirstOrDefaultAsync(p => p.PermisoId == permisoId);

            if (permiso is null)
                return Result.Fail($"El permiso con Id {permisoId} no existe.");

            if (permiso.Perfiles.Count != 0)
                return Result.Fail(
                    $"No se puede eliminar el permiso '{permiso.Nombre}' " +
                    $"porque está asignado a {permiso.Perfiles.Count} perfil(es).");

            _context.Permisos.Remove(permiso);
            await _context.SaveChangesAsync();

            return Result.Ok();
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

            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync();

            return Result.Ok();
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

            _context.Sucursales.Remove(sucursal);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }
    }
}