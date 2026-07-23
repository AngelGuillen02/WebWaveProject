using Microsoft.EntityFrameworkCore;
using SistemaVisionTech.Common;
using SistemaVisionTech.Features.Navegacion.Dtos;
using SistemaVisionTech.Features.Navegacion.Interfaces;
using SistemaVisionTech.Infrastructure;
using SistemaVisionTech.Infrastructure.Entities;

namespace SistemaVisionTech.Features.Navegacion.Services
{
    public class NavegacionService : INavegacionService
    {
        private readonly WebWaveDbContext _context;

        public NavegacionService(WebWaveDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<NavNodoDto>>> ObtenerArbolAsync()
        {
            List<NavNodo> nodos = await _context.NavNodos
                .AsNoTracking()
                .Include(n => n.Restricciones)
                .OrderBy(n => n.PadreId ?? 0)
                .ThenBy(n => n.Orden)
                .ThenBy(n => n.Nombre)
                .ToListAsync();

            IEnumerable<NavNodoDto> resultado = nodos.Select(MapearDto);
            return Result<IEnumerable<NavNodoDto>>.Ok(resultado);
        }

        public async Task<Result<NavNodoDto>> CrearNodoAsync(NavNodoCreacionDto dto)
        {
            Result? error = await ValidarAsync(dto, null);
            if (error is not null) return Result<NavNodoDto>.Fail(error.Error!, isValidation: true);

            NavNodo nodo = new()
            {
                Nombre = dto.Nombre.Trim(),
                Tipo = dto.Tipo.ToUpper(),
                PadreId = dto.PadreId,
                Orden = dto.Orden,
                Icono = dto.Icono,
                Controller = dto.Controller?.Trim(),
                Action = dto.Action?.Trim(),
                Visible = dto.Visible,
                Activo = dto.Activo
            };

            _context.NavNodos.Add(nodo);
            await _context.SaveChangesAsync();

            return Result<NavNodoDto>.Ok(MapearDto(nodo));
        }

        public async Task<Result<NavNodoDto>> ActualizarNodoAsync(int nodoId, NavNodoCreacionDto dto)
        {
            NavNodo? nodo = await _context.NavNodos
                .Include(n => n.Restricciones)
                .FirstOrDefaultAsync(n => n.NodoId == nodoId);

            if (nodo is null)
                return Result<NavNodoDto>.Fail($"El nodo con Id {nodoId} no existe.");

            if (dto.PadreId == nodoId)
                return Result<NavNodoDto>.Fail("Un nodo no puede ser su propio padre.", isValidation: true);

            if (dto.PadreId is not null && await EsDescendienteAsync(nodoId, dto.PadreId.Value))
                return Result<NavNodoDto>.Fail("No se puede mover un nodo dentro de uno de sus propios descendientes.", isValidation: true);

            Result? error = await ValidarAsync(dto, nodoId);
            if (error is not null) return Result<NavNodoDto>.Fail(error.Error!, isValidation: true);

            nodo.Nombre = dto.Nombre.Trim();
            nodo.Tipo = dto.Tipo.ToUpper();
            nodo.PadreId = dto.PadreId;
            nodo.Orden = dto.Orden;
            nodo.Icono = dto.Icono;
            nodo.Controller = dto.Controller?.Trim();
            nodo.Action = dto.Action?.Trim();
            nodo.Visible = dto.Visible;
            nodo.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            return Result<NavNodoDto>.Ok(MapearDto(nodo));
        }

        public async Task<Result> EliminarNodoAsync(int nodoId)
        {
            NavNodo? nodo = await _context.NavNodos
                .FirstOrDefaultAsync(n => n.NodoId == nodoId);

            if (nodo is null)
                return Result.Fail($"El nodo con Id {nodoId} no existe.");

            bool tieneHijos = await _context.NavNodos.AnyAsync(n => n.PadreId == nodoId);
            if (tieneHijos)
                return Result.Fail("No se puede eliminar: el nodo tiene hijos. Elimina o mueve los hijos primero.");

            _context.NavNodos.Remove(nodo);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }

        public async Task<Result<NavNodoDto>> GuardarPerfilesDelNodoAsync(int nodoId, GuardarPerfilesNodoDto dto)
        {
            NavNodo? nodo = await _context.NavNodos
                .Include(n => n.Restricciones)
                .FirstOrDefaultAsync(n => n.NodoId == nodoId);

            if (nodo is null)
                return Result<NavNodoDto>.Fail($"El nodo con Id {nodoId} no existe.");

            List<int> perfilesIds = dto.PerfilesIds.Distinct().ToList();

            if (perfilesIds.Count != 0)
            {
                List<int> perfilesExistentes = await _context.Perfiles
                    .Where(p => perfilesIds.Contains(p.PerfilId))
                    .Select(p => p.PerfilId)
                    .ToListAsync();

                List<int> noEncontrados = perfilesIds.Except(perfilesExistentes).ToList();
                if (noEncontrados.Count != 0)
                    return Result<NavNodoDto>.Fail($"Los siguientes perfiles no existen: {string.Join(", ", noEncontrados)}.");
            }

            _context.NavRestricciones.RemoveRange(nodo.Restricciones);

            List<NavRestriccion> nuevas = perfilesIds
                .Select(pid => new NavRestriccion { NodoId = nodoId, PerfilId = pid })
                .ToList();

            _context.NavRestricciones.AddRange(nuevas);
            await _context.SaveChangesAsync();

            nodo.Restricciones = nuevas;
            return Result<NavNodoDto>.Ok(MapearDto(nodo));
        }

        public async Task<Result<IEnumerable<MenuNodoDto>>> ObtenerMenuAsync(int perfilId)
        {
            bool esAdmin = await _context.PerfilesPermisos
                .Where(pp => pp.PerfilId == perfilId)
                .Select(pp => pp.Permiso!.Nombre)
                .AnyAsync(nombre => nombre == "*.*");

            List<NavNodo> nodos = await _context.NavNodos
                .AsNoTracking()
                .Include(n => n.Restricciones)
                .Where(n => n.Visible)
                .OrderBy(n => n.PadreId ?? 0)
                .ThenBy(n => n.Orden)
                .ThenBy(n => n.Nombre)
                .ToListAsync();

            HashSet<int> visibles;

            if (esAdmin)
            {
                visibles = nodos.Select(n => n.NodoId).ToHashSet();
            }
            else
            {
                // Igual que el CTE recursivo del Portal (get_nav_tree_by_rol_id):
                // el caso base son TODOS los nodos con restriccion explicita para
                // este perfil, sin importar el tipo (NODO o VISTA), y desde ahi se
                // sube por la cadena de padres para que las carpetas contenedoras
                // tambien aparezcan.
                HashSet<int> nodosConAcceso = nodos
                    .Where(n => n.Restricciones.Any(r => r.PerfilId == perfilId))
                    .Select(n => n.NodoId)
                    .ToHashSet();

                Dictionary<int, NavNodo> porId = nodos.ToDictionary(n => n.NodoId);
                visibles = new HashSet<int>(nodosConAcceso);

                foreach (int vistaId in nodosConAcceso)
                {
                    int? padreId = porId[vistaId].PadreId;
                    while (padreId is not null && porId.TryGetValue(padreId.Value, out NavNodo? padre))
                    {
                        visibles.Add(padre.NodoId);
                        padreId = padre.PadreId;
                    }
                }
            }

            IEnumerable<MenuNodoDto> resultado = nodos
                .Where(n => visibles.Contains(n.NodoId))
                .Select(n => new MenuNodoDto
                {
                    NodoId = n.NodoId,
                    Nombre = n.Nombre,
                    Tipo = n.Tipo,
                    PadreId = n.PadreId,
                    Orden = n.Orden,
                    Icono = n.Icono,
                    Controller = n.Controller,
                    Action = n.Action
                });

            return Result<IEnumerable<MenuNodoDto>>.Ok(resultado);
        }

        public async Task<Result<IEnumerable<NavNodoConAccesoDto>>> ObtenerArbolConAccesoAsync(int perfilId)
        {
            bool perfilExiste = await _context.Perfiles.AnyAsync(p => p.PerfilId == perfilId);
            if (!perfilExiste)
                return Result<IEnumerable<NavNodoConAccesoDto>>.Fail($"El perfil con Id {perfilId} no existe.");

            List<NavNodo> nodos = await _context.NavNodos
                .AsNoTracking()
                .Include(n => n.Restricciones)
                .OrderBy(n => n.PadreId ?? 0)
                .ThenBy(n => n.Orden)
                .ThenBy(n => n.Nombre)
                .ToListAsync();

            IEnumerable<NavNodoConAccesoDto> resultado = nodos.Select(n => new NavNodoConAccesoDto
            {
                NodoId = n.NodoId,
                Nombre = n.Nombre,
                Tipo = n.Tipo,
                PadreId = n.PadreId,
                Orden = n.Orden,
                Icono = n.Icono,
                Controller = n.Controller,
                Action = n.Action,
                TieneAcceso = n.Restricciones.Any(r => r.PerfilId == perfilId)
            });

            return Result<IEnumerable<NavNodoConAccesoDto>>.Ok(resultado);
        }

        public async Task<Result> GuardarRestriccionesRolAsync(int perfilId, SetRestriccionesRolDto dto)
        {
            bool perfilExiste = await _context.Perfiles.AnyAsync(p => p.PerfilId == perfilId);
            if (!perfilExiste)
                return Result.Fail($"El perfil con Id {perfilId} no existe.");

            List<int> nodoIds = dto.NodoIds.Distinct().ToList();

            if (nodoIds.Count != 0)
            {
                List<int> vistasExistentes = await _context.NavNodos
                    .Where(n => nodoIds.Contains(n.NodoId) && n.Tipo == "VISTA")
                    .Select(n => n.NodoId)
                    .ToListAsync();

                List<int> noEncontrados = nodoIds.Except(vistasExistentes).ToList();
                if (noEncontrados.Count != 0)
                    return Result.Fail($"Los siguientes nodos no existen o no son VISTA: {string.Join(", ", noEncontrados)}.");
            }

            List<NavRestriccion> actuales = await _context.NavRestricciones
                .Where(r => r.PerfilId == perfilId)
                .ToListAsync();

            _context.NavRestricciones.RemoveRange(actuales);

            List<NavRestriccion> nuevas = nodoIds
                .Select(nid => new NavRestriccion { NodoId = nid, PerfilId = perfilId })
                .ToList();

            _context.NavRestricciones.AddRange(nuevas);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }

        private async Task<bool> EsDescendienteAsync(int nodoId, int posibleDescendienteId)
        {
            int? actualId = posibleDescendienteId;

            while (actualId is not null)
            {
                if (actualId == nodoId) return true;
                actualId = await _context.NavNodos
                    .Where(n => n.NodoId == actualId)
                    .Select(n => n.PadreId)
                    .FirstOrDefaultAsync();
            }

            return false;
        }

        private async Task<Result?> ValidarAsync(NavNodoCreacionDto dto, int? nodoIdActual)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Result.Fail("El nombre es obligatorio.", isValidation: true);

            string tipo = dto.Tipo.ToUpper();
            if (tipo != "NODO" && tipo != "VISTA")
                return Result.Fail("El tipo debe ser NODO o VISTA.", isValidation: true);

            if (tipo == "VISTA" && (string.IsNullOrWhiteSpace(dto.Controller) || string.IsNullOrWhiteSpace(dto.Action)))
                return Result.Fail("Controller y Action son obligatorios para una vista.", isValidation: true);

            if (dto.PadreId is not null)
            {
                bool padreExiste = await _context.NavNodos.AnyAsync(n => n.NodoId == dto.PadreId);
                if (!padreExiste)
                    return Result.Fail($"El nodo padre con Id {dto.PadreId} no existe.");
            }

            return null;
        }

        private static NavNodoDto MapearDto(NavNodo n) => new()
        {
            NodoId = n.NodoId,
            Nombre = n.Nombre,
            Tipo = n.Tipo,
            PadreId = n.PadreId,
            Orden = n.Orden,
            Icono = n.Icono,
            Controller = n.Controller,
            Action = n.Action,
            Visible = n.Visible,
            Activo = n.Activo,
            PerfilesIds = n.Restricciones.Select(r => r.PerfilId).ToList()
        };
    }
}
