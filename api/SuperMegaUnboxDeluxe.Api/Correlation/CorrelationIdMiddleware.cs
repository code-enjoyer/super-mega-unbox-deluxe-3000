using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using SuperMegaUnboxDeluxe.Common.Extensions.EnumerableExtensions;
using SuperMegaUnboxDeluxe.Common.Extensions.StringExtensions;
using System;
using System.Threading.Tasks;

namespace SuperMegaUnboxDeluxe.Api.Correlation;

public sealed class CorrelationIdMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue<StringValues, string>(Constants.Headers.CorrelationId, out var correlationId) ||
            correlationId.IsEmpty())
            correlationId = Guid.NewGuid().ToString("N");

        context.Response.Headers.Append(Constants.Headers.CorrelationId, correlationId);

        return next(context);
    }
}