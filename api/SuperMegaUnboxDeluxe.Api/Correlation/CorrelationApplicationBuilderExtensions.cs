using Microsoft.AspNetCore.Builder;

namespace SuperMegaUnboxDeluxe.Api.Correlation;

public static class CorrelationApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
