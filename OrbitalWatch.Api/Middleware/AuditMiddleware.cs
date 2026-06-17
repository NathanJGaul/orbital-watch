namespace OrbitalWatch.Api.Middleware;

public class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Add CSP header to all responses
        context.Response.Headers.Append(
            "Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self'; " +
            "connect-src 'self' ws://localhost:5000 wss://localhost:5001; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https://unpkg.com;");

        // Log all hub negotiate requests (these are the WebSocket upgrade initiations)
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var user = context.User.Identity?.Name ?? "anonymous";
            var path = context.Request.Path;
            var method = context.Request.Method;

            logger.LogInformation("[AUDIT] {Method} {Path} | User: {user} | IP: {Ip} | Time: {Time:u}", method, path,
                user, ip, DateTime.UtcNow);

            await next(context);
        }
    }
}