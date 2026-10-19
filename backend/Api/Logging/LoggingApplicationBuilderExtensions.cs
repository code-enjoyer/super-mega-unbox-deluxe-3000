using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace SuperMegaUnboxDeluxe.Api.Logging;

public static class LoggingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set(Infrastructure.Constants.Logging.CorrelationId, httpContext.TraceIdentifier);
                diagnosticContext.Set(Infrastructure.Constants.Logging.ClientIp, httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set(Infrastructure.Constants.Logging.UserAgent, httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set(Infrastructure.Constants.Logging.UserId, httpContext.User.FindFirstValue("sub"));
            };
        });
    }
}
