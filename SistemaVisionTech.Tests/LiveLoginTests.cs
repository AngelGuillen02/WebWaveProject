using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaVisionTech.Features.Acceso.Dtos.Auth;
using SistemaVisionTech.Features.Acceso.Interfaces;
using SistemaVisionTech.Features.Acceso.Services;
using SistemaVisionTech.Infrastructure;
using Xunit;

namespace SistemaVisionTech.Tests
{
    public class LiveLoginTests
    {
        [Fact]
        public async Task TestDirectLoginAgainstDatabase()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "S1st3m4V1s10nT3ch_S3cr3tK3y_#2026!_MustBe32Chars+" },
                    { "Jwt:Issuer", "SistemaVisionTech.API" },
                    { "Jwt:Audience", "SistemaVisionTech.Client" }
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(b => b.AddConsole());
            services.AddHttpContextAccessor();

            string connStr = "Host=aws-1-ca-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.apiikgdljrnwbtpyojig;Password=Clave#3Base.Sistema.Facturacion;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;";

            services.AddDbContext<WebWaveDbContext>(options =>
            {
                options.UseNpgsql(connStr);
            });

            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            var result = await authService.LoginAsync(new LoginRequestDto
            {
                Email = "admin@visiontech.com",
                Contraseña = "Admin123!"
            });

            Assert.True(result.Success, result.Error ?? "Error desconocido en login");
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data.Token);
        }
    }
}
