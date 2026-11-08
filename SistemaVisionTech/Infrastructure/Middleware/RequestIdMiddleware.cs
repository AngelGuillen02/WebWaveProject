using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SistemaVisionTech.Infrastructure.Middleware
{
    public class RequestIdMiddleware
    {
        private const string HeaderKey = "X-Request-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestIdMiddleware> _logger;

        public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string requestId = context.Request.Headers[HeaderKey].ToString();
            if (string.IsNullOrWhiteSpace(requestId))
            {
                requestId = Guid.NewGuid().ToString("N");
            }

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(HeaderKey))
                {
                    context.Response.Headers[HeaderKey] = requestId;
                }
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["Path"] = context.Request.Path,
                ["Method"] = context.Request.Method
            }))
            {
                await _next(context);
            }
        }
    }
}
