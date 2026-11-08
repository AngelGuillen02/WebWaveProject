using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaVisionTech.Controllers;
using SistemaVisionTech.Features.Acceso.Authorization;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Features.Acceso.Services;
using SistemaVisionTech.Features.Auditoria.Interfaces;
using SistemaVisionTech.Features.Auditoria.Services;
using SistemaVisionTech.Infrastructure;
using Xunit;

namespace SistemaVisionTech.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void CanResolveAccesoController()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "ThisIsATestSecretKeyWithMoreThan32Chars!" },
                    { "Jwt:Issuer", "SistemaVisionTech.API" },
                    { "Jwt:Audience", "SistemaVisionTech.Client" }
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(b => b.AddConsole());
            services.AddHttpContextAccessor();
            services.AddMemoryCache();

            // Registrar DbContext
            services.AddDbContext<WebWaveDbContext>(options => options.UseNpgsql("Host=localhost;Database=dummy"));
            services.AddDbContextFactory<WebWaveDbContext>(options => options.UseNpgsql("Host=localhost;Database=dummy"));

            // Registros de Acceso
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUsuariosService, UsuariosService>();
            services.AddScoped<IPerfilesService, PerfilesService>();
            services.AddScoped<IPermisosService, PermisosService>();
            services.AddScoped<IEmpresasService, EmpresasService>();
            services.AddScoped<ISucursalesService, SucursalesService>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddSingleton<IPermisosCacheService, PermisosCacheService>();

            // AccesoController
            services.AddScoped<AccesoController>();

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var controller = scope.ServiceProvider.GetRequiredService<AccesoController>();
            Assert.NotNull(controller);
        }
    }
}
