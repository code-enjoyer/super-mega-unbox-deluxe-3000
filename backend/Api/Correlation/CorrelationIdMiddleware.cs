using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SuperMegaUnboxDeluxe.Common.Extensions.EnumerableExtensions;
using SuperMegaUnboxDeluxe.Common.Extensions.StringExtensions;

namespace SuperMegaUnboxDeluxe.Api.Correlation;

public sealed class CorrelationIdMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetString(Constants.Headers.CorrelationId, out var correlationId) ||
            correlationId.IsEmpty())
            correlationId = Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;

        // Exception handling can clear the response before writing a problem.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Constants.Headers.CorrelationId] = correlationId;

            return Task.CompletedTask;
        });

        return next(context);
    }
}
