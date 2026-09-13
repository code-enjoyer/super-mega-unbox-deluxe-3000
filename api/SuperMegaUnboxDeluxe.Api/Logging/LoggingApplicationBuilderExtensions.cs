using Microsoft.AspNetCore.Builder;
using Serilog;
using System.Security.Claims;

namespace SuperMegaUnboxDeluxe.Api.Logging;

public static class LoggingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set(Application.Constants.Logging.CorrelationId, httpContext.TraceIdentifier);
                diagnosticContext.Set(Application.Constants.Logging.ClientIp, httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set(Application.Constants.Logging.UserAgent, httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set(Application.Constants.Logging.UserId, httpContext.User.FindFirstValue("sub"));
            };
        });
    }
}
