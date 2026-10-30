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

            string? perfilIdClaim = context.User.FindFirst("PerfilId")?.Value;
            string? rolClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;

            bool esAdmin = perfilIdClaim == "1" ||
                           string.Equals(rolClaim, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(rolClaim, "Admin", StringComparison.OrdinalIgnoreCase);

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

            if (autorizado) context.Succeed(requirement);
            else context.Fail();
        }
    }
}
