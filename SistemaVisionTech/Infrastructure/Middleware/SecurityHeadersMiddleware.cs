using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SistemaVisionTech.Infrastructure.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                // 1. Content Security Policy (CSP)
                if (!headers.ContainsKey("Content-Security-Policy"))
                {
                    headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "script-src 'self'; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data: https:; " +
                        "font-src 'self'; " +
                        "object-src 'none'; " +
                        "frame-ancestors 'none'; " +
                        "base-uri 'self'; " +
                        "form-action 'self';";
                }

                // 2. Clickjacking Protection
                if (!headers.ContainsKey("X-Frame-Options"))
                {
                    headers["X-Frame-Options"] = "DENY";
                }

                // 3. MIME-Type Sniffing Protection
                if (!headers.ContainsKey("X-Content-Type-Options"))
                {
                    headers["X-Content-Type-Options"] = "nosniff";
                }

                // 4. Referrer Policy
                if (!headers.ContainsKey("Referrer-Policy"))
                {
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                }

                // 5. Permissions Policy (Feature Policy)
                if (!headers.ContainsKey("Permissions-Policy"))
                {
                    headers["Permissions-Policy"] =
                        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
                }

                // 6. X-XSS-Protection (Disable legacy auditor to rely on CSP)
                if (!headers.ContainsKey("X-XSS-Protection"))
                {
                    headers["X-XSS-Protection"] = "0";
                }

                // 7. Cache-Control for sensitive API endpoints
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
                    headers["Pragma"] = "no-cache";
                }

                // 8. Ocultar información del servidor/tecnología
                headers.Remove("Server");
                headers.Remove("X-Powered-By");
                headers.Remove("X-AspNet-Version");

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
