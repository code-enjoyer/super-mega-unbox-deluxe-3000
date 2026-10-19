using Microsoft.AspNetCore.Mvc;
using SuperMegaUnboxDeluxe.Common.Results;

namespace SuperMegaUnboxDeluxe.Api.ErrorHandling;

public static class ResultControllerExtensions
{
    public static ObjectResult ToProblem(this ControllerBase controller, Error error) => controller.Problem(
        statusCode: error.Kind == ErrorKind.NotFound ? 404 : 400,
        title: error.Title,
        detail: error.Detail);
}
