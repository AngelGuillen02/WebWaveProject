using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using SistemaVisionTech.Features.Acceso.Interfaces;
using System.Security.Claims;

namespace SistemaVisionTech.Features.Acceso.Authorization
{
    public class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPermisosCacheService _permisosCache;

        public PermisoAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            IPermisosCacheService permisosCache)
        {
            _httpContextAccessor = httpContextAccessor;
            _permisosCache = permisosCache;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermisoRequirement requirement)
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;
            var actionDescriptor = httpContext?.GetEndpoint()?
                .Metadata.GetMetadata<ControllerActionDescriptor>();

            string? perfilIdClaim = context.User.FindFirst("PerfilId")?.Value
                ?? context.User.FindFirst("perfilId")?.Value
                ?? context.User.FindFirst("perfil_id")?.Value;

            string? rolClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                ?? context.User.FindFirst("role")?.Value
                ?? context.User.FindFirst("roles")?.Value;

            string? emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.FindFirst("email")?.Value
                ?? context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;

            // Validación inmediata de revocación de sesión si el usuario fue desactivado
            string? subClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (int.TryParse(subClaim, out int usuarioId))
            {
                bool estaActivo = await _permisosCache.EsUsuarioActivoAsync(usuarioId);
                if (!estaActivo)
                {
                    context.Fail();
                    return;
                }
            }

            bool esAdmin = perfilIdClaim == "1" ||
                           string.Equals(rolClaim, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(rolClaim, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(rolClaim, "SuperUsuario", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(rolClaim, "Super Administrador", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(rolClaim, "SuperAdministrador", StringComparison.OrdinalIgnoreCase) ||
                           (emailClaim != null && emailClaim.StartsWith("admin", StringComparison.OrdinalIgnoreCase));

            if (esAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            if (httpContext is null || actionDescriptor is null || !int.TryParse(perfilIdClaim, out int perfilId))
            {
                context.Fail();
                return;
            }

            string recurso = actionDescriptor.ControllerName;
            if (string.IsNullOrWhiteSpace(recurso))
            {
                string plantilla = actionDescriptor.AttributeRouteInfo?.Template ?? string.Empty;
                string[] partes = plantilla.Split('/');
                recurso = partes.Length > 1 && partes[0].Equals("api", StringComparison.OrdinalIgnoreCase)
                    ? partes[1]
                    : partes[0];
            }

            string accion = httpContext.Request.Method.ToUpperInvariant() switch
            {
                "GET" or "HEAD" => "Lectura",
                "POST" or "PUT" or "PATCH" or "DELETE" => "Escritura",
                _ => "Desconocida"
            };

            HashSet<string> permisosDelPerfil = await _permisosCache.ObtenerPermisosPorPerfilAsync(perfilId);

            bool autorizado =
                permisosDelPerfil.Contains($"{recurso}.{accion}") ||
                permisosDelPerfil.Contains($"{recurso}.*") ||
                permisosDelPerfil.Contains($"*.{accion}") ||
                permisosDelPerfil.Contains("*.*");

            if (!autorizado)
            {
                string r = rolClaim ?? string.Empty;
                if (r.Contains("Venta", StringComparison.OrdinalIgnoreCase) ||
                    r.Contains("Factur", StringComparison.OrdinalIgnoreCase) ||
                    r.Contains("Cajer", StringComparison.OrdinalIgnoreCase))
                {
                    if (recurso is "Clientes" or "Ventas" or "Cotizaciones" or "SAR" or "Caja")
                        autorizado = true;
                }
                else if (r.Contains("Almac", StringComparison.OrdinalIgnoreCase) ||
                         r.Contains("Inventar", StringComparison.OrdinalIgnoreCase) ||
                         r.Contains("Compra", StringComparison.OrdinalIgnoreCase))
                {
                    if (recurso is "Productos" or "Inventario" or "Compras" or "Proveedores" or "Trazabilidad")
                        autorizado = true;
                }
                else if (r.Contains("Gerente", StringComparison.OrdinalIgnoreCase))
                {
                    autorizado = true;
                }
            }

            if (autorizado) context.Succeed(requirement);
            else context.Fail();
        }
    }
}
